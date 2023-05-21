using System;
using System.IO;
using System.Text;

namespace WatchDog.Lib
{
    public static class LogUtil
    {
        public static Action<string> EventMsgToLog { get; set; }

        #region LogTrace
        private static readonly object LockFile = new object();
        /// <summary>
        /// [一般寫入檔案] e.g. .\Logs\2022\09\20220930.txt
        /// </summary>
        /// <param name="Message">檔案內容</param>
        public static void LogTrace(string Message)
        {
            LogTrace(string.Empty, Message);
        }
        /// <summary>
        /// [特殊寫入檔案] e.g. .\Logs\{RMS}\2022\09\20220930.txt
        /// </summary>
        /// <param name="Dir">目錄名稱</param>
        /// <param name="Message">檔案內容</param>
        public static void LogTrace(string Dir, string Message)
        {
            // DirBase:當前程式執行根目錄
            // Year: 2022
            // Month: 09
            // CurrentDate: 20220930
            // FileType: .txt
            // CurrentDir: .\Logs\{RMS}\2022\09
            // FileName: .\Logs\{RMS}\2022\09\20220930.txt
            // NowTime: 2022-09-30 01:36:14.972
            DateTime GetDateTime = DateTime.Now;
            string DirBase = System.AppDomain.CurrentDomain.BaseDirectory + @"Logs\" + (string.IsNullOrEmpty(Dir) ? "" : Dir + @"\");
            string Year = GetDateTime.ToString("yyyy"), Month = GetDateTime.ToString("MM"), CurrentDate = GetDateTime.ToString("yyyyMMdd"), NowTime = GetDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"), FileType = ".txt";
            string CurrentDir = DirBase + Year + @"\" + Month + @"\";
            string FileName = CurrentDir + CurrentDate + FileType;
            try
            {
                if (!Directory.Exists(CurrentDir))
                    Directory.CreateDirectory(CurrentDir);
                if (!File.Exists(FileName))
                    using (var f = File.Create(FileName)) { f.Close(); }

                using (var fs = new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (var log = new StreamWriter(fs, Encoding.Default))
                    {
                        lock (LockFile)
                        {
                            log.WriteLine(NowTime + " → " + Message);
                        }
                    }
                }
                EventMsgToLog?.Invoke(NowTime + " → " + Message + "\r\n");
            }
            catch (Exception ex)
            {
                LogTrace(Dir, ex.Message);
            }
        }
        #endregion
    }
}
