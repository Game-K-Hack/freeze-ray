using System;
using System.Runtime.InteropServices;
using System.Text;

namespace KeepScreen
{
    internal static class Native
    {
        public const int WM_HOTKEY = 0x0312;

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOPMOST = 0x0008;
        public const int WS_EX_TOOLWINDOW = 0x0080;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        public static int GetWindowExStyle(IntPtr hWnd)
        {
            return IntPtr.Size == 8
                ? (int)GetWindowLongPtr64(hWnd, GWL_EXSTYLE).ToInt64()
                : GetWindowLong32(hWnd, GWL_EXSTYLE);
        }

        public static string GetTitle(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString().Trim();
            if (title.Length > 0) return title;

            sb.Length = 0;
            GetClassName(hWnd, sb, sb.Capacity);
            string cls = sb.ToString().Trim();
            return cls.Length > 0 ? cls : "(sans titre)";
        }

        /// <summary>Remonte a la fenetre racine appartenant a l'utilisateur (GA_ROOTOWNER).</summary>
        public static IntPtr GetRootWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return IntPtr.Zero;
            IntPtr root = GetAncestor(hWnd, 3);
            return root == IntPtr.Zero ? hWnd : root;
        }

        public static bool IsTopMost(IntPtr hWnd)
        {
            return (GetWindowExStyle(hWnd) & WS_EX_TOPMOST) != 0;
        }
    }
}
