using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FreezeRay
{
    /// <summary>
    /// Bulles de notification affichant le logo de l'application plutôt que le
    /// « i » bleu du système.
    ///
    /// WinForms ne sait pas le faire : <see cref="NotifyIcon.ShowBalloonTip(int)"/>
    /// n'accepte que les icônes système et rejette toute valeur hors de l'énumération
    /// <see cref="ToolTipIcon"/>. On s'adresse donc directement au shell avec
    /// l'indicateur NIIF_USER, qui demande d'utiliser l'icône fournie.
    ///
    /// L'entrée de la zone de notification appartenant toujours à WinForms, on
    /// réutilise son identification interne pour viser la même icône. Si cette
    /// mécanique venait à changer, on retombe simplement sur la bulle standard.
    /// </summary>
    internal static class Notifications
    {
        private const int NIM_MODIFY = 0x00000001;
        private const int NIF_INFO = 0x00000010;
        private const int NIIF_USER = 0x00000004;
        private const int NIIF_LARGE_ICON = 0x00000020;

        /// <summary>Taille attendue pour la grande icône d'une bulle.</summary>
        private const int BALLOON_ICON_SIZE = 32;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int message, ref NOTIFYICONDATA data);

        /// <summary>
        /// Affiche une bulle. <see cref="ToolTipIcon.Info"/> utilise le logo de
        /// l'application ; avertissements et erreurs gardent les icônes système,
        /// qui signalent bien mieux un problème.
        /// </summary>
        public static void Show(NotifyIcon tray, string title, string text,
                                ToolTipIcon icon, int timeout)
        {
            if (tray == null) return;
            if (icon != ToolTipIcon.Info || !ShowWithLogo(tray, title, text))
            {
                tray.BalloonTipTitle = title;
                tray.BalloonTipText = text;
                tray.BalloonTipIcon = icon;
                tray.ShowBalloonTip(timeout);
            }
        }

        private static bool ShowWithLogo(NotifyIcon tray, string title, string text)
        {
            IntPtr owner;
            int id;
            if (!Locate(tray, out owner, out id)) return false;

            NOTIFYICONDATA data = new NOTIFYICONDATA();
            data.cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA));
            data.hWnd = owner;
            data.uID = id;
            data.uFlags = NIF_INFO;
            data.szInfoTitle = Truncate(title, 63);
            data.szInfo = Truncate(text, 255);
            data.dwInfoFlags = NIIF_USER | NIIF_LARGE_ICON;
            data.hBalloonIcon = Assets.GetIcon(BALLOON_ICON_SIZE).Handle;

            return Shell_NotifyIcon(NIM_MODIFY, ref data);
        }

        /// <summary>Retrouve la fenêtre et l'identifiant que WinForms a déclarés au shell.</summary>
        private static bool Locate(NotifyIcon tray, out IntPtr owner, out int id)
        {
            owner = IntPtr.Zero;
            id = 0;
            try
            {
                // .NET Framework nomme ces champs « window » et « id » ; les
                // versions modernes de WinForms les préfixent d'un souligné.
                // On accepte les deux : sinon une migration ferait silencieusement
                // retomber les bulles sur l'icône système.
                FieldInfo windowField = FindField("window", "_window");
                FieldInfo idField = FindField("id", "_id");
                if (windowField == null || idField == null) return false;

                NativeWindow window = windowField.GetValue(tray) as NativeWindow;
                if (window == null || window.Handle == IntPtr.Zero) return false;

                owner = window.Handle;
                id = (int)idField.GetValue(tray);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static FieldInfo FindField(params string[] names)
        {
            foreach (string name in names)
            {
                FieldInfo field = typeof(NotifyIcon).GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) return field;
            }
            return null;
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length > max ? value.Substring(0, max) : value;
        }
    }
}
