namespace LimitedChromeManager
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.clstProcess = new System.Windows.Forms.CheckedListBox();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.pbMain = new System.Windows.Forms.ProgressBar();
            this.bwProcess = new System.ComponentModel.BackgroundWorker();
            this.btnExit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // clstProcess
            // 
            this.clstProcess.Enabled = false;
            this.clstProcess.FormattingEnabled = true;
            this.clstProcess.Location = new System.Drawing.Point(12, 12);
            this.clstProcess.Name = "clstProcess";
            this.clstProcess.Size = new System.Drawing.Size(279, 304);
            this.clstProcess.TabIndex = 0;
            // 
            // rtbLog
            // 
            this.rtbLog.Location = new System.Drawing.Point(297, 12);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(362, 304);
            this.rtbLog.TabIndex = 1;
            this.rtbLog.Text = "";
            this.rtbLog.WordWrap = false;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(489, 322);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(89, 23);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "Cancel All";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // pbMain
            // 
            this.pbMain.Location = new System.Drawing.Point(12, 322);
            this.pbMain.Name = "pbMain";
            this.pbMain.Size = new System.Drawing.Size(471, 23);
            this.pbMain.TabIndex = 3;
            // 
            // bwProcess
            // 
            this.bwProcess.WorkerSupportsCancellation = true;
            this.bwProcess.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bwProcess_DoWork);
            this.bwProcess.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bwProcess_RunWorkerCompleted);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(584, 322);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 4;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Visible = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(669, 353);
            this.ControlBox = false;
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.pbMain);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.rtbLog);
            this.Controls.Add(this.clstProcess);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Chrome limited Manager";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox clstProcess;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ProgressBar pbMain;
        private System.ComponentModel.BackgroundWorker bwProcess;
        private System.Windows.Forms.Button btnExit;
    }
}

