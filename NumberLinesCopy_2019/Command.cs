using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

using NumberLinesCopy.tool;
using EnvDTE;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace NumberLinesCopy_2019 {
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4129;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("404a04a4-5409-4eb5-a8a1-22a68cfe8673");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Command(AsyncPackage package, OleMenuCommandService commandService) {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider {
            get {
                return this.package;
            }
        }

        /// <summary>
        /// DTE
        /// </summary>
        private EnvDTE.DTE _dTE = null;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package) {
            // Switch to the main thread - the call to AddCommand in Command's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Command(package, commandService);

            Instance._dTE = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (null != Instance._dTE) {
                UserDebug.WriteLine(string.Format("Version={0}", Instance._dTE.Version));
                UserDebug.WriteLine(string.Format("Name={0}", Instance._dTE.Name));

                EnvDTE.Solution solution = Instance._dTE.Solution;

                UserDebug.WriteLine(string.Format("FullName={0}", solution.FullName));
                UserDebug.WriteLine(string.Format("FileName={0}", solution.FileName));
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e) {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            try {
                bool isShiftKey = false;

                if (Keys.Shift == (Control.ModifierKeys & Keys.Shift)) {
                    isShiftKey = true;
                }

                if (_dTE?.ActiveDocument.Object("TextDocument") is TextDocument textDocument) {
                    TextSelection textSelection = textDocument.Selection;

                    string text = textSelection.Text;
                    UserDebug.WriteLine("=============");
                    UserDebug.WriteLine("\n" + text);
                    UserDebug.WriteLine("=============");

                    // 行頭から行終了までを選択する
                    SelectLines(textSelection);

                    EditPoint startPoint = textSelection.TopPoint.CreateEditPoint();
                    EditPoint endPoint = textSelection.BottomPoint.CreateEditPoint();

                    text = textSelection.Text;
                    UserDebug.WriteLine("=============");
                    UserDebug.WriteLine("\n" + text);
                    UserDebug.WriteLine("=============");

                    // 正規表現で選択行を行ごとにリストする
                    string[] lineLists = Regex.Split(text, "\n|\r\n|\r");

                    string outStr = "";
                    int cntLine = startPoint.Line;

                    // 桁数を取得
                    int numDigit = (endPoint.Line == 0) ? 1 : ((int)Math.Log10(endPoint.Line) + 1);
                    string format = string.Format("{{0,{0}}}: ", numDigit);

                    // 桁数と文字列を結合
                    foreach (string str in lineLists) {

                        string outLineStr = "";

                        if (isShiftKey) {
                            outLineStr = cnvTabToSpace(str);
                        }
                        else {
                            outLineStr = str;
                        }

                        outStr += outLineStr.Insert(0, string.Format(format, cntLine)) + Environment.NewLine;
                        cntLine++;
                    }

                    UserDebug.WriteLine("=============");
                    UserDebug.WriteLine("\n" + outStr);
                    UserDebug.WriteLine("=============");

                    //クリップボードにコピーする(clearは例外が発生しないおまじない)
                    Clipboard.Clear();
                    System.Threading.Thread.Sleep(100);
                    Clipboard.SetText(outStr);
                }
            }
            catch (Exception ex) {
                UserDebug.ExceptionMessageBox(ex);
            }


            string cnvTabToSpace(string str) {
                string outStr = "";

                string[] tabLists = Regex.Split(str, "\t");

                for (int i = 0; i < tabLists.Length; i++) {
                    if (0 == i) {
                        outStr += tabLists[0];
                    }
                    else {
                        outStr += setSpace(outStr);
                        outStr += tabLists[i];
                    }
                }

                return outStr;
            }


            string setSpace(string str) {
                string outStr = "";

                int tabIdx = 4 - (str.Length % 4);
                for (int j = 0; j < tabIdx; j++) {
                    outStr += " ";
                }

                return outStr;
            }
        }

        /// <summary>
        /// 選択中の行を行選択状態にします。
        /// https://github.com/munyabe/ToggleComment
        /// </summary>
        private static void SelectLines(TextSelection selection) {
            ThreadHelper.ThrowIfNotOnUIThread();

            var startPoint = selection.TopPoint.CreateEditPoint();
            startPoint.StartOfLine();

            var endPoint = selection.BottomPoint.CreateEditPoint();
            if (endPoint.AtStartOfLine == false || startPoint.Line == endPoint.Line) {
                endPoint.EndOfLine();
            }

            if (selection.Mode == vsSelectionMode.vsSelectionModeBox) {
                selection.Mode = vsSelectionMode.vsSelectionModeStream;
            }

            selection.MoveToPoint(startPoint);
            selection.MoveToPoint(endPoint, true);
        }
    }
}
