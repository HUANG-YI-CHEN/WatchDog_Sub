using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace WatchDog
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main()
        {
            #region UnhandledExceptionMode CatchException 處理未捕捉的例外
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            #endregion

            #region ThreadException 處理非UI執行緒錯誤
            Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);
            #endregion

            #region UnhandledExceptionEventHandler 處理非UI執行緒錯誤
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);
            #endregion

            #region Mutex Form
            _ = new System.Threading.Mutex(true, Application.ProductName, out bool ret);
            if (ret)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            else
            {
                //string processName = Process.GetCurrentProcess().ProcessName;
                //Process[] processes = Process.GetProcessesByName(processName);
                //string a = "";
                //if (processes.Length > 1)
                //{
                //    foreach (var item in processes)
                //    {
                //        if (!item.Id.Equals(Process.GetCurrentProcess().Id))
                //            a += string.Format("[{0}]({1}), \r\n\t", item.ProcessName, item.Id.ToString());
                //    }
                //}
                //MessageBox.Show(null,
                //    string.Format("Current Process: [{0}]({1})\r\nOther Processes: {2}\r\nAnother Process is running.", Process.GetCurrentProcess().ProcessName, Process.GetCurrentProcess().Id, a),
                //    Application.ProductName,
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Warning);
                Environment.Exit(Environment.ExitCode);
            }
            #endregion            
        }
        #region ThreadExceptionEvent
        static void ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Exception err = e.Exception as Exception;
            string msg = (err is null) ?
                string.Format("Thread Exception. ClassType:{0};\r\nMessage:{1}\r\nStackTrace:{2}\r\n", err.GetType().Name, err.Message, err.StackTrace) :
                string.Format("Thread Exception. Message:{0}", e);
            _ = MessageBox.Show(msg);
        }
        #endregion

        #region UnhandledExceptionEvent
        static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception err = e.ExceptionObject as Exception;
            string msg = (err is null) ?
                string.Format("Application UnhandledException. ClassType:{0};\r\nMessage:{1}\r\nStackTrace:{2}\r\n", err.GetType().Name, err.Message, err.StackTrace) :
                string.Format("Application UnhandledException. Message:{0}", e);
            _ = MessageBox.Show(msg);
        }
        #endregion
    }
}
