﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace CMPG315_Test
{
    public partial class FormConnection : Form
    {
        // Fixed IP and Port for easy configuration
        private const string DefaultIP = "192.168.0.22";
        private const int DefaultPort = 8080;

        public FormConnection()
        {
            InitializeComponent();
            this.Load += FormConnection_Load;
        }

        // 🟢 Event Handler for Form Load
        private void FormConnection_Load(object? sender, EventArgs e) // Added `?` for nullability
        {
            ToggleMode();
            txtbPort.Text = DefaultPort.ToString(); // Default port displayed
            txtbIP.Text = DefaultIP; // Always display the static IP
        }

        // 🟢 Enable/Disable IP Textbox based on mode (Host or Client)
        private void ToggleMode()
        {
            if (rbHost.Checked)
            {
                txtbIP.Enabled = false; // Host does not need to enter IP
            }
            else if (rbClient.Checked)
            {
                txtbIP.Enabled = true;  // Client can enter IP if needed
            }
        }


        private void rbHost_CheckedChanged(object sender, EventArgs e)
        {
            ToggleMode();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            string username = txtbUsername.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Please enter a username.");
                return;
            }

            if (rbHost.Checked)
            {
                try
                {
                    // Start the server as the host
                    FormChat chatForm = new FormChat(isServer: true, port: DefaultPort, username: username);
                    chatForm.Show();
                    this.Hide();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting the server: {ex.Message}");
                }
            }
            else if (rbClient.Checked)
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(DefaultIP, DefaultPort);

                    // Send the username to the server
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = Encoding.UTF8.GetBytes(username);
                    stream.Write(buffer, 0, buffer.Length);

                    MessageBox.Show($"Connected successfully to {DefaultIP}:{DefaultPort}");

                    FormChat chatForm = new FormChat(client, username);
                    chatForm.Show();
                    this.Hide();
                }
                catch (SocketException)
                {
                    MessageBox.Show($"Failed to connect to {DefaultIP}:{DefaultPort}. Is the server running and port forwarded?");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unexpected error:\n" + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please select Host or Client.");
            }
        }
    }
}
