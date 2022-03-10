using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NumberLinesCopy.tool {
    internal interface IWriteLine {
        void WriteLine(string lineComment, int line, string name, string path);

        void ExceptionMessageBox(Exception exception, int line, string name, string path);
    }

    internal class NullObjectWriteLine : IWriteLine {
        public void WriteLine(string lineComment, int line, string name, string path) {
            // 何もしない
        }
        
        public void ExceptionMessageBox(Exception exception, int line, string name, string path) {
            // 何もしない
        }
    }

    internal class OutputWriteLine : IWriteLine {
        public void WriteLine(string lineComment, int line, string name, string path) {
            DateTime dt = DateTime.Now;

            System.Diagnostics.Trace.WriteLine(string.Format("### Debug:{0},{1},{2},{3},{4}", dt, dt.Millisecond, name, line, lineComment));
        }

        public void ExceptionMessageBox(Exception exception, int line, string name, string path) {
            DateTime dt = DateTime.Now;

            string str = string.Format("### Exception:{0},{1},{2},{3},{4},{5}", dt, dt.Millisecond, name, line, exception.Message, exception.StackTrace);

            System.Diagnostics.Trace.WriteLine(str);

            System.Windows.Forms.MessageBox.Show(str);
        }
    }

    public enum EnumDebugMode {
        NULL,
        Output,
        //NLog,
        //FileOut,
    }

    internal sealed class SingletonWriteLine {

        private static IWriteLine _instance = new OutputWriteLine();
        public static void setModel(EnumDebugMode enumDebugMode) {
            switch (enumDebugMode) {
                case EnumDebugMode.NULL:
                    _instance = new NullObjectWriteLine();
                    break;
                case EnumDebugMode.Output:
                    _instance = new NullObjectWriteLine();
                    break;
                default:
                    _instance = new NullObjectWriteLine();
                    break;
            }
        }

        public static IWriteLine getinstance() {
            return _instance;
        }
    }

    public class UserDebug {

        public static void setModel(EnumDebugMode enumDebugMode ) {
            SingletonWriteLine.setModel(enumDebugMode);
        }

        public static void WriteLine(string lineComment,
            [CallerLineNumber] int line = 0, [CallerMemberName] string name = "", [CallerFilePath] string path = "") {
  
            SingletonWriteLine.getinstance().WriteLine( lineComment, line, name, path );
        }

        public static void ExceptionMessageBox(Exception exception,
            [CallerLineNumber] int line = 0, [CallerMemberName] string name = "", [CallerFilePath] string path = "") {
            SingletonWriteLine.getinstance().ExceptionMessageBox(exception, line, name, path);
        }
    }


}