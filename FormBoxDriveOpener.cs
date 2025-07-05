#define xxCLIPHELPER
using LiteDB;
using Microsoft.Win32;
using MYINI;
using MYLOG;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using static BoxDriveOpener.FormBoxDriveOpener.NativeMethods;

namespace BoxDriveOpener
{
    public partial class FormBoxDriveOpener : Form
    {
        public static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            public extern static void AddClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll", SetLastError = true)]
            public extern static void RemoveClipboardFormatListener(IntPtr hwnd);
            public const int WM_CLIPBOARDUPDATE = 0x031D;
            [DllImport(@"USER32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr GetForegroundWindow();
            [DllImport(@"USER32.dll", CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
            public enum ProcessDPIAwareness
            {
                ProcessDPIUnaware = 0,
                ProcessSystemDPIAware = 1,
                ProcessPerMonitorDPIAware = 2
            }
            [DllImport(@"SHCORE.dll")]
            public static extern int SetProcessDpiAwareness(ProcessDPIAwareness value);
            [DllImport(@"USER32.dll")]
            public static extern bool SetProcessDPIAware();
            [DllImport(@"USER32.dll")]
            public static extern IntPtr MonitorFromWindow([In] IntPtr hwnd, [In] uint dwFlags);
            public enum DpiType
            {
                Effective = 0,
                Angular = 1,
                Raw = 2
            }
            [DllImport(@"SHCORE.dll")]
            public static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

            // マウスイベント関連の定数
            public const int MOUSEEVENTF_MOVED = 0x0001;
            public const int MOUSEEVENTF_ABSOLUTE = 0x8000;
            public const int screen_length = 0x10000;

            // マウス操作を制御するためのMOUSEINPUT構造体
            [StructLayout(LayoutKind.Sequential)]
            public struct MOUSEINPUT
            {
                public int dx;
                public int dy;
                public int mouseData;
                public int dwFlags;
                public int time;
                public IntPtr dwExtraInfo;
            }
            // SendInputメソッド用の構造体
            [StructLayout(LayoutKind.Sequential)]
            public struct INPUT
            {
                public int type;
                public MOUSEINPUT mi;
            }
            // SendInputメソッドを宣言
            [DllImport("user32.dll")]
            public extern static uint SendInput(
                uint nInputs,     // INPUT 構造体の数(イベント数)
                INPUT[] pInputs,  // INPUT 構造体
                int cbSize        // INPUT 構造体のサイズ
            );
        }

        protected override void OnLoad(EventArgs e)
        {
            NativeMethods.AddClipboardFormatListener(Handle);
            base.OnLoad(e);
        }

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            //  Designer.csから移動
            NativeMethods.RemoveClipboardFormatListener(Handle);
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                this.OnDrawClipboard();
                m.Result = IntPtr.Zero;
            }
            else
            base.WndProc(ref m);
        }

        public class MyClipboardEventArg : EventArgs
        {
            public bool ContainsFileDropList { get; set; }
            public bool ContainsText { get; set; }
            public string ClipboardText { get; set; }
            public MyClipboardEventArg()
            {
                ContainsFileDropList = false;
                ContainsText = false;
                ClipboardText = "";
            }
        }
        public delegate void MyClipboardTextEventHandler(object sender, MyClipboardEventArg e);
        public event MyClipboardTextEventHandler DrawClipboard = null;
        // STAで実行されるTaskオブジェクトを生成する
        class STATask
        {
            public static Task Run<T>(Func<T> func)
            {
                var tcs = new TaskCompletionSource<T>();
                var thread = new Thread(() =>
                {
                    try
                    {
                        tcs.SetResult(func());
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                return tcs.Task;
            }

            public static Task Run(Action act)
            {
                return Run(() =>
                {
                    act();
                    return true;
                });
            }
        }

        private object lock4ee = new object();
        private MyClipboardEventArg ee = null;

        protected void OnDrawClipboard()
        {
            if (this.DrawClipboard != null)
            {
                if (ee == null)
                {
                    lock (lock4ee)
                    {
                        ee = new MyClipboardEventArg();
                    }
                    var task = STATask.Run(async () =>
                    {
                        ee.ContainsFileDropList = Clipboard.ContainsFileDropList();
                        ee.ContainsText = Clipboard.ContainsText();
                        if (ee.ContainsText == true)
                        {
                            ee.ClipboardText = Clipboard.GetText();
                        }
                        else
                        if (ee.ContainsFileDropList == true)
                        {
                            ee.ClipboardText = "";
                            foreach (var item in Clipboard.GetFileDropList())
                            {
                                ee.ClipboardText += item + "\n";
                            }
                        }
                        if (ee.ClipboardText.Length < 4096 * 4 * 4)
                        {
                            Invoke((MethodInvoker)delegate ()
                            {
                                this.DrawClipboard(this, ee);
                            });
                            await Task.Delay(20);   //  10ms以内の再イベントは無視。ここを大きく取るとExcelからのイベントの来かたがおかしくなる！？
                        }
                        ee = null;
                    });
                }
            }
        }

        public FormBoxDriveOpener()
        {
            InitializeComponent();
        }

        private LiteDatabase litedb = null;
        private ILiteCollection<BsonDocument> BoxDrivePath;

        private ClassLog LOG = new ClassLog();
        private ClassINI INI = new ClassINI("", true, true);
        private int HttpPort = 62678;  // HTTPリスナーポート番号。ポート番号は利用中なら最大4*20番(20番毎)後方まで広げる
        private HttpListener listener = null;
        private string BoxURL = "";
        private string BoxDriveRoot = "";
        private const string boxUrlPattern = "https://.*.box.com/(folder|file)/([0-9]*)[?]?.*";   //  $2がID

        private Form FormPopup = null;
        private System.Windows.Forms.Timer PopupTimer1 = new System.Windows.Forms.Timer();

        private void PopupTimer1_Tick(object sender, EventArgs e)
        {
            if (this.FormPopup.Visible == true)
            {
                try
                {
                    this.PopupTimer1.Stop();
                }
                catch { }
                finally
                {
                    try
                    {
                        this.FormPopup.Close();
                    }
                    catch { }
                }
            }
        }
        private void L_PopupMesg(string P_Message)
        {
            this.L_PopupMesg(P_Message, 800, this.LBL_PopupColor.BackColor, this.LBL_PopupColor.ForeColor);
        }
        private void L_PopupMesg(string P_Message, int P_Interval)
        {
            this.L_PopupMesg(P_Message, P_Interval, this.LBL_PopupColor.BackColor, this.LBL_PopupColor.ForeColor);
        }
        private void L_PopupMesg(string P_Message, Color P_BackColor, Color P_ForeColor)
        {
            this.L_PopupMesg(P_Message, 800, P_BackColor, P_ForeColor);
        }
        private void L_PopupMesg(string P_Message, int P_Interval, Color P_BackColor, Color P_ForeColor)
        {
            //return; //  何もしない

            if (this.FormPopup != null)
            {
                this.PopupTimer1.Stop();
                this.FormPopup.Visible = false;
                this.FormPopup.Close();
        }
            this.FormPopup = new Form();
            try
            {
                this.FormPopup.StartPosition = FormStartPosition.Manual;
                this.FormPopup.AutoSize = false;
                this.FormPopup.BackColor = P_BackColor;
                this.FormPopup.ControlBox = false;
                this.FormPopup.Text = "";
                this.FormPopup.TopMost = true;
                this.FormPopup.ShowIcon = false;
                this.FormPopup.ShowInTaskbar = false;

                Label LBL_MSG = new Label();
                LBL_MSG.AutoSize = false;
                LBL_MSG.Font = this.LBL_PopupColor.Font;
                LBL_MSG.ForeColor = P_ForeColor;
                LBL_MSG.Location = new System.Drawing.Point(0, 0);
                LBL_MSG.Name = "LBL_MSG";
                LBL_MSG.Size = new System.Drawing.Size(52, 18);
                LBL_MSG.TabIndex = 0;
                LBL_MSG.Text = P_Message;
                LBL_MSG.Visible = true;
                LBL_MSG.AutoSize = true;
                this.FormPopup.Controls.Add(LBL_MSG);

                this.FormPopup.Width = LBL_MSG.Width + SystemInformation.FrameBorderSize.Width * 2 * 2 + SystemInformation.BorderSize.Width * 2;
                this.FormPopup.Height = LBL_MSG.Height + SystemInformation.CaptionHeight;

                IntPtr hWnd = NativeMethods.GetForegroundWindow();
                //  フォーカスウィンドウのスクリーン左下にチラっと画面を表示させる
                var SC = Screen.FromHandle(hWnd);
                this.FormPopup.Top = SC.WorkingArea.Bottom - FormPopup.Height;
                this.FormPopup.Left = SC.WorkingArea.Left;

                this.PopupTimer1.Interval = P_Interval;
                this.PopupTimer1.Start();

                this.FormPopup.Show();
                this.FormPopup.Refresh();
            }
            catch
            {
                try
                {
                    this.FormPopup.Close();
                }
                catch { }
            }
        }

        private string exeFile = @"cmd.exe";
        private string paraFile = "/c start explorer.exe /select,\"%nx1\"";
        private string exeFolder = @"cmd.exe";
        private string paraFolder = "/c start .";
        private string exeBrowser = "%1";
        private string paraBrowser = "";
        private async Task<bool> L_Exec(string P_Path)
        {
            return await L_Exec(P_Path, true, true);
        }
        private async Task<bool> L_Exec(string P_Path, bool P_DeployBoxlink, bool P_OpenBoxDrive)
        {
            bool rc = false;
            try
            {
                var path = ("\n" + P_Path).Replace("\n/", "\n" + this.BoxDriveRoot + "/");
                path = path.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE").Replace("\\", "/"));
                path = path.Replace("\r\n", "\n").Replace("file:", "").Trim();
                var relocalboxfolder = "([A-Z]:|\\\\.*\\)".Replace(".", "[.]").Replace("\\", "/").Replace("/", "[\\\\/]");  //  フォルダ／ファイルパス
                var re = new Regex("(" + "(^|[ |\t])" + "http[s]?:.*?" + "([ \t]|$)" //  URL
                    + "|" + "\"" + relocalboxfolder + @".*?" + "\""     //  "囲み
                    + "|" + "'" + relocalboxfolder + @".*?" + "'"       //  '囲み
                    + "|" + "(^|[ |\t])" + relocalboxfolder + @".*?" + "([ \t]|$)"  //  囲みなし（空白までで区切られる）
                    + "|" + "^%[0-9]*%[/]" + @".*?" + "([ \t]|$)"       //  %～%始まり
                    + ")"
                    , RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (re.Matches(path).Count > 1)
                {
                    var paths = "";
                    foreach (var rr in re.Matches(path))
                    {
                        var pp = rr.ToString().Trim();
                        if (((pp.StartsWith("\"") == true) && (pp.EndsWith("\"") == true))
                        || ((pp.StartsWith("'") == true) && (pp.EndsWith("'") == true)))
                        {
                            pp = pp.Substring(1, pp.Length - 2);
                        }
                        re = new Regex("^" + boxUrlPattern);   //  box URLで始まる
                        if (re.IsMatch(pp) == true)
                        {
                            //  box URLのとき
                            if ((P_DeployBoxlink == false) && (P_OpenBoxDrive == false))
                            {
                                //
                            }
                            else
                            {
                                var W_ID = re.Replace(pp, "$2");
                                var W_Path = this.L_GetBoxDriveFolder(this.L_GetBoxDrivePath(W_ID));
                                if (W_Path != "")
                                {
                                    //  存在する
                                    pp = W_Path.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE").Replace("\\", "/"));
                                }
                            }
                        }
                        else
                        {
                            pp = this.L_GetBoxDriveFolder(pp).Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE").Replace("\\", "/"));
                        }
                        if (paths.Contains("<>" + pp + "<>") == false)    //  同じものは1度だけ
                        {
                            paths += "<>" + pp + "<>";
                            rc = await this.L_Exec(pp, P_DeployBoxlink, P_OpenBoxDrive);    //  1つのURLまたはファイル毎に再帰。結果は最後のみ
                        }
                    }
                }
                else
                {
                    if (((path.StartsWith("\"") == true) && (path.EndsWith("\"") == true))
                    || ((path.StartsWith("'") == true) && (path.EndsWith("'") == true)))
                    {
                        path = path.Substring(1, path.Length - 2);
                    }
                    re = new Regex("^" + boxUrlPattern);   //  box URLで始まる
                    var isBoxUrl = re.IsMatch(path);
                    if (isBoxUrl == true)
                    {
                        //  box URLのとき
                        if ((P_DeployBoxlink == false) && (P_OpenBoxDrive == false))
                        {
                            //
                        }
                        else
                        {
                            var W_ID = re.Replace(path, "$2");
                            var W_Path = this.L_GetBoxDriveFolder(this.L_GetBoxDrivePath(W_ID));
                            this.LOG.PutLog2("Info", string.Format("{0},{1}={2}", "exec", path, W_Path));
                            if (W_Path != "")
                            {
                                //  存在する→explorerで開く
                                return await this.L_Exec(W_Path, P_DeployBoxlink, P_OpenBoxDrive);  //  再帰
                                                                                                    //  exit;
                            }
                        }
                    }
                    {
                        //  box URL以外のとき
                        path = this.L_GetBoxDriveFolder(path).Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"));
                        this.L_PopupMesg(path, this.LBL_ExecPopup.BackColor, this.LBL_ExecPopup.ForeColor);
                        Process Proc = new Process();
                        if (File.Exists(path) == true)
                        {
                            //  ファイルで存在
                            var fi = new FileInfo(path.Replace("/", "\\"));
                            if ((P_DeployBoxlink == true) && (fi.Extension.ToUpper() == ".BOXLNK"))
                            {
                                var S = "";
                                StreamReader SR = new StreamReader(fi.FullName, Encoding.Default);
                                if (SR.Peek() != -1) //  EOFでない
                                {
                                    S = SR.ReadToEnd();
                                    //var S = SR.ReadLine().Split(new char[] { ',' });
                                    //this.LOG.PutLog2("Info", string.Format("{0},{1}={2}", "exec", path, S[0]));
                                    //re = new Regex("^" + boxUrlPattern);   //  box URLで始まる
                                    //if (re.IsMatch(S[0]) == true)
                                    //{
                                    //    //  URL時のみ
                                    //    await this.L_Exec(S[0]);    //  再帰
                                    //}
                                }
                                SR.Close();
                                await this.L_Exec(S, P_DeployBoxlink & P_OpenBoxDrive, P_OpenBoxDrive);  //  再帰
                                return true;
                                //  exit;
                            }
                            else
                            if (P_OpenBoxDrive == true)
                            {
                                this.LOG.PutLog2("Info", string.Format("{0},{1}={2}", "exec", path, fi.FullName));
                                var exe = this.exeFile;
                                var para = this.paraFile;
                                Proc.StartInfo.FileName = exe.Replace("%1", fi.FullName);
                                Proc.StartInfo.Arguments = para.Replace("%nx1", fi.Name)
                                    .Replace("%x1", fi.Extension)
                                    .Replace("%n1", fi.Name.Replace(fi.Extension, ""))
                                    .Replace("%p1", fi.DirectoryName)
                                    .Replace("%1", fi.FullName);
                                Proc.StartInfo.WorkingDirectory = fi.DirectoryName;
                                rc = true;
                            }
                        }
                        else
                        if (Directory.Exists(path) == true)
                        {
                            if (P_OpenBoxDrive == true)
                            {
                                //  エクスプローラで開く。前面で開くため→cmd /c start .
                                var fo = new DirectoryInfo(path.Replace("/", "\\"));
                                this.LOG.PutLog2("Info", string.Format("{0},{1}={2}", "exec", path, fo.FullName));
                                var exe = this.exeFolder;
                                var para = this.paraFolder;
                                Proc.StartInfo.FileName = exe.Replace("%1", fo.FullName);
                                Proc.StartInfo.Arguments = para.Replace("%1", fo.FullName);
                                Proc.StartInfo.WorkingDirectory = fo.FullName;
                                rc = true;
                            }
                        }
                        else
                        {
                            var exe = this.exeBrowser;
                            var para = this.paraBrowser;
                            if ((P_OpenBoxDrive == true) && (isBoxUrl == true))
                            {
                                path = path.Replace("_doBoxDriveOpen_=1" + "?", "").Replace("_doBoxDriveOpen_=1" + "&", "");
                                path = path.Replace("?" + "_doBoxDriveOpen_=1", "").Replace("&" + "_doBoxDriveOpen_=1", "");
                                path += (path.Contains("?") == false ? "?" : "&") + "_doBoxDriveOpen_=1";   //  1=open
                                //para += (para.Contains("?") == false ? "?" : "&") + "_doBoxDriveOpen_=1";
                            }
                            Proc.StartInfo.FileName = exe.Replace("%1", path);
                            Proc.StartInfo.Arguments = para.Replace("%1", path);
                            this.LOG.PutLog2("Info", string.Format("{0},{1},{2}", "exec", Proc.StartInfo.FileName, Proc.StartInfo.Arguments));
                        }
                        this.textBox2.Text = "exe=" + Proc.StartInfo.FileName + " " + Proc.StartInfo.Arguments
                                + "\r\n" + this.textBox2.Text;
                        Proc.StartInfo.UseShellExecute = true;
                        Proc.StartInfo.Verb = "";
                        Proc.Start();
                    }
                }
            }
            catch { }

            return rc;
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            CustomExceptionHandler.ActiveForm = this;
            this.LOG.HiddenFile = false;
            this.LOG.SystemFile = false;
            this.LOG.MakeHIBETU_Flg = true;
            this.LOG.OutputMiliSec_Flg = true;
            this.LOG.ProcName = "onLoad";
            this.LOG.Enabled = this.INI.Read("Debug", "Log", true);
            this.LOG.OutputLevel = this.INI.Read("Debug", "LogLevel", 1);
            this.LOG.LogCleaningSpan = this.INI.Read("Debug", "LogCleaningSpan", 1);
            this.INI.Write("Debug", "Log", this.LOG.Enabled);
            this.INI.Write("Debug", "LogLevel", this.LOG.OutputLevel);
            this.INI.Write("Debug", "LogCleaningSpan", this.LOG.LogCleaningSpan);

            //  高DPI対応
            try
            {
                //  (Win8.1～Win10 1607以前)
                NativeMethods.SetProcessDpiAwareness(NativeMethods.ProcessDPIAwareness.ProcessPerMonitorDPIAware);
            }
            catch
            {
                try
                {
                    //  (Vista～Win8.0)
                    NativeMethods.SetProcessDPIAware();
                }
                catch { }
            }
            var mon = NativeMethods.MonitorFromWindow(this.Handle, 2/*MONITOR_DEFAULTTONEAREST*/);
            uint dpiX;
            uint dpiY;
            NativeMethods.GetDpiForMonitor(mon, NativeMethods.DpiType.Effective, out dpiX, out dpiY);
            if (dpiX > 96)
            {
                this.Font = new Font(this.Font.FontFamily, (float)(this.Font.Size * dpiX / 96 * 0.8));
            }
            //

            this.Top = Screen.PrimaryScreen.WorkingArea.Top + Screen.PrimaryScreen.WorkingArea.Height - this.Height - 5;
            this.Left = 5;
            this.textBox1.Text = "";
            this.textBox2.Text = "";

            if (Environment.GetCommandLineArgs().Length == 1)
            {
                //  引数なしのとき
                this.LOG.PutLog1("Info", string.Format("{0}", "開始"));
            }
            else
            {
                this.LOG.PutLog3("Info", string.Format("{0}", "開始"));
            }
#if DEBUG
            this.LOG.PutLog("Info", "起動," + Environment.CommandLine);
#endif
            RegistryKey regKey = null;
            if ((Environment.GetCommandLineArgs().Length == 1)
            || (Environment.GetCommandLineArgs().Length == 2) && (Environment.GetCommandLineArgs()[1] == @"/reg"))
            {
                //  拡張子関連付け（要、管理者権限）
                try
                {
                    regKey = Registry.ClassesRoot.OpenSubKey(@".boxlnk");
                    if (regKey == null)
                    {
                        regKey.Close();
                        regKey = Registry.ClassesRoot.CreateSubKey(@".boxlnk");
                        regKey.SetValue(@"", @"BoxDriveOpener");
                    }
                    regKey.Close();
                    regKey = Registry.ClassesRoot.OpenSubKey(@"BoxDriveOpener");
                    if (regKey == null)
                    {
                        regKey.Close();
                        regKey = Registry.ClassesRoot.CreateSubKey(@"BoxDriveOpener\shell\OPEN");
                        regKey.SetValue(@"EditFlags", new byte[] { 1, 0, 0, 0 });
                        regKey.Close();
                        regKey = Registry.ClassesRoot.CreateSubKey(@"BoxDriveOpener\shell\OPEN\command");
                        regKey.SetValue(@"", "dummy");
                    }
                    regKey.Close();
                    regKey = Registry.ClassesRoot.OpenSubKey(@"BoxDriveOpener\shell\OPEN\command");
                    if (regKey.GetValue(@"").ToString() != @"""" + Application.ExecutablePath + @""" ""%1"" ""%2"" ""%3"" ""%4"" ""%5"" ""%6"" ""%7"" ""%8"" ""%9""")
                    {
                        regKey.Close();
                        this.LOG.PutLog("Info", "レジストリ登録実施," + Application.ExecutablePath);
                        regKey = Registry.ClassesRoot.OpenSubKey(@"BoxDriveOpener\shell\OPEN\command", true);
                        regKey.SetValue(@"", @"""" + Application.ExecutablePath + @""" ""%1"" ""%2"" ""%3"" ""%4"" ""%5"" ""%6"" ""%7"" ""%8"" ""%9""");
                        regKey.Close();
                        regKey = Registry.ClassesRoot.OpenSubKey(@"BoxDriveOpener\DefaultIcon", true);
                        regKey.SetValue(@"", @"""" + Application.ExecutablePath + @""",1");
                    }
                    regKey.Close();
                    if ((Environment.GetCommandLineArgs().Length == 2) && (Environment.GetCommandLineArgs()[1] == @"/reg"))
                    {
                        this.INI.Write("Reg", "Do", "OK");
                        MessageBox.Show("拡張子.boxlnkの関連付けを(再)登録しました", this.Text);
                        Application.Exit();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if ((Environment.GetCommandLineArgs().Length != 2)
                    || (Environment.GetCommandLineArgs().Length >= 2) && (Environment.GetCommandLineArgs()[1] != @"/reg"))
                    {
                        if (this.INI.Read("Reg", "Do", "") == "Cancel")
                        {
                            //  skip
                            this.LOG.PutLog3("Debug", "レジストリ登録skip");
                        }
                        else
                        if (MessageBox.Show("この後、管理者権限(コマンドプロンプト)に移行して拡張子.boxlnkの関連付けを(再)登録します", this.Text, MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                        {
                            this.INI.Write("Reg", "Do", "Cancel");
                            this.INI.Write("Reg", "Help", "拡張子関連付けを行いたい場合は、[Reg]セクションを削除し、再起動する");
                        }
                        else
                        {
                            Process Proc = new Process();
                            //Proc.StartInfo.FileName = Application.ExecutablePath;
                            //Proc.StartInfo.WorkingDirectory = new FileInfo(Application.ExecutablePath).DirectoryName;
                            //Proc.StartInfo.Arguments = @"/reg";
                            Proc.StartInfo.FileName = @"cmd.exe";
                            Proc.StartInfo.Arguments = @"/c " + @"""" + Application.ExecutablePath + @"""" + @" /reg";
                            Proc.StartInfo.UseShellExecute = true;
                            Proc.StartInfo.Verb = "runas";
                            Proc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                            Proc.Start();
                            this.LOG.PutLog("Info", "レジストリ登録起動");
                        }
                    }
                    else
                    {
                        this.LOG.PutLog("Error", ex.Message);
                        MessageBox.Show(ex.Message, this.Text);
                    }
                }
            }
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                //  引数なしのとき
                regKey = Registry.LocalMachine.OpenSubKey(@"Software\Box\Box");
                try
                {
                    try
                    {
                        this.BoxDriveRoot = (string)regKey.GetValue(@"CustomBoxLocation");
                        this.BoxDriveRoot = this.BoxDriveRoot.Replace(Environment.GetEnvironmentVariable("USERPROFILE"), "%USERPROFILE%").Replace(@"\", "/");
                        regKey.Close();
#if DEBUG
                        this.LOG.PutLog2("Debug", @"Software\Box\Box." + @"CustomBoxLocation=" + this.BoxDriveRoot);
#endif
                    }
                    catch
                    {
                        this.BoxDriveRoot = "";
                    }
                }
                catch
                {
                    regKey.Close();
                }
                if (this.BoxDriveRoot == "")
                {
                    regKey = Registry.CurrentUser.OpenSubKey(@"Software\Box\Box\preferences");
                    try
                    {
                        this.BoxDriveRoot = (string)regKey.GetValue(@"sync_directory_path");
                        this.BoxDriveRoot = this.BoxDriveRoot.Replace(Environment.GetEnvironmentVariable("USERPROFILE"), "%USERPROFILE%").Replace(@"\", "/");
                        regKey.Close();
#if DEBUG
                        this.LOG.PutLog2("Debug", @"Software\Box\Box\preferences." + @"sync_directory_path=" + this.BoxDriveRoot);
#endif
                    }
                    catch
                    {
                        {
                            this.LOG.PutLog("Debug", "Box Driveパスが取得できませんでした");
                            MessageBox.Show("Box Driveパスが取得できませんでした。終了します", this.Text);
                            Application.Exit();
                            return;
                        }
                    }
                }
                this.LOG.PutLog1("Info", "BoxDriveRoot=" + this.BoxDriveRoot);

                if (this.INI.Read("File", "Exe", "(none)") == "(none)")
                {
                    this.INI.Write("File", "説明", @"ファイルを開く際のコマンドとパラメータ。ファイルの所在フォルダをカレントとし、%1にフルパスがセットされて実行されます");
                    this.INI.Write("File", "説明2", @"パラメータには次の変数も利用可能");
                    this.INI.Write("File", "説明%nx1", @"%nx1：ファイル名（拡張子あり）");
                    this.INI.Write("File", "説明%x1", @"%x1：拡張子（ピリオド含む）");
                    this.INI.Write("File", "説明%n1", @"%n1：ファイル名（拡張子なし）");
                    this.INI.Write("File", "説明%p1", @"%p1：フォルダ名");
                    this.INI.Write("File", "Exe", this.exeFile);
                    this.INI.Write("File", "Param", this.paraFile);
                    this.INI.Write("Folder", "説明", @"フォルダを開く際のコマンドとパラメータ。フォルダをカレントとし、%1にフルパスがセットされて実行されます");
                    this.INI.Write("Folder", "Exe", this.exeFolder);
                    this.INI.Write("Folder", "Param", this.paraFolder);
                    this.INI.Write("Browser", "説明", @"URLを開く際のコマンドとパラメータ。%1にURLがセットされて実行されます");
                    this.INI.Write("Browser", "Exe", this.exeBrowser);
                    this.INI.Write("Browser", "Param", this.paraBrowser);
                }
                this.exeFile = this.INI.Read("File", "Exe", this.exeFile);
                this.paraFile = this.INI.Read("File", "Param", this.paraFile);
                this.exeFolder = this.INI.Read("Folder", "Exe", this.exeFolder);
                this.paraFolder = this.INI.Read("Folder", "Param", this.paraFolder);
                this.exeBrowser = this.INI.Read("Browser", "Exe", this.exeBrowser);
                this.paraBrowser = this.INI.Read("Browser", "Param", this.paraBrowser);
                this.LOG.PutLog1("Info", "exeFile=" + this.exeFile);
                this.LOG.PutLog1("Info", "paraFile=" + this.paraFile);
                this.LOG.PutLog1("Info", "exeFolder=" + this.exeFolder);
                this.LOG.PutLog1("Info", "paraFolder=" + this.paraFolder);
                this.LOG.PutLog1("Info", "exeBrowser=" + this.exeBrowser);
                this.LOG.PutLog1("Info", "paraBrowser=" + this.paraBrowser);

                this.CB_USE_BOXLINK.Checked = this.INI.Read("Box", "BoxLink", this.CB_USE_BOXLINK.Checked);
                this.LOG.PutLog1("Info", "BoxLink=" + this.CB_USE_BOXLINK.Checked);
                this.CB_CLOSE_SIDEBAR_FOLDER.Checked = this.INI.Read("Box", "FolderSidebarClose", this.CB_CLOSE_SIDEBAR_FOLDER.Checked);
                this.LOG.PutLog1("Info", "FolderSidebarClose=" + this.CB_CLOSE_SIDEBAR_FOLDER.Checked);
                this.CB_CLOSE_SIDEBAR_FILE.Checked = this.INI.Read("Box", "FileSidebarClose", this.CB_CLOSE_SIDEBAR_FILE.Checked);
                this.LOG.PutLog1("Info", "FileSidebarClose=" + this.CB_CLOSE_SIDEBAR_FILE.Checked);
                this.BoxURL = this.INI.Read("Box", "URL", this.BoxURL);
                this.LOG.PutLog1("Info", "BoxURL=" + this.BoxURL);
                this.litedb = new LiteDatabase(Application.ExecutablePath + "/../BoxDriveOpener.db");
                this.BoxDrivePath = this.litedb.GetCollection("BoxDrivePath");
                this.BoxDrivePath.EnsureIndex("Id");
                this.BoxDrivePath.EnsureIndex("Path");
                this.textBox1.Tag = DateTime.Now;

                //  http listener
                var task = Task.Run(() =>
                {
                    // HTTPリスナー作成。ポート番号は利用中なら最大4*20番(20番毎)後方まで広げる
                    this.listener = null;
                    var i = 0;
                    for (i = 0; i < 4; i++)
                    {
                        try
                        {
                            // リスナー設定
                            this.listener = new HttpListener();
                            this.listener.Prefixes.Clear();
                            this.listener.Prefixes.Add(string.Format(@"http://localhost:{0}/", this.HttpPort + i * 20));

                            // リスナー開始
                            this.listener.Start();
                            Invoke((MethodInvoker)delegate ()
                            {
                                this.textBox2.Text = "Listen," + string.Format(@"http://localhost:{0}/", this.HttpPort + i * 20) 
                                    + "\r\n" + this.textBox2.Text;
                            });
                            this.LOG.PutLog("listener", "Info", "listener.Start=" + (this.HttpPort + i * 20).ToString(), 1);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Invoke((MethodInvoker)delegate ()
                            {
                                this.textBox1.Text = "listener.Start(" + (this.HttpPort + i * 20).ToString() + ")=" + ex.Message
                                + "\r\n" + this.textBox1.Text;
                            });
                            this.LOG.PutLog("listener", "Error", "listener.Start(" + (this.HttpPort + i * 20).ToString() + ")=" + ex.Message, 1);
                        }
                    }
                    if (this.listener.IsListening == false)
                    {
                        Invoke((MethodInvoker)delegate ()
                        {
                            this.textBox1.Text = "Listenerを開始できませんでした。起動しなおしてください"
                                + "\r\n" + "（Browserの再起動が必要な場合もあります）"
                                + "\r\n" + this.textBox1.Text;
                            this.WindowState = FormWindowState.Normal;
                        });
                    }
                    else
                    {
                        Invoke((MethodInvoker)delegate ()
                        {
                            this.textBox1.Text = "Listening"
                                + "\r\n" + this.textBox1.Text;
                        });
                        while (this.listener.IsListening == true)
                        {
                            try
                            {
                                // リクエスト取得
                                HttpListenerContext context = this.listener.GetContext();
                                HttpListenerRequest request = context.Request;

                                // レスポンス取得
                                HttpListenerResponse response = context.Response;

                                if (request != null)
                                {
                                    if (Directory.Exists(this.BoxDriveRoot.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"))) == false)
                                    {
                                        // リスナー終了
                                        this.listener.Close();
                                        Invoke((MethodInvoker)delegate ()
                                        {
                                            this.textBox1.Text = "boxドライブにアクセスできないようです"
                                                + "\r\n" + this.textBox1.Text;
                                            this.WindowState = FormWindowState.Normal;
                                        });
                                        break;
                                    }
                                    var rawurl = HttpUtility.UrlDecode(request.RawUrl, Encoding.UTF8);
                                    this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl, 3);
                                    if (request.RawUrl.StartsWith("/boxurl?") == true)
                                    {
                                        Invoke((MethodInvoker)delegate ()
                                        {
                                            this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                + "\r\n" + this.textBox1.Text;
                                            this.textBox1.Tag = DateTime.Now;
                                        });
                                        if (request.QueryString["url"] != null)
                                        {
                                            if (this.BoxURL != request.QueryString["url"])
                                            {
                                                this.BoxURL = request.QueryString["url"];
                                                this.INI.Write("Box", "URL", this.BoxURL);
                                                Invoke((MethodInvoker)delegate ()
                                                {
                                                    this.LOG.PutLog("listener", "Info", "BoxURL=" + this.BoxURL, 1);
                                                    this.textBox1.Text = string.Format("Listening,{0},return,{1}", request.RawUrl, "ok")
                                                        + "\r\n" + this.textBox1.Text;
                                                });
                                            }
                                            string rc = "ok";
                                            byte[] text = Encoding.UTF8.GetBytes(rc);
                                            this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                            response.OutputStream.Write(text, 0, text.Length);
                                        }
                                    }
                                    else
                                    if (request.RawUrl.StartsWith("/dbglog?") == true)
                                    {
                                        var raw = rawurl.Split('?');
                                        var vallv = raw[1].Substring(0, raw[1].IndexOf('&'));   //vals[0];
                                        var valevt = raw[1].Substring(vallv.Length + 1).Substring(0, raw[1].Substring(vallv.Length + 1).IndexOf('&'));  //vals[1];
                                        var valmsg = raw[1].Substring(vallv.Length + 1 + valevt.Length + 1);    //vals[2];
                                        var lv = "";
                                        if (vallv != "")
                                        {
                                            lv = vallv.Split('=')[1];
                                        }
                                        var evt = "";
                                        if (valevt != "")
                                        {
                                            evt = valevt.Split('=')[1];
                                        }
                                        var msg = "";
                                        if (valmsg != "")
                                        {
                                            msg = valmsg.Split('=')[1];
                                        }
                                        this.LOG.PutLog("extension", evt, msg, int.Parse(lv));
                                        var rc = "ok";
                                        byte[] text = Encoding.UTF8.GetBytes(rc);
                                        response.OutputStream.Write(text, 0, text.Length);
                                    }
                                    else
                                    if (this.BoxURL != "")
                                    {

                                        if (request.RawUrl.StartsWith("/checkdbglevel") == true)
                                        {
                                            Invoke((MethodInvoker)delegate ()
                                            {
                                                this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                    + "\r\n" + this.textBox1.Text;
                                                this.textBox1.Tag = DateTime.Now;
                                            });
                                            var rc = "ok," + this.LOG.OutputLevel.ToString();
                                            byte[] text = Encoding.UTF8.GetBytes(rc);
                                            this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                            response.OutputStream.Write(text, 0, text.Length);
                                        }
                                        else
                                        if (request.RawUrl.StartsWith("/getfolder?") == true)
                                        {
                                            Invoke((MethodInvoker)delegate ()
                                            {
                                                this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                    + "\r\n" + this.textBox1.Text;
                                                this.textBox1.Tag = DateTime.Now;
                                            });
                                            var raw = rawurl.Split('?');
                                            var valid = raw[1];
                                            var id = "";
                                            if (valid != "")
                                            {
                                                id = valid.Split('=')[1];
                                            }
                                            if (id != "")
                                            {
                                                var rc = "";
                                                var rec = this.BoxDrivePath.FindOne("$.Id = '" + id + "'");
                                                if (rec == null)
                                                {
                                                    rc = "(none)";
                                                }
                                                else
                                                {
                                                    var path = this.L_GetBoxDrivePath(id);   //  物理が存在しなくなったIDは空白が返る
                                                    if (path != "")
                                                    {
                                                        path = rec["Path"].AsString;
                                                    }
                                                    else
                                                    {
                                                        path = "(lost)";
                                                    }
                                                    rc = path;
                                                }
                                                byte[] text = Encoding.UTF8.GetBytes(rc);
                                                this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                                response.OutputStream.Write(text, 0, text.Length);
                                                Invoke((MethodInvoker)delegate ()
                                                {
                                                    this.textBox1.Text = string.Format("Listening,{0},return,{1}", request.RawUrl, rc)
                                                        + "\r\n" + this.textBox1.Text;
                                                });
                                            }
                                        }
                                        else
                                        if (request.RawUrl.StartsWith("/folder?") == true)
                                        {
                                            Invoke((MethodInvoker)delegate ()
                                            {
                                                this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                    + "\r\n" + this.textBox1.Text;
                                                this.textBox1.Tag = DateTime.Now;
                                            });
                                            var raw = rawurl.Split('?');
                                            //var vals = raw[1].Split('&');
                                            var valid = raw[1].Substring(0, raw[1].IndexOf('&'));   //vals[0];
                                            var valpath = raw[1].Substring(valid.Length + 1);       //vals[1];
                                            var id = "";
                                            if (valid != "")
                                            {
                                                id = valid.Split('=')[1];
                                            }
                                            var path = "";
                                            if (valpath != "")
                                            {
                                                path = valpath.Split('=')[1];
                                            }

                                            if (id != "")
                                            {
                                                if (path != "")
                                                {
                                                    var rc = "ok";
                                                    var rec = this.BoxDrivePath.FindOne("$.Id = '" + id + "'");
                                                    if (rec == null)
                                                    {
                                                        rec = new BsonDocument { ["Id"] = id, ["Path"] = path };
                                                        this.BoxDrivePath.Insert(rec);
                                                        this.litedb.Checkpoint();
                                                        this.LOG.PutLog("listener", "Info", "BoxDrivePath.Insert=" + string.Format("{0}:{1}", id, path), 2);
                                                        Invoke((MethodInvoker)delegate ()
                                                        {
                                                            this.textBox2.Text = string.Format("Listening,{0},return,{1}", request.RawUrl, path)
                                                                + "\r\n" + this.textBox2.Text;
                                                        });
                                                    }
                                                    else
                                                    if (rec["Path"].AsString != path)
                                                    {
                                                        rec["Path"] = path;
                                                        this.BoxDrivePath.Update(rec);
                                                        this.litedb.Checkpoint();
                                                        this.LOG.PutLog("listener", "Info", "BoxDrivePath.Update=" + string.Format("{0}:{1}", id, path), 2);
                                                        Invoke((MethodInvoker)delegate ()
                                                        {
                                                            this.textBox2.Text = string.Format("Listening,{0},return,{1}", request.RawUrl, path)
                                                                + "\r\n" + this.textBox2.Text;
                                                        });
                                                    }
                                                    byte[] text = Encoding.UTF8.GetBytes(rc);
                                                    this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                                    response.OutputStream.Write(text, 0, text.Length);
                                                    Invoke((MethodInvoker)delegate ()
                                                    {
                                                        this.textBox1.Text = string.Format("Listening,{0},return,{1}", request.RawUrl, rc)
                                                            + "\r\n" + this.textBox1.Text;
                                                    });
                                                }
                                            }
                                        }
                                        else
                                        if (request.RawUrl.StartsWith("/copyclipboard?") == true)
                                        {
                                            Invoke((MethodInvoker)delegate ()
                                            {
                                                this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                    + "\r\n" + this.textBox1.Text;
                                                this.textBox1.Tag = DateTime.Now;
                                            });
                                            var raw = rawurl.Split('?');
                                            //var vals = raw[1].Split('&');
                                            var valurl = raw[1].Substring(0, raw[1].IndexOf('&'));   //vals[0];
                                            var valpath = raw[1].Substring(valurl.Length + 1);       //vals[1];
                                            var url = "";
                                            if (valurl != "")
                                            {
                                                url = valurl.Split('=')[1];
                                            }
                                            var path = "";
                                            if (valpath != "")
                                            {
                                                path = valpath.Split('=')[1];
                                            }

                                            if (url != "")
                                            {
                                                var ss = url;
                                                if (path != "")
                                                {
                                                    var wpath = this.BoxDriveRoot + path;
                                                    {
                                                        var Q = ""; //(dataPathValue.Contains(" ") == true ? "\"" : "");
                                                        ss += (ss == "" ? "" : "\r\n") + Q + "file:" + wpath + Q;
                                                    }
                                                }
                                                Invoke((MethodInvoker)delegate ()
                                                {
                                                    string rc = "ok";
                                                    if (path != "")
                                                    {
                                                        try
                                                        {
                                                            Clipboard.SetText(ss);
                                                            this.L_PopupMesg(ss, 600);
                                                        }
                                                        catch { }
                                                    }
                                                    else
                                                    {
                                                        this.timerChangeClipboard.Interval = 50;    //  即時、2回コピー処理に進む
                                                        Clipboard.SetText(ss);      //  WM_CLIPBOARDUPDATE
                                                    }
                                                    byte[] text = Encoding.UTF8.GetBytes(rc);
                                                    this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                                    response.OutputStream.Write(text, 0, text.Length);
                                                    this.textBox2.Text = string.Format("Listening,{0},return,{1}", request.RawUrl, rc)
                                                        + "\r\n" + this.textBox2.Text;
                                                });
                                            }
                                        }
                                        else
                                        if ((request.RawUrl.StartsWith("/open?") == true) || (request.RawUrl.StartsWith("/open2?") == true))
                                        {
                                            Invoke((MethodInvoker)delegate ()
                                            {
                                                this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                    + "\r\n" + this.textBox1.Text;
                                                this.textBox1.Tag = DateTime.Now;
                                            });
                                            var raw = rawurl.Split('?');
                                            var valurl = "";
                                            var valpath = "";
                                            if (request.RawUrl.StartsWith("/open?") == true)
                                            {
                                                //var vals = raw[1].Split('&');
                                                valurl = raw[1].Substring(0, raw[1].IndexOf('&'));   //vals[0];
                                                valpath = raw[1].Substring(valurl.Length + 1);       //vals[1];
                                            }
                                            else
                                            if (request.RawUrl.StartsWith("/open2?") == true)
                                            {
                                                valurl = raw[1];
                                            }
                                            var url = "";
                                            if (valurl != "")
                                            {
                                                url = valurl.Split('=')[1];
                                            }
                                            var path = "";
                                            if (valpath != "")
                                            {
                                                path = valpath.Split('=')[1];
                                            }
                                            if (url != "")
                                            {
                                                Invoke((MethodInvoker)delegate ()
                                                {
                                                    string rc = "ok";
                                                    if (path != "")
                                                    {
                                                        var wpath = (path.StartsWith("%") ? "" : this.BoxDriveRoot) + path;
                                                        this.L_Exec(wpath, false, true);
                                                    }
                                                    else
                                                    {
                                                        if (request.RawUrl.StartsWith("/open2?") == true)
                                                        {
                                                            this.L_Exec(url, true, false);
                                                        }
                                                        else
                                                        {
                                                            this.L_Exec(url);
                                                        }
                                                    }
                                                    byte[] text = Encoding.UTF8.GetBytes(rc);
                                                    this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                                    response.OutputStream.Write(text, 0, text.Length);
                                                    this.textBox2.Text = string.Format("Listening,{0},return,{1}", request.RawUrl, rc)
                                                        + "\r\n" + this.textBox2.Text;
                                                });
                                            }
                                        }
                                        else
                                        if (request.RawUrl.StartsWith("/firemousemove") == true)
                                        {
                                            var rc = "ok";
                                            int x = Cursor.Position.X;
                                            int y = Cursor.Position.Y;

                                            // マウス操作イベントの作成
                                            INPUT[] input = new INPUT[2];
                                            input[0].mi.dx = -1;
                                            input[0].mi.dy = +1;
                                            input[0].mi.dwFlags = NativeMethods.MOUSEEVENTF_MOVED;  //  相対移動

                                            input[1].mi.dx = +1;
                                            input[1].mi.dy = -1;
                                            input[1].mi.dwFlags = NativeMethods.MOUSEEVENTF_MOVED;  //  相対移動
                                            NativeMethods.SendInput(2, input, Marshal.SizeOf(input[0]));

                                            Cursor.Position = new Point(x, y);  //  元に戻す

                                            Invoke((MethodInvoker)delegate ()
                                            {
                                                this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                    + "\r\n" + this.textBox1.Text;
                                                this.textBox1.Tag = DateTime.Now;
                                            });
                                            byte[] text = Encoding.UTF8.GetBytes(rc);
                                            this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                            response.OutputStream.Write(text, 0, text.Length);
                                        }
                                        else
                                        if (request.RawUrl.StartsWith("/checkoption") == true)
                                        {
                                            Invoke((MethodInvoker)delegate ()
                                            {
                                                this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                    + "\r\n" + this.textBox1.Text;
                                                this.textBox1.Tag = DateTime.Now;
                                            });
                                            var rc = "ok";
                                            if (this.CB_USE_BOXLINK.Checked == true)
                                            {
                                                rc += "," + "makelink" + ",";
                                            }
                                            if (this.CB_CLOSE_SIDEBAR_FOLDER.Checked == true)
                                            {
                                                rc += "," + "closefoldersidebar" + ",";
                                            }
                                            if (this.CB_CLOSE_SIDEBAR_FILE.Checked == true)
                                            {
                                                rc += "," + "closefilesidebar" + ",";
                                            }
                                            byte[] text = Encoding.UTF8.GetBytes(rc);
                                            this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                            response.OutputStream.Write(text, 0, text.Length);
                                        }
                                        else
                                        if (request.RawUrl.StartsWith("/makelink?") == true)
                                        {
                                            Invoke((MethodInvoker)delegate ()
                                            {
                                                this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                    + "\r\n" + this.textBox1.Text;
                                                this.textBox1.Tag = DateTime.Now;
                                            });
                                            var raw = rawurl.Split('?');
                                            //var vals = raw[1].Split('&');
                                            var valid = raw[1].Substring(0, raw[1].IndexOf('&'));   //vals[0];
                                            var valurl = raw[1].Substring(valid.Length + 1, raw[1].IndexOf('&', valid.Length + 1) - (valid.Length + 1));    //vals[1];
                                            var valpath = raw[1].Substring(raw[1].IndexOf('&', valid.Length + 1) + 1);  //vals[2];
                                            var id = "";
                                            if (valid != "")
                                            {
                                                id = valid.Split('=')[1];
                                            }
                                            var url = "";
                                            if (valurl != "")
                                            {
                                                url = valurl.Split('=')[1];
                                            }
                                            if (id != "")
                                            {
                                                if (url != "")
                                                {
                                                    var rc = "ok";
                                                    var path = valpath.Split('=')[1];
                                                    var wpath = this.L_GetBoxDriveFolder(this.L_GetBoxDrivePath(id));
                                                    if (wpath == "")
                                                    {
                                                        wpath = (path.StartsWith("%") ? "" : this.BoxDriveRoot) + path;
                                                    }
                                                    wpath = wpath.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"));
                                                    var n = 1;
                                                    while (true)
                                                    {
                                                        var S = wpath + " - ショートカット" + (n == 1 ? "" : string.Format(" ({0})", n)) + @".boxlnk";
                                                        if (new FileInfo(S).Exists == false)
                                                        {
                                                            try
                                                            {
                                                                using (StreamWriter SW = new StreamWriter(S, false, Encoding.UTF8))
                                                                {
                                                                    SW.WriteLine(url + "\n" + path);
                                                                }
                                                                this.LOG.PutLog("listener", "Info", string.Format("{0}={1}", "ショートカット作成", S), 2);
                                                                Invoke((MethodInvoker)delegate ()
                                                                {
                                                                    this.L_Exec(S, false, true);
                                                                });
                                                                rc += "," + S;
                                                                break;
                                                            }
                                                            catch
                                                            {
                                                                //  Box直下に作成
                                                                wpath = this.BoxDriveRoot + @"\" + (new FileInfo(path)).Name;
                                                                S = wpath + " - ショートカット" + (n == 1 ? "" : string.Format(" ({0})", n)) + @".boxlnk";
                                                                using (StreamWriter SW = new StreamWriter(S, false, Encoding.UTF8))
                                                                {
                                                                    SW.WriteLine(url + "," + path);
                                                                }
                                                                this.LOG.PutLog("listener", "Info", string.Format("{0}={1}", "ショートカット作成", S), 1);
                                                                Invoke((MethodInvoker)delegate ()
                                                                {
                                                                    this.L_Exec(S, false, true);
                                                                });
                                                                rc += "," + S;
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            n++;
                                                        }
                                                    }
                                                    byte[] text = Encoding.UTF8.GetBytes(rc);
                                                    this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                                    response.OutputStream.Write(text, 0, text.Length);
                                                    Invoke((MethodInvoker)delegate ()
                                                    {
                                                        this.textBox1.Text = string.Format("Listening,{0},return,{1}", request.RawUrl, rc)
                                                            + "\r\n" + this.textBox1.Text;
                                                    });
                                                }
                                            }
                                        }
                                        else
                                        if ((request.RawUrl.StartsWith("/timestamp?") == true)
                                        || (request.RawUrl.StartsWith("/data?") == true))
                                        {
                                            Invoke((MethodInvoker)delegate ()
                                            {
                                                this.textBox1.Text = string.Format("Listening,{0}", request.RawUrl)
                                                    + "\r\n" + this.textBox1.Text;
                                                this.textBox1.Tag = DateTime.Now;
                                            });
                                            var raw = rawurl.Split('?');
                                            var valpath = raw[1];
                                            var path = "";
                                            if (valpath != "")
                                            {
                                                path = valpath.Split('=')[1];
                                            }
                                            var rc = "ng";
                                            if (path != "")
                                            {
                                                var wpath = (path.StartsWith("%") ? "" : this.BoxDriveRoot) + path;
                                                wpath = this.L_GetBoxDriveFolder(wpath);
                                                wpath = wpath.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"));
                                                try
                                                {
                                                    if (new DirectoryInfo(wpath).Exists == true)
                                                    {
                                                        rc = string.Format("{0},{1}", "ok", (new DirectoryInfo(wpath)).LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss"));
                                                    }
                                                    else
                                                    if (new FileInfo(wpath).Exists == true)
                                                    {
                                                        if (request.RawUrl.StartsWith("/timestamp?") == true)
                                                        {
                                                            //  timestamp
                                                            rc = string.Format("{0},{1}", "ok", (new FileInfo(wpath)).LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss"));
                                                        }
                                                        else
                                                        {
                                                            //  data
                                                            var S = "";
                                                            StreamReader SR = new StreamReader(wpath, Encoding.Default);
                                                            if (SR.Peek() != -1) //  EOFでない
                                                            {
                                                                S = SR.ReadToEnd();
                                                            }
                                                            SR.Close();
                                                            rc = string.Format("{0},{1}", "ok", S);
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                            byte[] text = Encoding.UTF8.GetBytes(rc);
                                            this.LOG.PutLog("listener", "Debug", "request.Url=" + rawurl + ", response=" + rc, 3);
                                            response.OutputStream.Write(text, 0, text.Length);
                                        }
                                    }
                                }
                                else
                                {
                                    this.LOG.PutLog("listener", "Error", string.Format("{0}", "404"), 1);
                                    response.StatusCode = 404;
                                }
                                response.Close();
                            }
                            catch
                            {
                                System.Threading.Thread.Sleep(1000);    //  待機
                            }
                        }
                    }
                });
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                //  引数なしのとき
                this.PopupTimer1.Tick -= new EventHandler(this.PopupTimer1_Tick);
                this.DrawClipboard -= new MyClipboardTextEventHandler(this.Form1_DrawClipboard);
            }
            if (this.listener != null)
            {
                if (this.listener.IsListening == true)
                {
                    this.listener.Stop();
                }
            }
            try
            {
                this.litedb.Checkpoint();
            }
            catch { }
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                //  引数なしのとき
                this.LOG.PutLog1("Info", string.Format("{0}", "終了"));
            }
            else
            {
                this.LOG.PutLog3("Info", string.Format("{0}", "終了"));
            }
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                //  引数なし（常駐）のとき
                this.Visible = true;
                this.WindowState = FormWindowState.Minimized;
                this.PopupTimer1.Tick += new EventHandler(this.PopupTimer1_Tick);
                this.DrawClipboard += new MyClipboardTextEventHandler(this.Form1_DrawClipboard);
            }
            else
            {
                //  引数ありのとき
                this.Visible = false;
                try
                {
                    this.LOG.PutLog1("Info", string.Format("{0},Args={1}", "execopen", Environment.GetCommandLineArgs().Length - 1));
                    for (var i = 1; i < Environment.GetCommandLineArgs().Length; i++)
                    {
                        var arg = Environment.GetCommandLineArgs()[i];
                        if ((arg != "") && (arg != @""""""))
                        {
                            this.textBox1.Text = "arg=" + arg
                                + "\r\n" + this.textBox1.Text;
                            this.LOG.PutLog2("Info", string.Format("{0}={1}", i, arg));
                            using (var cli = new HttpClient())
                            {
                                try
                                {
                                    for (var j = 0; j < 4; j++)
                                    {
                                        var url = string.Format(@"http://localhost:{0}/open?url={1}&path={2}", this.HttpPort + j * 20, HttpUtility.UrlEncode(arg), "");
                                        this.LOG.PutLog3("Info", string.Format("{0}={1}", "httpget", url));
                                        var res = await cli.GetAsync(url);
                                    }
                                }
                                catch { }
                            }
                            //this.L_Exec(arg);
                        }
                    }
                }
                finally
                {
#if DEBUG
                    Debug.Assert(false);
                    this.Visible = true;
#else
                    this.Close();
#endif
                }
            }
        }

        private int countClipboard = 0;
        private string dataClipboard = "";

        private void timerChangeClipboard_Tick(object sender, EventArgs e)
        {
            try
            {
                var clipText = this.dataClipboard;
                if ((this.countClipboard == 2) || (this.countClipboard == 3) || (this.countClipboard == 5))
                {
                    var flgUrlOnly = false;
                    var dataUrlValue = "";
                    var flgPathOnly = false;
                    var dataPathValue = "";
                    var re = new Regex("^" + boxUrlPattern + "$");    //  box URLのみであれば
                    if (re.Match(clipText).Length != 0)
                    {
                        //  box URLのみのとき、box Driveパスを取得
                        flgUrlOnly = true;
                        dataUrlValue = clipText;
                        var W_ID = re.Replace(dataUrlValue, "$2");
                        dataPathValue = this.L_GetBoxDriveFolder(this.L_GetBoxDrivePath(W_ID));
                        if (dataPathValue == "")
                        {
                            Process Proc = new Process();
                            var exe = this.exeBrowser;
                            var para = this.paraBrowser;
                            {
                                dataUrlValue += (dataUrlValue.Contains("?") == false ? "?" : "&") + "_doBoxDriveOpen_=2";   //  2=copyclipboard
                            }
                            Proc.StartInfo.FileName = exe.Replace("%1", dataUrlValue);
                            Proc.StartInfo.Arguments = para.Replace("%1", dataUrlValue);
                            Proc.StartInfo.UseShellExecute = true;
                            Proc.StartInfo.Verb = "";
                            Proc.Start();
                            return;     //  exit
                        }
                    }
                    else
                    {
                        var path = clipText.Replace(Environment.GetEnvironmentVariable("USERPROFILE"), "%USERPROFILE%");
                        if (((path.StartsWith("\"") == true) && (path.EndsWith("\"") == true))
                        || ((path.StartsWith("'") == true) && (path.EndsWith("'") == true)))
                        {
                            path = path.Substring(1, path.Length - 2);
                        }
                        path = path.Replace("file:", "");
                        var relocalboxfolder = this.BoxDriveRoot.Replace(".", "[.]").Replace("\\", "/").Replace("/", "[\\\\/]");     //  box Driveフォルダ
                        re = new Regex("^" + relocalboxfolder + ".*$");    //  box Driveパスのみであれば
                        if (re.Match(path).Length != 0)
                        {
                            //  box Driveパスのみのとき、box URLを取得
                            flgPathOnly = true;
                            dataPathValue = path;
                            path = path.Replace("\\", "/").Replace("file:", "").Trim();
                            dataUrlValue = this.L_GetBoxDriveID(path);
                        }
                    }
                    if (this.countClipboard == 5)
                    {
                        //  5回連続コピー
                        if (dataUrlValue != "")
                        {
                            //  →ブラウザで実行
                            this.L_Exec(dataUrlValue, false, false);
                        }
                        else
                        if (dataPathValue != "")
                        {
                            //  →実行
                            this.L_Exec(dataPathValue);
                        }
                        else
                        {
                            //  →実行
                            this.L_Exec(clipText);
                        }
                    }
                    else
                    {
                        var ss = "";
                        if (this.countClipboard == 2)
                        {
                            //  両方
                            if (dataUrlValue != "")
                            {
                                ss = dataUrlValue;
                            }
                            if (dataPathValue != "")
                            {
                                var Q = ""; //(dataPathValue.Contains(" ") == true ? "\"" : "");
                                ss += (ss == "" ? "" : "\r\n") + Q + "file:" + dataPathValue + Q;
                            }
                        }
                        else
                        {
                            //  反対
                            if (flgPathOnly == true)
                            {
                                ss = dataUrlValue;
                            }
                            else
                            if (flgUrlOnly == true)
                            {
                                var Q = ""; //(dataPathValue.Contains(" ") == true ? "\"" : "");
                                ss += Q + dataPathValue + Q;    //  file:は付けない
                            }
                        }
                        if ((ss != "") && (clipText != ss))
                        {
                            clipText = ss;
                            this.textBox2.Text = "Clipboard->" + clipText.Split(new char[] { '\r', '\n' })[0] + "," + this.timerChangeClipboard.Enabled.ToString()
                                    + "\r\n" + this.textBox2.Text;
                            try
                            {
                                Clipboard.SetText(clipText);
                                this.L_PopupMesg(clipText, 600);
                                return;     //  exit
                            }
                            catch { }
                        }
                    }
                }
                else
                if (this.countClipboard == 4)
                {
                    //  4回連続コピー→クリップボードの値で実行
                    this.textBox2.Text = "exec=" + this.dataClipboard
                            + "\r\n" + this.textBox2.Text;
                    this.L_Exec(this.dataClipboard);
                }
            }
            finally
            {
                this.timerChangeClipboard.Stop();
                this.timerChangeClipboard.Enabled = false;
                this.timerChangeClipboard.Interval = this.C_ClipboardIntegval;
            }
        }

        /// <summary>
        ///  クリップボード監視。AddClipboardFormatListenerに登録することで、クリップボード変化時に呼び出される
        ///  1回目変化=何もしない。コピーまたは切り取り時
        ///  2回目連続コピー時=box URLのみまたはbox Driveパスのみコピー時、URL＋パス並記へ変換
        ///  3回目連続コピー時=box URLのみまたはbox Driveパスのみコピー時、パスまたはURLへ変換
        ///  4回目連続コピー時=コピー値からURLまたはパスを抽出し、開く。boxに限らない
        ///  前のコピーから400ms以内でカウントUp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private int C_ClipboardIntegval = 400;
        private void Form1_DrawClipboard(object sender, MyClipboardEventArg e)
        {
            try
            {
                if ((e.ContainsText == true) || (e.ContainsFileDropList == true))
                {
                    {
                        var clipText = e.ClipboardText;
                        {
                            this.textBox2.Text = "clipText=" + clipText.Split(new char[] { '\r', '\n' })[0] + ", " + DateTime.Now.Millisecond.ToString("000")
                                + "\r\n" + this.textBox2.Text;
                        }
                        if ((clipText.Contains("https://") == true) || (clipText.Contains("http://") == true))
                        {
                            //  URLを含むとき
                        }
                        else
                        {
                            var wpath = clipText.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"));
                            var re = new Regex(@"([a-zA-Z]:\\|\\\\.*?\\)");
                            if (re.Match(wpath).Length != 0)
                            {
                                //  パス名を含むとき
                            }
                            else
                            {
                                return; //  skip, exit
                            }
                        }

                        if (this.timerChangeClipboard.Interval == 50)
                        {
                            //  copyclipboardからセット時、強制的に2回目になるようにする
                            this.dataClipboard = clipText;
                            this.countClipboard = 1;
                            this.timerChangeClipboard.Enabled = true;
                            this.timerChangeClipboard.Start();
                        }
                        if (this.timerChangeClipboard.Enabled == false)
                        {
                            this.dataClipboard = clipText;
                            this.countClipboard = 1;
                            this.timerChangeClipboard.Enabled = true;
                            this.timerChangeClipboard.Start();
                        }
                        else
                        {
                            if (this.dataClipboard != clipText)
                            {
                                this.timerChangeClipboard.Stop();
                                this.timerChangeClipboard.Enabled = false;
                                this.timerChangeClipboard.Interval = this.C_ClipboardIntegval;
                                {
                                    this.textBox2.Text = "Clipboard--" + this.timerChangeClipboard.Enabled.ToString() + ", " + DateTime.Now.Millisecond.ToString("000")
                                        + "\r\n" + this.textBox2.Text;
                                }
                            }
                            else
                            {
                                this.countClipboard++;
                                {
                                    this.textBox2.Text = "Clipboard++" + this.dataClipboard.Split(new char[] { '\r', '\n' })[0] + ", " + this.countClipboard.ToString() + ", " + DateTime.Now.Millisecond.ToString("000")
                                            + "\r\n" + this.textBox2.Text;
                                }
                                this.timerChangeClipboard.Stop();
                                this.timerChangeClipboard.Enabled = false;
                                this.timerChangeClipboard.Enabled = true;
                                this.timerChangeClipboard.Start();
                            }
                        }
                    }
                }
                else
                {
                    this.timerChangeClipboard.Stop();
                    this.timerChangeClipboard.Enabled = false;
                    this.timerChangeClipboard.Interval = this.C_ClipboardIntegval;
                }
            }
            catch { }
        }
        private string L_GetBoxDriveFolder(string P_Path)
        {
            var re = new Regex("^.*%([0-9]*)%[/].*$");   //  %～%/記述はフォルダID
            if (re.Match(P_Path).Length != 0)
            {
                var folderid = re.Replace(P_Path, "$1");
                var folderpath = this.L_GetBoxDrivePath(folderid);
                if (folderpath != "")
                {
                    P_Path = P_Path.Replace("%" + folderid + "%", folderpath);
                }
            }
            return P_Path;
        }
        private string L_GetBoxDrivePath(string P_ID)
        {
            var rec = this.BoxDrivePath.FindOne("$.Id = '" + P_ID + "'");
            if (rec != null)
            {
                var path = ((rec["Path"].AsString.StartsWith("/") == true ? this.BoxDriveRoot : "") + rec["Path"].AsString).Replace(Environment.GetEnvironmentVariable("USERPROFILE"), "%USERPROFILE%");
                var wpath = this.L_GetBoxDriveFolder(path);
                wpath = wpath.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"));
                if ((File.Exists(wpath) == true) || (Directory.Exists(wpath) == true))
                {
                    //  存在するとき
                    return path;
                }
                else
                {
                    //  存在しない（しなくなった）とき
                    //this.BoxDrivePath.Delete(rec["_id"]);   //  暗黙のid
                }
            }

            return "";
        }

        private string L_GetBoxDriveID(string P_Path)
        {
            BsonDocument rec = null;
            foreach (var rr in this.BoxDrivePath.FindAll())
            {
                if (this.L_GetBoxDriveFolder((rr["Path"].AsString.StartsWith("/") == true ? this.BoxDriveRoot : "") + rr["Path"].AsString) == P_Path)
                {
                    rec = rr;
                    break;
                }
            }
            if (rec != null)
            {
                var path = P_Path.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"));
                if (File.Exists(path) == true)
                {
                    //  ファイルとして存在するとき
                    return this.BoxURL + "/file/" + rec["Id"].AsString;
                }
                else
                if (Directory.Exists(path) == true)
                {
                    //  フォルダとして存在するとき
                    return this.BoxURL + "/folder/" + rec["Id"].AsString;
                }
            }

            return "";
        }

        private void L_CleanBoxDrive()
        {
            foreach (var rr in this.BoxDrivePath.FindAll())
            {
                var path = ((rr["Path"].AsString.StartsWith("/") == true ? this.BoxDriveRoot : "") + rr["Path"].AsString).Replace(Environment.GetEnvironmentVariable("USERPROFILE"), "%USERPROFILE%");
                var wpath = this.L_GetBoxDriveFolder(path);
                wpath = wpath.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"));
                if ((File.Exists(wpath) == true) || (Directory.Exists(wpath) == true))
                {
                }
                else
                {
                    //  存在しない（しなくなった）とき
                    this.BoxDrivePath.Delete(rr["_id"]);   //  暗黙のid
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if ((this.textBox1.Text.Contains("Listenerを開始できませんでした。起動しなおしてください") == true)
             || (this.textBox1.Text.Contains("boxドライブにアクセスできないようです") == true))
            {
                this.WindowState = FormWindowState.Normal;
                //this.Close();
                return;
            }

            if (NativeMethods.GetForegroundWindow() != this.Handle)
            {
                if (this.WindowState != FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Minimized;
                }
            }

            this.LOG.Enabled = this.INI.Read("Debug", "Log", this.LOG.Enabled);
            this.LOG.OutputLevel = this.INI.Read("Debug", "LogLevel", this.LOG.OutputLevel);
            var S = this.INI.Read("File", "Exe", this.exeFile);
            if (S != this.exeFile)
            {
                this.exeFile = S;
                this.LOG.PutLog1("Info", "exeFile=" + this.exeFile);
            }
            S = this.INI.Read("File", "Param", this.paraFile);
            if (S != this.paraFile)
            {
                this.paraFile = S;
                this.LOG.PutLog1("Info", "paraFile=" + this.paraFile);
            }
            S = this.INI.Read("Folder", "Exe", this.exeFolder);
            if (S != this.exeFolder)
            {
                this.exeFolder = S;
                this.LOG.PutLog1("Info", "exeFolder=" + this.exeFolder);
            }
            S = this.INI.Read("Folder", "Param", this.paraFolder);
            if (S != this.paraFolder)
            {
                this.paraFolder = S;
                this.LOG.PutLog1("Info", "paraFolder=" + this.paraFolder);
            }
            S = this.INI.Read("Browser", "Exe", this.exeBrowser);
            if (S != this.exeBrowser)
            {
                this.exeBrowser = S;
                this.LOG.PutLog1("Info", "exeBrowser=" + this.exeBrowser);
            }
            S = this.INI.Read("Browser", "Param", this.paraBrowser);
            if (S != this.paraBrowser)
            {
                this.paraBrowser = S;
                this.LOG.PutLog1("Info", "paraBrowser=" + this.paraBrowser);
            }
            S = this.INI.Read("Box", "BoxLink", this.CB_USE_BOXLINK.Checked).ToString();
            if (S != this.CB_USE_BOXLINK.Checked.ToString())
            {
                this.CB_USE_BOXLINK.Checked = bool.Parse(S);
                this.LOG.PutLog1("Info", "BoxLink=" + this.CB_USE_BOXLINK.Checked);
            }
            S = this.INI.Read("Box", "FolderSidebarClose", this.CB_CLOSE_SIDEBAR_FOLDER.Checked).ToString();
            if (S != this.CB_CLOSE_SIDEBAR_FOLDER.Checked.ToString())
            {
                this.CB_CLOSE_SIDEBAR_FOLDER.Checked = bool.Parse(S);
                this.LOG.PutLog1("Info", "FolderSidebarClose=" + this.CB_CLOSE_SIDEBAR_FOLDER.Checked);
            }
            S = this.INI.Read("Box", "FileSidebarClose", this.CB_CLOSE_SIDEBAR_FILE.Checked).ToString();
            if (S != this.CB_CLOSE_SIDEBAR_FILE.Checked.ToString())
            {
                this.CB_CLOSE_SIDEBAR_FILE.Checked = bool.Parse(S);
                this.LOG.PutLog1("Info", "FileSidebarClose=" + this.CB_CLOSE_SIDEBAR_FILE.Checked);
            }

            try
            {
                this.textBox1.Text = this.textBox1.Text.Substring(0, 4096);
            }
            catch { }
            try
            {
                this.textBox2.Text = this.textBox2.Text.Substring(0, 4096);
            }
            catch { }
        }

        private void CB_USE_BOXLINK_CheckedChanged(object sender, EventArgs e)
        {
            this.INI.Write("Box", "BoxLink", this.CB_USE_BOXLINK.Checked);
        }

        private void CB_CLOSE_SIDEBAR_FOLDER_CheckedChanged(object sender, EventArgs e)
        {
            this.INI.Write("Box", "FolderSidebarClose", this.CB_CLOSE_SIDEBAR_FOLDER.Checked);
        }

        private void CB_CLOSE_SIDEBAR_FILE_CheckedChanged(object sender, EventArgs e)
        {
            this.INI.Write("Box", "FileSidebarClose", this.CB_CLOSE_SIDEBAR_FILE.Checked);
        }
    }
}
    