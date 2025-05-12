using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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
                            else if (message.StartsWith("PRIVATE_LOG::"))
                            {
                                string logMessage = message.Replace("PRIVATE_LOG::", "");
                                _syncContext.Post(_=>
{
                                    txtbChat.AppendText(logMessage + Environment.NewLine);
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

                    // ✅ Create chat files for the new user with all existing users
                    CreateUserChatFiles(username);

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

        private void CreateUserChatFiles(string newUser)
        {
            string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");

            if (!Directory.Exists(documentsPath))
            {
                Directory.CreateDirectory(documentsPath);
            }

            // ✅ Create a single group chat log file
            string groupChatFile = Path.Combine(documentsPath, "Company_Group.txt");
            if (!File.Exists(groupChatFile))
            {
                using (StreamWriter writer = new StreamWriter(groupChatFile))
                {
                    writer.WriteLine("Chat Log for Group Chat - Company Group");
                    writer.WriteLine("=========================================");
                }
            }

            // ✅ Create private chat logs only for individual users
            foreach (var existingUser in _clientUsernames.Values)
            {
                if (existingUser != newUser)
                {
                    // Determine the chat file name
                    string privateChatFile1 = Path.Combine(documentsPath, $"{newUser}_to_{existingUser}.txt");
                    string privateChatFile2 = Path.Combine(documentsPath, $"{existingUser}_to_{newUser}.txt");

                    // Create the chat file if neither exists
                    if (!File.Exists(privateChatFile1) && !File.Exists(privateChatFile2))
                    {
                        using (StreamWriter writer = new StreamWriter(privateChatFile1))
                        {
                            writer.WriteLine($"Chat Log between {newUser} and {existingUser}");
                            writer.WriteLine("=========================================");
                        }
                    }
                }
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
                            // ✅ Check for client disconnect
                            if (message.StartsWith("DISCONNECTED::"))
                            {
                                string username = message.Split("::")[1];
                                HandleClientDisconnect(client, username);
                                continue;
                            }

                            // ✅ Pass the message to the server handler
                            ServerHandleMessage(message, client);
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

        private void HandleClientDisconnect(TcpClient client, string username)
        {
            if (_clientUsernames.ContainsKey(client))
            {
                _clientUsernames.Remove(client);
                _connectedClients.Remove(client);

                _syncContext.Post(_ =>
                {
                    if (lstUsers.Items.Contains(username))
                    {
                        lstUsers.Items.Remove(username);
                        txtbChat.AppendText($"{username} has left the chat." + Environment.NewLine);
                    }
                }, null);

                BroadcastToAllClients($"USER_LEFT:{username}");

                // ✅ Remove the user's perspective of others, keep logs clean
                DeleteUserFiles(username);
            }
        }

        private void DeleteUserFiles(string username)
        {
            string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");

            if (Directory.Exists(documentsPath))
            {
                var files = Directory.GetFiles(documentsPath, $"{username}_to_*.txt");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete file: {file}. Error: {ex.Message}");
                    }
                }
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
                    NetworkStream stream = _client.GetStream();
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        string formattedMessage;

                        // ✅ Determine if it's a Group or Private Message
                        if (messageType == "Group")
                        {
                            // Format for Group Message
                            formattedMessage = $"GROUP::{_username}::{message}";
                        }
                        else
                        {
                            // Format for Private Message
                            string targetUser = lstUsers.SelectedItem.ToString();
                            formattedMessage = $"PRIVATE::{_username}::{targetUser}::{message}";
                        }

                        // ✅ Send the message to the server
                        writer.WriteLine(formattedMessage);
                        writer.Flush();
                    }

                    // ✅ Display on sender's chat window
                    txtbChat.AppendText($"[{_username}]: {message}" + Environment.NewLine);

                    // ✅ Clear the text box after sending
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

        private void ServerHandleMessage(string message, TcpClient senderClient)
        {
            try
            {
                if (message.StartsWith("GROUP::"))
                {
                    // Message format: GROUP::Sender::Message
                    string[] parts = message.Split(new[] { "::" }, StringSplitOptions.None);
                    string sender = parts[1];
                    string content = parts[2];

                    // ✅ Save the group message
                    ServerSaveMessage("Company Group", sender, content);

                    // ✅ Broadcast to all clients
                    ServerBroadcastMessage($"[{sender}]: {content}", senderClient, true);

                }
                else if (message.StartsWith("PRIVATE::"))
                {
                    // Message format: PRIVATE::Sender::Receiver::Message
                    string[] parts = message.Split(new[] { "::" }, StringSplitOptions.None);
                    string sender = parts[1];
                    string receiver = parts[2];
                    string content = parts[3];

                    // ✅ Save the private message
                    ServerSaveMessage(receiver, sender, content);

                    // ✅ Broadcast to the specific client
                    var targetClient = _connectedClients.FirstOrDefault(c => _clientUsernames[c] == receiver);
                    if (targetClient != null)
                    {
                        ServerBroadcastMessage($"[Private from {sender}]: {content}", targetClient, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error handling message: {ex.Message}");
            }
        }

        private void ServerSaveMessage(string receiver, string sender, string message)
        {
            try
            {
                string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");

                if (!Directory.Exists(documentsPath))
                {
                    Directory.CreateDirectory(documentsPath);
                }

                if (receiver == "Company Group")
                {
                    // ✅ Save to the Group Chat log
                    string groupChatFile = Path.Combine(documentsPath, "Company_Group.txt");

                    // ✅ Append the message
                    using (StreamWriter writer = new StreamWriter(groupChatFile, append: true))
                    {
                        writer.WriteLine($"[{sender}]: {message}");
                    }
                }
                else
                {
                    // ✅ For private messages, check which file exists or create one
                    string senderToReceiverFile = Path.Combine(documentsPath, $"{sender}to{receiver}.txt");
                    string receiverToSenderFile = Path.Combine(documentsPath, $"{receiver}to{sender}.txt");

                    string finalFilePath = File.Exists(senderToReceiverFile) ? senderToReceiverFile : receiverToSenderFile;

                    // ✅ If neither exists, create one with the default name
                    if (finalFilePath == receiverToSenderFile && !File.Exists(receiverToSenderFile))
                    {
                        finalFilePath = senderToReceiverFile;
                    }

                    using (StreamWriter writer = new StreamWriter(finalFilePath, append: true))
                    {
                        writer.WriteLine($"{sender}: {message}");
                    }
                }
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"IO Error while storing message: {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                MessageBox.Show($"Access Denied: {uaEx.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to store message: {ex.Message}");
            }
        }

        private void ServerBroadcastMessage(string message, TcpClient senderClient, bool isGroup)
        {
            lock (_connectedClients)
            {
                if (isGroup)
                {
                    // Broadcast to all clients except the sender
                    foreach (var client in _connectedClients.ToList())
                    {
                        try
                        {
                            if (client.Connected && client != senderClient) // ✅ Exclude the sender
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
                else
                {
                    // Private Message: Only to the specific client
                    try
                    {
                        if (senderClient.Connected)
                        {
                            NetworkStream stream = senderClient.GetStream();
                            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                writer.WriteLine(message);
                                writer.Flush();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to send private message: {ex.Message}");
                    }
                }
            }
        }

        private void lstUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstUsers.SelectedItem != null)
            {
                // ✅ Set the current chat to the selected item
                _currentChat = lstUsers.SelectedItem.ToString();
                messageType = _currentChat == "Company Group" ? "Group" : "Private";
                lblChatSelected.Text = lstUsers.SelectedItem.ToString();

                // ✅ Clear the current chat window
                txtbChat.Clear();

                // ✅ Load the history of the newly selected chat
                LoadChatHistory(_currentChat);
            }
        }

        private void LoadChatHistory(string selectedUser)
        {
            try
            {
                txtbChat.Clear();

                string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");

                if (selectedUser == "Company Group")
                {
                    string groupChatFile = Path.Combine(documentsPath, "Company_Group.txt");

                    if (File.Exists(groupChatFile))
                    {
                        string[] lines = File.ReadAllLines(groupChatFile);
                        foreach (string line in lines)
                        {
                            if (!line.Contains("Chat Log for Group Chat") && !line.Contains("========================================="))
                            {
                                txtbChat.AppendText(line + Environment.NewLine);
                            }
                        }
                    }
                }
                else
                {
                    // ✅ Private Chat
                    string chatFile1 = Path.Combine(documentsPath, $"{_username}_to_{selectedUser}.txt");
                    string chatFile2 = Path.Combine(documentsPath, $"{selectedUser}_to_{_username}.txt");

                    string finalFilePath = File.Exists(chatFile1) ? chatFile1 : chatFile2;

                    if (File.Exists(finalFilePath))
                    {
                        string[] lines = File.ReadAllLines(finalFilePath);
                        foreach (string line in lines)
                        {
                            txtbChat.AppendText(line + Environment.NewLine);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load chat history: {ex.Message}");
            }
        }

        private void FormChat_Load(object sender, EventArgs e)
        {
            // Ensure Company Group is in the list and selected
            if (!lstUsers.Items.Contains("Company Group"))
            {
                lstUsers.Items.Add("Company Group");
            }
            lstUsers.SelectedIndex = 0;  // Select "Company Group" when the form loads

            // Load the chat history for the "Company Group"
            _currentChat = "Company Group";
            LoadChatHistory(_currentChat);
        }
    }
}