using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using static WatchDog.Lib.LogUtil;
using static WatchDog.Lib.XmlUtil;
using static WatchDog.Lib.ShortCutUtil;

namespace WatchDog
{
    public partial class MainForm : Form
    {
        //https://www.cnblogs.com/Jeffrey-Chou/articles/12238382.html
        #region Error Win Delegate
        WinEventDelegate dele { get; set; } = null;
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            IntPtr handle = IntPtr.Zero;
            StringBuilder Buff = new StringBuilder(nChars);
            handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            WriteLog("[START] WinEventProc");
            WriteLog(GetActiveWindowTitle());
            WriteLog("[END] WinEventProc");
        }
        #endregion

        #region Exec Infomation
        public string CurExecPath { get; set; }
        public string CurExecName { get; set; }
        public string CurExecVer { get; set; }
        public string XmlPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory + "PROCESS.xml";
        public ProcessWatcher PW { get; set; } = new ProcessWatcher();

        int CounterExit = 0;
        #endregion

        #region MainForm
        public MainForm()
        {
            InitializeComponent();

            #region WinEventDelegate
            if (dele != null)
            {
                dele = new WinEventDelegate(WinEventProc);
                IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
            }
            #endregion

            #region Exec Infomation
            CurExecPath = System.Windows.Forms.Application.ExecutablePath;
            CurExecName = Path.GetFileNameWithoutExtension(CurExecPath);
            CurExecVer = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion.ToString();
            #endregion

            #region NotifyIcon  
            this.Text = string.Format("{0} ({1})", CurExecName, CurExecVer);
            this.Icon = WatchDog_Sub.Properties.Resources.watchdog_001_16x161;
            this.notifyIcon1.Text = string.Format("{0} ({1})", CurExecName, CurExecVer);
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.Icon = WatchDog_Sub.Properties.Resources.watchdog_001_16x161;
            this.notifyIcon1.ContextMenuStrip = new ContextMenuStrip();
            //this.notifyIcon1.ContextMenuStrip.Items.Add("Open", null, this.MenuStrip_Open);
            //this.notifyIcon1.ContextMenuStrip.Items.Add("Shrink", null, this.MenuStrip_Shrink);
            this.notifyIcon1.ContextMenuStrip.Items.Add("Exit", null, this.MenuStrip_Exit);
            #endregion            
        }
        #endregion

        #region ~MainForm
        ~MainForm()
        {
            this.Dispose();
            this.Close();
            Environment.Exit(Environment.ExitCode);
        }
        #endregion

