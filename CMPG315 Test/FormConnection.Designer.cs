namespace CMPG315_Test
{
    partial class FormConnection
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
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            txtbUsername = new TextBox();
            txtbIP = new TextBox();
            txtbPort = new TextBox();
            rbHost = new RadioButton();
            rbClient = new RadioButton();
            btnConnect = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 12);
            label1.Name = "label1";
            label1.Size = new Size(60, 15);
            label1.TabIndex = 0;
            label1.Text = "Username";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 41);
            label2.Name = "label2";
            label2.Size = new Size(62, 15);
            label2.TabIndex = 1;
            label2.Text = "IP Address";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 70);
            label3.Name = "label3";
            label3.Size = new Size(29, 15);
            label3.TabIndex = 2;
            label3.Text = "Port";
            // 
            // txtbUsername
            // 
            txtbUsername.Location = new Point(139, 12);
            txtbUsername.Name = "txtbUsername";
            txtbUsername.Size = new Size(100, 23);
            txtbUsername.TabIndex = 3;
            // 
            // txtbIP
            // 
            txtbIP.Location = new Point(139, 41);
            txtbIP.Name = "txtbIP";
            txtbIP.Size = new Size(100, 23);
            txtbIP.TabIndex = 4;
            // 
            // txtbPort
            // 
            txtbPort.Location = new Point(139, 70);
            txtbPort.Name = "txtbPort";
            txtbPort.Size = new Size(100, 23);
            txtbPort.TabIndex = 5;
            // 
            // rbHost
            // 
            rbHost.AutoSize = true;
            rbHost.Location = new Point(71, 99);
            rbHost.Name = "rbHost";
            rbHost.Size = new Size(50, 19);
            rbHost.TabIndex = 6;
            rbHost.TabStop = true;
            rbHost.Text = "Host";
            rbHost.UseVisualStyleBackColor = true;
            rbHost.CheckedChanged += rbHost_CheckedChanged;
            // 
            // rbClient
            // 
            rbClient.AutoSize = true;
            rbClient.Location = new Point(71, 124);
            rbClient.Name = "rbClient";
            rbClient.Size = new Size(56, 19);
            rbClient.TabIndex = 7;
            rbClient.TabStop = true;
            rbClient.Text = "Client";
            rbClient.UseVisualStyleBackColor = true;
            rbClient.CheckedChanged += rbHost_CheckedChanged;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(12, 149);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(227, 38);
            btnConnect.TabIndex = 8;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // FormConnection
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(254, 202);
            Controls.Add(btnConnect);
            Controls.Add(rbClient);
            Controls.Add(rbHost);
            Controls.Add(txtbPort);
            Controls.Add(txtbIP);
            Controls.Add(txtbUsername);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            MaximizeBox = false;
            Name = "FormConnection";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FormConnection";
            Load += FormConnection_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private TextBox txtbUsername;
        private TextBox txtbIP;
        private TextBox txtbPort;
        private RadioButton rbHost;
        private RadioButton rbClient;
        private Button btnConnect;
    }
}