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
        private readonly TcpClient _client;
        private TcpListener? _listener; // Nullable
        private Thread? _listenerThread; // Nullable
        private readonly string _username;
        private readonly bool _isServer;
        private readonly int _serverPort;
        private bool _serverRunning = false;

        private readonly List<TcpClient> _connectedClients = new();
        private readonly Dictionary<TcpClient, string> _clientUsernames = new();

        // Client constructor
        public FormChat(TcpClient client, string username)
        {
            InitializeComponent();
            cbUsers.Items.Add("Company Group");
            cbUsers.SelectedIndex = 0;

            _client = client;
            _username = username;
            _isServer = false;

            _listenerThread = new Thread(ListenForServerMessages)
            {
                IsBackground = true
            };
            _listenerThread.Start();

            // Display the welcome message
            txtbChat.AppendText("You joined the group chat." + Environment.NewLine);

            // Register the FormClosing event to handle disconnect
            this.FormClosing += FormChat_FormClosing;
        }
        private void FormChat_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                if (_client != null && _client.Connected)
                {
                    _client.Close();
                }
                if (_listener != null && _serverRunning)
                {
                    _listener.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while disconnecting: {ex.Message}");
            }
            finally
            {
                Application.Exit(); // Close the application entirely
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
                        if (IsHandleCreated) // ✅ Check if the form handle is created
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
                if (IsHandleCreated) // ✅ Avoid invoking if not created
                {
                    Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("Error receiving message: " + ex.Message);
                    });
                }
            }
        }

        // Host constructor
        public FormChat(bool isServer, int port, string username)
        {
            InitializeComponent();
            cbUsers.Items.Add("Company Group");
            cbUsers.SelectedIndex = 0;

            _isServer = isServer;
            _serverPort = port;
            _username = username;

            if (_isServer)
            {
                StartServer();
            }

            // Register the FormClosing event to handle disconnect
            this.FormClosing += FormChat_FormClosing;
        }

        private void StartServer()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Parse("192.168.0.22"), _serverPort);
                _listener.Start();
                _serverRunning = true;

                _listenerThread = new Thread(ListenForClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();

                MessageBox.Show($"Server started on IP: 192.168.0.22, Port: {_serverPort}");
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
                    TcpClient client = _listener!.AcceptTcpClient();
                    _connectedClients.Add(client);

                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string username = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    _clientUsernames[client] = username;

                    // Add to the user list in the UI
                    Invoke((MethodInvoker)(() =>
                    {
                        cbUsers.Items.Add(username);
                        txtbChat.AppendText($"{username} has joined the chat." + Environment.NewLine);
                    }));

                    // Start a new thread to listen for messages from this client
                    Thread clientThread = new Thread(() => ListenForClientMessages(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();

                    BroadcastUserList();
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
                byte[] buffer = new byte[1024];

                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // Show the message in the host's own chat window
                        Invoke((MethodInvoker)delegate
                        {
                            txtbChat.AppendText(message + Environment.NewLine);
                        });

                        // Broadcast the message to all other clients
                        BroadcastMessage(message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error receiving message from client: " + ex.Message);
            }
        }

        private void BroadcastMessage(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            foreach (var client in _connectedClients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to send message to client: " + ex.Message);
                }
            }

            // Also display the message in the host's window
            Invoke((MethodInvoker)(() =>
            {
                txtbChat.AppendText(message + Environment.NewLine);
            }));
        }


        private void BroadcastUserList()
        {
            string userList = "USER_LIST:" + string.Join(",", _clientUsernames.Values);
            byte[] message = Encoding.UTF8.GetBytes(userList);

            foreach (var client in _connectedClients.ToList()) // Make a copy to avoid modification during iteration
            {
                try
                {
                    if (client.Connected) // ✅ Check if the socket is still connected
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(message, 0, message.Length);
                    }
                    else
                    {
                        // If the client is disconnected, remove it from the list
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

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = $"{_username}: {txtbText.Text}";
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            foreach (var client in _connectedClients.ToList()) // ✅ ToList() to avoid modification errors
            {
                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(buffer, 0, buffer.Length);
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

            txtbChat.AppendText($"Me: {txtbText.Text}" + Environment.NewLine);
            txtbText.Clear();
        }
    }
}
