using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ProducerConsumerSystem
{
    public struct BufferItem
    {
        public int Data { get; set; }
        public int ProducerIndex { get; set; }

        public BufferItem(int data, int producerIndex)
        {
            Data = data;
            ProducerIndex = producerIndex;
        }
    }

    public partial class Form1 : Form
    {
        private LinkedList<BufferItem> buffer = new LinkedList<BufferItem>();
        private const int BUFFER_SIZE = 10;
        private Random random = new Random();

        private Thread[] consumerThreads = new Thread[2];
        private Thread[] producerThreads = new Thread[4];

        private bool[] isProducing = new bool[4];
        private bool isConsuming = false;

        private object lockObject = new object();

        private Color[] producerColors = { Color.Red, Color.Blue, Color.Green, Color.Orange };

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Size = new Size(800, 600);
            this.Text = "生产者-消费者系统";

            consumerTextBox1 = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 10),
                Size = new Size(200, 200)
            };
            this.Controls.Add(consumerTextBox1);

            consumerTextBox2 = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(220, 10),
                Size = new Size(200, 200)
            };
            this.Controls.Add(consumerTextBox2);

            producerTextBox = new RichTextBox
            {
                Location = new Point(430, 10),
                Size = new Size(350, 200),
                ReadOnly = true
            };
            this.Controls.Add(producerTextBox);

            linkedListPictureBox = new PictureBox
            {
                Location = new Point(10, 220),
                Size = new Size(770, 100),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(linkedListPictureBox);

            startConsumersButton = new Button
            {
                Text = "开始/停止消费",
                Location = new Point(10, 330),
                Size = new Size(120, 30)
            };
            startConsumersButton.Click += StartConsumersButton_Click;
            this.Controls.Add(startConsumersButton);

            for (int i = 0; i < 4; i++)
            {
                producerButtons[i] = new Button
                {
                    Text = $"生产者 {i + 1}",
                    Location = new Point(140 + i * 130, 330),
                    Size = new Size(120, 30)
                };
                int index = i;
                producerButtons[i].Click += (sender, e) => ProducerButton_Click(sender, e, index);
                this.Controls.Add(producerButtons[i]);
            }
        }

        private void StartConsumersButton_Click(object? sender, EventArgs e)
        {
            if (!isConsuming)
            {
                isConsuming = true;
                startConsumersButton.Text = "停止消费";
                for (int i = 0; i < 2; i++)
                {
                    int consumerIndex = i;
                    consumerThreads[i] = new Thread(() => ConsumerThread(consumerIndex));
                    consumerThreads[i].Start();
                }
            }
            else
            {
                isConsuming = false;
                startConsumersButton.Text = "开始消费";
                for (int i = 0; i < 2; i++)
                {
                    consumerThreads[i]?.Join();
                }
            }
        }

        private void ProducerButton_Click(object? sender, EventArgs e, int producerIndex)
        {
            if (!isProducing[producerIndex])
            {
                isProducing[producerIndex] = true;
                producerButtons[producerIndex].Text = $"停止生产者 {producerIndex + 1}";
                producerThreads[producerIndex] = new Thread(() => ProducerThread(producerIndex));
                producerThreads[producerIndex].Start();
            }
            else
            {
                isProducing[producerIndex] = false;
                producerButtons[producerIndex].Text = $"生产者 {producerIndex + 1}";
                producerThreads[producerIndex]?.Join();
            }
        }

        private void ConsumerThread(int consumerIndex)
        {
            TextBox consumerTextBox = consumerIndex == 0 ? consumerTextBox1 : consumerTextBox2;

            while (isConsuming)
            {
                BufferItem? item = null;
                lock (lockObject)
                {
                    if (buffer.Count > 0)
                    {
                        item = buffer.First?.Value;
                        if (item.HasValue)
                        {
                            buffer.RemoveFirst();
                            UpdateUI(() =>
                            {
                                consumerTextBox.AppendText($"消费者 {consumerIndex + 1} 消费: {item.Value.Data} (来自生产者 {item.Value.ProducerIndex + 1})\r\n");
                                DrawLinkedList();
                            });
                        }
                    }
                }

                if (item.HasValue)
                {
                    Thread.Sleep(random.Next(500, 1500));
                }
                else
                {
                    Thread.Sleep(100); // 如果缓冲区为空，稍微等待一下再检查
                }
            }
        }


        private void ProducerThread(int producerIndex)
        {
            while (isProducing[producerIndex])
            {
                int data = random.Next(1, 100);
                lock (lockObject)
                {
                    if (buffer.Count < BUFFER_SIZE)
                    {
                        buffer.AddLast(new BufferItem(data, producerIndex));
                        UpdateUI(() =>
                        {
                            producerTextBox.SelectionStart = producerTextBox.TextLength;
                            producerTextBox.SelectionLength = 0;
                            producerTextBox.SelectionColor = producerColors[producerIndex];
                            producerTextBox.AppendText($"生产者 {producerIndex + 1} 生产: {data}\r\n");
                            producerTextBox.ScrollToCaret();
                            DrawLinkedList();
                        });
                    }
                }
                Thread.Sleep(random.Next(500, 1500));
            }
        }

        private void UpdateUI(Action action)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
        }

        private void DrawLinkedList()
        {
            Bitmap bmp = new Bitmap(linkedListPictureBox.Width, linkedListPictureBox.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                int x = 10;
                int y = linkedListPictureBox.Height / 2;
                Font font = new Font("Arial", 10);
                Pen pen = new Pen(Color.Black, 2);

                foreach (BufferItem item in buffer)
                {
                    g.FillRectangle(new SolidBrush(producerColors[item.ProducerIndex]), x, y - 15, 30, 30);
                    g.DrawRectangle(pen, x, y - 15, 30, 30);
                    g.DrawString(item.Data.ToString(), font, Brushes.Black, x + 5, y - 10);
                    if (x + 60 < linkedListPictureBox.Width - 40)
                    {
                        g.DrawLine(pen, x + 30, y, x + 60, y);
                        x += 60;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            linkedListPictureBox.Image = bmp;
        }
    }
}
