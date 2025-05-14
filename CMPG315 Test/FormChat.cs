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
        private readonly string _serverIP;
        private readonly List<TcpClient> _connectedClients = new();
        private readonly Dictionary<string, TcpClient> _clientConnections = new();
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        private string _currentChat = "Company Group";

        public FormChat(TcpClient client, string username, string serverIP, int serverPort, bool isOnline)
        {
            InitializeComponent();
            this.Text = "Company Group Chat";
            _client = client;
            _username = username;
            _serverIP = serverIP;
            _serverPort = serverPort;
            _isServer = false;

            lblConnectionStatus.Text = isOnline ? "Online" : "Offline";
            lblConnectionStatus.ForeColor = isOnline ? Color.LimeGreen : Color.Red;

            _listenerThread = new Thread(ListenForServerMessages) { IsBackground = true };
            _listenerThread.Start();
            this.FormClosing += FormChat_FormClosing;


        }

        public FormChat(bool isServer, int port, string username, bool isOnline)
        {
            InitializeComponent();
            this.Text = "Company Group Chat";

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
                lblConnectionStatus.Text = isOnline ? "Online" : "Offline";
                lblConnectionStatus.ForeColor = isOnline ? Color.LimeGreen : Color.Red;
            }

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
                string groupChatFile = Path.Combine(documentsPath, "Company_Group.txt");

                if (File.Exists(groupChatFile))
                    File.Delete(groupChatFile);
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
                                        lstUsers.Items.Add(username);
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
                                    foreach (var user in users)
                                    {
                                        if (!string.IsNullOrWhiteSpace(user) && user != _username && !lstUsers.Items.Contains(user))
                                            lstUsers.Items.Add(user);
                                    }
                                }, null);
                            }
                            else
                            {
                                _syncContext.Post(_ =>
{
                                    txtbChat.AppendText(message + Environment.NewLine);

                                    if (ShouldNotify())
                                    {
                                        notification.BalloonTipTitle = "New Message";
                                        notification.BalloonTipText = message.Length > 100 ? message.Substring(0, 100) + "..." : message;
                                        notification.ShowBalloonTip(3000); // duration in ms
                                    }
                                }, null);
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Server forcibly closed the connection
                _syncContext.Post(_ =>
                {
                    lblConnectionStatus.Text = "Offline";
                    lblConnectionStatus.ForeColor = Color.Red;
                }, null);
            }
            catch (Exception ex)
            {
                _syncContext.Post(_ =>
                {
                    lblConnectionStatus.Text = "Offline";
                    lblConnectionStatus.ForeColor = Color.Red;
                    // Optionally log to file or silent fail, no MessageBox
                }, null);
            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtbText.Text.Trim();
            string timestamp = DateTime.Now.ToString("HH:mm");
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (_client != null && _client.Connected)
            {
                try
                {
                    NetworkStream stream = _client.GetStream();
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        string formattedMessage = $"GROUP::{_username}::{message}";
                        writer.WriteLine(formattedMessage);
                        writer.Flush();
                    }

                    txtbChat.AppendText($"[{timestamp} | {_username}]: {message}\n");
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
                    string[] parts = message.Split(new[] { "::" }, StringSplitOptions.None);
                    string sender = parts[1];
                    string content = parts[2];
                    string timestamp = DateTime.Now.ToString("HH:mm");

                    string formatted = $"[{timestamp} | {sender}]: {content}";

                    string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");
                    string groupChatFile = Path.Combine(documentsPath, "Company_Group.txt");
                    if (!Directory.Exists(documentsPath))
                        Directory.CreateDirectory(documentsPath);

                    using (StreamWriter writer = new StreamWriter(groupChatFile, append: true))
                    {
                        writer.WriteLine(formatted);
                    }

                    ServerBroadcastMessage(formatted, senderClient);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error handling message: {ex.Message}");
            }
        }

        private void ServerBroadcastMessage(string message, TcpClient senderClient)
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
                        MessageBox.Show($"Failed to send group message: {ex.Message}");
                    }
                }
            }
        }

        private void lstUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
 
        }

        private void LoadChatHistory(string selectedUser)
        {
            try
            {
                txtbChat.Clear();

                if (selectedUser == "Company Group")
                {
                    string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");
                    string groupChatFile = Path.Combine(documentsPath, "Company_Group.txt");

                    if (File.Exists(groupChatFile))
                    {
                        string[] lines = File.ReadAllLines(groupChatFile);
                        foreach (string line in lines)
                        {
                            if (!line.Contains("Chat Log for Group Chat") && !line.Contains("========================================="))
                            {
                                if (line.StartsWith($"[{_username}]"))
                                {
                                    txtbChat.SelectionStart = txtbChat.TextLength;
                                    txtbChat.SelectionLength = 0;
                                    txtbChat.SelectionFont = new Font(txtbChat.Font, FontStyle.Bold);
                                    txtbChat.AppendText(line + Environment.NewLine);
                                }
                                else
                                {
                                    txtbChat.SelectionStart = txtbChat.TextLength;
                                    txtbChat.SelectionLength = 0;
                                    txtbChat.SelectionFont = new Font(txtbChat.Font, FontStyle.Regular);
                                    txtbChat.AppendText(line + Environment.NewLine);
                                }
                            }
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
            LoadChatHistory(_currentChat);
        }

        private void StartServer()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _serverPort);
                _listener.Start();
                _serverRunning = true;

                _listenerThread = new Thread(ListenForClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();

                Thread statusThread = new Thread(ListenForServerStatus)
                {
                    IsBackground = true
                };
                statusThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting server: " + ex.Message);
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

                    if (!_clientConnections.ContainsKey(username))
                    {
                        _clientConnections[username] = client;
                    }

                    string timestamp = DateTime.Now.ToString("HH:mm");
                    string joinMessage = $"{username} has joined the chat.";

                    _syncContext.Post(_ =>
                    {
                        if (!lstUsers.Items.Contains(username))
                        {
                            lstUsers.Items.Add(username);
                            txtbChat.AppendText(joinMessage + Environment.NewLine);
                        }
                    }, null);

                    string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");
                    string groupChatFile = Path.Combine(documentsPath, "Company_Group.txt");
                    if (!Directory.Exists(documentsPath))
                        Directory.CreateDirectory(documentsPath);

                    using (StreamWriter writer = new StreamWriter(groupChatFile, append: true))
                    {
                        writer.WriteLine(joinMessage);
                    }
                    BroadcastToAllClients($"USER_JOINED:{username}");
                    SendFullUserList(client);


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
                    byte[] response = Encoding.UTF8.GetBytes("1");
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

        private void SendFullUserList(TcpClient client)
        {
            try
            {
                var userList = _clientConnections.Keys.ToList();

                string clientUsername = _clientConnections.FirstOrDefault(x => x.Value == client).Key;
                if (userList.Count == 1 && userList[0] == clientUsername)
                    return;

                userList.Remove(clientUsername);

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

                            ServerHandleMessage(message, client);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var user = _clientConnections.FirstOrDefault(x => x.Value == client);
                HandleClientDisconnect(client, !string.IsNullOrEmpty(user.Key) ? user.Key : "Unknown");
                MessageBox.Show($"Error receiving message from client: {ex.Message}");
            }
        }

        private void HandleClientDisconnect(TcpClient client, string username)
        {
            var user = _clientConnections.FirstOrDefault(x => x.Value == client);
            if (!string.IsNullOrEmpty(user.Key))
            {
                _clientConnections.Remove(user.Key);
                _connectedClients.Remove(client);

                string timestamp = DateTime.Now.ToString("HH:mm");
                string leftMessage = $"{username} has left the chat.";

                _syncContext.Post(_ =>
                {
                    if (lstUsers.Items.Contains(username))
                    {
                        lstUsers.Items.Remove(username);
                        txtbChat.AppendText(leftMessage + Environment.NewLine);
                    }
                }, null);

                string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CMPG315_Test");
                string groupChatFile = Path.Combine(documentsPath, "Company_Group.txt");
                if (!Directory.Exists(documentsPath))
                    Directory.CreateDirectory(documentsPath);

                using (StreamWriter writer = new StreamWriter(groupChatFile, append: true))
                {
                    writer.WriteLine(leftMessage);
                }

                BroadcastToAllClients($"USER_LEFT:{username}");
            }
        }

        private void txtbChat_TextChanged(object sender, EventArgs e)
        {

        }

        private bool ShouldNotify()
        {
            return this.WindowState == FormWindowState.Minimized || !this.Focused;
        }
    }
}
