using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace CMPG315_Test
{
    public partial class FormConnection : Form
    {


        public FormConnection()
        {
            InitializeComponent();
            this.Load += FormConnection_Load;
        }

        private void FormConnection_Load(object? sender, EventArgs e) // Added `?` for nullability
        {
            ToggleMode();
            rbClient.Checked = true;
        }

        private void ToggleMode()
        {
            if (rbHost.Checked)
            {
                txtbIP.Enabled = true; // Host does not need to enter IP
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
            string ip = txtbIP.Text.Trim();
            int port = int.TryParse(txtbPort.Text, out int parsedPort) ? parsedPort : 8080;

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
                    FormChat chatForm = new FormChat(isServer: true, port: port, username: username);
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
                    if (IsServerAvailable(ip, port))
                    {
                        TcpClient client = new TcpClient();
                        client.Connect(ip, port);

                        // Send the username to the server
                        NetworkStream stream = client.GetStream();
                        byte[] buffer = Encoding.UTF8.GetBytes(username);
                        stream.Write(buffer, 0, buffer.Length);

                        MessageBox.Show($"Connected successfully to {ip}:{port}");

                        FormChat chatForm = new FormChat(client, username);
                        chatForm.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show($"Cannot connect to {ip}:{port}. The server is not running.");
                    }
                }
                catch (SocketException)
                {
                    MessageBox.Show($"Failed to connect to {ip}:{port}. Is the server running and port forwarded?");
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
        private bool IsServerAvailable(string ip, int port)
        {
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    tcpClient.Connect(ip, port + 1); // Connect to the status port
                    NetworkStream stream = tcpClient.GetStream();
                    byte[] buffer = new byte[1];
                    stream.Read(buffer, 0, buffer.Length);

                    string response = Encoding.UTF8.GetString(buffer);
                    return response == "1"; // Only return true if the server responded with "1"
                }
            }
            catch
            {
                return false;
            }
        }

    }
}