        #region How to fix the flickering in User controls
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }
        #endregion

        #region MainForm_Load
        private void MainForm_Load(object sender, EventArgs e)
        {
            EventMsgToLog += new Action<string>((msg) =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(EventMsgToLog, msg);
                }
                else
                {
                    this.rtb_Msg.Text += msg;
                }
            });

            #region Create ShortCut
            //try
            //{
            //    string StartupLinkPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            //    string StartupDesktopShortCut = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\" + "Startup.lnk";
            //    string ExecPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            //    string ExecName = Path.GetFileNameWithoutExtension(ExecPath);
            //    string ExecDesktopLinkPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //    string ExecDesktopShortCut = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\" + ExecName + ".lnk";
            //    string ExecStartupShortCut = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\" + ExecName + ".lnk";
            //    // 建立起動區捷徑
            //    if (!File.Exists(StartupDesktopShortCut))
            //        ShortCutCreate(StartupLinkPath, StartupDesktopShortCut, "");
            //    // 建立桌面捷徑
            //    if (!File.Exists(ExecDesktopShortCut))
            //        ShortCutCreate(ExecPath, ExecDesktopLinkPath, "");
            //    // 建立程式啟動捷徑
            //    if (!File.Exists(ExecStartupShortCut))
            //        ShortCutCreate(ExecPath, StartupLinkPath, "AutoStartup");
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}
            #endregion

            #region WatchProcess          
            try
            {
                ProcessWatcher pw = new ProcessWatcher();
                int PsState = default(int);
                PsState = pw.Init(XmlPath);
                switch (PsState)
                {
                    case (int)ProcessInit.SetupExist:
                        #region 設定一秒延遲,讓程式順利開啟
                        string ExecPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        int DelayTimer = 1;
                        Process P_new = new Process();
                        P_new.StartInfo = new ProcessStartInfo("cmd.exe", string.Format("/C choice /C Y /N /D Y /T {0} & \"{1}\"", DelayTimer, ExecPath));
                        P_new.StartInfo.CreateNoWindow = true;
                        P_new.StartInfo.UseShellExecute = false;
                        P_new.Start();
                        #endregion
                        Environment.Exit(Environment.ExitCode);
                        break;
                    case (int)ProcessInit.ProcessGet:
                        break;
                    case (int)ProcessInit.ProcessCorrect:
                        try
                        {
                            pw.Watch();
                        }
                        catch
                        {
                            pw.UnWatch();
                        }
                        break;
                    case (int)ProcessInit.ProcessIncorrect:
                        break;
                    case (int)ProcessInit.ProcessException:
                        break;
                }
            }
            catch
            {

            }
            #endregion
        }
        #endregion

        #region MainForm_Shown
        private void MainForm_Shown(object sender, EventArgs e)
        {
            //this.TopMost = true;
            //this.TopMost = false;
            this.WindowState = FormWindowState.Minimized;
            this.Visible = false;
            this.ShowInTaskbar = false;
        }
        #endregion

        #region MainForm_FormClosing
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            //this.notifyIcon1.Visible = true;
            this.WindowState = FormWindowState.Minimized;
            this.Visible = false;
            this.ShowInTaskbar = false;
        }
        #endregion

        #region MainForm_SizeChanged
        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case FormWindowState.Minimized:
                    //this.notifyIcon1.Visible = true;
                    this.WindowState = FormWindowState.Minimized;
                    this.Visible = false;
                    this.ShowInTaskbar = false;
                    break;
                case FormWindowState.Normal:
                    //this.notifyIcon1.Visible = false;
                    this.Visible = true;
                    this.WindowState = FormWindowState.Normal;
                    this.ShowInTaskbar = true;
                    break;
            }
        }
        #endregion

        #region notifyIcon1_MouseDoubleClick
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            switch (this.WindowState)
            {
                case FormWindowState.Minimized:
                    //this.notifyIcon1.Visible = false;
                    this.Visible = true;
                    this.WindowState = FormWindowState.Normal;
                    this.ShowInTaskbar = true;
                    break;
                case FormWindowState.Normal:
                    //this.notifyIcon1.Visible = true;
                    this.WindowState = FormWindowState.Minimized;
                    this.Visible = false;
                    this.ShowInTaskbar = false;
                    break;
            }
        }
        #endregion

        #region notifyIcon MenuStrip_Open
        private void MenuStrip_Open(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case FormWindowState.Minimized:
                    //this.notifyIcon1.Visible = false;
                    this.Visible = true;
                    this.WindowState = FormWindowState.Normal;
                    this.ShowInTaskbar = true;
                    break;
            }
        }
        #endregion

        #region notifyIcon MenuStrip_Shrink
        private void MenuStrip_Shrink(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case FormWindowState.Normal:
                    //this.notifyIcon1.Visible = true;
                    this.WindowState = FormWindowState.Minimized;
                    this.Visible = false;
                    this.ShowInTaskbar = false;
                    break;
            }
        }
        #endregion

        #region notifyIcon MenuStrip_Exit
        private void MenuStrip_Exit(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you want to exit the program?", "Alarm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                WriteLog("[MSG] User Click MenuStrip - Exit");
                if (CounterExit++ > -1)
                {
                    WriteLog("[MSG] Exit Program");
                    string curExecName = Path.GetFileNameWithoutExtension(CurExecPath).ToUpper();
                    bool psKillSn = curExecName.Equals("WatchDog_Main".ToUpper()) ? true : false;

                    new Thread(() =>
                    {
                        foreach (Process ps in System.Diagnostics.Process.GetProcesses())
                        {
                            if (psKillSn)
                            {
                                if (ps.ProcessName.ToUpper().Equals("WatchDog_Sub".ToUpper()))
                                    ps.Kill();
                                if (ps.ProcessName.ToUpper().Equals("WatchDog_Main".ToUpper()))
                                    ps.Kill();
                            }
                            else
                            {
                                if (ps.ProcessName.ToUpper().Equals("WatchDog_Main".ToUpper()))
                                    ps.Kill();
                                if (ps.ProcessName.ToUpper().Equals("WatchDog_Sub".ToUpper()))
                                    ps.Kill();
                            }
                        }
                    }).Start();

                    if (this.notifyIcon1 != null)
                    {
                        this.notifyIcon1.Visible = false;
                        this.notifyIcon1.Icon.Dispose();
                        this.notifyIcon1.Dispose();
                        this.notifyIcon1 = null;
                    }
                    this.Dispose();
                    this.Close();
                    Environment.Exit(Environment.ExitCode);
                }
            }
        }
        #endregion

        #region rtb_Msg_TextChanged
        private void rtb_Msg_TextChanged(object sender, EventArgs e)
        {
            if (rtb_Msg.TextLength > 4000)
            {
                WriteLog("[MSG] Clear Message Log.");
                rtb_Msg.Text = "";
            }
            rtb_Msg.SelectionStart = rtb_Msg.TextLength;
            // Scrolls the contents of the control to the current caret position.
            rtb_Msg.ScrollToCaret();
        }
        #endregion

        #region WriteLog
        public void WriteLog(string Message)
        {
            string CurName = Path.GetFileNameWithoutExtension(System.Windows.Forms.Application.ExecutablePath);
            string Dir = CurName.ToUpper().Equals("WatchDog_Main".ToUpper()) ? "MAIN" : "SUB";
            LogTrace(Dir, Message);
        }
        #endregion
    }

    #region ProcessWatcher 主監控
    public class ProcessWatcher
    {
        public List<IProcess> PsList { get; set; } = new List<IProcess>();
        private List<Thread> WatchThread { get; set; } = new List<Thread>();
        private PS IPS { get; set; } = new PS();
        private string CurName { get; set; } = Path.GetFileNameWithoutExtension(System.Windows.Forms.Application.ExecutablePath);

        #region DeConstruct
        ~ProcessWatcher()
        {
            if (WatchThread != null)
                WatchThread = null;
            if (PsList != null)
                PsList = null;
            if (IPS != null)
                IPS = null;
        }
        #endregion

        #region Init
        public int Init(string XmlPath)
        {
            WriteLog("[START] Init Process");
            if (!File.Exists(XmlPath))
            {
                try
                {
                    IPS = new PS();
                    IPS.MainProcess = new List<ProcessItem>();
                    IPS.MainProcess.Add(new ProcessItem() { Name = "WatchDog_Sub", Path = AppDomain.CurrentDomain.BaseDirectory + "WatchDog_Sub.exe", Timer = "2" });
                    IPS.SubProcess = new List<ProcessItem>();
                    IPS.SubProcess.Add(new ProcessItem() { Name = "WatchDog_Main", Path = AppDomain.CurrentDomain.BaseDirectory + "WatchDog_Main.exe", Timer = "2" });
                    SerializeToXml(XmlPath, IPS);
                    WriteLog("[MSG] 設定檔 PROCESS.xml 不存在!!");
                    MessageBox.Show("設定檔 PROCESS.xml 不存在!!");
                    return (int)ProcessInit.SetupExist;
                }
                catch (Exception ex)
                {
                    WriteLog("[ERROR] 監控程式例外錯誤, 取得設定檔異常: " + ex.Message);
                    MessageBox.Show("監控程式例外錯誤, 取得設定檔異常: " + ex.Message);
                    return (int)ProcessInit.ProcessException;
                }
            }
            else
            {
                try
                {
                    IPS = DeserializeFromXml<PS>(XmlPath);
                    if (IPS != null)
                    {
                        string chkReplyString = string.Empty;
                        bool chkReply = true;
                        foreach (ProcessItem GItem in IPS.MainProcess)
                        {
                            if (!File.Exists(GItem.Path))
                            {
                                chkReplyString += string.Format("[MainProcess]\r\n[Name]:[{0}]. Path:({1}). Path is not exists.\r\n", GItem.Name, GItem.Path);
                                chkReply = false;
                            }
                        }

                        foreach (ProcessItem GItem in IPS.SubProcess)
                        {
                            if (!File.Exists(GItem.Path))
                            {
                                chkReplyString += string.Format("[SubProcess]\r\n[Name]:[{0}]. Path:({1}). Path is not exists.\r\n", GItem.Name, GItem.Path);
                                chkReply = false;
                            }
                        }

                        if (!chkReply)
                        {
                            WriteLog("[MSG] 設定檔檢查有錯誤:\r\n" + chkReplyString);
                            MessageBox.Show("設定檔檢查有錯誤:\r\n" + chkReplyString, "ERROR", MessageBoxButtons.OK);
                            return (int)ProcessInit.ProcessIncorrect;
                        }
                    }
                    else
                    {
                        WriteLog("[MSG] 找不到監控項目!!");
                        MessageBox.Show("找不到監控項目!!");
                        return (int)ProcessInit.SetupExist;
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("[ERROR] 監控程式例外錯誤, 取得檢查異常: " + ex.Message);
                    MessageBox.Show("監控程式例外錯誤, 取得檢查異常: " + ex.Message);
                    return (int)ProcessInit.ProcessException;
                }
            }
            WriteLog("[MSG] 監控程式檢查完成, 可正常執行!!");
            WriteLog("[END] Init Process");
            return (int)ProcessInit.ProcessCorrect;
        }
        #endregion

        #region Watch
        public void Watch()
        {
            PsList = new List<IProcess>();
            WatchThread = new List<Thread>();

            WriteLog("[START] Start Watch Process");
            if (CurName.ToUpper().Equals("WatchDog_Main".ToUpper()))
            {
                foreach (ProcessItem GItem in IPS.MainProcess)
                {
                    if (File.Exists(GItem.Path))
                    {
                        PsList.Add(new IProcess(GItem));
                    }
                }
            }
            else
            {
                foreach (ProcessItem GItem in IPS.SubProcess)
                {
                    if (File.Exists(GItem.Path))
                    {
                        PsList.Add(new IProcess(GItem));
                    }
                }
            }
            if (PsList != null && PsList.Count > 0)
            {
                int index = 0;
                foreach (IProcess ps in PsList)
                {
                    WatchThread.Add(new Thread(() => { this.Start(ps); }));
                    WatchThread[index++].Start();
                }
            }
            WriteLog("[END] Start Watch Process");
        }
        #endregion

        #region UnWatch
        public void UnWatch()
        {
            if (WatchThread != null)
            {
                WriteLog("[START] End Watch Process");
                foreach (Thread thd in WatchThread)
                {
                    thd.Abort();
                }
                foreach (IProcess ps in PsList)
                {
                    this.Stop(ps);
                }
                WriteLog("[END] End Watch Process");
            }
        }
        #endregion

        #region Start
        public void Start(IProcess ps)
        {
            while (true)
            {
                Thread.Sleep((int)(1000 * float.Parse(ps.Timer)));
                if (!ps.IsAlive())
                {
                    WriteLog(string.Format("[MSG] Process named {0} is closed.\r\nName: {1}\r\nPath: {2}\r\nTimer: {3} second(s)", ps.Name, ps.Name, ps.Path, (int)(1000 * float.Parse(ps.Timer)) / 1000));
                    ps.Start();
                }
            }
        }
        #endregion

        #region Stop
        public void Stop(IProcess ps)
        {
            if (ps.IsAlive())
            {
                ps.Stop();
                WriteLog(string.Format("[MSG] Process named {0} will be closed.", ps.Name));
            }
        }
        #endregion

        #region WriteLog
        public void WriteLog(string Message)
        {
            string CurName = Path.GetFileNameWithoutExtension(System.Windows.Forms.Application.ExecutablePath);
            string Dir = CurName.ToUpper().Equals("WatchDog_Main".ToUpper()) ? "MAIN" : "SUB";
            LogTrace(Dir, Message);
        }
        #endregion
    }
    #endregion

    #region IProcess 基本 Process 操作
    public class IProcess
    {
        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        public string Name { get; set; }
        public string Path { get; set; }
        public string Timer { get; set; }
        private IntPtr ptrHide { get; set; }

        #region Construct
        public IProcess(ProcessItem item)
        {
            this.Name = item.Name;
            this.Path = item.Path;
            this.Timer = item.Timer;
            this.ptrHide = IntPtr.Zero;
        }
        #endregion

        #region IsAlive
        public Boolean IsAlive()
        {
            Process p = this.GetProcess();
            if (p == null)
                return false;
            if (p.Responding == true)
                return true;
            return !p.HasExited;
        }
        #endregion

        #region Start
        public void Start()
        {
            if (!this.IsAlive())
                Process.Start(this.Path);
        }
        #endregion

        #region Stop
        public void Stop()
        {
            try
            {
                if (this.IsAlive())
                {
                    foreach (Process ps in Process.GetProcessesByName(this.Name))
                    {
                        ps.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Hide
        public void Hide()
        {
            if (this.GetProcess() != null)
            {
                if (ptrHide != IntPtr.Zero) return;
                ptrHide = this.GetProcess().MainWindowHandle;
                ShowWindow(ptrHide, 0);
            }
        }
        #endregion

        #region Show
        public void Show()
        {
            if (this.GetProcess() != null)
            {
                if (this.ptrHide == IntPtr.Zero)
                {
                    ShowWindow(this.GetProcess().MainWindowHandle, 1);
                }
                else
                {
                    ShowWindow(ptrHide, 1);
                    ptrHide = IntPtr.Zero;
                }
            }
        }
        #endregion

        #region GetProcess
        private Process GetProcess()
        {
            Process[] p = Process.GetProcessesByName(this.Name);
            if (p.Length > 0)
            {
                return p[0];
            }
            return null;
        }
        #endregion
    }
    #endregion

    #region ENUM ProcessInit 狀態回傳
    public enum ProcessInit
    {
        SetupExist = -1,        // 監控程式設定檔不存在
        ProcessGet = 0,         // 監控程式找不到監控對象
        ProcessCorrect = 1,     // 監控程式檢查路徑成功
        ProcessIncorrect = 2,   // 監控程式檢查路徑異常
        ProcessException = 3    // 監控程式例外錯誤
    }
    #endregion

    #region PS XML DEFINE
    public class PS
    {
        public List<ProcessItem> MainProcess { get; set; } = new List<ProcessItem>();
        public List<ProcessItem> SubProcess { get; set; } = new List<ProcessItem>();
    }
    public class ProcessItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Timer { get; set; }
    }
    #endregion
}
