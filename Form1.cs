using System.Globalization;
using System.Windows.Input;

namespace Working
{
    public partial class Form1 : Form
    {
        public static IniConfig config;                       //�����ļ�
        public static DateTime startTime;
        public static DateTime endTime;

        public Form1()
        {
            InitializeComponent();
            // �ж������ļ��Ƿ����
            config = new IniConfig(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "config.ini");
            if (!config.FileExist())
            {
                config.WriteKey("auto_run", "true");
                config.WriteKey("start_time", "0:00:00");
                config.WriteKey("end_time", "23:59:59");
            }

            string autoRun = config.ReadKey("auto_run");

            if (autoRun == "true")
            {
                timer1.Start();
                label1.Text = "״̬���Ѵ�";
                this.notifyIcon1.Icon = Properties.Resources.work_on;
            }

            startTime = DateTime.ParseExact(config.ReadKey("start_time"), "H:mm:ss", CultureInfo.InvariantCulture);
            endTime = DateTime.ParseExact(config.ReadKey("end_time"), "H:mm:ss", CultureInfo.InvariantCulture);

            dateTimePicker1.Value = startTime;
            dateTimePicker2.Value = endTime;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool enabled = timer1.Enabled;
            if (enabled)
            {
                // ��״̬���ر�
                timer1.Stop();
                label1.Text = "״̬���ѹر�";
                config.WriteKey("auto_run", "false");
                this.notifyIcon1.Icon = Properties.Resources.work_off;
            }
            else
            {
                // �ر�״̬����
                timer1.Start();
                label1.Text = "״̬���Ѵ�";
                config.WriteKey("auto_run", "true");
                this.notifyIcon1.Icon = Properties.Resources.work_on;
            }
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            // �ظ�һ�λָ�����״̬
            wake_up();
            wake_up();
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
                this.Show();
                this.Activate();
            }
        }

        private void �˳�ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.notifyIcon1.Visible = false;
            Environment.Exit(Environment.ExitCode);
        }

        private void dateTimePicker1_TextChanged(object sender, EventArgs e)
        {
            startTime = DateTime.ParseExact(dateTimePicker1.Text, "H:mm:ss", CultureInfo.InvariantCulture);
            config.WriteKey("start_time", startTime.ToLongTimeString());
        }

        private void dateTimePicker2_TextChanged(object sender, EventArgs e)
        {
            endTime = DateTime.ParseExact(dateTimePicker2.Text, "H:mm:ss", CultureInfo.InvariantCulture);
            config.WriteKey("end_time", endTime.ToLongTimeString());
        }


    }
}