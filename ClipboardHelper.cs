//#define PASTEMODE
using System;
using System.Runtime.InteropServices;

#if PASTEMODE
using System.Runtime.InteropServices;
#endif
using System.Windows.Forms;

namespace MYWINAPI
{
    public class ClipboardHelper
    {
        private static class NativeMethods
        {
            [DllImport(@"USER32.dll", CharSet = CharSet.Auto)]
            public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);
#if PASTEMODE
            [DllImport(@"USER32.dll", SetLastError = true)]
            public static extern bool ChangeWindowMessageFilter(int message, uint dwFlag);
#endif
            [DllImport(@"USER32.dll", CharSet = CharSet.Auto)]
            public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
            [DllImport(@"USER32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);
            public const int WM_DRAWCLIPBOARD = 0x0308;
            public const int WM_CHANGECBCHAIN = 0x030D;
#if PASTEMODE
            public const int WM_PASTE = 0x302;
            public const int WM_APP = 0x8000;
            public const uint MSGFLT_ADD = 1;
            public const uint MSGFLT_REMOVE = 2;
#endif
        }

        #region イベント定義

        /// <summary>
        /// クリップボードの内容が更新されたときに発生するイベント
        /// </summary>
        public event EventHandler DrawClipboard = null;
        public event EventHandler Paste = null;

        protected void OnDrawClipboard()
        {
            if (this.DrawClipboard != null)
            {
                this.DrawClipboard(this, new EventArgs());
            }
        }
        protected void OnPaste()
        {
            if (this.Paste != null)
            {
                this.Paste(this, new EventArgs());
            }
        }
        #endregion

        /// <summary>
        /// ウィンドウプロシージャをHookするクラス
        /// </summary>
        private class Hook : NativeWindow
        {
            private ClipboardHelper _helper = null;

            private IntPtr nextHandle = IntPtr.Zero;

            public Hook(Form target, ClipboardHelper helper)
            {
                target.Load += new EventHandler(target_Load);
                target.FormClosed += new FormClosedEventHandler(target_FormClosed);
                target.HandleCreated += new EventHandler(target_HandleCreated);
                target.HandleDestroyed += new EventHandler(target_HandleDestroyed);

                this._helper = helper;
            }

            void target_Load(object sender, EventArgs e)
            {
                this.nextHandle = NativeMethods.SetClipboardViewer(((Form)sender).Handle);
#if PASTEMODE
                ChangeWindowMessageFilter(ClassEnumWindows.NativeMethods.WM_APP + 210, MSGFLT_ADD);
                NativeMethods.StartCallWndProcRetHook(((Form)sender).Handle);
#endif
            }

            void target_FormClosed(object sender, FormClosedEventArgs e)
            {
#if PASTEMODE
                NativeMethods.StopCallWndProcRetHook();
#endif
                NativeMethods.ChangeClipboardChain(((Form)sender).Handle, this.nextHandle);
            }

            void target_HandleCreated(object sender, EventArgs e)
            {
                AssignHandle(((Form)sender).Handle);
            }

            void target_HandleDestroyed(object sender, EventArgs e)
            {
                ReleaseHandle();
            }


            protected override void WndProc(ref Message m)
            {
                try
                {
                    switch (m.Msg)
                    {
#if PASTEMODE
                        case NativeMethods.WM_APP + 210:
                            if ((int)m.WParam == WM_PASTE)
                            {
                                this._helper.OnPaste();
                            }
                            break;
#endif

                        case NativeMethods.WM_DRAWCLIPBOARD:
                            this._helper.OnDrawClipboard();
                            if (this.nextHandle != IntPtr.Zero)
                            {
                                NativeMethods.SendMessage(this.nextHandle, m.Msg, m.WParam, m.LParam);
                            }
                            break;

                        case NativeMethods.WM_CHANGECBCHAIN:
                            if ((IntPtr)m.WParam == this.nextHandle)
                            {
                                this.nextHandle = (IntPtr)m.LParam;
                            }
                            else if (this.nextHandle != IntPtr.Zero)
                            {
                                NativeMethods.SendMessage(this.nextHandle, m.Msg, m.WParam, m.LParam);
                            }
                            break;
                    }
                }
                finally
                {
                    base.WndProc(ref m);
                }
            }
        }
        private Hook hook = null;

        public ClipboardHelper(Form wnd)
        {
            this.hook = new Hook(wnd, this);
        }
    }
}
