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
        public FormChat(TcpClient client, string username)
        {
            InitializeComponent();
            cbUsers.Items.Add("Company Group");
            cbUsers.SelectedIndex = 0;

            _client = client;
            _username = username;
            _isServer = false;

            // Set status to Offline initially
            lblConnectionStatus.Text = "Offline";
            lblConnectionStatus.ForeColor = Color.Red;

            // Start the listener thread for server messages
            _listenerThread = new Thread(ListenForServerMessages)
            {
                IsBackground = true
            };
            _listenerThread.Start();

            txtbChat.AppendText("You joined the group chat." + Environment.NewLine);

            this.FormClosing += FormChat_FormClosing;

            // Start monitoring connection status
            StartConnectionMonitor();
        }

        private void StartConnectionMonitor()
        {
            Thread connectionMonitor = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(3000); // Check every 3 seconds

                    if (_client != null && _client.Connected)
                    {
                        if (CheckServerAvailability())
                        {
                            // Server is still reachable
                            Invoke((MethodInvoker)delegate
                            {
                                lblConnectionStatus.Text = "Online";
                                lblConnectionStatus.ForeColor = Color.LimeGreen;
                            });
                        }
                        else
                        {
                            // Server is not reachable
                            Invoke((MethodInvoker)delegate
                            {
                                lblConnectionStatus.Text = "Offline";
                                lblConnectionStatus.ForeColor = Color.Red;
                            });
                        }
                    }
                    else
                    {
                        // No connection
                        Invoke((MethodInvoker)delegate
                        {
                            lblConnectionStatus.Text = "Offline";
                            lblConnectionStatus.ForeColor = Color.Red;
                        });
                    }
                }
            });

            connectionMonitor.IsBackground = true;
            connectionMonitor.Start();
        }

        private bool CheckServerAvailability()
        {
            try
            {
                if (_client != null && _client.Connected)
                {
                    // Send a small ping packet
                    NetworkStream stream = _client.GetStream();
                    byte[] ping = Encoding.UTF8.GetBytes("PING");
                    stream.Write(ping, 0, ping.Length);

                    // Expect a pong response
                    byte[] buffer = new byte[1024];
                    stream.ReadTimeout = 1000;
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    return response == "PONG";
                }
                return false;
            }
            catch
            {
                return false;
            }
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

                        if (message == "CONFIRMED")
                        {
                            Invoke((MethodInvoker)delegate
                            {
                                lblConnectionStatus.Text = "Online";
                                lblConnectionStatus.ForeColor = Color.LimeGreen;
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
                // Set status to Hosting (Light Blue)
                lblConnectionStatus.Text = "Hosting";
                lblConnectionStatus.ForeColor = Color.Blue;
            }

            this.FormClosing += FormChat_FormClosing;
        }

        private void StartServer()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _serverPort); // Accepts any IP
                _listener.Start();
                _serverRunning = true;

                _listenerThread = new Thread(ListenForClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();

                // ✅ Start the server status listener on a separate thread
                Thread statusThread = new Thread(ListenForServerStatus)
                {
                    IsBackground = true
                };
                statusThread.Start();

                MessageBox.Show($"Server started on Port: {_serverPort}");
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

                        // ✅ Immediately send confirmation to the client
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
                byte[] buffer = new byte[1024];

                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        Invoke((MethodInvoker)delegate
                        {
                            txtbChat.AppendText(message + Environment.NewLine);
                        });

                        BroadcastMessage(message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error receiving message from client: " + ex.Message);
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
                    byte[] response = Encoding.UTF8.GetBytes("1"); // Send "1" as server is running
                    stream.Write(response, 0, response.Length);
                    statusClient.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error with server status check: " + ex.Message);
            }
        }


        private void BroadcastMessage(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            foreach (var client in _connectedClients.ToList())
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

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = $"{_username}: {txtbText.Text}";
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            txtbChat.AppendText($"Me: {txtbText.Text}" + Environment.NewLine);
            txtbText.Clear();

            BroadcastMessage(message);
        }
    }
}
