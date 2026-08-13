using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
                    MessageBox.Show("KeepScreen est déjà lancé (icône dans la zone de notification).",
                        "KeepScreen", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayContext());
            }
        }
    }

    /// <summary>Ce que fera le prochain clic de désignation.</summary>
    internal enum PickAction
    {
        AllDesktops,
        TopMost
    }

    internal sealed class TrayContext : ApplicationContext
    {
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RUN_VALUE = "KeepScreen";

        /// <summary>Suivi des marques : assez court pour coller au déplacement.</summary>
        private const int TRACK_INTERVAL = 90;

        /// <summary>Un tick sur dix vérifie l'état réel, qui coûte des appels COM.</summary>
        private const int SYNC_EVERY = 10;

        private readonly NotifyIcon _tray;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _pickAllDesktops;
        private readonly ToolStripMenuItem _pickTopMost;
        private readonly ToolStripMenuItem _managedRoot;
        private readonly ToolStripMenuItem _releaseAll;
        private readonly ToolStripMenuItem _autoStart;
        private readonly ToolStripMenuItem _cleanOnExit;
        private readonly WindowPicker _picker;
        private readonly Timer _pickStarter;
        private readonly Timer _tracker;
        private readonly ForegroundHelper _helper;
        private readonly int _ownProcessId;

        /// <summary>Une marque par fenêtre verrouillée : c'est aussi la liste des cibles.</summary>
        private readonly List<WindowMarker> _markers = new List<WindowMarker>();

        private PickAction _pendingAction;
        private int _tick;

        public TrayContext()
        {
            _ownProcessId = Process.GetCurrentProcess().Id;
            _helper = new ForegroundHelper();

            _picker = new WindowPicker();
            _picker.Picked += OnWindowPicked;
            _picker.Cancelled += delegate { UpdateTrayState(); };

            // Le menu détient encore la capture souris au moment du clic sur une
            // entrée : démarrer la désignation tout de suite la ferait échouer.
            _pickStarter = new Timer();
            _pickStarter.Interval = 60;
            _pickStarter.Tick += delegate
            {
                _pickStarter.Stop();
                _picker.Start();
                UpdateTrayState();
            };

            _tracker = new Timer();
            _tracker.Interval = TRACK_INTERVAL;
            _tracker.Tick += delegate { OnTrack(); };

            _menu = new ContextMenuStrip();
            _menu.Opening += delegate { RefreshMenu(); };

            _pickAllDesktops = new ToolStripMenuItem("Maintenir à l'écran (tous les bureaux)…");
            _pickAllDesktops.ToolTipText =
                "Cliquez ici, puis désignez la fenêtre à conserver lors des changements de bureau.";
            _pickAllDesktops.Click += delegate { BeginPick(PickAction.AllDesktops); };

            _pickTopMost = new ToolStripMenuItem("Premier plan (toujours visible)…");
            _pickTopMost.ToolTipText =
                "Cliquez ici, puis désignez la fenêtre à garder au-dessus des autres.";
            _pickTopMost.Click += delegate { BeginPick(PickAction.TopMost); };

            _managedRoot = new ToolStripMenuItem("Aucune fenêtre verrouillée");
            _managedRoot.Enabled = false;

            _releaseAll = new ToolStripMenuItem("Tout libérer");
            _releaseAll.Click += delegate { ReleaseAll(true); };

            _autoStart = new ToolStripMenuItem("Démarrer avec Windows");
            _autoStart.CheckOnClick = true;
            _autoStart.Checked = IsAutoStartEnabled();
            _autoStart.Click += delegate { SetAutoStart(_autoStart.Checked); };

            _cleanOnExit = new ToolStripMenuItem("Tout libérer en quittant");
            _cleanOnExit.CheckOnClick = true;
            _cleanOnExit.Checked = true;

            ToolStripMenuItem quit = new ToolStripMenuItem("Quitter");
            quit.Click += delegate { ExitThread(); };

            _menu.Items.Add(_pickAllDesktops);
            _menu.Items.Add(_pickTopMost);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_managedRoot);
            _menu.Items.Add(_releaseAll);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_autoStart);
            _menu.Items.Add(_cleanOnExit);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(quit);

            _tray = new NotifyIcon();
            _tray.Icon = Assets.TrayIcon;
            _tray.Text = "KeepScreen";
            _tray.Visible = true;
            _tray.ContextMenuStrip = _menu;
            _tray.MouseClick += OnTrayClick;

            if (!VirtualDesktop.Available)
            {
                Notify("Bureaux virtuels indisponibles",
                    "Impossible de joindre le shell Windows : " +
                    (VirtualDesktop.InitError ?? "raison inconnue"),
                    ToolTipIcon.Error);
            }
        }

        // --- Interaction ---

        private void OnTrayClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return; // le clic droit est géré par NotifyIcon

            // Un clic pendant la désignation sert d'annulation.
            if (_picker.IsActive)
            {
                _picker.Cancel();
                return;
            }
            ShowMenu();
        }

        private void ShowMenu()
        {
            // NotifyIcon sait placer son menu selon le bord où se trouve la barre
            // des tâches ; on réutilise ce placement plutôt que de le deviner.
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

        private void BeginPick(PickAction action)
        {
            if (action == PickAction.AllDesktops && !VirtualDesktop.Available)
            {
                Notify("Bureaux virtuels indisponibles",
                    VirtualDesktop.InitError ?? "Le shell n'a pas répondu.", ToolTipIcon.Error);
                return;
            }
            _pendingAction = action;
            _pickStarter.Start();
        }

        private void OnWindowPicked(IntPtr hwnd)
        {
            UpdateTrayState();

            // Désigner une marque revient à désigner la fenêtre qu'elle signale.
            WindowMarker marker = FindMarkerByHandle(hwnd);
            if (marker != null) hwnd = marker.Target;

            // Cliquer le bureau, la barre des tâches ou l'icône revient à
            // renoncer : on abandonne sans avertissement, comme le ferait Échap.
            if (IsAbortSurface(hwnd)) return;

            // Tout autre refus doit se voir : un échec muet donne l'impression
            // que la fonction ne marche plus.
            if (!IsUsableTarget(hwnd))
            {
                Notify("Fenêtre inutilisable",
                    "KeepScreen n'a pas pu identifier de fenêtre à cet endroit.",
                    ToolTipIcon.Warning);
                return;
            }

            if (_pendingAction == PickAction.AllDesktops)
                ToggleAllDesktops(hwnd);
            else
                ToggleTopMost(hwnd);
        }

        /// <summary>
        /// Surfaces sur lesquelles un clic signifie « laisse tomber » : bureau,
        /// barre des tâches, ou nos propres fenêtres.
        /// </summary>
        private bool IsAbortSurface(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return true;
            if (Native.GetProcessId(hwnd) == _ownProcessId) return true;

            switch (Native.GetClass(hwnd))
            {
                case "Shell_TrayWnd":
                case "Shell_SecondaryTrayWnd":
                case "NotifyIconOverflowWindow":
                case "Progman":
                case "WorkerW":
                case "MultitaskingViewFrame":
                    return true;
            }
            return false;
        }

        private bool IsUsableTarget(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd) || !Native.IsWindowVisible(hwnd))
                return false;
            if (Native.GetProcessId(hwnd) == _ownProcessId)
                return false;

            switch (Native.GetClass(hwnd))
            {
                case "Shell_TrayWnd":            // barre des tâches
                case "Shell_SecondaryTrayWnd":
                case "NotifyIconOverflowWindow": // débordement de la zone de notification
                case "Progman":                  // bureau
                case "WorkerW":
                case "Windows.UI.Core.CoreWindow": // menu Démarrer, recherche, notifications
                case "MultitaskingViewFrame":      // vue des tâches
                    return false;
            }
            return true;
        }

        // --- Actions sur les fenêtres ---

        private void ToggleAllDesktops(IntPtr hwnd)
        {
            string title = Native.GetTitle(hwnd);
            bool wasPinned = VirtualDesktop.IsPinned(hwnd);
            bool ok = wasPinned ? VirtualDesktop.Unpin(hwnd) : VirtualDesktop.Pin(hwnd);

            if (!ok)
            {
                Notify("Échec",
                    "Impossible de modifier « " + title + " ».\n" +
                    "Les fenêtres d'applications lancées en administrateur exigent " +
                    "que KeepScreen le soit aussi.",
                    ToolTipIcon.Error);
                return;
            }

            SyncMarker(hwnd);
            Notify(wasPinned ? "Ne suit plus les bureaux" : "Maintenue sur tous les bureaux",
                title, ToolTipIcon.Info);
        }

        private void ToggleTopMost(IntPtr hwnd)
        {
            string title = Native.GetTitle(hwnd);
            bool wasTop = Native.IsTopMost(hwnd);
            IntPtr after = wasTop ? Native.HWND_NOTOPMOST : Native.HWND_TOPMOST;
            bool ok = Native.SetWindowPos(hwnd, after, 0, 0, 0, 0, Native.SWP_TOPMOST_FLAGS);

            // Une application privilégiée rejette silencieusement nos appels :
            // on relit l'état plutôt que de croire le code de retour.
            if (!ok || Native.IsTopMost(hwnd) == wasTop)
            {
                Notify("Échec",
                    "Impossible de modifier « " + title + " ».\n" +
                    "Une application lancée en administrateur exige que KeepScreen " +
                    "le soit aussi.",
                    ToolTipIcon.Error);
                return;
            }

            SyncMarker(hwnd);
            Notify(wasTop ? "Premier plan désactivé" : "Toujours au premier plan",
                title, ToolTipIcon.Info);
        }

        private bool IsPinned(IntPtr hwnd)
        {
            return VirtualDesktop.Available && VirtualDesktop.IsPinned(hwnd);
        }

        private bool IsLocked(IntPtr hwnd)
        {
            return Native.IsWindow(hwnd) && (IsPinned(hwnd) || Native.IsTopMost(hwnd));
        }

        private void Release(IntPtr hwnd)
        {
            if (Native.IsWindow(hwnd))
            {
                if (IsPinned(hwnd)) VirtualDesktop.Unpin(hwnd);
                if (Native.IsTopMost(hwnd))
                {
                    Native.SetWindowPos(hwnd, Native.HWND_NOTOPMOST, 0, 0, 0, 0,
                        Native.SWP_TOPMOST_FLAGS);
                }
            }
            RemoveMarker(hwnd);
        }

        private void ReleaseAll(bool notify)
        {
            int count = _markers.Count;
            foreach (WindowMarker marker in _markers.ToArray()) Release(marker.Target);
            UpdateTrayState();
            if (notify)
                Notify("KeepScreen", count + " fenêtre(s) libérée(s).", ToolTipIcon.Info);
        }

        // --- Marques posées sur les barres de titre ---

        private WindowMarker FindMarker(IntPtr target)
        {
            foreach (WindowMarker m in _markers)
                if (m.Target == target) return m;
            return null;
        }

        private WindowMarker FindMarkerByHandle(IntPtr handle)
        {
            foreach (WindowMarker m in _markers)
                if (m.Handle == handle) return m;
            return null;
        }

        /// <summary>Crée, met à jour ou retire la marque selon l'état réel de la fenêtre.</summary>
        private void SyncMarker(IntPtr hwnd)
        {
            WindowMarker marker = FindMarker(hwnd);

            if (!IsLocked(hwnd))
            {
                if (marker != null) RemoveMarker(hwnd);
                UpdateTrayState();
                return;
            }

            if (marker == null)
            {
                marker = new WindowMarker(hwnd);
                marker.Clicked += OnMarkerClicked;
                _markers.Add(marker);
                if (!_tracker.Enabled) _tracker.Start();
            }

            // Une fenêtre présente sur tous les bureaux doit emmener sa marque
            // avec elle, sinon la marque resterait sur le bureau d'origine.
            if (IsPinned(hwnd) && !VirtualDesktop.IsPinned(marker.Handle))
                VirtualDesktop.Pin(marker.Handle);
            else if (!IsPinned(hwnd) && VirtualDesktop.IsPinned(marker.Handle))
                VirtualDesktop.Unpin(marker.Handle);

            marker.Update();
            UpdateTrayState();
        }

        private void RemoveMarker(IntPtr target)
        {
            WindowMarker marker = FindMarker(target);
            if (marker == null) return;
            _markers.Remove(marker);
            marker.Dispose();
            if (_markers.Count == 0) _tracker.Stop();
        }

        private void OnMarkerClicked(IntPtr target)
        {
            string title = Native.GetTitle(target);
            Release(target);
            UpdateTrayState();
            Notify("Fenêtre libérée", title, ToolTipIcon.Info);
        }

        private void OnTrack()
        {
            foreach (WindowMarker marker in _markers.ToArray()) marker.Update();

            // La vérification d'état est plus coûteuse : on l'espace.
            if (++_tick % SYNC_EVERY != 0) return;
            _tick = 0;

            foreach (WindowMarker marker in _markers.ToArray())
            {
                if (!IsLocked(marker.Target)) RemoveMarker(marker.Target);
            }
            UpdateTrayState();
        }

        // --- Zone de notification ---

        private void UpdateTrayState()
        {
            string text;
            if (_picker.IsActive)
                text = "KeepScreen — désignez une fenêtre (Échap pour annuler)";
            else if (_markers.Count > 0)
                text = "KeepScreen — " + _markers.Count + " fenêtre(s) verrouillée(s)";
            else
                text = "KeepScreen";

            // NotifyIcon.Text est limité à 63 caractères.
            _tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        private static string Ellipsize(string text, int max)
        {
            return text.Length > max ? text.Substring(0, max - 1) + "…" : text;
        }

        private static string DescribeState(bool pinned, bool topMost)
        {
            if (pinned && topMost) return "tous les bureaux + premier plan";
            if (pinned) return "tous les bureaux";
            return "premier plan";
        }

        private void RefreshMenu()
        {
            foreach (WindowMarker marker in _markers.ToArray())
            {
                if (!IsLocked(marker.Target)) RemoveMarker(marker.Target);
            }

            _pickAllDesktops.Enabled = VirtualDesktop.Available;

            _managedRoot.DropDownItems.Clear();
            _managedRoot.Enabled = _markers.Count > 0;
            _releaseAll.Enabled = _markers.Count > 0;
            _managedRoot.Text = _markers.Count > 0
                ? "Fenêtres verrouillées (" + _markers.Count + ")"
                : "Aucune fenêtre verrouillée";

            foreach (WindowMarker marker in _markers)
            {
                IntPtr captured = marker.Target;
                string state = DescribeState(IsPinned(captured), Native.IsTopMost(captured));
                ToolStripMenuItem item = new ToolStripMenuItem(
                    Ellipsize(Native.GetTitle(captured), 55) + "   [" + state + "]");
                item.ToolTipText = "Cliquer pour libérer cette fenêtre.";
                item.Click += delegate
                {
                    Release(captured);
                    UpdateTrayState();
                };
                _managedRoot.DropDownItems.Add(item);
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

        // --- Démarrage automatique ---

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
                MessageBox.Show("Impossible de modifier le démarrage automatique : " + ex.Message,
                    "KeepScreen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _autoStart.Checked = IsAutoStartEnabled();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pickStarter.Stop();
                _tracker.Stop();
                _picker.Dispose();

                if (_cleanOnExit != null && _cleanOnExit.Checked) ReleaseAll(false);
                foreach (WindowMarker marker in _markers.ToArray()) marker.Dispose();
                _markers.Clear();

                _tray.Visible = false;
                _tray.Dispose();
                _menu.Dispose();
                _pickStarter.Dispose();
                _tracker.Dispose();
                _helper.DestroyHandle();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Fenêtre invisible servant uniquement de cible à SetForegroundWindow
    /// lorsque le menu doit être affiché manuellement.
    /// </summary>
    internal sealed class ForegroundHelper : NativeWindow
    {
        public ForegroundHelper()
        {
            CreateHandle(new CreateParams());
        }
    }
}
