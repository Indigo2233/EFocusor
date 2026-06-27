namespace ASCOM.EFucoser
{
    partial class SetupDialogForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox comboBoxTransport;
        private System.Windows.Forms.ComboBox comboBoxComPort;
        private System.Windows.Forms.TextBox textBoxTcpHost;
        private System.Windows.Forms.TextBox textBoxTcpPort;
        private System.Windows.Forms.TextBox textBoxTimeout;
        private System.Windows.Forms.TextBox textBoxMaxStep;
        private System.Windows.Forms.CheckBox chkTrace;
        private System.Windows.Forms.CheckBox checkBoxHold;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnRefreshCom;
        private System.Windows.Forms.Button btnAutoDetect;
        private System.Windows.Forms.Label lblDetectStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupDialogForm));
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.comboBoxTransport = new System.Windows.Forms.ComboBox();
            this.comboBoxComPort = new System.Windows.Forms.ComboBox();
            this.textBoxTcpHost = new System.Windows.Forms.TextBox();
            this.textBoxTcpPort = new System.Windows.Forms.TextBox();
            this.textBoxTimeout = new System.Windows.Forms.TextBox();
            this.textBoxMaxStep = new System.Windows.Forms.TextBox();
            this.chkTrace = new System.Windows.Forms.CheckBox();
            this.checkBoxHold = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();

            // cmdOK
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.Location = new System.Drawing.Point(245, 441);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(75, 29);
            this.cmdOK.TabIndex = 0;
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);

            // cmdCancel
            this.cmdCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(326, 441);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 29);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);

            // pictureBox1
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(332, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(69, 43);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.BrowseToAscom);

            // label1
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 145);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "COM Port";

            // label2
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 213);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "TCP Host";

            // label3
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 241);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 17);
            this.label3.TabIndex = 5;
            this.label3.Text = "TCP Port";

            // label5
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 118);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 17);
            this.label5.TabIndex = 7;
            this.label5.Text = "Transport";

            // label6
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 269);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(117, 17);
            this.label6.TabIndex = 8;
            this.label6.Text = "Timeout (ms)";

            // label7
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 297);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(65, 17);
            this.label7.TabIndex = 9;
            this.label7.Text = "Max Step";

            // label8
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 12);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(179, 17);
            this.label8.TabIndex = 10;
            this.label8.Text = "EFucoser ESP8266 Focuser";

            // label9
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(12, 40);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(73, 17);
            this.label9.TabIndex = 13;
            this.label9.Text = "ASCOM Driver Configuration";

            // comboBoxTransport
            this.comboBoxTransport.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTransport.FormattingEnabled = true;
            this.comboBoxTransport.Location = new System.Drawing.Point(142, 115);
            this.comboBoxTransport.Name = "comboBoxTransport";
            this.comboBoxTransport.Size = new System.Drawing.Size(120, 24);
            this.comboBoxTransport.TabIndex = 2;
            this.comboBoxTransport.SelectedIndexChanged += new System.EventHandler(this.comboBoxTransport_SelectedIndexChanged);

            // comboBoxComPort
            this.comboBoxComPort.FormattingEnabled = true;
            this.comboBoxComPort.Location = new System.Drawing.Point(142, 142);
            this.comboBoxComPort.Name = "comboBoxComPort";
            this.comboBoxComPort.Size = new System.Drawing.Size(90, 24);
            this.comboBoxComPort.TabIndex = 3;

            // btnRefreshCom
            this.btnRefreshCom = new System.Windows.Forms.Button();
            this.btnRefreshCom.Location = new System.Drawing.Point(238, 142);
            this.btnRefreshCom.Name = "btnRefreshCom";
            this.btnRefreshCom.Size = new System.Drawing.Size(28, 24);
            this.btnRefreshCom.TabIndex = 14;
            this.btnRefreshCom.Text = "↻";
            this.btnRefreshCom.UseVisualStyleBackColor = true;
            this.btnRefreshCom.Click += new System.EventHandler(this.btnRefreshCom_Click);

            // btnAutoDetect
            this.btnAutoDetect = new System.Windows.Forms.Button();
            this.btnAutoDetect.Location = new System.Drawing.Point(272, 142);
            this.btnAutoDetect.Name = "btnAutoDetect";
            this.btnAutoDetect.Size = new System.Drawing.Size(90, 24);
            this.btnAutoDetect.TabIndex = 15;
            this.btnAutoDetect.Text = "Auto Detect";
            this.btnAutoDetect.UseVisualStyleBackColor = true;
            this.btnAutoDetect.Click += new System.EventHandler(this.btnAutoDetect_Click);

            // lblDetectStatus
            this.lblDetectStatus = new System.Windows.Forms.Label();
            this.lblDetectStatus.AutoSize = true;
            this.lblDetectStatus.Location = new System.Drawing.Point(142, 172);
            this.lblDetectStatus.Name = "lblDetectStatus";
            this.lblDetectStatus.Size = new System.Drawing.Size(100, 13);
            this.lblDetectStatus.TabIndex = 16;

            // textBoxTcpHost
            this.textBoxTcpHost.Location = new System.Drawing.Point(142, 210);
            this.textBoxTcpHost.Name = "textBoxTcpHost";
            this.textBoxTcpHost.Size = new System.Drawing.Size(120, 22);
            this.textBoxTcpHost.TabIndex = 4;
            this.textBoxTcpHost.TextChanged += new System.EventHandler((s, e) => checkTextBox());

            // textBoxTcpPort
            this.textBoxTcpPort.Location = new System.Drawing.Point(142, 238);
            this.textBoxTcpPort.Name = "textBoxTcpPort";
            this.textBoxTcpPort.Size = new System.Drawing.Size(120, 22);
            this.textBoxTcpPort.TabIndex = 5;
            this.textBoxTcpPort.TextChanged += new System.EventHandler((s, e) => checkTextBox());

            // textBoxTimeout
            this.textBoxTimeout.Location = new System.Drawing.Point(142, 266);
            this.textBoxTimeout.Name = "textBoxTimeout";
            this.textBoxTimeout.Size = new System.Drawing.Size(120, 22);
            this.textBoxTimeout.TabIndex = 6;
            this.textBoxTimeout.TextChanged += new System.EventHandler((s, e) => checkTextBox());

            // textBoxMaxStep
            this.textBoxMaxStep.Location = new System.Drawing.Point(142, 294);
            this.textBoxMaxStep.Name = "textBoxMaxStep";
            this.textBoxMaxStep.Size = new System.Drawing.Size(120, 22);
            this.textBoxMaxStep.TabIndex = 7;
            this.textBoxMaxStep.TextChanged += new System.EventHandler((s, e) => checkTextBox());

            // chkTrace
            this.chkTrace.AutoSize = true;
            this.chkTrace.Location = new System.Drawing.Point(15, 330);
            this.chkTrace.Name = "chkTrace";
            this.chkTrace.Size = new System.Drawing.Size(109, 21);
            this.chkTrace.TabIndex = 8;
            this.chkTrace.Text = "Trace Logger";
            this.chkTrace.UseVisualStyleBackColor = true;

            // checkBoxHold
            this.checkBoxHold.AutoSize = true;
            this.checkBoxHold.Location = new System.Drawing.Point(140, 330);
            this.checkBoxHold.Name = "checkBoxHold";
            this.checkBoxHold.Size = new System.Drawing.Size(140, 21);
            this.checkBoxHold.TabIndex = 9;
            this.checkBoxHold.Text = "Continuous Hold";
            this.checkBoxHold.UseVisualStyleBackColor = true;

            // groupBox1
            this.groupBox1.Controls.Add(this.lblDetectStatus);
            this.groupBox1.Controls.Add(this.btnAutoDetect);
            this.groupBox1.Controls.Add(this.btnRefreshCom);
            this.groupBox1.Controls.Add(this.comboBoxComPort);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.checkBoxHold);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.chkTrace);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.textBoxMaxStep);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.textBoxTimeout);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.textBoxTcpPort);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.textBoxTcpHost);
            this.groupBox1.Controls.Add(this.comboBoxTransport);
            this.groupBox1.Location = new System.Drawing.Point(15, 65);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(386, 370);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connection & Hardware";

            // SetupDialogForm
            this.AcceptButton = this.cmdOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(413, 482);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupDialogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "EFucoser Focuser Setup";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
