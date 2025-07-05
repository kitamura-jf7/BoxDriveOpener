using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoxDriveOpener
{
    /// <summary>
    /// システム想定の中断エラー、メッセージを表示して終了する
    /// </summary>
    public class MyExitException : Exception
    {
        public MyExitException(string message)
            : base(message) 
        { 
        }
        public MyExitException(string message, Exception inner)
            : base(message) 
        { 
        }
    }

    /// <summary>
    /// システム想定外のエラー、システム担当者呼び出しメッセージを表示して終了する
    /// </summary>
    public class MyStopException : Exception
    {
        public MyStopException(string message)
            : base(message)
        {
        }
        public MyStopException(string message, Exception inner)
            : base(message)
        {
        }
    }

    // Creates a class to handle the exception event.
    public class CustomExceptionHandler
    {
        public static Form ActiveForm = null;
        private string AppTitle;

        public CustomExceptionHandler()
        {
            AppTitle = System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.ExecutablePath).ProductName;
        }

        private void CloseOrReboot()
        {
            {
                if (Debugger.IsAttached == false)
                {
                    //  再起動
                    while (true)
                    {
                        try
                        {
                            if (Environment.CommandLine.IndexOf(" OnException") >= 0)
                            {
                                System.Threading.Thread.Sleep(60 * 1000);  //  待機
                            }
                            var task = Task.Run(() =>
                            {
                                System.Threading.Thread.Sleep(100);  //  待機
                                Process.Start(Application.ExecutablePath, Environment.CommandLine.Replace(Application.ExecutablePath, ""));
                            });
                            System.Threading.Thread.Sleep(100);  //  待機
                            break;
                        }
                        catch (Exception ex)
                        {
                            System.Threading.Thread.Sleep(1000);  //  待機
                        }
                    }
                }
            }
            //System.Threading.Thread.Sleep(6000); //  待機
            Application.Exit();
        }

        // Handles the exception event.
        private void OnUnhandledExceptionMain(object sender, UnhandledExceptionEventArgs t, bool ShowDialog)
        {
            this.CloseOrReboot();
        }

        // Handles the exception event.
        private void OnThreadExceptionMain(object sender, ThreadExceptionEventArgs t, bool ShowDialog)
        {
            DialogResult result = DialogResult.Cancel;
            if (ActiveForm != null)
            {
                if ((ShowDialog == true) && (ActiveForm.Visible == true))
                {
                    FormBoxDriveOpener.NativeMethods.SetForegroundWindow(ActiveForm.Handle);
                }
            }
            if (t.Exception is ApplicationException)
            {
                if (t.Exception.Message != "")
                {
                    result = this.ShowExceptionDialog(t.Exception, ShowDialog);
                }
            }
            else
            {
                //  メッセージダイアログ表示
                if (t.Exception is MyExitException)
                {
                    result = this.ShowExceptionDialog(t.Exception, ShowDialog);
                }
                else
                {
                    result = this.ShowUnkownExceptionDialog(t.Exception, ShowDialog);
                }

                this.CloseOrReboot();
            }
        }

        private DialogResult ShowExceptionDialog(Exception ex, bool ShowDialog)
        {
            if (ShowDialog == true)     //if ((ShowDialog == true) && (ActiveForm.Visible == true))
            {
                return MessageBox.Show(ex.Message, (ActiveForm == null ? "" : ActiveForm.Text + " - ") + AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                return DialogResult.OK;
            }
        }

        private DialogResult ShowUnkownExceptionDialog(Exception ex, bool ShowDialog)
        {
            string errorMsg = "予期せぬエラーが発生しました" + "\n";
            errorMsg = errorMsg + Application.ExecutablePath + " " + Application.ProductVersion + " " + System.Environment.CommandLine + "\n";
            errorMsg = errorMsg + ex.Message + "\n";
            errorMsg = errorMsg + ex;
            if (ShowDialog == true)
            {
                return MessageBox.Show(errorMsg, "Application Error - " + (ActiveForm == null ? "" : ActiveForm.Text + " - ") + AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else
            {
                return DialogResult.OK;
            }
        }


        // Handles the exception event.
        public void OnThreadException(object sender, ThreadExceptionEventArgs t)
        {
            this.OnThreadExceptionMain(sender, t, true);
        }

        public void OnThreadExceptionForBatch(object sender, ThreadExceptionEventArgs t)
        {
            this.OnThreadExceptionMain(sender, t, false);
        }

        public void OnUnhandledExceptionForBatch(object sender, UnhandledExceptionEventArgs t)
        {
            this.OnUnhandledExceptionMain(sender, t, false);
        }
    }
}
