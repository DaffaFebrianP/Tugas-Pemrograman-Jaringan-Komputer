using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClientChat
{
    public partial class MainWindow : Window
    {
        private TcpClient? client;
        private StreamWriter? writer;
        private StreamReader? reader;
        private string username = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpText.Text.Trim();
            if (!int.TryParse(PortText.Text, out int port))
            {
                AppendSystem("Invalid port.");
                return;
            }

            username = UsernameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                AppendSystem("Invalid username.");
                return;
            }

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(ip, port);
                var ns = client.GetStream();
                reader = new StreamReader(ns, Encoding.UTF8);
                writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };

                // kirim pesan join
                var join = new Message { Type = "join", From = username, Text = $"{username} joined" };
                await writer.WriteLineAsync(JsonSerializer.Serialize(join));

                // mulai loop baca pesan
                _ = Task.Run(ReceiveLoop);

                AppendSystem("Connected to server.");
                ConnectBtn.IsEnabled = false;
                SendBtn.IsEnabled = true;
                DisconnectBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AppendSystem("Connection failed: " + ex.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (client != null && client.Connected && reader != null)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null) break;

                    Message? msg = null;
                    try { msg = JsonSerializer.Deserialize<Message>(line); } catch { }

                    if (msg == null) continue;

                    Dispatcher.Invoke(() =>
                    {
                        if (msg.Type == "sys")
                        {
                            AppendSystem(msg.Text ?? "");
                        }
                        else if (msg.Type == "msg")
                        {
                            AppendChat($"{msg.From}: {msg.Text}");
                        }
                        else if (msg.Type == "pm")
                        {
                            AppendChat($"(PM) {msg.From} -> {msg.To}: {msg.Text}");
                        }
                        else if (msg.Type == "userlist" && msg.Users != null)
                        {
                            // update daftar user online di ListBox
                            UserListBox.Items.Clear();
                            foreach (var u in msg.Users)
                                UserListBox.Items.Add(u);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AppendSystem("Receive error: " + ex.Message));
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    AppendSystem("Disconnected.");
                    ConnectBtn.IsEnabled = true;
                    SendBtn.IsEnabled = false;
                    DisconnectBtn.IsEnabled = false;
                    UserListBox.Items.Clear(); // kosongkan daftar user saat disconnect
                });
            }
        }

        private void AppendChat(string text)
        {
            ChatPanel.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            });
        }

        private void AppendSystem(string text)
        {
            ChatPanel.Children.Add(new TextBlock
            {
                Text = "[SYSTEM] " + text,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 2, 0, 2)
            });
        }

        private async void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void MessageText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await SendMessage();
            }
        }

        private async Task SendMessage()
        {
            if (writer == null) return;
            string text = MessageText.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (text.StartsWith("/w "))
            {
                var parts = text.Substring(3).Trim();
                var idx = parts.IndexOf(' ');
                if (idx > 0)
                {
                    var to = parts.Substring(0, idx);
                    var msgText = parts.Substring(idx + 1);
                    var pm = new Message { Type = "pm", From = username, To = to, Text = msgText };
                    await writer.WriteLineAsync(JsonSerializer.Serialize(pm));
                }
                else
                {
                    AppendSystem("Format PM salah. Gunakan: /w username pesan");
                }
            }
            else
            {
                var msg = new Message { Type = "msg", From = username, Text = text };
                await writer.WriteLineAsync(JsonSerializer.Serialize(msg));
            }

            MessageText.Clear();
        }

        private async void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (writer != null)
                {
                    var leave = new Message { Type = "leave", From = username, Text = $"{username} leaving" };
                    await writer.WriteLineAsync(JsonSerializer.Serialize(leave));
                }
            }
            catch { }

            client?.Close();
            ConnectBtn.IsEnabled = true;
            SendBtn.IsEnabled = false;
            DisconnectBtn.IsEnabled = false;
            UserListBox.Items.Clear();
            AppendSystem("Disconnected.");
        }
    }
}
