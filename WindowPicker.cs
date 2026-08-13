using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KeepScreen
{
    /// <summary>
    /// Mode « désignation » à la DeskPin : le curseur devient une épingle et le
    /// prochain clic gauche choisit la fenêtre cible.
    ///
    /// La capture souris (SetCapture) est ce qui permet de recevoir le clic où
    /// qu'il ait lieu à l'écran, et de le consommer avant que la fenêtre visée ne
    /// le reçoive — cliquer pour désigner ne doit pas actionner un bouton.
    /// </summary>
    internal sealed class WindowPicker : NativeWindow, IDisposable
    {
        private readonly IntPtr _cursor;
        private readonly Timer _escapeWatcher;
        private bool _active;

        /// <summary>Fenêtre désignée par l'utilisateur.</summary>
        public event Action<IntPtr> Picked;

        /// <summary>Désignation abandonnée (Échap, clic droit, perte de capture).</summary>
        public event Action Cancelled;

        public bool IsActive { get { return _active; } }

        public WindowPicker()
        {
            CreateParams cp = new CreateParams();
            cp.Style = unchecked((int)0x80000000); // WS_POPUP : fenêtre jamais affichée
            CreateHandle(cp);

            _cursor = BuildPinCursor();

            // La capture souris détourne le clavier des messages habituels :
            // on interroge donc Échap directement.
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
            Native.SetCapture(Handle);
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
            if (Native.GetCapture() == Handle) Native.ReleaseCapture();

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
                    case Native.WM_MOUSEMOVE:
                        // Windows réinitialise le curseur en permanence : il faut
                        // le réimposer à chaque message.
                        Native.SetCursor(_cursor);
                        if (m.Msg == Native.WM_SETCURSOR)
                        {
                            m.Result = new IntPtr(1);
                            return;
                        }
                        break;

                    case Native.WM_LBUTTONUP:
                        Finish(true, WindowUnderCursor());
                        return;

                    case Native.WM_RBUTTONUP:
                        Finish(false, IntPtr.Zero);
                        return;

                    case Native.WM_CAPTURECHANGED:
                    case Native.WM_CANCELMODE:
                        // Une autre application a pris la capture : on abandonne
                        // plutôt que de rester bloqué dans un mode invisible.
                        Finish(false, IntPtr.Zero);
                        return;
                }
            }
            base.WndProc(ref m);
        }

        private static IntPtr WindowUnderCursor()
        {
            Native.POINT pt;
            if (!Native.GetCursorPos(out pt)) return IntPtr.Zero;
            return Native.GetRootWindow(Native.WindowFromPoint(pt));
        }

        /// <summary>
        /// Épingle dessinée à la volée, pointe en bas : le point chaud est la
        /// pointe, c'est lui qui désigne réellement la fenêtre.
        /// </summary>
        private static IntPtr BuildPinCursor()
        {
            IntPtr hIcon;
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    using (SolidBrush head = new SolidBrush(Color.FromArgb(220, 90, 60)))
                    using (Pen outline = new Pen(Color.White, 2f))
                    using (Pen needle = new Pen(Color.White, 4f))
                    using (Pen needleCore = new Pen(Color.FromArgb(40, 40, 40), 2f))
                    {
                        // Contour blanc : lisibilité sur fond sombre comme clair.
                        g.DrawLine(needle, 12, 14, 12, 29);
                        g.DrawLine(needleCore, 12, 14, 12, 29);
                        g.FillEllipse(head, 4, 2, 17, 15);
                        g.DrawEllipse(outline, 4, 2, 17, 15);
                    }
                }
                hIcon = bmp.GetHicon();
            }

            Native.ICONINFO info;
            if (!Native.GetIconInfo(hIcon, out info))
            {
                Native.DestroyIcon(hIcon);
                return IntPtr.Zero; // SetCursor(NULL) : curseur masqué, mais le mode reste utilisable
            }

            info.fIcon = false;   // fIcon = FALSE transforme l'icône en curseur
            info.xHotspot = 12;
            info.yHotspot = 29;
            IntPtr hCursor = Native.CreateIconIndirect(ref info);

            if (info.hbmColor != IntPtr.Zero) Native.DeleteObject(info.hbmColor);
            if (info.hbmMask != IntPtr.Zero) Native.DeleteObject(info.hbmMask);
            Native.DestroyIcon(hIcon);

            return hCursor;
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
