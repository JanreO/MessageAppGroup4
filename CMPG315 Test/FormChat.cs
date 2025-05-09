using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CMPG315_Test
{
    public partial class FormChat : Form
    {
        private readonly TcpClient? _client;
        private TcpListener? _listener;
        private Thread? _listenerThread;
        private readonly string _username;
        private readonly bool _isServer;
        private readonly int _serverPort;
        private bool _serverRunning = false;

        private readonly List<TcpClient> _connectedClients = new();
        private readonly Dictionary<TcpClient, string> _clientUsernames = new();

        // Client constructor
        public FormChat(TcpClient client, string username, bool isOnline)
        {
            InitializeComponent();
            cbUsers.Items.Add("Company Group");
            cbUsers.SelectedIndex = 0;

            _client = client;
            _username = username;
            _isServer = false;

            if (isOnline)
            {
                lblConnectionStatus.Text = "Online";
                lblConnectionStatus.ForeColor = Color.LimeGreen;
            }
            else
            {
                lblConnectionStatus.Text = "Offline";
                lblConnectionStatus.ForeColor = Color.Red;
            }

            _listenerThread = new Thread(ListenForServerMessages)
            {
                IsBackground = true
            };
            _listenerThread.Start();

            txtbChat.AppendText("You joined the group chat." + Environment.NewLine);

            this.FormClosing += FormChat_FormClosing;
        }
        private void FormChat_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                if (_isServer && _serverRunning)
                {
                    ShutdownServer();
                    _listener?.Stop();
                    _serverRunning = false;
                }

                if (_client != null && _client.Connected)
                {
                    NetworkStream stream = _client.GetStream();
                    byte[] disconnectMessage = Encoding.UTF8.GetBytes($"DISCONNECTED::{_username}");
                    stream.Write(disconnectMessage, 0, disconnectMessage.Length);
                    _client.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while disconnecting: {ex.Message}");
            }
            finally
            {
                Application.Exit();
            }
        }


        private void ListenForServerMessages()
        {
            try
            {
                NetworkStream stream = _client.GetStream();
                byte[] buffer = new byte[1024];

                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        if (message.StartsWith("DISCONNECTED::"))
                        {
                            string username = message.Split("::")[1];
                            Invoke((MethodInvoker)delegate
                            {
                                txtbChat.AppendText($"{username} has left the chat." + Environment.NewLine);
                                cbUsers.Items.Remove(username);
                            });
                            continue;
                        }

                        if (message == "SERVER_DOWN")
                        {
                            Invoke((MethodInvoker)delegate
                            {
                                lblConnectionStatus.Text = "Offline";
                                lblConnectionStatus.ForeColor = Color.Red;
                                txtbChat.AppendText("Server has disconnected." + Environment.NewLine);
                            });
                            continue;
                        }

                        if (IsHandleCreated)
                        {
                            Invoke((MethodInvoker)delegate
                            {
                                txtbChat.AppendText(message + Environment.NewLine);
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsHandleCreated)
                {
                    Invoke((MethodInvoker)delegate
                    {
                        lblConnectionStatus.Text = "Offline";
                        lblConnectionStatus.ForeColor = Color.Red;
                        MessageBox.Show("Error receiving message: " + ex.Message);
                    });
                }
            }
        }

        // Host constructor
        public FormChat(bool isServer, int port, string username, bool isOnline)
        {
            InitializeComponent();
            cbUsers.Items.Add("Company Group");
            cbUsers.SelectedIndex = 0;

            _isServer = isServer;
            _serverPort = port;
            _username = username;

            if (isServer)
            {
                StartServer();
                lblConnectionStatus.Text = "Hosting";
                lblConnectionStatus.ForeColor = Color.Blue;
            }
            else
            {
                if (isOnline)
                {
                    lblConnectionStatus.Text = "Online";
                    lblConnectionStatus.ForeColor = Color.LimeGreen;
                }
                else
                {
                    lblConnectionStatus.Text = "Offline";
                    lblConnectionStatus.ForeColor = Color.Red;
                }
            }

            this.FormClosing += FormChat_FormClosing;
        }


        private void StartServer()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _serverPort);
                _listener.Start();
                _serverRunning = true;

                // ✅ Start the client connection listener
                _listenerThread = new Thread(ListenForClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();

                // ✅ Start the status listener on a different port (port + 1)
                Thread statusThread = new Thread(ListenForServerStatus)
                {
                    IsBackground = true
                };
                statusThread.Start();

                MessageBox.Show($"Server started on Port: {_serverPort} and Status Port: {_serverPort + 1}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting server: " + ex.Message);
            }
        }

        private void ListenForClients()
        {
            try
            {
                while (_serverRunning)
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    _connectedClients.Add(client);

                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string username = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (!_clientUsernames.ContainsValue(username))
                    {
                        _clientUsernames[client] = username;

                        Invoke((MethodInvoker)(() =>
                        {
                            if (!cbUsers.Items.Contains(username))
                            {
                                cbUsers.Items.Add(username);
                                txtbChat.AppendText($"{username} has joined the chat." + Environment.NewLine);
                            }
                        }));

                        // Confirm connection to the client
                        byte[] confirmation = Encoding.UTF8.GetBytes("CONFIRMED");
                        stream.Write(confirmation, 0, confirmation.Length);

                        // Start listening for client messages
                        Thread clientThread = new Thread(() => ListenForClientMessages(client))
                        {
                            IsBackground = true
                        };
                        clientThread.Start();

                        BroadcastUserList();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error accepting client: " + ex.Message);
            }
        }

        private void ListenForClientMessages(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (true)
                    {
                        string message = reader.ReadLine();

                        if (!string.IsNullOrEmpty(message))
                        {
                            Console.WriteLine($"Message received from client: {message}"); // Log to check if it's coming in

                            if (message.StartsWith("DISCONNECTED::"))
                            {
                                string username = message.Split("::")[1];
                                Invoke((MethodInvoker)delegate
                                {
                                    txtbChat.AppendText($"{username} has left the chat." + Environment.NewLine);
                                    cbUsers.Items.Remove(username);
                                });
                                BroadcastMessage($"{username} has left the chat.");
                                continue;
                            }

                            if (message == "SERVER_DOWN")
                            {
                                Invoke((MethodInvoker)delegate
                                {
                                    lblConnectionStatus.Text = "Offline";
                                    lblConnectionStatus.ForeColor = Color.Red;
                                    txtbChat.AppendText("Server has disconnected." + Environment.NewLine);
                                });
                                continue;
                            }

                            string clientUsername = _clientUsernames.ContainsKey(client) ? _clientUsernames[client] : "Unknown";
                            string displayMessage = $"[{clientUsername}]: {message}";

                            Console.WriteLine($"Displaying on server: {displayMessage}"); // Log to check display

                            // Display directly
                            Invoke((MethodInvoker)delegate
                            {
                                txtbChat.AppendText(displayMessage + Environment.NewLine);
                            });

                            // Broadcast to all other clients
                            BroadcastToAllClients(displayMessage, client);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving message from client: {ex.Message}");
            }
        }

        private void BroadcastToAllClients(string message, TcpClient senderClient)
        {
            lock (_connectedClients)
            {
                foreach (var client in _connectedClients.ToList())
                {
                    try
                    {
                        if (client.Connected && client != senderClient)
                        {
                            NetworkStream stream = client.GetStream();
                            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                writer.WriteLine(message);
                                writer.Flush();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to send message to client: {ex.Message}");
                    }
                }
            }
        }


        private void ListenForServerStatus()
        {
            try
            {
                TcpListener statusListener = new TcpListener(IPAddress.Any, _serverPort + 1);
                statusListener.Start();

                while (_serverRunning)
                {
                    TcpClient statusClient = statusListener.AcceptTcpClient();
                    NetworkStream stream = statusClient.GetStream();
                    byte[] response = Encoding.UTF8.GetBytes("1"); // Server is running
                    stream.Write(response, 0, response.Length);
                    statusClient.Close();
                }

                statusListener.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error with server status check: " + ex.Message);
            }
        }


        private void BroadcastMessage(string message)
        {
            lock (_connectedClients)
            {
                foreach (var client in _connectedClients.ToList())
                {
                    try
                    {
                        if (client.Connected)
                        {
                            NetworkStream stream = client.GetStream();
                            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                writer.WriteLine(message);
                                writer.Flush();
                            }
                        }
                        else
                        {
                            _connectedClients.Remove(client);
                            _clientUsernames.Remove(client);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to send message to client: " + ex.Message);
                    }
                }
            }
        }


        private void BroadcastUserList()
        {
            string userList = "USER_LIST:" + string.Join(",", _clientUsernames.Values);
            byte[] message = Encoding.UTF8.GetBytes(userList);

            foreach (var client in _connectedClients.ToList())
            {
                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(message, 0, message.Length);
                    }
                    else
                    {
                        _connectedClients.Remove(client);
                        _clientUsernames.Remove(client);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to send user list: " + ex.Message);
                }
            }
        }

        private void ShutdownServer()
        {
            byte[] message = Encoding.UTF8.GetBytes("SERVER_DOWN");

            foreach (var client in _connectedClients.ToList())
            {
                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(message, 0, message.Length);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to send shutdown message to client: " + ex.Message);
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = $"{_username}: {txtbText.Text}";

            // Check if the client is connected
            if (_client != null && _client.Connected)
            {
                try
                {
                    NetworkStream stream = _client.GetStream();
                    byte[] buffer = Encoding.UTF8.GetBytes(message + Environment.NewLine); // Add newline for proper read
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();

                    txtbChat.AppendText($"Me: {txtbText.Text}" + Environment.NewLine);
                    txtbText.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error sending message: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("You are not connected to the server.");
            }
        }
    }
}
