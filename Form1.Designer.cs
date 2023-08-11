using System.Globalization;
using System.Runtime.InteropServices;


namespace Working
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // 模拟键盘输入
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private void wake_up()
        {
            const int KEYEVENTF_EXTENDEDKEY = 0x1;
            const int KEYEVENTF_KEYUP = 0x2;
            keybd_event((byte)Keys.Scroll, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event((byte)Keys.Scroll, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }


        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            timer1 = new System.Windows.Forms.Timer(components);
            label1 = new Label();
            button1 = new Button();
            notifyIcon1 = new NotifyIcon(components);
            contextMenuStrip1 = new ContextMenuStrip(components);
            退出ToolStripMenuItem = new ToolStripMenuItem();
            dateTimePicker1 = new DateTimePicker();
            label2 = new Label();
            label3 = new Label();
            dateTimePicker2 = new DateTimePicker();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // timer1
            // 
            timer1.Interval = 180000;
            timer1.Tick += timer1_Tick;
            // 
            // label1
            // 
            label1.Font = new Font("Microsoft YaHei UI", 30F, FontStyle.Regular, GraphicsUnit.Point);
            label1.ForeColor = Color.Red;
            label1.Location = new Point(-3, 209);
            label1.Name = "label1";
            label1.Size = new Size(461, 55);
            label1.TabIndex = 1;
            label1.Text = "状态：已关闭";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // button1
            // 
            button1.Font = new Font("Microsoft YaHei UI", 50F, FontStyle.Regular, GraphicsUnit.Point);
            button1.ForeColor = SystemColors.MenuHighlight;
            button1.Location = new Point(35, 7);
            button1.Name = "button1";
            button1.Size = new Size(392, 151);
            button1.TabIndex = 0;
            button1.Text = "不休眠";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.Icon = Properties.Resources.work_off;
            notifyIcon1.Text = "Working";
            notifyIcon1.Visible = true;
            notifyIcon1.MouseClick += notifyIcon1_MouseClick;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { 退出ToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(101, 26);
            // 
            // 退出ToolStripMenuItem
            // 
            退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            退出ToolStripMenuItem.Size = new Size(100, 22);
            退出ToolStripMenuItem.Text = "退出";
            退出ToolStripMenuItem.Click += 退出ToolStripMenuItem_Click;
            // 
            // dateTimePicker1
            // 
            dateTimePicker1.CustomFormat = "";
            dateTimePicker1.Format = DateTimePickerFormat.Time;
            dateTimePicker1.Location = new Point(109, 169);
            dateTimePicker1.Name = "dateTimePicker1";
            dateTimePicker1.ShowUpDown = true;
            dateTimePicker1.Size = new Size(117, 23);
            dateTimePicker1.TabIndex = 2;
            dateTimePicker1.TextChanged += dateTimePicker1_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(35, 174);
            label2.Name = "label2";
            label2.Size = new Size(68, 17);
            label2.TabIndex = 3;
            label2.Text = "开始时间：";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(236, 173);
            label3.Name = "label3";
            label3.Size = new Size(68, 17);
            label3.TabIndex = 5;
            label3.Text = "结束时间：";
            // 
            // dateTimePicker2
            // 
            dateTimePicker2.CustomFormat = "";
            dateTimePicker2.Format = DateTimePickerFormat.Time;
            dateTimePicker2.Location = new Point(310, 168);
            dateTimePicker2.Name = "dateTimePicker2";
            dateTimePicker2.ShowUpDown = true;
            dateTimePicker2.Size = new Size(117, 23);
            dateTimePicker2.TabIndex = 4;
            dateTimePicker2.TextChanged += dateTimePicker2_TextChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(459, 293);
            Controls.Add(label3);
            Controls.Add(dateTimePicker2);
            Controls.Add(label2);
            Controls.Add(dateTimePicker1);
            Controls.Add(label1);
            Controls.Add(button1);
            Icon = Properties.Resources.work_off;
            MaximizeBox = false;
            Name = "Form1";
            Text = "Working";
            FormClosing += Form1_FormClosing;
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label1;
        private Button button1;
        private System.Windows.Forms.Timer timer1;
        private NotifyIcon notifyIcon1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem 退出ToolStripMenuItem;
        private DateTimePicker dateTimePicker1;
        private Label label2;
        private Label label3;
        private DateTimePicker dateTimePicker2;
    }
}