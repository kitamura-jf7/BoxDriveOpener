#define xCPUCHECK
using BoxDriveOpener;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MYLOG
{
    public class ClassLog
    {
#if !NOWAIT
        private int V_SLEEPWAIT = 20;
#endif
        private BlockingCollection<LogData> QUE = new BlockingCollection<LogData>();
        private bool FEnabled = false;
        private string FFoldername;
        private string FFilename;
        private string FTitle = "日付,時刻,端末,ユーザ,処理,イベント,";
        private string FSubTitle = "詳細";
        private string FClient = "(none)";
        private string FUser = "(none)";
        private string FProcName = "(none)";
        private bool FHiddenFile = true;
        private bool FSystemFile = true;
        private bool FLogCleaning_Flg;
        private bool FMakeHIBETU_Flg;
        private bool FMakeJIKANBETU_Flg;
        private bool FUseTitle_Flg;
        private bool FOutputMiliSec_Flg;
        private string FErrorMsg = "";

        private bool DeletedFlg = false;    //  過去ログの削除は最初の1回とするためのフラグ
        private int FLogCleaningSpan = 2;  //  過去ログの保持月数
        public int OutputLevel = 0;         //  出力／抑止制御レベル
#if CPUCHECK
        private System.Diagnostics.PerformanceCounter cpuCounter = null;
        private float cpuCounter_LastValue = -1;
#endif

        /// <summary>
        /// 有効かどうか。有効時のみログ出力する
        /// </summary>
        public bool Enabled
        {
            get
            {
                return this.FEnabled;
            }
            set
            {
                if (this.FEnabled != value)
                {
                    this.FEnabled = value;
                    System.Threading.Thread.Sleep(100);  //  待機
                    if (this.FEnabled == true)
                    {
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            this.PutLogThread();
                        });
                    }
                }
            }
        }

        /// <summary>
        /// タイトル使用フラグ
        /// </summary>
        public bool UseTitle_Flg
        {
            get
            {
                return this.FUseTitle_Flg;
            }
            set
            {
                this.FUseTitle_Flg = value;
            }
        }

        /// <summary>
        /// 詳細タイトル
        /// </summary>
        public string SubTitle
        {
            get
            {
                return this.FSubTitle;
            }
            set
            {
                this.FSubTitle = value;
            }
        }

        /// <summary>
        /// タイトル
        /// </summary>
        public string Title
        {
            get
            {
                return (this.FUseTitle_Flg == true ? this.FTitle : "") + this.FSubTitle;
            }
        }

        /// <summary>
        /// 端末名
        /// </summary>
        public string Client
        {
            get
            {
                return this.FClient;
            }
            set
            {
                this.FClient = value;
            }
        }

        /// <summary>
        /// ユーザ名
        /// </summary>
        public string User
        {
            get
            {
                return this.FUser;
            }
            set
            {
                this.FUser = value;
            }
        }

        /// <summary>
        /// ログフォルダ
        /// </summary>
        public string Foldername
        {
            get
            {
                return this.FFoldername;
            }
            set
            {
                if (value == "")
                {
                    this.FFoldername = "";
                }
                else
                {
                    this.FFoldername = value + @".\";
                }
            }
        }

        /// <summary>
        /// ログファイル名
        /// </summary>
        public string Filename
        {
            get
            {
                return this.FFilename;
            }
            set
            {
                if (value.IndexOf(@"\") != -1)
                {
                    throw new ApplicationException("Filenameにパス名を含めることはできません[" + value + "]");
                }
                this.FFilename = value;
            }
        }

        /// <summary>
        /// ログファイル非表示属性
        /// </summary>
        public bool HiddenFile
        {
            get
            {
                return this.FHiddenFile;
            }
            set
            {
                this.FHiddenFile = value;
            }
        }

        /// <summary>
        /// ログファイルシステム属性
        /// </summary>
        public bool SystemFile
        {
            get
            {
                return this.FSystemFile;
            }
            set
            {
                this.FSystemFile = value;
            }
        }

        /// <summary>
        /// 処理名
        /// </summary>
        public string ProcName
        {
            get
            {
                return this.FProcName;
            }
            set
            {
                this.FProcName = value;
            }
        }

        /// <summary>
        /// クリーニングフラグ
        /// true時はLogCleaningSpan月経過したログファイルを削除する
        /// false時は過去ログのクリア機能は無し
        /// </summary>
        public bool LogCleaning_Flg
        {
            get
            {
                return this.LogCleaningSpan > 0;
            }
        }

        /// <summary>
        /// 日別出力フラグ
        /// true時はログファイル名を日別に生成する。１年以上前の日付ファイルは自動的に削除する
        /// false時は追記出力のみ。過去ログのクリア機能は無し
        /// </summary>
        public bool MakeHIBETU_Flg
        {
            get
            {
                return this.FMakeHIBETU_Flg;
            }
            set
            {
                this.FMakeHIBETU_Flg = value;
            }
        }

        /// <summary>
        /// 時間別出力フラグ
        /// true時はログファイル名を時間別に生成する
        /// </summary>
        public bool MakeJIKANBETU_Flg
        {
            get
            {
                return this.FMakeJIKANBETU_Flg;
            }
            set
            {
                this.FMakeJIKANBETU_Flg = value;
            }
        }

        /// <summary>
        /// ミリ秒出力フラグ
        /// </summary>
        public bool OutputMiliSec_Flg
        {
            get
            {
                return this.FOutputMiliSec_Flg;
            }
            set
            {
                this.FOutputMiliSec_Flg = value;
            }
        }

        /// <summary>
        /// クリーニング期間（保持月数）
        /// 1以上は指定の月数を経過したログファイルを削除する
        /// 0は過去ログのクリア機能は無し
        /// </summary>
        public int LogCleaningSpan
        {
            get
            {
                return (this.FLogCleaningSpan < 0 ? 0 : this.FLogCleaningSpan);
            }
            set
            {
                this.FLogCleaningSpan = value;
            }
        }

        /// <summary>
        /// エラーメッセージ
        /// ログ出力失敗時／エラー時にセットされる
        /// </summary>
        public string ErrorMsg
        {
            get
            {
                return this.FErrorMsg;
            }
        }
        public void ResetErrorMsg()
        {
            this.FErrorMsg = "";
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="LogFoldername">ログフォルダ</param>
        /// <param name="LogFilename">ログファイル名</param>
        public void Create(string LogFoldername, string LogFilename)
        {
            this.Client = Environment.MachineName;
            this.User = Environment.UserName;
            if (LogFoldername == null)
            {
                this.Foldername = this.ExeName().DirectoryName;
            }
            else
            {
                this.Foldername = LogFoldername;
            }
            if (LogFilename == null)
            {
                this.Filename = this.ExeName().Name.Replace(this.ExeName().Extension, ".log");
            }
            else
            {
                this.Filename = LogFilename;
            }
            this.ProcName = Application.ProductName;
            this.UseTitle_Flg = true;
        }

        public ClassLog(string LogFoldername, string LogFilename)
        {
            this.Create(LogFoldername, LogFilename);
        }
        public ClassLog(string LogFilename)
        {
            this.Create(null, LogFilename);
        }
        public ClassLog()
        {
            this.Create(null, null);
        }

        /// <summary>
        /// exeファイル名を返す
        /// </summary>
        private FileInfo ExeName()
        {
            return new FileInfo(Application.ExecutablePath);
        }

        public Converter<string, string> Encrypt = null;
        private string WriteLine(string text)
        {
            if (this.Encrypt == null)
            {
                return text;
            }
            else
            {
                return this.Encrypt(text);
            }
        }

        /// <summary>
        /// ログファイル生成
        /// </summary>
        /// <returns></returns>
        private void MkLogFile(string PFileName, string PTitle, int POutputLevel)
        {
            string WK_LOGFILENAME = PFileName;
            FileInfo fi;
            fi = new FileInfo(WK_LOGFILENAME);

            if (Directory.Exists(fi.DirectoryName) != true)
            {
                //  フォルダを作成
                Directory.CreateDirectory(fi.DirectoryName);
                if (POutputLevel == 0)
                {
                    fi.Directory.Attributes = fi.Directory.Attributes | FileAttributes.Hidden;
                }
                else
                {
                    fi.Directory.Attributes = fi.Directory.Attributes | FileAttributes.Hidden | FileAttributes.System;
                }
            }

            if (fi.Exists == false)
            {
                //  LOGファイルが存在しない時、タイトル行出力
                StreamWriter SW = new StreamWriter(fi.FullName, false, Encoding.UTF8);
                try
                {
                    try
                    {
                        var S = PTitle;
                        if (S != "")
                        {
                            SW.WriteLine(this.WriteLine(S));
                            SW.Flush();
                        }
                    }
                    catch { }   //  エラー時は無視（？）
                }
                finally
                {
                    SW.Close();
                    fi.Refresh();
                    if (POutputLevel == 0)
                    {
                        fi.Attributes = fi.Attributes | (this.FHiddenFile ? FileAttributes.Hidden : 0);
                    }
                    else
                    {
                        fi.Attributes = fi.Attributes | (this.FHiddenFile ? FileAttributes.Hidden : 0) | (this.FSystemFile ? FileAttributes.System : 0);
                    }
                }
            }
        }

        /// <summary>
        /// ログファイル名生成
        /// </summary>
        /// <returns></returns>
        private string MkLogFileName(DateTime PNow, bool? NeedCreate)
        {
            string WK_LOGFILENAME = "";
            FileInfo fi;
            if ((this.Foldername + this.FFilename) == "")
            {
                //  Filename未指定の時は、EXEと同じフォルダのlogとする
                WK_LOGFILENAME = this.ExeName().FullName.Replace(this.ExeName().Extension, ".log");
            }
            else
            {
                if (this.Foldername == "")
                {
                    //  Foldername未指定の時は、EXEと同じフォルダとする
                    WK_LOGFILENAME = this.ExeName().DirectoryName + @".\" + this.FFilename;
                }
                else
                {
                    WK_LOGFILENAME = this.Foldername + @".\";
                    if (this.FFilename == "")
                    {
                        //  Filename未指定の時は、EXEと同じ名前のlogとする
                        WK_LOGFILENAME += this.ExeName().Name.Replace(this.ExeName().Extension, ".log");
                    }
                    else
                    {
                        WK_LOGFILENAME += this.FFilename;
                    }
                }
            }

            int n = WK_LOGFILENAME.LastIndexOf(".");    //  拡張子の位置

            if (NeedCreate == true)
            {
                if (this.LogCleaning_Flg == true && this.DeletedFlg == false && this.FMakeHIBETU_Flg == true)
                {
                    //  LogCleaningSpan月以上前のログファイルを削除
                    for (int j = 1; j <= 12; j++)
                    {
                        for (int i = 1; i <= 31; i++)
                        {
                            string WK_FILENAME;
                            if (n == 0)
                            {
                                WK_FILENAME = WK_LOGFILENAME + j.ToString("00") + i.ToString("00");
                            }
                            else
                            {
                                WK_FILENAME = WK_LOGFILENAME.Substring(0, n) + j.ToString("00") + i.ToString("00") + WK_LOGFILENAME.Substring(n);
                            }
                            fi = new FileInfo(WK_FILENAME);
                            if (fi.Exists == true)
                            {
                                if (fi.LastWriteTime.AddMonths(this.LogCleaningSpan).CompareTo(PNow.Date.Add(TimeSpan.FromDays(1))) < 0)
                                {
                                    try
                                    {
                                        File.Delete(fi.FullName);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    this.DeletedFlg = true;
                }
            }

            if (this.FMakeHIBETU_Flg == true)
            {
                if (n == 0)
                {
                    WK_LOGFILENAME = WK_LOGFILENAME + PNow.ToString("yyyyMMdd") + ((this.FMakeJIKANBETU_Flg != true) ? "" : "_" + PNow.AddSeconds(0 - (PNow.Second % 10)).ToString("HH"));  //  "HHmmss"
                }
                else
                {
                    WK_LOGFILENAME = WK_LOGFILENAME.Substring(0, n) + PNow.ToString("yyyyMMdd") + ((this.FMakeJIKANBETU_Flg != true) ? "" : "_" + PNow.AddSeconds(0 - (PNow.Second % 10)).ToString("HH")) + WK_LOGFILENAME.Substring(n);    //  "HHmmss"
                }
            }

            if (NeedCreate != false)    //  true or null
            {
                this.MkLogFile(WK_LOGFILENAME, this.Title, this.OutputLevel);
            }
            return WK_LOGFILENAME;
        }

        /// <summary>
        /// 空ログファイル生成
        /// </summary>
        /// <param name="LastNow">最終ログ作成日時</param>
        /// <param name="PNow">最新ログ作成日時</param>
        /// <returns></returns>
        public void CreateBlankLog(DateTime PNow)
        {
            this.MkLogFileName(PNow, null);
        }

        /// <summary>
        /// ログ出力
        /// </summary>
        /// <param name="PEvent">イベント内容</param>
        /// <param name="PMessage">詳細内容</param>
        /// <returns></returns>
        public bool PutLog1(string PEvent, string PMessage)
        {
            return this.PutLog(PEvent, PMessage, 1);
        }
        public bool PutLog2(string PEvent, string PMessage)
        {
            return this.PutLog(PEvent, PMessage, 2);
        }
        public bool PutLog3(string PEvent, string PMessage)
        {
            return this.PutLog(PEvent, PMessage, 3);
        }
        public bool PutLog4(string PEvent, string PMessage)
        {
            return this.PutLog(PEvent, PMessage, 4);
        }
        public bool PutLog1(string PEvent, string PMessage, DateTime PNow)
        {
            return this.PutLog(PEvent, PMessage, PNow, 1);
        }
        public bool PutLog2(string PEvent, string PMessage, DateTime PNow)
        {
            return this.PutLog(PEvent, PMessage, PNow, 2);
        }
        public bool PutLog3(string PEvent, string PMessage, DateTime PNow)
        {
            return this.PutLog(PEvent, PMessage, PNow, 3);
        }
        //
        public bool PutFomattedLog(string PMessage)
        {
            return this.PutLog("", "", PMessage, DateTime.Now, true, 0);
        }
        public bool PutFomattedLog(string PMessage, DateTime PNow)
        {
            return this.PutLog("", "", PMessage, PNow, true, 0);
        }
        public bool PutLog(string PEvent, string PMessage)
        {
            return this.PutLog(this.FProcName, PEvent, PMessage, 0);
        }
        public bool PutLog(string PProcName, string PEvent, string PMessage)
        {
            return this.PutLog(PProcName, PEvent, PMessage, 0);
        }
        public bool PutLog(string PEvent, string PMessage, int POutputLevel)
        {
            return this.PutLog(this.FProcName, PEvent, PMessage, DateTime.Now, false, POutputLevel);
        }
        public bool PutLog(string PProcName, string PEvent, string PMessage, int POutputLevel)
        {
            return this.PutLog(PProcName, PEvent, PMessage, DateTime.Now, false, POutputLevel);
        }
        public bool PutLog(string PEvent, string PMessage, DateTime PNow)
        {
            return this.PutLog(this.FProcName, PEvent, PMessage, PNow);
        }
        public bool PutLog(string PProcName, string PEvent, string PMessage, DateTime PNow)
        {
            return this.PutLog(PProcName, PEvent, PMessage, PNow, false, 0);
        }
        public bool PutLog(string PEvent, string PMessage, DateTime PNow, int POutputLevel)
        {
            return this.PutLog(this.FProcName, PEvent, PMessage, PNow, false, POutputLevel);
        }
        public bool PutLog(string PProcName, string PEvent, string PMessage, DateTime PNow, bool PFormattedMessage_Flg, int POutputLevel)
        {
            if (this.Enabled == false)
            {
                return true;
            }
            if (POutputLevel > this.OutputLevel)
            {
                return true;    //  exit
            }

#if CPUCHECK
            {
                var ww = true;
                if (this.cpuCounter == null)
                {
                    this.cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                    if (ww == true)
                    {
                        var task = System.Threading.Tasks.Task.Run(() =>
                        {
                            DateTime W_LastTime = DateTime.MinValue;
                            var loop = true;
                            while (loop)
                            {
                                System.Threading.Thread.Sleep(1);
                                var W_Now = DateTime.Now.AddMilliseconds(1);
                                TimeSpan interval = TimeSpan.FromMilliseconds(5);
                                W_Now = new DateTime((((W_Now.Ticks + interval.Ticks) / interval.Ticks) - 1) * interval.Ticks, W_Now.Kind);

                                if (W_Now.Ticks != W_LastTime.Ticks)
                                {
                                    this.cpuCounter_LastValue = this.cpuCounter.NextValue();
                                    W_LastTime = W_Now;
                                }
                            }
                        });
                    }
                }
                if (ww != true)
                {
                    this.cpuCounter_LastValue = this.cpuCounter.NextValue();
                }
            }
#endif
            var LogFileName = this.MkLogFileName(PNow, false);
            if ((PProcName == "") && (PEvent == "") && (PMessage == ""))
            {
                if (File.Exists(LogFileName) == true)
                {
                    File.SetLastWriteTime(LogFileName, DateTime.Now);     //  タイムスタンプのみ更新
                }
            }
            else
            {
                LogData PARAM = new LogData(LogFileName, this.Title, this.OutputLevel, this.FOutputMiliSec_Flg, this.FClient, this.FUser, PProcName, PEvent, PMessage, PNow, PFormattedMessage_Flg);
                this.QUE.Add(PARAM);
            }
            return true;
            //return this.QUE.TryAdd(PARAM, System.Threading.Timeout.Infinite);
            //return this.PutLogMain(this.MkLogFileName(), this.FOutputMiliSec_Flg, this.FClient, this.FUser, this.FProcName, PEvent, PMessage, PNow, ref this.FErrorMsg);
        }
        private bool PutLogMain(string PFileName, string PTitle, int POutputLevel, bool POutputMiliSec_Flg, string PClient, string PUser, string PProcName, string PEvent, string PMessage, DateTime PNow, bool PFormattedMessage_Flg, ref string PErrorMsg)
        {
            int m = 0;
            while (true)
            {
                var LogFileName = PFileName;
                try
                {
                    this.MkLogFile(LogFileName, PTitle, POutputLevel);
                    StreamWriter SW = new StreamWriter(LogFileName, true, Encoding.UTF8);
                    try
                    {
                        SW.WriteLine(this.WriteLine(PFormattedMessage_Flg == true ? PMessage : ""
                                + PNow.ToString("yyyy/MM/dd,HH:mm:ss")
                                + ((POutputMiliSec_Flg == false) ? "" : "." + PNow.Millisecond.ToString("000"))
                                + "," + PClient
#if CPUCHECK
                                + "/cpu" + this.cpuCounter_LastValue.ToString("0.0") + "%"
#endif
                                + "," + PUser
                                + "," + '"' + PProcName.Replace("\"", "\"\"") + '"'
                                + "," + '"' + PEvent.Replace("\"", "\"\"") + '"'
                                + "," + PMessage
                                ));
                        SW.Flush();
#if !NOWAIT
                        System.Threading.Thread.Sleep(this.V_SLEEPWAIT);   //  待機
#endif
                        break;  //  exit while
                    }
                    finally
                    {
                        SW.Close();
                    }
                }
                catch (Exception ex)
                {
                    if (m == 100)    //  リトライ回数
                    {
                        PErrorMsg = "ログファイル(" + LogFileName + ")の出力に失敗しました　" + "Error:" + ex.Message;
                        return false;
                    }
                    else
                    {
                        m++;
                        System.Threading.Thread.Sleep(50); //  待機
                    }
                }
            }
            return true;
        }
        private void PutLogThread()
        {
            LogData PARAM;
            while (this.FEnabled == true)
            {
                while (this.QUE.TryTake(out PARAM) == true)
                //if (this.QUE.TryTake(out PARAM, 1000) == true)
                {
                    PARAM.ErrorMsg = "";
                    this.PutLogMain(PARAM.FileName, PARAM.Title, PARAM.OutputLevel, PARAM.OutputMiliSec_Flg, PARAM.Client, PARAM.User, PARAM.ProcName, PARAM.Event, PARAM.Message, PARAM.Now, PARAM.FormattedMessage_Flg, ref PARAM.ErrorMsg);
                    if (PARAM.ErrorMsg != "")
                    {
                        this.FErrorMsg = PARAM.ErrorMsg;
                    }
                    PARAM.Clear();
                }
                System.Threading.Thread.Sleep(10);  //  待機
            }
            while (this.QUE.TryTake(out PARAM) == true)
            {
                PARAM.ErrorMsg = "";
                this.PutLogMain(PARAM.FileName, PARAM.Title, PARAM.OutputLevel, PARAM.OutputMiliSec_Flg, PARAM.Client, PARAM.User, PARAM.ProcName, PARAM.Event, PARAM.Message, PARAM.Now, PARAM.FormattedMessage_Flg, ref PARAM.ErrorMsg);
                if (PARAM.ErrorMsg != "")
                {
                    this.FErrorMsg = PARAM.ErrorMsg;
                }
                PARAM.Clear();
            }
        }
    }
    /// <summary>
    /// スレッドに引数を渡すために中継するクラス
    /// </summary>
    public class LogData
    {
        public string FileName;
        public string Title;
        public int OutputLevel;
        public bool OutputMiliSec_Flg;
        public string Client;
        public string User;
        public string ProcName;
        public string Event;
        public string Message;
        public DateTime Now;
        public string ErrorMsg = "";
        public bool FormattedMessage_Flg = false;

        public LogData(string PFileName, string PTitle, int POutputLevel, bool POutputMiliSec_Flg, string PClient, string PUser, string PProcName, string PEvent, string PMessage, DateTime PNow, bool PFormattedMessage_Flg)
        {
            this.FileName = PFileName;
            this.Title = PTitle;
            this.OutputLevel = POutputLevel;
            this.OutputMiliSec_Flg = POutputMiliSec_Flg;
            this.Client = PClient;
            this.User = PUser;
            this.ProcName = PProcName;
            this.Event = PEvent;
            this.Message = PMessage;
            this.Now = PNow;
            this.FormattedMessage_Flg = PFormattedMessage_Flg;
        }

        public void Clear()
        {
            this.FileName = null;
            this.Title = null;
            this.Client = null;
            this.User = null;
            this.ProcName = null;
            this.Event = null;
            this.Message = null;
            this.ErrorMsg = null;
        }
    }
}
