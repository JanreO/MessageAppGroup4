namespace CMPG315_Test
{
    partial class FormChat
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
            cbUsers = new ComboBox();
            label2 = new Label();
            lblIP = new Label();
            txtbChat = new RichTextBox();
            txtbText = new RichTextBox();
            btnSend = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(69, 15);
            label1.TabIndex = 0;
            label1.Text = "Select Chat:";
            // 
            // cbUsers
            // 
            cbUsers.FormattingEnabled = true;
            cbUsers.Location = new Point(87, 9);
            cbUsers.Name = "cbUsers";
            cbUsers.Size = new Size(121, 23);
            cbUsers.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(585, 9);
            label2.Name = "label2";
            label2.Size = new Size(83, 15);
            label2.TabIndex = 2;
            label2.Text = "Connected To:";
            // 
            // lblIP
            // 
            lblIP.AutoSize = true;
            lblIP.Location = new Point(674, 9);
            lblIP.Name = "lblIP";
            lblIP.Size = new Size(36, 15);
            lblIP.TabIndex = 3;
            lblIP.Text = "None";
            // 
            // txtbChat
            // 
            txtbChat.Location = new Point(12, 38);
            txtbChat.Name = "txtbChat";
            txtbChat.Size = new Size(776, 364);
            txtbChat.TabIndex = 4;
            txtbChat.Text = "";
            // 
            // txtbText
            // 
            txtbText.Location = new Point(12, 408);
            txtbText.Name = "txtbText";
            txtbText.Size = new Size(656, 30);
            txtbText.TabIndex = 5;
            txtbText.Text = "";
            // 
            // btnSend
            // 
            btnSend.Location = new Point(674, 408);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(114, 30);
            btnSend.TabIndex = 6;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // FormChat
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnSend);
            Controls.Add(txtbText);
            Controls.Add(txtbChat);
            Controls.Add(lblIP);
            Controls.Add(label2);
            Controls.Add(cbUsers);
            Controls.Add(label1);
            Name = "FormChat";
            Text = "FormChat";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox cbUsers;
        private Label label2;
        private Label lblIP;
        private RichTextBox txtbChat;
        private RichTextBox txtbText;
        private Button btnSend;
    }
}