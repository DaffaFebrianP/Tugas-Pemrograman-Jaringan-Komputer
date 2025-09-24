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
    private static ConcurrentDictionary<string, ClientInfo> clients = new();

    static async Task Main(string[] args)
    {
        int port = 9000;
        if (args.Length >= 1 && int.TryParse(args[0], out var p)) port = p;

        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[SERVER] Listening on port {port} ...");

        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var tcp = await listener.AcceptTcpClientAsync(cts.Token);
                    _ = HandleClientAsync(tcp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SERVER] Accept error: " + ex.Message);
                }
            }
        });

        Console.WriteLine("[SERVER] Press Enter to stop server...");
        Console.ReadLine();
        cts.Cancel();
        listener.Stop();
        Console.WriteLine("[SERVER] Stopped.");
    }

    private static async Task HandleClientAsync(TcpClient tcp)
    {
        var endPoint = tcp.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"[CONN] New connection from {endPoint}");

        using var ns = tcp.GetStream();
        using var reader = new StreamReader(ns, Encoding.UTF8);
        using var writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };

        try
        {
            // baca pesan join pertama
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                tcp.Close();
                return;
            }

            var joinMsg = JsonSerializer.Deserialize<Message>(line);
            if (joinMsg == null || joinMsg.Type != "join" || string.IsNullOrWhiteSpace(joinMsg.From))
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new Message { Type = "sys", Text = "invalid_join" }));
                tcp.Close();
                return;
            }

            var username = joinMsg.From.Trim();

            if (clients.ContainsKey(username))
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new Message { Type = "sys", Text = "username_taken" }));
                tcp.Close();
                Console.WriteLine($"[REJECT] Username taken: {username} from {endPoint}");
                return;
            }

            var clientInfo = new ClientInfo { Username = username, Tcp = tcp, Writer = writer };
            clients.TryAdd(username, clientInfo);

            Console.WriteLine($"[JOIN] {username} joined from {endPoint}");
            await BroadcastAsync(new Message { Type = "sys", From = username, Text = $"{username} has joined." });
            await SendUserListAsync(); // kirim daftar user ke semua client

            while (tcp.Connected)
            {
                string? msgLine;
                try { msgLine = await reader.ReadLineAsync(); }
                catch { break; }
                if (msgLine == null) break;

                Message? msg = null;
                try { msg = JsonSerializer.Deserialize<Message>(msgLine); } catch { }

                if (msg == null) continue;

                msg.Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (msg.Type == "msg")
                {
                    Console.WriteLine($"[MSG] {msg.From}: {msg.Text}");
                    await BroadcastAsync(msg);
                }
                else if (msg.Type == "pm" && !string.IsNullOrWhiteSpace(msg.To))
                {
                    Console.WriteLine($"[PM] {msg.From} -> {msg.To}: {msg.Text}");
                    await SendPrivateAsync(msg.To!, msg);
                    if (clients.TryGetValue(msg.From, out var senderInfo))
                        await senderInfo.Writer.WriteLineAsync(JsonSerializer.Serialize(msg));
                }
                else if (msg.Type == "leave")
                {
                    break;
                }
            }
        }
        finally
        {
            var removedUser = "";
            foreach (var kv in clients)
            {
                if (kv.Value.Tcp == tcp)
                {
                    removedUser = kv.Key;
                    clients.TryRemove(kv.Key, out _);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(removedUser))
            {
                Console.WriteLine($"[LEAVE] {removedUser} disconnected.");
                _ = BroadcastAsync(new Message { Type = "sys", From = removedUser, Text = $"{removedUser} has left." });
                _ = SendUserListAsync(); // update daftar user online
            }

            try { tcp.Close(); } catch { }
        }
    }

    private static async Task BroadcastAsync(Message message)
    {
        var json = JsonSerializer.Serialize(message);
        foreach (var kv in clients)
        {
            try { await kv.Value.Writer.WriteLineAsync(json); } catch { }
        }
    }

    private static async Task SendPrivateAsync(string toUsername, Message message)
    {
        if (clients.TryGetValue(toUsername, out var target))
        {
            try { await target.Writer.WriteLineAsync(JsonSerializer.Serialize(message)); } catch { }
        }
        else if (clients.TryGetValue(message.From, out var sender))
        {
            var sys = new Message { Type = "sys", From = "server", Text = $"User {toUsername} not found." };
            try { await sender.Writer.WriteLineAsync(JsonSerializer.Serialize(sys)); } catch { }
        }
    }

    // kirim daftar user online ke semua client
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

    private class ClientInfo
    {
        public string Username { get; set; } = "";
        public TcpClient Tcp { get; set; } = null!;
        public StreamWriter Writer { get; set; } = null!;
    }
}
