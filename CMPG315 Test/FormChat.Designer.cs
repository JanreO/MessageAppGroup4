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
            components = new System.ComponentModel.Container();
            label2 = new Label();
            lblConnectionStatus = new Label();
            txtbChat = new RichTextBox();
            txtbText = new RichTextBox();
            btnSend = new Button();
            lstUsers = new ListBox();
            notifyIcon1 = new NotifyIcon(components);
            SuspendLayout();
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(651, 12);
            label2.Name = "label2";
            label2.Size = new Size(72, 15);
            label2.TabIndex = 2;
            label2.Text = "Connection:";
            // 
            // lblConnectionStatus
            // 
            lblConnectionStatus.AutoSize = true;
            lblConnectionStatus.BackColor = SystemColors.Menu;
            lblConnectionStatus.ForeColor = Color.Red;
            lblConnectionStatus.Location = new Point(729, 12);
            lblConnectionStatus.Name = "lblConnectionStatus";
            lblConnectionStatus.Size = new Size(43, 15);
            lblConnectionStatus.TabIndex = 3;
            lblConnectionStatus.Text = "Offline";
            // 
            // txtbChat
            // 
            txtbChat.Location = new Point(12, 12);
            txtbChat.Name = "txtbChat";
            txtbChat.Size = new Size(633, 554);
            txtbChat.TabIndex = 4;
            txtbChat.Text = "";
            // 
            // txtbText
            // 
            txtbText.Location = new Point(12, 572);
            txtbText.Name = "txtbText";
            txtbText.Size = new Size(633, 30);
            txtbText.TabIndex = 5;
            txtbText.Text = "";
            // 
            // btnSend
            // 
            btnSend.Location = new Point(651, 572);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(137, 30);
            btnSend.TabIndex = 6;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // lstUsers
            // 
            lstUsers.FormattingEnabled = true;
            lstUsers.ItemHeight = 15;
            lstUsers.Location = new Point(651, 37);
            lstUsers.Name = "lstUsers";
            lstUsers.Size = new Size(137, 529);
            lstUsers.TabIndex = 7;
            lstUsers.SelectedIndexChanged += lstUsers_SelectedIndexChanged;
            // 
            // notifyIcon1
            // 
            notifyIcon1.Text = "notifyIcon1";
            notifyIcon1.Visible = true;
            // 
            // FormChat
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(800, 616);
            Controls.Add(lstUsers);
            Controls.Add(btnSend);
            Controls.Add(txtbText);
            Controls.Add(txtbChat);
            Controls.Add(lblConnectionStatus);
            Controls.Add(label2);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FormChat";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FormChat";
            Load += FormChat_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label2;
        private Label lblConnectionStatus;
        private RichTextBox txtbChat;
        private RichTextBox txtbText;
        private Button btnSend;
        private ListBox lstUsers;
        private NotifyIcon notifyIcon1;
    }
}