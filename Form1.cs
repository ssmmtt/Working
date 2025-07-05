using System.Globalization;
using System.Windows.Input;

namespace Working
{
    public partial class Form1 : Form
    {
        public static IniConfig config;                       //配置文件
        public static DateTime startTime;
        public static DateTime endTime;

        public Form1()
        {
            InitializeComponent();
            // 判断配置文件是否存在
            config = new IniConfig(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "config.ini");
            if (!config.FileExist())
            {
                config.WriteKey("auto_run", "true");
                config.WriteKey("start_time", "0:00:00");
                config.WriteKey("end_time", "23:59:59");
                config.WriteKey("auto_mini", "true");
            }

            string autoRun = config.ReadKey("auto_run");

            if (autoRun == "true")
            {
                timer1.Start();
                label1.Text = "状态：已打开";
                this.notifyIcon1.Icon = Properties.Resources.work_on;
            }

            startTime = DateTime.ParseExact(config.ReadKey("start_time"), "H:mm:ss", CultureInfo.InvariantCulture);
            endTime = DateTime.ParseExact(config.ReadKey("end_time"), "H:mm:ss", CultureInfo.InvariantCulture);

            dateTimePicker1.Value = startTime;
            dateTimePicker2.Value = endTime;

            string autoMini = config.ReadKey("auto_mini");

            if (autoMini == "true")
            {
                // 将窗口最小化到托盘
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool enabled = timer1.Enabled;
            if (enabled)
            {
                // 打开状态，关闭
                timer1.Stop();
                label1.Text = "状态：已关闭";
                config.WriteKey("auto_run", "false");
                this.notifyIcon1.Icon = Properties.Resources.work_off;
            }
            else
            {
                // 关闭状态，打开
                timer1.Start();
                label1.Text = "状态：已打开";
                config.WriteKey("auto_run", "true");
                this.notifyIcon1.Icon = Properties.Resources.work_on;
            }
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            DateTime currentTime = DateTime.Now;
            if (currentTime.TimeOfDay >= startTime.TimeOfDay && currentTime.TimeOfDay <= endTime.TimeOfDay)
            {
                wake_up();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Show();
                this.Activate();
            }
        }

        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.notifyIcon1.Visible = false;
            Environment.Exit(Environment.ExitCode);
        }

        private void dateTimePicker1_TextChanged(object sender, EventArgs e)
        {
            startTime = DateTime.ParseExact(dateTimePicker1.Text, "H:mm:ss", CultureInfo.InvariantCulture);
            config.WriteKey("start_time", startTime.ToLongTimeString());
            label1.Focus();
        }

        private void dateTimePicker2_TextChanged(object sender, EventArgs e)
        {
            endTime = DateTime.ParseExact(dateTimePicker2.Text, "H:mm:ss", CultureInfo.InvariantCulture);
            config.WriteKey("end_time", endTime.ToLongTimeString());
            label1.Focus();
        }
    }
}