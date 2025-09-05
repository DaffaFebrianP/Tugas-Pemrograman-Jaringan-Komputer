using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace MovingObjectServer
{
    public partial class Form1 : Form
    {
        Pen red = new Pen(Color.Red);
        Rectangle rect = new Rectangle(20, 20, 30, 30);
        SolidBrush fillBlue = new SolidBrush(Color.Blue);
        int slide = 10;

        // Socket server
        TcpListener listener;
        List<TcpClient> clients = new List<TcpClient>();

        public Form1()
        {
            InitializeComponent();

            // Start server
            StartServer();

            // Timer aktif
            timer1.Interval = 50;
            timer1.Enabled = true;
        }

        private void StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 11111);
                listener.Start();
                listener.BeginAcceptTcpClient(OnClientConnect, null);

                this.Text = "MovingObject Server - Listening on port 11111";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Server error: " + ex.Message);
            }
        }

        private void OnClientConnect(IAsyncResult ar)
        {
            try
            {
                TcpClient client = listener.EndAcceptTcpClient(ar);
                lock (clients)
                {
                    clients.Add(client);
                }
                listener.BeginAcceptTcpClient(OnClientConnect, null);
            }
            catch { }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            MoveObject();
            BroadcastPosition();
            Invalidate(); // redraw
        }

        private void MoveObject()
        {
            rect.X += slide;
            if (rect.X >= this.Width - rect.Width * 2)
                slide = -10;
            else if (rect.X <= rect.Width / 2)
                slide = 10;
        }

        private void BroadcastPosition()
        {
            string msg = $"{rect.X},{rect.Y}";
            byte[] data = Encoding.ASCII.GetBytes(msg);

            lock (clients)
            {
                List<TcpClient> removeList = new List<TcpClient>();
                foreach (var client in clients)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    catch
                    {
                        removeList.Add(client);
                    }
                }

                foreach (var c in removeList)
                    clients.Remove(c);
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawRectangle(red, rect);
            g.FillRectangle(fillBlue, rect);
        }
    }
}
