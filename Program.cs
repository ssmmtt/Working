namespace Working
{

    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            bool bCreatedNew;
            Mutex m = new Mutex(false, "Product_Index_Cntvs", out bCreatedNew);
            if (bCreatedNew)
            {
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());
            }
            else
            {
                MessageBox.Show("程序已运行，不能再次打开！");
                Environment.Exit(1);
            }
        }
    }
}