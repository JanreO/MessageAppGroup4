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
        private string messageType = "Group"; // Default to group chat

        // Declare _currentChat to store the currently selected chat
        private string _currentChat = "Company Group"; // Default to the main group chat

        // Client constructor
        public FormChat(TcpClient client, string username, bool isOnline)
        {
            InitializeComponent();
            lstUsers.Items.Add("Company Group");
            lstUsers.SelectedIndex = 0;
            _currentChat = "Company Group";

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

                    // ✅ Delete all user chat files from Documents\CMPG315_Test
                    DeleteAllChatFiles();
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

        private void DeleteAllChatFiles()
        {
            try
            {
                string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");

                if (Directory.Exists(documentsPath))
                {
                    var files = Directory.GetFiles(documentsPath, "*.txt");
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            Console.WriteLine($"Deleted file: {file}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to delete file: {file}. Error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting chat files: {ex.Message}");
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

                                _syncContext.Post(_ =>
                                {
                                    lstUsers.Items.Clear();
                                    lstUsers.Items.Add("Company Group");

                                    foreach (var user in users)
                                    {
                                        if (!string.IsNullOrWhiteSpace(user) && user != _username)
                                        {
                                            if (!lstUsers.Items.Contains(user))
                                            {
                                                lstUsers.Items.Add(user);
                                            }
                                        }
                                    }

                                    // ✅ Remove empty indexes
                                    for (int i = lstUsers.Items.Count - 1; i >= 0; i--)
                                    {
                                        if (string.IsNullOrWhiteSpace(lstUsers.Items[i].ToString()))
                                        {
                                            lstUsers.Items.RemoveAt(i);
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
            catch (IOException ioEx)
            {
                MessageBox.Show($"Connection lost: {ioEx.Message}");
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

                // ✅ If the list only contains the new user, don't send the list
                if (userList.Count == 1 && userList[0] == _clientUsernames[client])
                {
                    return; // Skip sending the list to avoid blank spaces
                }

                // ✅ Remove the client's own username before sending
                userList.Remove(_clientUsernames[client]);

                // ✅ Only send if there are still users in the list
                if (userList.Count > 0)
                {
                    string userListMessage = "USER_LIST:" + string.Join(",", userList);

                    NetworkStream stream = client.GetStream();
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        writer.WriteLine(userListMessage);
                        writer.Flush();
                    }
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
                                HandleClientDisconnect(client, username);
                                continue;
                            }

                            if (message.StartsWith("PRIVATE::"))
                            {
                                // Extract the sender, receiver, and message
                                string[] parts = message.Split(new[] { "::" }, StringSplitOptions.None);
                                string sender = parts[1];
                                string receiver = parts[2];
                                string privateMessage = parts[3];

                                // Find the target client
                                var targetClient = _connectedClients.FirstOrDefault(c => _clientUsernames[c] == receiver);
                                if (targetClient != null)
                                {
                                    // Send only to the target client
                                    BroadcastToSingleClient(targetClient, $"PRIVATE::{sender}::{privateMessage}");
                                }
                            }
                            else
                            {
                                string clientUsername = _clientUsernames.ContainsKey(client) ? _clientUsernames[client] : "Unknown";
                                string formattedMessage = $"[{clientUsername}]: {message}";

                                _syncContext.Post(_ =>
                                {
                                    if (!string.IsNullOrWhiteSpace(formattedMessage))
                                    {
                                        txtbChat.AppendText(formattedMessage + Environment.NewLine);
                                    }
                                }, null);

                                // Broadcast to all other clients
                                BroadcastToAllClients(formattedMessage, client);
                            }
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

        private void BroadcastToSingleClient(TcpClient client, string message)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send private message: {ex.Message}");
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
            string message = txtbText.Text.Trim();

            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show("Message cannot be empty.");
                return;
            }

            // ✅ Check if a chat is selected
            if (lstUsers.SelectedItem == null)
            {
                MessageBox.Show("Please select a chat first.");
                return;
            }

            if (_client != null && _client.Connected)
            {
                try
                {
                    if (messageType == "Group")
                    {
                        // ✅ Group Message
                        NetworkStream stream = _client.GetStream();
                        using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                        {
                            string formattedMessage = message;
                            writer.WriteLine(formattedMessage);
                            writer.Flush();
                        }

                        // Display on sender's chat window
                        txtbChat.AppendText($"You: {message}" + Environment.NewLine);
                        StoreMessageInLog(_username, "Company Group", message);
                    }
                    else if (messageType == "Private")
                    {
                        // ✅ Private Message
                        string targetUser = lstUsers.SelectedItem.ToString();
                        SendPrivateMessage(targetUser, message);
                    }

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



        private void lstUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstUsers.SelectedItem != null)
            {
                SaveChatHistory(); // Save current chat window
                _currentChat = lstUsers.SelectedItem.ToString();
                LoadChatHistory(_currentChat); // Load the new chat

                // Set the message type
                messageType = _currentChat == "Company Group" ? "Group" : "Private";
            }
        }



        private void SendPrivateMessage(string targetUser, string message)
        {
            try
            {
                NetworkStream stream = _client.GetStream();
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    string formattedMessage = $"PRIVATE::{_username}::{targetUser}::{message}";
                    writer.WriteLine(formattedMessage);
                    writer.Flush();
                }

                // Display on sender's chat window
                txtbChat.AppendText($"You (Private to {targetUser}): {message}" + Environment.NewLine);
                StoreMessageInLog(_username, targetUser, message);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send private message: {ex.Message}");
            }
        }


        private void StoreMessageInLog(string sender, string receiver, string message)
        {
            try
            {
                string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");

                if (!Directory.Exists(documentsPath))
                {
                    Directory.CreateDirectory(documentsPath);
                }

                string fileName = Path.Combine(documentsPath, $"{sender}_to_{receiver}.txt");

                using (StreamWriter writer = new StreamWriter(fileName, append: true))
                {
                    writer.WriteLine($"{sender}: {message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to store message: {ex.Message}");
            }
        }


        private void ListenForPrivateServerMessages()
        {
            try
            {
                NetworkStream stream = _client.GetStream();

                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (true)
                    {
                        string message = reader.ReadLine();

                        if (!string.IsNullOrEmpty(message) && message.StartsWith("PRIVATE::"))
                        {
                            string[] parts = message.Split(new[] { "::" }, StringSplitOptions.None);
                            string sender = parts[1];
                            string content = parts[2];

                            _syncContext.Post(_ =>
                            {
                                if (_currentChat == sender)
                                {
                                    txtbChat.AppendText($"[Private from {sender}]: {content}" + Environment.NewLine);
                                }
                            }, null);

                            StoreMessageInLog(sender, _username, content);
                        }
                    }
                }
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Private message connection lost: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error receiving private message: " + ex.Message);
            }
        }
        private void LoadChatHistory(string selectedUser)
        {
            txtbChat.Clear(); // Clear the current chat window

            string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");

            string fileName = selectedUser == "Company Group"
                ? Path.Combine(documentsPath, $"{_username}_to_Company Group.txt")
                : Path.Combine(documentsPath, $"{_username}_to_{selectedUser}.txt");

            if (File.Exists(fileName))
            {
                var lines = File.ReadAllLines(fileName);
                foreach (var line in lines)
                {
                    txtbChat.AppendText(line + Environment.NewLine);
                }
            }
        }

        private void SaveChatHistory()
        {
            if (_currentChat == null) return;

            string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");

            if (!Directory.Exists(documentsPath))
            {
                Directory.CreateDirectory(documentsPath);
            }

            string fileName = _currentChat == "Company Group"
                ? Path.Combine(documentsPath, $"{_username}_to_Company Group.txt")
                : Path.Combine(documentsPath, $"{_username}_to_{_currentChat}.txt");

            File.WriteAllText(fileName, txtbChat.Text);
        }

    }
}