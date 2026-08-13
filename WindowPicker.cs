using System;
using System.Drawing;
using System.Windows.Forms;

namespace FreezeRay
{
    /// <summary>
    /// Mode « désignation » à la DeskPin : le curseur prend la forme du logo et
    /// le prochain clic gauche choisit la fenêtre cible.
    ///
    /// La mise en œuvre passe par un calque transparent couvrant tous les
    /// moniteurs, et non par SetCapture. La capture souris ne redirige en effet
    /// les messages que si un bouton est maintenu enfoncé ou si le pointeur
    /// survole la fenêtre capturante : sans bouton pressé, chaque fenêtre
    /// survolée continue d'imposer son propre curseur, et le logo n'apparaissait
    /// jamais. Avec le calque, le pointeur est en permanence au-dessus de notre
    /// fenêtre : elle impose son curseur et reçoit le clic, qui n'atteint donc
    /// pas ce qui se trouve dessous.
    /// </summary>
    internal sealed class WindowPicker : NativeWindow, IDisposable
    {
        /// <summary>Taille standard d'un curseur ; au-delà, Windows le réduirait.</summary>
        private const int CURSOR_SIZE = 32;

        /// <summary>
        /// Opacité minimale : invisible à l'œil, mais un calque totalement
        /// transparent ne recevrait aucun clic.
        /// </summary>
        private const byte OVERLAY_ALPHA = 1;

        private readonly IntPtr _cursor;
        private readonly IntPtr _background;
        private readonly Timer _escapeWatcher;
        private bool _active;

        /// <summary>Fenêtre désignée par l'utilisateur.</summary>
        public event Action<IntPtr> Picked;

        /// <summary>Désignation abandonnée (Échap, clic droit).</summary>
        public event Action Cancelled;

        public bool IsActive { get { return _active; } }

        public WindowPicker()
        {
            CreateParams cp = new CreateParams();
            cp.Style = Native.WS_POPUP;
            cp.ExStyle = Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW
                         | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOPMOST;
            CreateHandle(cp);

            Native.SetLayeredWindowAttributes(Handle, 0, OVERLAY_ALPHA, Native.LWA_ALPHA);

            _cursor = BuildLogoCursor();
            _background = Native.GetStockObject(Native.BLACK_BRUSH);

            // Le calque ne prend jamais le focus clavier : Échap se lit donc
            // directement plutôt qu'en attendant un message.
            _escapeWatcher = new Timer();
            _escapeWatcher.Interval = 50;
            _escapeWatcher.Tick += delegate
            {
                if ((Native.GetAsyncKeyState(Native.VK_ESCAPE) & 0x8000) != 0)
                    Finish(false, IntPtr.Zero);
            };
        }

        public void Start()
        {
            if (_active) return;
            _active = true;

            // Couvre l'ensemble des écrans, y compris à gauche ou au-dessus de
            // l'écran principal (coordonnées négatives).
            Rectangle screens = SystemInformation.VirtualScreen;
            Native.SetWindowPos(Handle, Native.HWND_TOPMOST,
                screens.X, screens.Y, screens.Width, screens.Height,
                Native.SWP_NOACTIVATE);
            Native.ShowWindow(Handle, Native.SW_SHOWNOACTIVATE);
            Native.SetCursor(_cursor);

            _escapeWatcher.Start();
        }

        public void Cancel()
        {
            if (_active) Finish(false, IntPtr.Zero);
        }

        private void Finish(bool picked, IntPtr hwnd)
        {
            if (!_active) return;
            _active = false;
            _escapeWatcher.Stop();
            Native.ShowWindow(Handle, Native.SW_HIDE);

            if (picked)
            {
                if (Picked != null) Picked(hwnd);
            }
            else
            {
                if (Cancelled != null) Cancelled();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (_active)
            {
                switch (m.Msg)
                {
                    case Native.WM_SETCURSOR:
                        Native.SetCursor(_cursor);
                        m.Result = new IntPtr(1);
                        return;

                    case Native.WM_MOUSEACTIVATE:
                        // Le calque ne doit jamais devenir la fenêtre active.
                        m.Result = new IntPtr(Native.MA_NOACTIVATE);
                        return;

                    case Native.WM_ERASEBKGND:
                        // Un calque non peint afficherait un contenu indéfini ;
                        // à cette opacité le noir reste imperceptible.
                        EraseBackground(m.WParam);
                        m.Result = new IntPtr(1);
                        return;

                    case Native.WM_LBUTTONUP:
                        Finish(true, WindowUnderCursor());
                        return;

                    case Native.WM_RBUTTONUP:
                        Finish(false, IntPtr.Zero);
                        return;
                }
            }
            base.WndProc(ref m);
        }

        private void EraseBackground(IntPtr hdc)
        {
            Native.RECT client;
            if (Native.GetClientRect(Handle, out client))
                Native.FillRect(hdc, ref client, _background);
        }

        /// <summary>
        /// Fenêtre réellement sous le pointeur. Le calque doit être masqué avant
        /// l'interrogation, sinon il se désignerait lui-même.
        /// </summary>
        private IntPtr WindowUnderCursor()
        {
            Native.POINT pt;
            if (!Native.GetCursorPos(out pt)) return IntPtr.Zero;

            Native.ShowWindow(Handle, Native.SW_HIDE);
            IntPtr hwnd = Native.WindowFromPoint(pt);
            return Native.GetRootWindow(hwnd);
        }

        /// <summary>
        /// Curseur repris du logo de l'application : c'est lui qui signale à
        /// l'utilisateur qu'une fenêtre est attendue.
        /// </summary>
        private static IntPtr BuildLogoCursor()
        {
            IntPtr hIcon;
            using (Bitmap bmp = Assets.RenderLogo(CURSOR_SIZE))
            {
                hIcon = bmp.GetHicon();
            }

            Native.ICONINFO info;
            if (!Native.GetIconInfo(hIcon, out info))
            {
                Native.DestroyIcon(hIcon);
                return Native.LoadCursor(IntPtr.Zero, Native.IDC_CROSS); // repli visible
            }

            info.fIcon = false;   // fIcon = FALSE transforme l'icône en curseur
            info.xHotspot = CURSOR_SIZE / 2;
            info.yHotspot = CURSOR_SIZE / 2;
            IntPtr hCursor = Native.CreateIconIndirect(ref info);

            if (info.hbmColor != IntPtr.Zero) Native.DeleteObject(info.hbmColor);
            if (info.hbmMask != IntPtr.Zero) Native.DeleteObject(info.hbmMask);
            Native.DestroyIcon(hIcon);

            return hCursor != IntPtr.Zero
                ? hCursor
                : Native.LoadCursor(IntPtr.Zero, Native.IDC_CROSS);
        }

        public void Dispose()
        {
            Cancel();
            _escapeWatcher.Dispose();
            if (_cursor != IntPtr.Zero) Native.DestroyCursor(_cursor);
            DestroyHandle();
        }
    }
}
