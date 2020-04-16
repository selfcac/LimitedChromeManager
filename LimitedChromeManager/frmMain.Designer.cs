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
            this.button1 = new System.Windows.Forms.Button();
            this.pbMain = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // clstProcess
            // 
            this.clstProcess.Enabled = false;
            this.clstProcess.FormattingEnabled = true;
            this.clstProcess.Location = new System.Drawing.Point(12, 12);
            this.clstProcess.Name = "clstProcess";
            this.clstProcess.Size = new System.Drawing.Size(413, 304);
            this.clstProcess.TabIndex = 0;
            // 
            // rtbLog
            // 
            this.rtbLog.Location = new System.Drawing.Point(431, 12);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(442, 304);
            this.rtbLog.TabIndex = 1;
            this.rtbLog.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(798, 322);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Close all";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // pbMain
            // 
            this.pbMain.Location = new System.Drawing.Point(12, 322);
            this.pbMain.Name = "pbMain";
            this.pbMain.Size = new System.Drawing.Size(780, 23);
            this.pbMain.TabIndex = 3;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(885, 353);
            this.ControlBox = false;
            this.Controls.Add(this.pbMain);
            this.Controls.Add(this.button1);
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
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ProgressBar pbMain;
    }
}

