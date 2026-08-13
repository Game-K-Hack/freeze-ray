using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KeepScreen
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (System.Threading.Mutex mutex =
                   new System.Threading.Mutex(true, "KeepScreen.SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("KeepScreen est deja lance (icone dans la zone de notification).",
                        "KeepScreen", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayContext());
            }
        }
    }

    internal sealed class TrayContext : ApplicationContext
    {
        private const int HOTKEY_PIN = 1;
        private const int HOTKEY_TOPMOST = 2;
        private const int HOTKEY_UNPIN_ALL = 3;

        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RUN_VALUE = "KeepScreen";

        private readonly HotkeyWindow _window;
        private readonly NotifyIcon _tray;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _pinnedRoot;
        private readonly ToolStripMenuItem _autoStart;
        private readonly ToolStripMenuItem _cleanOnExit;
        private readonly List<IntPtr> _pinned = new List<IntPtr>();
        private readonly Icon _iconIdle;
        private readonly Icon _iconActive;

        public TrayContext()
        {
            _iconIdle = BuildIcon(Color.FromArgb(120, 130, 140));
            _iconActive = BuildIcon(Color.FromArgb(220, 90, 60));

            _window = new HotkeyWindow();
            _window.HotkeyPressed += OnHotkey;

            _menu = new ContextMenuStrip();
            _menu.Opening += delegate { RefreshMenu(); };

            ToolStripMenuItem pinItem = new ToolStripMenuItem(
                "Epingler / desepingler la fenetre active\tCtrl+Alt+K");
            pinItem.Click += delegate { TogglePin(Native.GetForegroundWindow()); };

            ToolStripMenuItem topItem = new ToolStripMenuItem(
                "Toujours au premier plan\tCtrl+Alt+T");
            topItem.Click += delegate { ToggleTopMost(Native.GetForegroundWindow()); };

            _pinnedRoot = new ToolStripMenuItem("Fenetres epinglees");
            _pinnedRoot.Enabled = false;

            ToolStripMenuItem unpinAll = new ToolStripMenuItem("Tout desepingler\tCtrl+Alt+U");
            unpinAll.Click += delegate { UnpinAll(true); };

            _autoStart = new ToolStripMenuItem("Demarrer avec Windows");
            _autoStart.CheckOnClick = true;
            _autoStart.Checked = IsAutoStartEnabled();
            _autoStart.Click += delegate { SetAutoStart(_autoStart.Checked); };

            _cleanOnExit = new ToolStripMenuItem("Tout desepingler en quittant");
            _cleanOnExit.CheckOnClick = true;
            _cleanOnExit.Checked = true;

            ToolStripMenuItem quit = new ToolStripMenuItem("Quitter");
            quit.Click += delegate { ExitThread(); };

            _menu.Items.Add(pinItem);
            _menu.Items.Add(topItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_pinnedRoot);
            _menu.Items.Add(unpinAll);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_autoStart);
            _menu.Items.Add(_cleanOnExit);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(quit);

            _tray = new NotifyIcon();
            _tray.Icon = _iconIdle;
            _tray.Text = "KeepScreen";
            _tray.Visible = true;
            _tray.ContextMenuStrip = _menu;
            _tray.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                    TogglePin(Native.GetForegroundWindow());
            };

            RegisterHotkeys();

            if (!VirtualDesktop.Available)
            {
                Notify("Bureaux virtuels indisponibles",
                    "Impossible de joindre le shell Windows : " +
                    (VirtualDesktop.InitError ?? "raison inconnue"),
                    ToolTipIcon.Error);
            }
            else
            {
                Notify("KeepScreen actif",
                    "Ctrl+Alt+K : garder la fenetre active sur tous les bureaux.\n" +
                    "Ctrl+Alt+T : toujours au premier plan.",
                    ToolTipIcon.Info);
            }
        }

        private void RegisterHotkeys()
        {
            uint mods = Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT;
            List<string> failed = new List<string>();

            if (!Native.RegisterHotKey(_window.Handle, HOTKEY_PIN, mods, (uint)Keys.K))
                failed.Add("Ctrl+Alt+K");
            if (!Native.RegisterHotKey(_window.Handle, HOTKEY_TOPMOST, mods, (uint)Keys.T))
                failed.Add("Ctrl+Alt+T");
            if (!Native.RegisterHotKey(_window.Handle, HOTKEY_UNPIN_ALL, mods, (uint)Keys.U))
                failed.Add("Ctrl+Alt+U");

            if (failed.Count > 0)
            {
                MessageBox.Show(
                    "Raccourci(s) deja utilise(s) par une autre application : " +
                    string.Join(", ", failed.ToArray()) +
                    "\n\nLe menu de l'icone reste utilisable.",
                    "KeepScreen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnHotkey(object sender, int id)
        {
            switch (id)
            {
                case HOTKEY_PIN:
                    TogglePin(Native.GetForegroundWindow());
                    break;
                case HOTKEY_TOPMOST:
                    ToggleTopMost(Native.GetForegroundWindow());
                    break;
                case HOTKEY_UNPIN_ALL:
                    UnpinAll(true);
                    break;
            }
        }

        private IntPtr ResolveTarget(IntPtr hwnd)
        {
            IntPtr root = Native.GetRootWindow(hwnd);
            if (root == IntPtr.Zero || !Native.IsWindow(root)) return IntPtr.Zero;
            // On ignore notre propre fenetre-message et le bureau.
            if (root == _window.Handle) return IntPtr.Zero;
            return root;
        }

        private void TogglePin(IntPtr hwnd)
        {
            IntPtr target = ResolveTarget(hwnd);
            if (target == IntPtr.Zero)
            {
                Notify("KeepScreen", "Aucune fenetre active exploitable.", ToolTipIcon.Warning);
                return;
            }

            if (!VirtualDesktop.Available)
            {
                Notify("Bureaux virtuels indisponibles",
                    VirtualDesktop.InitError ?? "Le shell n'a pas repondu.", ToolTipIcon.Error);
                return;
            }

            string title = Native.GetTitle(target);
            bool wasPinned = VirtualDesktop.IsPinned(target);
            bool ok = wasPinned ? VirtualDesktop.Unpin(target) : VirtualDesktop.Pin(target);

            if (!ok)
            {
                Notify("Echec",
                    "Impossible de modifier \"" + title + "\".\n" +
                    "Les fenetres d'applications lancees en administrateur exigent " +
                    "que KeepScreen le soit aussi.",
                    ToolTipIcon.Error);
                return;
            }

            if (wasPinned)
            {
                _pinned.Remove(target);
                Notify("Desepinglee", title, ToolTipIcon.Info);
            }
            else
            {
                if (!_pinned.Contains(target)) _pinned.Add(target);
                Notify("Epinglee sur tous les bureaux", title, ToolTipIcon.Info);
            }

            UpdateTrayState();
        }

        private void ToggleTopMost(IntPtr hwnd)
        {
            IntPtr target = ResolveTarget(hwnd);
            if (target == IntPtr.Zero) return;

            bool top = Native.IsTopMost(target);
            IntPtr after = top ? Native.HWND_NOTOPMOST : Native.HWND_TOPMOST;
            bool ok = Native.SetWindowPos(target, after, 0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);

            Notify(ok ? (top ? "Premier plan desactive" : "Toujours au premier plan") : "Echec",
                Native.GetTitle(target),
                ok ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        private void UnpinAll(bool notify)
        {
            PruneDeadWindows();
            int count = 0;
            foreach (IntPtr hwnd in _pinned.ToArray())
            {
                if (VirtualDesktop.Unpin(hwnd)) count++;
            }
            _pinned.Clear();
            UpdateTrayState();
            if (notify)
                Notify("KeepScreen", count + " fenetre(s) desepinglee(s).", ToolTipIcon.Info);
        }

        private void PruneDeadWindows()
        {
            _pinned.RemoveAll(delegate(IntPtr h) { return !Native.IsWindow(h); });
        }

        private void UpdateTrayState()
        {
            PruneDeadWindows();
            _tray.Icon = _pinned.Count > 0 ? _iconActive : _iconIdle;
            string text = _pinned.Count > 0
                ? "KeepScreen - " + _pinned.Count + " fenetre(s) epinglee(s)"
                : "KeepScreen";
            // NotifyIcon.Text est limite a 63 caracteres.
            _tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        private void RefreshMenu()
        {
            PruneDeadWindows();
            // On resynchronise avec l'etat reel : l'utilisateur a pu desepingler
            // depuis la vue des taches.
            _pinned.RemoveAll(delegate(IntPtr h) { return !VirtualDesktop.IsPinned(h); });

            _pinnedRoot.DropDownItems.Clear();
            _pinnedRoot.Enabled = _pinned.Count > 0;
            _pinnedRoot.Text = _pinned.Count > 0
                ? "Fenetres epinglees (" + _pinned.Count + ")"
                : "Aucune fenetre epinglee";

            foreach (IntPtr hwnd in _pinned)
            {
                IntPtr captured = hwnd;
                string title = Native.GetTitle(captured);
                if (title.Length > 60) title = title.Substring(0, 57) + "...";
                ToolStripMenuItem item = new ToolStripMenuItem(title + "   (cliquer pour desepingler)");
                item.Click += delegate { TogglePin(captured); };
                _pinnedRoot.DropDownItems.Add(item);
            }

            UpdateTrayState();
        }

        private void Notify(string title, string text, ToolTipIcon icon)
        {
            _tray.BalloonTipTitle = title;
            _tray.BalloonTipText = text;
            _tray.BalloonTipIcon = icon;
            _tray.ShowBalloonTip(2500);
        }

        private static bool IsAutoStartEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false))
            {
                return key != null && key.GetValue(RUN_VALUE) != null;
            }
        }

        private void SetAutoStart(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                {
                    if (key == null) return;
                    if (enabled)
                        key.SetValue(RUN_VALUE, "\"" + Application.ExecutablePath + "\"");
                    else
                        key.DeleteValue(RUN_VALUE, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible de modifier le demarrage automatique : " + ex.Message,
                    "KeepScreen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _autoStart.Checked = IsAutoStartEnabled();
            }
        }

        /// <summary>Petite epingle dessinee a la volee : evite d'embarquer un .ico.</summary>
        private static Icon BuildIcon(Color color)
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (SolidBrush brush = new SolidBrush(color))
                using (Pen pen = new Pen(color, 3f))
                {
                    g.FillEllipse(brush, 9, 3, 14, 14);
                    g.DrawLine(pen, 16, 16, 16, 29);
                }
                IntPtr h = bmp.GetHicon();
                using (Icon tmp = Icon.FromHandle(h))
                {
                    return (Icon)tmp.Clone();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cleanOnExit != null && _cleanOnExit.Checked) UnpinAll(false);

                Native.UnregisterHotKey(_window.Handle, HOTKEY_PIN);
                Native.UnregisterHotKey(_window.Handle, HOTKEY_TOPMOST);
                Native.UnregisterHotKey(_window.Handle, HOTKEY_UNPIN_ALL);

                _tray.Visible = false;
                _tray.Dispose();
                _menu.Dispose();
                _window.DestroyHandle();
                _iconIdle.Dispose();
                _iconActive.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>Fenetre invisible dediee a la reception des WM_HOTKEY.</summary>
    internal sealed class HotkeyWindow : NativeWindow
    {
        public event EventHandler<int> HotkeyPressed;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY && HotkeyPressed != null)
                HotkeyPressed(this, m.WParam.ToInt32());
            base.WndProc(ref m);
        }
    }
}
