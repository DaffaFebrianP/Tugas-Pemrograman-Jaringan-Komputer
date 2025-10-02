using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

// timer buat waktu typing
using Timer = System.Timers.Timer;

namespace ClientChat
{
    public partial class MainWindow : Window
    {
        private TcpClient? client;
        private StreamWriter? writer;
        private StreamReader? reader;
        private string username = "";

        // indikator typing
        private bool isTyping = false;
        private readonly Timer typingTimer;

        // file lokal chat
        private const string HistoryFileName = "chat_history.json";
        private readonly string HistoryFilePath = Path.Combine(
            AppContext.BaseDirectory,
            HistoryFileName
        );

        public MainWindow()
        {
            InitializeComponent();
            // typing timer
            typingTimer = new Timer(2000);
            typingTimer.AutoReset = false;
            typingTimer.Elapsed += async (s, e) =>
            {
                await Dispatcher.InvokeAsync(async () => await SendTypingAsync(false));
            };

            //muat local chat
            LoadChatHistory();
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
                            // disconnect kalau username sama
                            if (msg.Text == "username_taken" || msg.Text == "invalid_join")
                            {
                                AppendSystem($"ERROR: {msg.Text}. Disconnecting...");
                                client?.Close(); // Paksa disconnect
                            }
                            else
                            {
                                AppendSystem(msg.Text ?? "");
                            }
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
                            // update listbox
                            UserListBox.Items.Clear();
                            foreach (var u in msg.Users)
                                UserListBox.Items.Add(u);
                        }
                        else if (msg.Type == "typing" && msg.From != username) // menghindari menampilkan tulisan typing diri sendiri
                        {
                            // tulisan typing
                            if (msg.IsTyping == true)
                                TypingIndicator.Text = $"{msg.From} sedang mengetik...";
                            else
                                TypingIndicator.Text = "";
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
                    // nyimpan file kalau server mati
                    SaveChatHistory();

                    AppendSystem("Disconnected.");
                    ConnectBtn.IsEnabled = true;
                    SendBtn.IsEnabled = false;
                    DisconnectBtn.IsEnabled = false;
                    UserListBox.Items.Clear();
                    TypingIndicator.Text = "";
                    client?.Close();
                    client = null;
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
            // Auto scroll ke bawah
            (ChatPanel.Parent as ScrollViewer)?.ScrollToEnd();
        }

        private void AppendSystem(string text)
        {
            ChatPanel.Children.Add(new TextBlock
            {
                Text = "[SYSTEM] " + text,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 2, 0, 2)
            });
            // Auto scroll ke bawah
            (ChatPanel.Parent as ScrollViewer)?.ScrollToEnd();
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

        // deteksi mengetik
        private void MessageText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (client == null || !client.Connected) return; // Abaikan jika belum terhubung

            if (!isTyping)
            {
                _ = SendTypingAsync(true);
            }
            // update ngetik
            typingTimer.Stop();
            typingTimer.Start();
        }

        private async Task SendMessage()
        {
            if (writer == null) return;
            string text = MessageText.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // private message
            if (text.StartsWith("@"))
            {
                var parts = text.Substring(1).Trim();
                var idx = parts.IndexOf(' ');

                if (idx > 0)
                {
                    var to = parts.Substring(0, idx);
                    var msgText = parts.Substring(idx + 1).Trim();

                    if (!string.IsNullOrWhiteSpace(msgText))
                    {
                        var pm = new Message { Type = "pm", From = username, To = to, Text = msgText };
                        await writer.WriteLineAsync(JsonSerializer.Serialize(pm));
                    }
                    else
                    {
                        AppendSystem("Pesan PM kosong. Gunakan: @username pesan");
                    }
                }
                else
                {
                    AppendSystem("Format PM salah. Gunakan: @username pesan");
                }
            }
            // chat umum
            else
            {
                var msg = new Message { Type = "msg", From = username, Text = text };
                await writer.WriteLineAsync(JsonSerializer.Serialize(msg));
            }

            MessageText.Clear();
            // mematikan tulisan mengetik ketika pesan dikirim
            typingTimer.Stop();
            await SendTypingAsync(false);
        }

