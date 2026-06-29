using System.Globalization;

namespace Working
{
    public partial class Form1 : Form
    {
        private bool _suppressInitialShow;
        private readonly IdleManager _power = new();
        private IniConfig _config = null!;
        private DateTime _startTime;
        private DateTime _endTime;

        public Form1()
        {
            InitializeComponent();
            LoadConfig();
            if (_config.ReadKey("auto_run") == "true") SetWorking(true);
            if (_config.ReadKey("auto_mini") == "true")
            {
                _suppressInitialShow = true;
                ShowInTaskbar = false;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!(_suppressInitialShow && value)) base.SetVisibleCore(value);
        }

        private void LoadConfig()
        {
            _config = new IniConfig(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "config.ini");
            if (!_config.FileExist())
            {
                _config.WriteKey("auto_run", "true");
                _config.WriteKey("start_time", "0:00:00");
                _config.WriteKey("end_time", "23:59:59");
                _config.WriteKey("auto_mini", "true");
            }

            _startTime = ParseTime(_config.ReadKey("start_time"));
            _endTime = ParseTime(_config.ReadKey("end_time"));
            dateTimePicker1.Value = _startTime;
            dateTimePicker2.Value = _endTime;
        }

        private static DateTime ParseTime(string s) =>
            DateTime.ParseExact(s, "H:mm:ss", CultureInfo.InvariantCulture);

        private bool InWorkHours()
        {
            var now = DateTime.Now.TimeOfDay;
            return now >= _startTime.TimeOfDay && now <= _endTime.TimeOfDay;
        }

        private void SetWorking(bool on)
        {
            if (on)
            {
                timer1.Start();
                label1.Text = "状态：已打开";
                notifyIcon1.Icon = Properties.Resources.work_on;
                _config.WriteKey("auto_run", "true");
                _power.Enable(InWorkHours);
            }
            else
            {
                timer1.Stop();
                label1.Text = "状态：已关闭";
                notifyIcon1.Icon = Properties.Resources.work_off;
                _config.WriteKey("auto_run", "false");
                _power.Disable();
            }
        }

        private void button1_Click(object sender, EventArgs e) => SetWorking(!timer1.Enabled);

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (InWorkHours()) _power.KeepAlive();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _suppressInitialShow = false;
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Show();
            Activate();
        }

        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            _power.Dispose();
            Environment.Exit(0);
        }

        private void dateTimePicker1_TextChanged(object sender, EventArgs e) =>
            SaveTime(dateTimePicker1, ref _startTime, "start_time");

        private void dateTimePicker2_TextChanged(object sender, EventArgs e) =>
            SaveTime(dateTimePicker2, ref _endTime, "end_time");

        private void SaveTime(DateTimePicker picker, ref DateTime field, string key)
        {
            field = ParseTime(picker.Text);
            _config.WriteKey(key, field.ToLongTimeString());
            label1.Focus();
        }
    }
}
