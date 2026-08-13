using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FreezeRay
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Strings.Current = Settings.Load().Language;

            // Appelé depuis « Applications et fonctionnalités ». Traité avant le
            // verrou d'instance unique : on doit pouvoir désinstaller même si une
            // copie tourne déjà.
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, "--uninstall", StringComparison.OrdinalIgnoreCase))
                {
                    Application.EnableVisualStyles();
                    Uninstaller.Run();
                    return;
                }
            }

            bool createdNew;
            using (System.Threading.Mutex mutex =
                   new System.Threading.Mutex(true, "FreezeRay.SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(Strings.T("app.alreadyRunning"), Strings.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        private const string RUN_VALUE = "Freeze Ray";

        /// <summary>Nom porté par l'application avant d'être renommée.</summary>
        private const string LEGACY_RUN_VALUE = "KeepScreen";

        /// <summary>Suivi des marques : assez court pour coller au déplacement.</summary>
        private const int TRACK_INTERVAL = 90;

        /// <summary>Un tick sur dix vérifie l'état réel, qui coûte des appels COM.</summary>
        private const int SYNC_EVERY = 10;

        private readonly Settings _settings;
        private readonly NotifyIcon _tray;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _pickAllDesktops;
        private readonly ToolStripMenuItem _pickTopMost;
        private readonly ToolStripMenuItem _managedRoot;
        private readonly ToolStripMenuItem _releaseAll;
        private readonly ToolStripMenuItem _settingsItem;
        private readonly ToolStripMenuItem _quit;
        private readonly WindowPicker _picker;
        private readonly Timer _pickStarter;
        private readonly Timer _tracker;
        private readonly ForegroundHelper _helper;
        private readonly int _ownProcessId;

        /// <summary>Une marque par fenêtre verrouillée : c'est aussi la liste des cibles.</summary>
        private readonly List<WindowMarker> _markers = new List<WindowMarker>();

        private PickAction _pendingAction;
        private int _tick;
        private SettingsForm _settingsForm;
        private Control _sync;

        /// <summary>Page à ouvrir si l'utilisateur clique la bulle de mise à jour.</summary>
        private string _updatePageUrl;

        public TrayContext()
        {
            MigrateLegacyAutoStart();

            _settings = Settings.Load();
            Strings.Current = _settings.Language;

            // Premier lancement : on dépose le fichier de réglages tout de suite,
            // pour que le dossier d'installation le contienne visiblement sans
            // attendre une première visite des paramètres.
            if (!System.IO.File.Exists(Settings.FilePath)) _settings.Save();

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

            _pickAllDesktops = new ToolStripMenuItem();
            _pickAllDesktops.Click += delegate { BeginPick(PickAction.AllDesktops); };

            _pickTopMost = new ToolStripMenuItem();
            _pickTopMost.Click += delegate { BeginPick(PickAction.TopMost); };

            _managedRoot = new ToolStripMenuItem();
            _managedRoot.Enabled = false;

            _releaseAll = new ToolStripMenuItem();
            _releaseAll.Click += delegate { ReleaseAll(true); };

            _settingsItem = new ToolStripMenuItem();
            _settingsItem.Click += delegate { ShowSettings(); };

            _quit = new ToolStripMenuItem();
            _quit.Click += delegate { ExitThread(); };

            _menu.Items.Add(_pickAllDesktops);
            _menu.Items.Add(_pickTopMost);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_managedRoot);
            _menu.Items.Add(_releaseAll);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_settingsItem);
            _menu.Items.Add(_quit);

            _tray = new NotifyIcon();
            _tray.Icon = Assets.TrayIcon;
            _tray.Visible = true;
            _tray.ContextMenuStrip = _menu;
            _tray.MouseClick += OnTrayClick;
            _tray.BalloonTipClicked += OnBalloonClicked;

            ApplyLanguage();

            // La réponse arrive depuis un thread de travail : ce contrôle sert
            // uniquement à repasser sur le fil de l'interface.
            _sync = new Control();
            _sync.CreateControl();
            if (_settings.CheckUpdatesAtStartup) CheckUpdatesInBackground();

            if (!VirtualDesktop.Available)
            {
                Notify(Strings.T("notif.vd.title"),
                    Strings.T("notif.vd.text",
                        VirtualDesktop.InitError ?? Strings.T("notif.vd.unknown")),
                    ToolTipIcon.Error);
            }
        }

        // --- Langue ---

        /// <summary>Réapplique tous les libellés : appelé au démarrage et à chaque changement de langue.</summary>
        private void ApplyLanguage()
        {
            _pickAllDesktops.Text = Strings.T("menu.allDesktops");
            _pickAllDesktops.ToolTipText = Strings.T("menu.allDesktops.tip");
            _pickTopMost.Text = Strings.T("menu.topMost");
            _pickTopMost.ToolTipText = Strings.T("menu.topMost.tip");
            _releaseAll.Text = Strings.T("menu.releaseAll");
            _settingsItem.Text = Strings.T("menu.settings");
            _quit.Text = Strings.T("menu.quit");
            _managedRoot.Text = Strings.T("menu.noLocked");
            UpdateTrayState();
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

        private void ShowSettings()
        {
            // Une seule fenêtre à la fois : un second clic ramène la première.
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                if (_settingsForm.WindowState == FormWindowState.Minimized)
                    _settingsForm.WindowState = FormWindowState.Normal;
                _settingsForm.Activate();
                return;
            }

            _settingsForm = new SettingsForm(_settings, IsAutoStartEnabled, SetAutoStart,
                ApplyLanguage);
            _settingsForm.FormClosed += delegate { _settingsForm = null; };
            _settingsForm.Show();
            _settingsForm.Activate();
        }

        private void BeginPick(PickAction action)
        {
            if (action == PickAction.AllDesktops && !VirtualDesktop.Available)
            {
                Notify(Strings.T("notif.vd.title"),
                    VirtualDesktop.InitError ?? Strings.T("notif.vd.noAnswer"),
                    ToolTipIcon.Error);
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
                Notify(Strings.T("notif.unusable.title"), Strings.T("notif.unusable.text"),
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
                Notify(Strings.T("notif.failed.title"), Strings.T("notif.failed.pin", title),
                    ToolTipIcon.Error);
                return;
            }

            SyncMarker(hwnd);
            Notify(Strings.T(wasPinned ? "notif.desktops.off" : "notif.desktops.on"),
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
                Notify(Strings.T("notif.failed.title"), Strings.T("notif.failed.topMost", title),
                    ToolTipIcon.Error);
                return;
            }

            SyncMarker(hwnd);
            Notify(Strings.T(wasTop ? "notif.topMost.off" : "notif.topMost.on"),
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
                Notify(Strings.AppName, Strings.T("notif.released.count", count), ToolTipIcon.Info);
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
            Notify(Strings.T("notif.released.title"), title, ToolTipIcon.Info);
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
                text = Strings.T("tray.picking");
            else if (_markers.Count > 0)
                text = Strings.T("tray.locked", _markers.Count);
            else
                text = Strings.AppName;

            // NotifyIcon.Text est limité à 63 caractères.
            _tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        private static string Ellipsize(string text, int max)
        {
            return text.Length > max ? text.Substring(0, max - 1) + "…" : text;
        }

        private static string DescribeState(bool pinned, bool topMost)
        {
            if (pinned && topMost) return Strings.T("state.both");
            if (pinned) return Strings.T("state.desktops");
            return Strings.T("state.topMost");
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
                ? Strings.T("menu.locked", _markers.Count)
                : Strings.T("menu.noLocked");

            foreach (WindowMarker marker in _markers)
            {
                IntPtr captured = marker.Target;
                string state = DescribeState(IsPinned(captured), Native.IsTopMost(captured));
                ToolStripMenuItem item = new ToolStripMenuItem(
                    Ellipsize(Native.GetTitle(captured), 55) + "   [" + state + "]");
                item.ToolTipText = Strings.T("menu.releaseTip");
                item.Click += delegate
                {
                    Release(captured);
                    UpdateTrayState();
                };
                _managedRoot.DropDownItems.Add(item);
            }

            UpdateTrayState();
        }

        /// <summary>
        /// Les erreurs passent toujours : les taire redonnerait l'impression
        /// d'une fonction qui ne marche pas. Le réglage ne concerne donc que les
        /// notifications d'information.
        /// </summary>
        private void Notify(string title, string text, ToolTipIcon icon)
        {
            if (!_settings.ShowNotifications && icon == ToolTipIcon.Info) return;
            Notifications.Show(_tray, title, text, icon, 2500);
        }

        // --- Mise à jour au démarrage ---

        /// <summary>
        /// Vérification discrète au lancement : rien ne s'affiche si la version
        /// est à jour, ni si GitHub est injoignable. Seule une nouvelle version
        /// mérite d'interrompre l'utilisateur, et par une bulle plutôt que par
        /// une fenêtre qui volerait le focus au démarrage de la session.
        /// </summary>
        private void CheckUpdatesInBackground()
        {
            Updater.CheckAsync(delegate(UpdateResult result)
            {
                if (result.Status != UpdateStatus.Available) return;
                if (_sync == null || _sync.IsDisposed || !_sync.IsHandleCreated) return;

                _sync.BeginInvoke((MethodInvoker)delegate
                {
                    _updatePageUrl = result.PageUrl;
                    Notify(Strings.T("update.availableTitle"),
                        Strings.T("update.availableBalloon", result.LatestVersion),
                        ToolTipIcon.Info);
                });
            });
        }

        private void OnBalloonClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_updatePageUrl)) return;
            string url = _updatePageUrl;
            _updatePageUrl = null;
            try
            {
                Process.Start(url);
            }
            catch (Exception)
            {
                // Navigateur indisponible : la page reste accessible depuis les paramètres.
            }
        }

        // --- Démarrage automatique ---

        /// <summary>
        /// Reprend l'entrée de démarrage laissée par l'ancien nom : elle pointe
        /// vers un exécutable qui n'existe plus, et resterait sinon orpheline.
        /// </summary>
        private static void MigrateLegacyAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                {
                    if (key == null || key.GetValue(LEGACY_RUN_VALUE) == null) return;
                    key.DeleteValue(LEGACY_RUN_VALUE, false);
                    key.SetValue(RUN_VALUE, "\"" + Application.ExecutablePath + "\"");
                }
            }
            catch (Exception)
            {
                // Le démarrage automatique reste réglable depuis les paramètres.
            }
        }

        private static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false))
                {
                    return key != null && key.GetValue(RUN_VALUE) != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Renvoie faux si le registre a refusé, pour que l'interface se réaligne.</summary>
        private bool SetAutoStart(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
                {
                    if (key == null) return false;
                    if (enabled)
                        key.SetValue(RUN_VALUE, "\"" + Application.ExecutablePath + "\"");
                    else
                        key.DeleteValue(RUN_VALUE, false);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.T("notif.autostart.error", ex.Message),
                    Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pickStarter.Stop();
                _tracker.Stop();
                _picker.Dispose();

                if (_settingsForm != null && !_settingsForm.IsDisposed) _settingsForm.Close();
                if (_settings != null && _settings.ReleaseAllOnExit) ReleaseAll(false);

                foreach (WindowMarker marker in _markers.ToArray()) marker.Dispose();
                _markers.Clear();

                _tray.Visible = false;
                _tray.Dispose();
                _menu.Dispose();
                _pickStarter.Dispose();
                _tracker.Dispose();
                _helper.DestroyHandle();
                if (_sync != null) _sync.Dispose();
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
