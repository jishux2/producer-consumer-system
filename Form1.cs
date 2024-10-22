using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProducerConsumerSystem
{
    public static class FormExtensions
    {
        public static Task InvokeAsync(this Form form, Action action)
        {
            return Task.Factory.FromAsync(form.BeginInvoke(action), form.EndInvoke);
        }
    }

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

        private Task[] consumerTasks = new Task[2];
        private bool consumersStarted = false;
        private Task[] producerTasks = new Task[4];

        private CancellationTokenSource[] producerCts = new CancellationTokenSource[4];
        private CancellationTokenSource consumerCts = new CancellationTokenSource();

        private Color[] producerColors = { Color.Red, Color.Blue, Color.Green, Color.Orange };

        // 信号量
        private SemaphoreSlim emptyCount;
        private SemaphoreSlim fullCount;
        private SemaphoreSlim mutex;

        public Form1()
        {
            InitializeComponent();
            InitializeUI();

            // 初始化信号量
            emptyCount = new SemaphoreSlim(BUFFER_SIZE, BUFFER_SIZE);
            fullCount = new SemaphoreSlim(0, BUFFER_SIZE);
            mutex = new SemaphoreSlim(1, 1);
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

        private async void StartConsumersButton_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;

            if (!consumersStarted)
            {
                consumersStarted = true;
                consumerCts = new CancellationTokenSource();
                startConsumersButton.Text = "停止消费";
                for (int i = 0; i < 2; i++)
                {
                    int consumerIndex = i;
                    consumerTasks[i] = ConsumerTask(consumerIndex, consumerCts.Token);
                }
            }
            else
            {
                startConsumersButton.Text = "开始消费";
                consumerCts.Cancel();
                try
                {
                    await Task.WhenAll(consumerTasks.Where(t => t != null));
                }
                catch (OperationCanceledException)
                {
                    // 预期的取消异常，可以忽略
                }
                finally
                {
                    consumerCts.Dispose();
                    consumersStarted = false;
                }
            }
        }

        private async void ProducerButton_Click(object? sender, EventArgs e, int producerIndex)
        {
            if (producerCts[producerIndex]?.IsCancellationRequested ?? true)
            {
                producerCts[producerIndex] = new CancellationTokenSource();
                producerButtons[producerIndex].Text = $"停止生产者 {producerIndex + 1}";
                producerTasks[producerIndex] = ProducerTask(producerIndex, producerCts[producerIndex].Token);
            }
            else
            {
                producerButtons[producerIndex].Text = $"生产者 {producerIndex + 1}";
                producerCts[producerIndex].Cancel();
                try
                {
                    await producerTasks[producerIndex];
                }
                catch (OperationCanceledException)
                {
                    // 预期的取消异常，可以忽略
                }
                finally
                {
                    producerCts[producerIndex].Dispose();
                }
            }
        }

        private async Task ConsumerTask(int consumerIndex, CancellationToken cancellationToken)
        {
            TextBox consumerTextBox = consumerIndex == 0 ? consumerTextBox1 : consumerTextBox2;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await fullCount.WaitAsync(cancellationToken);
                    await mutex.WaitAsync(cancellationToken);

                    BufferItem? item = null;
                    if (buffer.Count > 0)
                    {
                        var first = buffer.First;
                        if (first != null)
                        {
                            item = first.Value;
                            buffer.RemoveFirst();
                        }
                    }

                    mutex.Release();

                    if (item.HasValue)
                    {
                        emptyCount.Release();

                        await UpdateUIAsync(() =>
                        {
                            consumerTextBox.AppendText($"消费者 {consumerIndex + 1} 消费: {item.Value.Data} (来自生产者 {item.Value.ProducerIndex + 1})\r\n");
                            DrawLinkedList();
                        });

                        await Task.Delay(random.Next(500, 1500), cancellationToken);
                    }
                    else
                    {
                        fullCount.Release();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ProducerTask(int producerIndex, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    int data = random.Next(1, 100);

                    await emptyCount.WaitAsync(cancellationToken);
                    await mutex.WaitAsync(cancellationToken);

                    buffer.AddLast(new BufferItem(data, producerIndex));

                    mutex.Release();
                    fullCount.Release();

                    await UpdateUIAsync(() =>
                    {
                        producerTextBox.SelectionStart = producerTextBox.TextLength;
                        producerTextBox.SelectionLength = 0;
                        producerTextBox.SelectionColor = producerColors[producerIndex];
                        producerTextBox.AppendText($"生产者 {producerIndex + 1} 生产: {data}\r\n");
                        producerTextBox.ScrollToCaret();
                        DrawLinkedList();
                    });

                    await Task.Delay(random.Next(500, 1500), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task UpdateUIAsync(Action action)
        {
            if (this.InvokeRequired)
            {
                await this.InvokeAsync(action);
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
