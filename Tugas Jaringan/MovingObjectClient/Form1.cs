using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MovingObjectClient
{
    public partial class Form1 : Form
    {
        Rectangle rect = new Rectangle(20, 20, 30, 30);
        SolidBrush fillBlue = new SolidBrush(Color.Blue);

        TcpClient client;
        NetworkStream stream;

        public Form1()
        {
            InitializeComponent();
            ConnectToServer();
        }

        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 11111); // ganti IP kalau server beda PC
                stream = client.GetStream();

                Thread t = new Thread(ReceiveData);
                t.IsBackground = true;
                t.Start();

                this.Text = "MovingObject Client - Connected to Server";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Error: " + ex.Message);
            }
        }

        private void ReceiveData()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes > 0)
                    {
                        string msg = Encoding.ASCII.GetString(buffer, 0, bytes);
                        string[] parts = msg.Split(',');

                        if (parts.Length == 2 &&
                            int.TryParse(parts[0], out int x) &&
                            int.TryParse(parts[1], out int y))
                        {
                            rect.X = x;
                            rect.Y = y;

                            this.Invoke((MethodInvoker)delegate { Invalidate(); });
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Disconnected from server!");
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.FillRectangle(fillBlue, rect);
        }
    }
}
