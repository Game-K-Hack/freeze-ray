using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
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
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RUN_VALUE = "KeepScreen";

        private readonly NotifyIcon _tray;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _targetLabel;
        private readonly ToolStripMenuItem _pinItem;
        private readonly ToolStripMenuItem _topMostItem;
        private readonly ToolStripMenuItem _pinnedRoot;
        private readonly ToolStripMenuItem _unpinAll;
        private readonly ToolStripMenuItem _autoStart;
        private readonly ToolStripMenuItem _cleanOnExit;
        private readonly List<IntPtr> _pinned = new List<IntPtr>();
        private readonly Icon _iconIdle;
        private readonly Icon _iconActive;
        private readonly Timer _focusWatcher;
        private readonly ForegroundHelper _helper;
        private readonly int _ownProcessId;

        /// <summary>
        /// Derniere fenetre "utile" passee au premier plan. Cliquer sur l'icone
        /// donne le focus a la barre des taches : sans ce suivi, la cible serait
        /// perdue au moment meme ou l'on ouvre le menu.
        /// </summary>
        private IntPtr _target = IntPtr.Zero;

        public TrayContext()
        {
            _ownProcessId = Process.GetCurrentProcess().Id;
            _iconIdle = BuildIcon(Color.FromArgb(120, 130, 140));
            _iconActive = BuildIcon(Color.FromArgb(220, 90, 60));
            _helper = new ForegroundHelper();

            _menu = new ContextMenuStrip();
            _menu.Opening += delegate { RefreshMenu(); };

            _targetLabel = new ToolStripMenuItem("Aucune fenetre");
            _targetLabel.Enabled = false;

            _pinItem = new ToolStripMenuItem("Epingler sur tous les bureaux");
            _pinItem.Click += delegate { TogglePin(_target); };

            _topMostItem = new ToolStripMenuItem("Toujours au premier plan");
            _topMostItem.Click += delegate { ToggleTopMost(_target); };

            _pinnedRoot = new ToolStripMenuItem("Aucune fenetre epinglee");
            _pinnedRoot.Enabled = false;

            _unpinAll = new ToolStripMenuItem("Tout desepingler");
            _unpinAll.Click += delegate { UnpinAll(true); };

            _autoStart = new ToolStripMenuItem("Demarrer avec Windows");
            _autoStart.CheckOnClick = true;
            _autoStart.Checked = IsAutoStartEnabled();
            _autoStart.Click += delegate { SetAutoStart(_autoStart.Checked); };

            _cleanOnExit = new ToolStripMenuItem("Tout desepingler en quittant");
            _cleanOnExit.CheckOnClick = true;
            _cleanOnExit.Checked = true;

            ToolStripMenuItem quit = new ToolStripMenuItem("Quitter");
            quit.Click += delegate { ExitThread(); };

            _menu.Items.Add(_targetLabel);
            _menu.Items.Add(_pinItem);
            _menu.Items.Add(_topMostItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_pinnedRoot);
            _menu.Items.Add(_unpinAll);
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
            _tray.MouseClick += OnTrayClick;

            // Le suivi doit rester leger : un sondage court suffit et evite
            // d'installer un hook systeme.
            _focusWatcher = new Timer();
            _focusWatcher.Interval = 200;
            _focusWatcher.Tick += delegate { TrackForeground(); };
            _focusWatcher.Start();
            TrackForeground();

            if (!VirtualDesktop.Available)
            {
                Notify("Bureaux virtuels indisponibles",
                    "Impossible de joindre le shell Windows : " +
                    (VirtualDesktop.InitError ?? "raison inconnue"),
                    ToolTipIcon.Error);
            }
        }

        private void OnTrayClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return; // le clic droit est gere par NotifyIcon
            ShowMenu();
        }

        private void ShowMenu()
        {
            // NotifyIcon sait placer son menu correctement selon le bord ou se
            // trouve la barre des taches ; on reutilise ce placement plutot que
            // de deviner une position a partir du curseur.
            MethodInfo show = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (show != null)
            {
                show.Invoke(_tray, null);
                return;
            }

            // Repli : le passage au premier plan est indispensable pour que le
            // menu se referme quand on clique ailleurs.
            Native.SetForegroundWindow(_helper.Handle);
            _menu.Show(Cursor.Position);
        }

        private void TrackForeground()
        {
            IntPtr hwnd = Native.GetRootWindow(Native.GetForegroundWindow());
            if (!IsUsableTarget(hwnd)) return;
            _target = hwnd;
        }

        private bool IsUsableTarget(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd) || !Native.IsWindowVisible(hwnd))
                return false;
            if (Native.GetProcessId(hwnd) == _ownProcessId)
                return false;

            switch (Native.GetClass(hwnd))
            {
                case "Shell_TrayWnd":            // barre des taches
                case "Shell_SecondaryTrayWnd":
                case "NotifyIconOverflowWindow": // debordement de la zone de notification
                case "Progman":                  // bureau
                case "WorkerW":
                case "Windows.UI.Core.CoreWindow": // menu Demarrer, recherche, centre de notifications
                case "MultitaskingViewFrame":      // vue des taches
                    return false;
            }
            return true;
        }

        private void TogglePin(IntPtr hwnd)
        {
            if (!IsUsableTarget(hwnd))
            {
                Notify("KeepScreen", "Aucune fenetre exploitable.", ToolTipIcon.Warning);
                return;
            }

            if (!VirtualDesktop.Available)
            {
                Notify("Bureaux virtuels indisponibles",
                    VirtualDesktop.InitError ?? "Le shell n'a pas repondu.", ToolTipIcon.Error);
                return;
            }

            string title = Native.GetTitle(hwnd);
            bool wasPinned = VirtualDesktop.IsPinned(hwnd);
            bool ok = wasPinned ? VirtualDesktop.Unpin(hwnd) : VirtualDesktop.Pin(hwnd);

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
                _pinned.Remove(hwnd);
                Notify("Desepinglee", title, ToolTipIcon.Info);
            }
            else
            {
                if (!_pinned.Contains(hwnd)) _pinned.Add(hwnd);
                Notify("Epinglee sur tous les bureaux", title, ToolTipIcon.Info);
            }

            UpdateTrayState();
        }

        private void ToggleTopMost(IntPtr hwnd)
        {
            if (!IsUsableTarget(hwnd)) return;

            bool top = Native.IsTopMost(hwnd);
            IntPtr after = top ? Native.HWND_NOTOPMOST : Native.HWND_TOPMOST;
            bool ok = Native.SetWindowPos(hwnd, after, 0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);

            Notify(ok ? (top ? "Premier plan desactive" : "Toujours au premier plan") : "Echec",
                Native.GetTitle(hwnd),
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

        private static string Ellipsize(string text, int max)
        {
            return text.Length > max ? text.Substring(0, max - 3) + "..." : text;
        }

        private void RefreshMenu()
        {
            PruneDeadWindows();
            // Resynchronisation avec l'etat reel : l'utilisateur a pu desepingler
            // depuis la vue des taches.
            if (VirtualDesktop.Available)
                _pinned.RemoveAll(delegate(IntPtr h) { return !VirtualDesktop.IsPinned(h); });

            bool hasTarget = IsUsableTarget(_target);
            _targetLabel.Text = hasTarget
                ? "Fenetre : " + Ellipsize(Native.GetTitle(_target), 50)
                : "Aucune fenetre selectionnee";

            _pinItem.Enabled = hasTarget && VirtualDesktop.Available;
            _pinItem.Checked = hasTarget && VirtualDesktop.Available && VirtualDesktop.IsPinned(_target);
            _pinItem.Text = _pinItem.Checked
                ? "Ne plus garder sur tous les bureaux"
                : "Garder sur tous les bureaux";

            _topMostItem.Enabled = hasTarget;
            _topMostItem.Checked = hasTarget && Native.IsTopMost(_target);

            _pinnedRoot.DropDownItems.Clear();
            _pinnedRoot.Enabled = _pinned.Count > 0;
            _pinnedRoot.Text = _pinned.Count > 0
                ? "Fenetres epinglees (" + _pinned.Count + ")"
                : "Aucune fenetre epinglee";
            _unpinAll.Enabled = _pinned.Count > 0;

            foreach (IntPtr hwnd in _pinned)
            {
                IntPtr captured = hwnd;
                ToolStripMenuItem item = new ToolStripMenuItem(
                    Ellipsize(Native.GetTitle(captured), 60) + "   (cliquer pour desepingler)");
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
                if (_focusWatcher != null) _focusWatcher.Stop();
                if (_cleanOnExit != null && _cleanOnExit.Checked) UnpinAll(false);

                _tray.Visible = false;
                _tray.Dispose();
                _menu.Dispose();
                if (_focusWatcher != null) _focusWatcher.Dispose();
                _helper.DestroyHandle();
                _iconIdle.Dispose();
                _iconActive.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Fenetre invisible servant uniquement de cible a SetForegroundWindow
    /// lorsque le menu doit etre affiche manuellement.
    /// </summary>
    internal sealed class ForegroundHelper : NativeWindow
    {
        public ForegroundHelper()
        {
            CreateHandle(new CreateParams());
        }
    }
}