        private async void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (writer != null && client != null && client.Connected)
                {
                    var leave = new Message { Type = "leave", From = username, Text = $"{username} leaving" };
                    await writer.WriteLineAsync(JsonSerializer.Serialize(leave));
                }
            }
            catch { }
            client?.Close();
        }

        // mengirim tulisan mengetik
        private async Task SendTypingAsync(bool typing)
        {
            if (writer == null || typing == isTyping) return; // Hanya kirim jika status berubah

            isTyping = typing;
            var msg = new Message
            {
                Type = "typing",
                From = username,
                IsTyping = typing
            };
            try
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(msg));
            }
            catch { }
        }

        // simpan dan muat chat

        private void SaveChatHistory()
        {
            var history = new List<Message>();

            foreach (var element in ChatPanel.Children)
            {
                if (element is TextBlock tb)
                {
                    string fullText = tb.Text;
                    string type = "msg";
                    string from = "Unknown";
                    string text = fullText;

                    // melewati pesan status yang tidak perlu disimpan
                    if (fullText.Contains("Disconnected.") || fullText.Contains("Connected to server.") || fullText.Contains("Chat history loaded") || fullText.Contains("Gagal menyimpan history") || fullText.Contains("Gagal memuat history") || fullText.Contains("ERROR:")) continue;

                    if (fullText.StartsWith("[SYSTEM]"))
                    {
                        type = "sys";
                        text = fullText.Substring("[SYSTEM] ".Length).Trim();
                        from = "System";
                    }
                    else if (fullText.StartsWith("(PM)"))
                    {
                        // Pesan PM: (PM) From -> To: Text
                        type = "msg";
                        if (fullText.Contains(" -> ") && fullText.Contains(": "))
                        {
                            var parts = fullText.Substring("(PM) ".Length).Split(new[] { " -> " }, 2, StringSplitOptions.None);
                            if (parts.Length == 2)
                            {
                                from = parts[0];
                                text = fullText.Substring(fullText.IndexOf(": ") + 2).Trim();
                                text = $"(PM) {text}";
                            }
                        }
                    }
                    else if (fullText.Contains(": "))
                    {
                        // Pesan chat biasa: Username: Pesan
                        var parts = fullText.Split(new[] { ": " }, 2, StringSplitOptions.None);
                        if (parts.Length == 2)
                        {
                            from = parts[0].Trim();
                            text = parts[1].Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(text))
                    {
                        history.Add(new Message { Type = type, From = from, Text = text, Ts = 0 });
                    }
                }
            }

            try
            {
                string json = JsonSerializer.Serialize(history);
                File.WriteAllText(HistoryFilePath, json);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AppendSystem($"[ERROR SAVE] Gagal menyimpan history: {ex.Message}"));
            }
        }

        private void LoadChatHistory()
        {
            if (!File.Exists(HistoryFilePath)) return;

            try
            {
                string json = File.ReadAllText(HistoryFilePath);
                var history = JsonSerializer.Deserialize<List<Message>>(json);

                if (history != null)
                {
                    ChatPanel.Children.Clear();
                    foreach (var msg in history)
                    {
                        if (msg.Type == "sys")
                        {
                            AppendSystem(msg.Text ?? "");
                        }
                        else if (msg.Type == "msg")
                        {
                            // Pesan chat pribadi yang disimpan dengan tanda PM di awal teks
                            AppendChat($"{msg.From}: {msg.Text}");
                        }
                    }
                    AppendSystem($"Chat history loaded from {HistoryFileName}.");
                }
            }
            catch (Exception ex)
            {
                AppendSystem($"[ERROR LOAD] Gagal memuat history: {ex.Message}");
            }
        }
    }
}
