namespace BoxDriveOpener
{
    partial class FormBoxDriveOpener
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        ///// <summary>
        ///// 使用中のリソースをすべてクリーンアップします。
        ///// </summary>
        ///// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing && (components != null))
        //    {
        //        components.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormBoxDriveOpener));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.LBL_PopupColor = new System.Windows.Forms.Label();
            this.LBL_ExecPopup = new System.Windows.Forms.Label();
            this.timerChangeClipboard = new System.Windows.Forms.Timer(this.components);
            this.CB_USE_BOXLINK = new System.Windows.Forms.CheckBox();
            this.CB_CLOSE_SIDEBAR_FILE = new System.Windows.Forms.CheckBox();
            this.CB_CLOSE_SIDEBAR_FOLDER = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBox1.Location = new System.Drawing.Point(0, 0);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(480, 60);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = "1\r\n2\r\n3\r\n4\r\n5";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 10000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // textBox2
            // 
            this.textBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox2.Location = new System.Drawing.Point(0, 60);
            this.textBox2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox2.Size = new System.Drawing.Size(480, 240);
            this.textBox2.TabIndex = 1;
            this.textBox2.Text = "1\r\n2\r\n3\r\n4\r\n5";
            // 
            // LBL_PopupColor
            // 
            this.LBL_PopupColor.AutoSize = true;
            this.LBL_PopupColor.BackColor = System.Drawing.SystemColors.Info;
            this.LBL_PopupColor.ForeColor = System.Drawing.SystemColors.InfoText;
            this.LBL_PopupColor.Location = new System.Drawing.Point(324, 6);
            this.LBL_PopupColor.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.LBL_PopupColor.Name = "LBL_PopupColor";
            this.LBL_PopupColor.Size = new System.Drawing.Size(35, 12);
            this.LBL_PopupColor.TabIndex = 2;
            this.LBL_PopupColor.Text = "label1";
            this.LBL_PopupColor.Visible = false;
            // 
            // LBL_ExecPopup
            // 
            this.LBL_ExecPopup.AutoSize = true;
            this.LBL_ExecPopup.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.LBL_ExecPopup.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.LBL_ExecPopup.Location = new System.Drawing.Point(324, 18);
            this.LBL_ExecPopup.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.LBL_ExecPopup.Name = "LBL_ExecPopup";
            this.LBL_ExecPopup.Size = new System.Drawing.Size(35, 12);
            this.LBL_ExecPopup.TabIndex = 3;
            this.LBL_ExecPopup.Text = "label1";
            this.LBL_ExecPopup.Visible = false;
            // 
            // timerChangeClipboard
            // 
            this.timerChangeClipboard.Interval = 400;
            this.timerChangeClipboard.Tick += new System.EventHandler(this.timerChangeClipboard_Tick);
            // 
            // CB_USE_BOXLINK
            // 
            this.CB_USE_BOXLINK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CB_USE_BOXLINK.AutoSize = true;
            this.CB_USE_BOXLINK.Location = new System.Drawing.Point(2, 112);
            this.CB_USE_BOXLINK.Margin = new System.Windows.Forms.Padding(1);
            this.CB_USE_BOXLINK.Name = "CB_USE_BOXLINK";
            this.CB_USE_BOXLINK.Size = new System.Drawing.Size(162, 16);
            this.CB_USE_BOXLINK.TabIndex = 4;
            this.CB_USE_BOXLINK.Text = "boxショートカットの作成を使う";
            this.CB_USE_BOXLINK.UseVisualStyleBackColor = true;
            this.CB_USE_BOXLINK.CheckedChanged += new System.EventHandler(this.CB_USE_BOXLINK_CheckedChanged);
            // 
            // CB_CLOSE_SIDEBAR_FILE
            // 
            this.CB_CLOSE_SIDEBAR_FILE.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CB_CLOSE_SIDEBAR_FILE.AutoSize = true;
            this.CB_CLOSE_SIDEBAR_FILE.Location = new System.Drawing.Point(2, 89);
            this.CB_CLOSE_SIDEBAR_FILE.Margin = new System.Windows.Forms.Padding(1);
            this.CB_CLOSE_SIDEBAR_FILE.Name = "CB_CLOSE_SIDEBAR_FILE";
            this.CB_CLOSE_SIDEBAR_FILE.Size = new System.Drawing.Size(171, 16);
            this.CB_CLOSE_SIDEBAR_FILE.TabIndex = 3;
            this.CB_CLOSE_SIDEBAR_FILE.Text = "ファイルサイドバーを自動で隠す";
            this.CB_CLOSE_SIDEBAR_FILE.UseVisualStyleBackColor = true;
            this.CB_CLOSE_SIDEBAR_FILE.CheckedChanged += new System.EventHandler(this.CB_CLOSE_SIDEBAR_FILE_CheckedChanged);
            // 
            // CB_CLOSE_SIDEBAR_FOLDER
            // 
            this.CB_CLOSE_SIDEBAR_FOLDER.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CB_CLOSE_SIDEBAR_FOLDER.AutoSize = true;
            this.CB_CLOSE_SIDEBAR_FOLDER.Checked = true;
            this.CB_CLOSE_SIDEBAR_FOLDER.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CB_CLOSE_SIDEBAR_FOLDER.Location = new System.Drawing.Point(2, 67);
            this.CB_CLOSE_SIDEBAR_FOLDER.Margin = new System.Windows.Forms.Padding(1);
            this.CB_CLOSE_SIDEBAR_FOLDER.Name = "CB_CLOSE_SIDEBAR_FOLDER";
            this.CB_CLOSE_SIDEBAR_FOLDER.Size = new System.Drawing.Size(172, 16);
            this.CB_CLOSE_SIDEBAR_FOLDER.TabIndex = 2;
            this.CB_CLOSE_SIDEBAR_FOLDER.Text = "フォルダサイドバーを自動で隠す";
            this.CB_CLOSE_SIDEBAR_FOLDER.UseVisualStyleBackColor = true;
            this.CB_CLOSE_SIDEBAR_FOLDER.CheckedChanged += new System.EventHandler(this.CB_CLOSE_SIDEBAR_FOLDER_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.AutoSize = true;
            this.panel1.Controls.Add(this.CB_CLOSE_SIDEBAR_FOLDER);
            this.panel1.Controls.Add(this.CB_USE_BOXLINK);
            this.panel1.Controls.Add(this.CB_CLOSE_SIDEBAR_FILE);
            this.panel1.Location = new System.Drawing.Point(227, 142);
            this.panel1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(235, 139);
            this.panel1.TabIndex = 5;
            // 
            // FormBoxDriveOpener
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 300);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.LBL_ExecPopup);
            this.Controls.Add(this.LBL_PopupColor);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "FormBoxDriveOpener";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "BoxDrive Supporting";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label LBL_PopupColor;
        private System.Windows.Forms.Label LBL_ExecPopup;
        private System.Windows.Forms.Timer timerChangeClipboard;
        private System.Windows.Forms.CheckBox CB_USE_BOXLINK;
        private System.Windows.Forms.CheckBox CB_CLOSE_SIDEBAR_FILE;
        private System.Windows.Forms.CheckBox CB_CLOSE_SIDEBAR_FOLDER;
        private System.Windows.Forms.Panel panel1;
    }
}

