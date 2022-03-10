using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NumberLinesCopy.tool
{
    internal class UserDebug
    {
        public static void WriteLine(string lineComment,
            [CallerLineNumber] int line = 0, [CallerMemberName] string name = "", [CallerFilePath] string path = "" )
        {
            DateTime dt = DateTime.Now;

            System.Diagnostics.Trace.WriteLine(string.Format("### Debug:{0},{1},{2},{3},{4}", dt, dt.Millisecond, name, line, lineComment));
        }

        public static void ExceptionMessageBox(Exception exception,
            [CallerLineNumber] int line = 0, [CallerMemberName] string name = "", [CallerFilePath] string path = "") {
            DateTime dt = DateTime.Now;

            string str = string.Format("### Exception:{0},{1},{2},{3},{4},{5}", dt, dt.Millisecond, name, line, exception.Message, exception.StackTrace);

            System.Diagnostics.Trace.WriteLine(str);

            System.Windows.Forms.MessageBox.Show(str);
        }
    }
}
