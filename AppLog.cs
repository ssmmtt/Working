using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Working
{
    internal static class AppLog
    {
        internal static bool Enabled = true;

        [DllImport("kernel32.dll")] private static extern bool AllocConsole();
        [DllImport("kernel32.dll")] private static extern bool AttachConsole(int pid);

        public static void Initialize()
        {
            if (!Enabled) return;
            if (Debugger.IsAttached) return;
            if (AttachConsole(-1) || AllocConsole())
                Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        public static void Print(string category, string message)
        {
            if (!Enabled) return;
            string line = $"[{DateTime.Now:HH:mm:ss}] [{category}] {message}";
            Debug.WriteLine(line);
            try { Console.WriteLine(line); } catch { }
        }
    }
}
