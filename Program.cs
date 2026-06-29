namespace Working
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            AppLog.Initialize();
            using var mutex = new Mutex(true, "Product_Index_Cntvs", out bool created);
            if (!created)
            {
                MessageBox.Show("程序已运行，不能再次打开！");
                return;
            }
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
