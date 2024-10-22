namespace ProducerConsumerSystem
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "Form1";
        }

        #endregion

        private System.Windows.Forms.TextBox consumerTextBox1 = null!;
        private System.Windows.Forms.TextBox consumerTextBox2 = null!;
        private System.Windows.Forms.RichTextBox producerTextBox = null!;
        private System.Windows.Forms.PictureBox linkedListPictureBox = null!;
        private System.Windows.Forms.Button startConsumersButton = null!;
        private System.Windows.Forms.Button[] producerButtons = new System.Windows.Forms.Button[4];
    }
}
