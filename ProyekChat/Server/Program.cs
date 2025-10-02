using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    // nyimpan info client connect
    private static ConcurrentDictionary<string, ClientInfo> clients = new();

    static async Task Main(string[] args)
    {
        // kode port
        int port = 9000;
        if (args.Length >= 1 && int.TryParse(args[0], out var p)) port = p;

        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[SERVER] Listening on port {port} ...");

        var cts = new CancellationTokenSource();

        // koneksi baru
        _ = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var tcp = await listener.AcceptTcpClientAsync(cts.Token);
                    // Menangani klien di task terpisah
                    _ = HandleClientAsync(tcp);
                }
                catch (OperationCanceledException)
                {
                    // error
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SERVER] Accept error: " + ex.Message);
                }
            }
        });

        Console.WriteLine("[SERVER] Press Enter to stop server...");
        Console.ReadLine();

        // Mematikan server
        cts.Cancel();
        listener.Stop();
        Console.WriteLine("[SERVER] Stopped.");
    }

    private static async Task HandleClientAsync(TcpClient tcp)
    {
        var endPoint = tcp.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"[CONN] New connection from {endPoint}");

        // Menggunakan using declaration untuk memastikan resource ditutup
        using var ns = tcp.GetStream();
        using var reader = new StreamReader(ns, Encoding.UTF8);
        using var writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };

        string? username = null;

        try
        {
            // pesan join pertama
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                tcp.Close();
                return;
            }

            var joinMsg = JsonSerializer.Deserialize<Message>(line);

            //  pesan join
            if (joinMsg == null || joinMsg.Type != "join" || string.IsNullOrWhiteSpace(joinMsg.From))
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new Message { Type = "sys", Text = "invalid_join" }));
                tcp.Close();
                return;
            }

            username = joinMsg.From.Trim();

            // Cek username sama
            if (clients.ContainsKey(username))
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new Message { Type = "sys", Text = "username_taken" }));
                tcp.Close();
                Console.WriteLine($"[REJECT] Username taken: {username} from {endPoint}");
                return;
            }

            // Tambahkan klien ke daftar listbox
            var clientInfo = new ClientInfo { Username = username, Tcp = tcp, Writer = writer };
            clients.TryAdd(username, clientInfo);

            Console.WriteLine($"[JOIN] {username} joined from {endPoint}");
            await BroadcastAsync(new Message { Type = "sys", From = username, Text = $"{username} has joined." });
            await SendUserListAsync(); // Kirim daftar user ke semua client

            // Loop utama untuk menerima pesan
            while (tcp.Connected)
            {
                string? msgLine;
                try { msgLine = await reader.ReadLineAsync(); }
                catch { break; } // Error kalau klien disconnect atau error
                if (msgLine == null) break;

                Message? msg = null;
                try { msg = JsonSerializer.Deserialize<Message>(msgLine); } catch { }

                if (msg == null) continue;

                msg.Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (msg.Type == "msg")
                {
                    // Pesan umum
                    Console.WriteLine($"[MSG] {msg.From}: {msg.Text}");
                    await BroadcastAsync(msg);
                }
                else if (msg.Type == "pm" && !string.IsNullOrWhiteSpace(msg.To))
                {
                    // Pesan pribadi
                    Console.WriteLine($"[PM] {msg.From} -> {msg.To}: {msg.Text}");
                    await SendPrivateAsync(msg.To!, msg);

                    // Kirim pesan PM kembali ke pengirim
                    if (clients.TryGetValue(msg.From, out var senderInfo))
                        await senderInfo.Writer.WriteLineAsync(JsonSerializer.Serialize(msg));
                }
                else if (msg.Type == "typing")
                {
                    // indikator mengetik
                    Console.WriteLine($"[TYPING] {msg.From} IsTyping={msg.IsTyping}");

                    // Teruskan hanya ke user lain
                    var json = JsonSerializer.Serialize(msg);
                    foreach (var kv in clients)
                    {
                        if (kv.Key == msg.From) continue;
                        try { await kv.Value.Writer.WriteLineAsync(json); } catch { }
                    }
                }
                else if (msg.Type == "leave")
                {
                    break;
                }
            }
        }
        finally
        {
            // pembersihan ketika disconnect
            var removedUser = "";
            if (username != null)
            {
                if (clients.TryRemove(username, out _))
                {
                    removedUser = username;
                }
            }
            else
            {
                foreach (var kv in clients)
                {
                    if (kv.Value.Tcp == tcp)
                    {
                        removedUser = kv.Key;
                        clients.TryRemove(kv.Key, out _);
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(removedUser))
            {
                Console.WriteLine($"[LEAVE] {removedUser} disconnected.");
                _ = BroadcastAsync(new Message { Type = "sys", From = removedUser, Text = $"{removedUser} has left." });
                _ = SendUserListAsync();
            }

            try { tcp.Close(); } catch { }
        }
    }

    // Mengirim pesan ke semua klien
    private static async Task BroadcastAsync(Message message)
    {
        var json = JsonSerializer.Serialize(message);
        foreach (var kv in clients)
        {
            try { await kv.Value.Writer.WriteLineAsync(json); } catch { }
        }
    }

    // Mengirim pesan pribadi
    private static async Task SendPrivateAsync(string toUsername, Message message)
    {
        if (clients.TryGetValue(toUsername, out var target))
        {
            // Kirim ke penerima
            try { await target.Writer.WriteLineAsync(JsonSerializer.Serialize(message)); } catch { }
        }
        else if (clients.TryGetValue(message.From, out var sender))
        {
            //jika user tidak ditemukan
            var sys = new Message { Type = "sys", From = "server", Text = $"User {toUsername} not found." };
            try { await sender.Writer.WriteLineAsync(JsonSerializer.Serialize(sys)); } catch { }
        }
    }

    // Mengirim daftar user online ke semua klien
    private static async Task SendUserListAsync()
    {
        var listMsg = new Message
        {
            Type = "userlist",
            From = "server",
            Text = "Online users",
            Users = new List<string>(clients.Keys)
        };

        var json = JsonSerializer.Serialize(listMsg);
        foreach (var kv in clients)
        {
            try { await kv.Value.Writer.WriteLineAsync(json); } catch { }
        }
    }

    //menyimpan informasi klien
    private class ClientInfo
    {
        public string Username { get; set; } = "";
        public TcpClient Tcp { get; set; } = null!;
        public StreamWriter Writer { get; set; } = null!;
    }
}
