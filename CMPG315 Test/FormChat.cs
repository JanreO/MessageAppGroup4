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
        private static string usernameFilter = string.Empty;

        private readonly List<TcpClient> _connectedClients = new();
        private readonly Dictionary<TcpClient, string> _clientUsernames = new();
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        private List<string> localUserList = new List<string>();



        // Client constructor
        public FormChat(TcpClient client, string username, bool isOnline)
        {
            InitializeComponent();
            lstUsers.Items.Add("Company Group");
            lstUsers.SelectedIndex = 0;

            _client = client;
            _username = username;
            _isServer = false;

            // ✅ Set the username filter to the client's username
            usernameFilter = username;

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

                Invoke((MethodInvoker)(() =>
                {
                    lstUsers.Items.Remove(_username);
                }));
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

                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (true)
                    {
                        string message = reader.ReadLine();

                        if (!string.IsNullOrEmpty(message))
                        {
                            if (message.StartsWith("USER_JOINED:"))
                            {
                                string username = message.Replace("USER_JOINED:", "").Trim();
                                _syncContext.Post(_ =>
                                {
                                    if (username != _username && !lstUsers.Items.Contains(username))
                                    {
                                        lstUsers.Items.Add(username);
                                        txtbChat.AppendText($"{username} has joined the chat." + Environment.NewLine);
                                    }
                                }, null);
                            }
                            else if (message.StartsWith("USER_LEFT:"))
                            {
                                string username = message.Replace("USER_LEFT:", "").Trim();
                                _syncContext.Post(_ =>
                                {
                                    if (lstUsers.Items.Contains(username))
                                    {
                                        lstUsers.Items.Remove(username);
                                        txtbChat.AppendText($"{username} has left the chat." + Environment.NewLine);
                                    }
                                }, null);
                            }
                            else if (message.StartsWith("USER_LIST:"))
                            {
                                string[] users = message.Replace("USER_LIST:", "").Split(',');

                                // ✅ Clear the list and re-add "Company Group"
                                _syncContext.Post(_ =>
                                {
                                    lstUsers.Items.Clear();
                                    lstUsers.Items.Add("Company Group");

                                    foreach (var user in users)
                                    {
                                        if (!string.IsNullOrWhiteSpace(user) && user != _username)
                                        {
                                            lstUsers.Items.Add(user);
                                        }
                                    }
                                }, null);
                            }
                            else
                            {
                                _syncContext.Post(_ =>
                                {
                                    txtbChat.AppendText(message + Environment.NewLine);
                                }, null);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _syncContext.Post(_ =>
                {
                    lblConnectionStatus.Text = "Offline";
                    lblConnectionStatus.ForeColor = Color.Red;
                    MessageBox.Show("Error receiving message: " + ex.Message);
                }, null);
            }
        }


        // Host constructor
        public FormChat(bool isServer, int port, string username, bool isOnline)
        {
            InitializeComponent();
            lstUsers.Items.Add("Company Group"); // Adding the default group
            lstUsers.SelectedIndex = 0;

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
                    string username = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    // ✅ Store in dictionary for identification
                    if (!_clientUsernames.ContainsKey(client))
                    {
                        _clientUsernames[client] = username;
                    }

                    // ✅ Add to server's listbox
                    _syncContext.Post(_ =>
                    {
                        if (!lstUsers.Items.Contains(username))
                        {
                            lstUsers.Items.Add(username);
                            txtbChat.AppendText($"{username} has joined the chat." + Environment.NewLine);
                        }
                    }, null);

                    // ✅ Broadcast the join message to all clients
                    BroadcastToAllClients($"USER_JOINED:{username}");

                    // ✅ Send the full list of connected users to the newly joined client
                    SendFullUserList(client);

                    // ✅ Start listening for client messages
                    Thread clientThread = new Thread(() => ListenForClientMessages(client))
                    {
                        IsBackground = true
                    };
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error accepting client: " + ex.Message);
            }
        }

        private void SendFullUserList(TcpClient client)
        {
            try
            {
                var userList = _clientUsernames.Values.ToList();
                string userListMessage = "USER_LIST:" + string.Join(",", userList);

                NetworkStream stream = client.GetStream();
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.WriteLine(userListMessage);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send user list to client: {ex.Message}");
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
                        string message = reader.ReadLine()?.Trim();

                        if (!string.IsNullOrEmpty(message))
                        {
                            if (message.StartsWith("DISCONNECTED::"))
                            {
                                string username = message.Split("::")[1];
                                HandleClientDisconnect(client, username); // ✅ Call the new method
                                continue;
                            }

                            string clientUsername = _clientUsernames.ContainsKey(client) ? _clientUsernames[client] : "Unknown";
                            string formattedMessage = $"[{clientUsername}]: {message}";

                            // ✅ Use SynchronizationContext to update the UI
                            _syncContext.Post(_ =>
                            {
                                if (!string.IsNullOrWhiteSpace(formattedMessage))
                                {
                                    txtbChat.AppendText(formattedMessage + Environment.NewLine);
                                }
                            }, null);

                            // ✅ Broadcast to all other clients
                            BroadcastToAllClients(formattedMessage, client);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleClientDisconnect(client, _clientUsernames.ContainsKey(client) ? _clientUsernames[client] : "Unknown");
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
                                writer.WriteLine(message); // ✅ WriteLine adds the newline
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


        private void HandleClientDisconnect(TcpClient client, string username)
        {
            if (_clientUsernames.ContainsKey(client))
            {
                _clientUsernames.Remove(client);
                _connectedClients.Remove(client);

                // ✅ Remove from the server's list
                _syncContext.Post(_ =>
                {
                    if (lstUsers.Items.Contains(username))
                    {
                        lstUsers.Items.Remove(username);
                        txtbChat.AppendText($"{username} has left the chat." + Environment.NewLine);
                    }
                }, null);

                // ✅ Notify all clients that this user left
                BroadcastToAllClients($"USER_LEFT:{username}");
            }
        }


        private void BroadcastToAllClients(string message)
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



        private void BroadcastUserList()
        {
            // ✅ Get the current list of connected clients from the server dictionary
            var userList = _clientUsernames.Values.ToList();

            // ✅ Build the message to send
            string userListMessage = "USER_LIST:" + string.Join(",", userList);

            foreach (var client in _connectedClients.ToList())
            {
                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                        {
                            writer.WriteLine(userListMessage);
                            writer.Flush();
                        }
                    }
                    else
                    {
                        _connectedClients.Remove(client);
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
            string message = txtbText.Text; // Only send the message, not the username

            // Check if the client is connected
            if (_client != null && _client.Connected)
            {
                try
                {
                    NetworkStream stream = _client.GetStream();
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        string formattedMessage = $"{message}";
                        writer.WriteLine(formattedMessage); // ✅ WriteLine to auto-append newline
                        writer.Flush();
                    }

                    // Display on sender's chat window
                    txtbChat.AppendText($"You: {txtbText.Text}" + Environment.NewLine);
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