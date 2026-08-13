using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FreezeRay
{
    internal static class SetupProgram
    {
        [STAThread]
        private static void Main()
        {
            Strings.Current = Strings.Detect();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }
    }

    /// <summary>
    /// Assistant d'installation en trois volets : licence, options, résultat.
    ///
    /// Construit en code, comme le reste du projet : aucun outil tiers n'est
    /// nécessaire, l'installeur se compile avec le compilateur livré par Windows.
    /// L'application et la licence voyagent en ressources dans cet exécutable.
    /// </summary>
    internal sealed class SetupForm : Form
    {
        private const string APP_RESOURCE = "FreezeRay.app.exe";
        private const string LICENSE_RESOURCE = "FreezeRay.license.md";

        private readonly Panel _licensePage;
        private readonly Panel _optionsPage;
        private readonly Panel _donePage;

        private readonly TextBox _license;
        private readonly CheckBox _accept;
        private readonly TextBox _folder;
        private readonly CheckBox _desktop;
        private readonly CheckBox _startMenu;
        private readonly CheckBox _launch;
        private readonly Label _doneLabel;

        private readonly Button _back;
        private readonly Button _next;
        private readonly Button _cancel;

        private readonly PictureBox _banner;
        private readonly Label _heading;

        private int _page;
        private bool _installed;

        public SetupForm()
        {
            Text = Strings.T("setup.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(520, 420);
            Icon = Assets.GetIcon(32);

            _banner = new PictureBox();
            _banner.Image = Assets.RenderBanner(48);
            _banner.SizeMode = PictureBoxSizeMode.AutoSize;
            _banner.Location = new Point(16, 12);

            _heading = new Label();
            _heading.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
            _heading.AutoSize = true;
            _heading.Location = new Point(76, 26);

            // --- Volet 1 : licence ---
            _licensePage = NewPage();

            Label intro = new Label();
            intro.Text = Strings.T("setup.licenseIntro");
            intro.AutoSize = true;
            intro.Location = new Point(0, 0);

            _license = new TextBox();
            _license.Multiline = true;
            _license.ReadOnly = true;
            _license.ScrollBars = ScrollBars.Vertical;
            _license.WordWrap = true;
            _license.BackColor = Color.White;
            _license.Location = new Point(0, 24);
            _license.Size = new Size(488, 220);
            _license.Text = ReadLicense();
            // Sans cela la zone reçoit le focus au démarrage et affiche tout le
            // texte en surbrillance, comme si l'utilisateur l'avait sélectionné.
            _license.TabStop = false;
            _license.Select(0, 0);

            _accept = new CheckBox();
            _accept.Text = Strings.T("setup.accept");
            _accept.AutoSize = true;
            _accept.Location = new Point(0, 254);
            _accept.CheckedChanged += delegate { UpdateButtons(); };

            _licensePage.Controls.Add(intro);
            _licensePage.Controls.Add(_license);
            _licensePage.Controls.Add(_accept);

            // --- Volet 2 : options ---
            _optionsPage = NewPage();

            Label folderLabel = new Label();
            folderLabel.Text = Strings.T("setup.folder");
            folderLabel.AutoSize = true;
            folderLabel.Location = new Point(0, 4);

            _folder = new TextBox();
            _folder.Location = new Point(0, 24);
            _folder.Size = new Size(386, 20);
            _folder.Text = InstallPaths.DefaultFolder;

            Button browse = new Button();
            browse.Text = Strings.T("setup.browse");
            browse.Location = new Point(394, 22);
            browse.Size = new Size(94, 24);
            browse.Click += OnBrowse;

            Label folderNote = new Label();
            folderNote.Text = Strings.T("setup.folderNote");
            folderNote.ForeColor = SystemColors.GrayText;
            folderNote.Location = new Point(0, 50);
            folderNote.Size = new Size(488, 32);

            _desktop = new CheckBox();
            _desktop.Text = Strings.T("setup.desktopShortcut");
            _desktop.AutoSize = true;
            _desktop.Checked = true;
            _desktop.Location = new Point(0, 92);

            _startMenu = new CheckBox();
            _startMenu.Text = Strings.T("setup.startMenuShortcut");
            _startMenu.AutoSize = true;
            _startMenu.Checked = true;
            _startMenu.Location = new Point(0, 118);

            _optionsPage.Controls.Add(folderLabel);
            _optionsPage.Controls.Add(_folder);
            _optionsPage.Controls.Add(browse);
            _optionsPage.Controls.Add(folderNote);
            _optionsPage.Controls.Add(_desktop);
            _optionsPage.Controls.Add(_startMenu);

            // --- Volet 3 : résultat ---
            _donePage = NewPage();

            _doneLabel = new Label();
            _doneLabel.Location = new Point(0, 8);
            _doneLabel.Size = new Size(488, 60);

            _launch = new CheckBox();
            _launch.Text = Strings.T("setup.launch");
            _launch.AutoSize = true;
            _launch.Checked = true;
            _launch.Location = new Point(0, 76);

            _donePage.Controls.Add(_doneLabel);
            _donePage.Controls.Add(_launch);

            // --- Boutons ---
            _back = new Button();
            _back.Text = Strings.T("setup.back");
            _back.Size = new Size(100, 28);
            _back.Location = new Point(196, 376);
            _back.Click += delegate { Show(_page - 1); };

            _next = new Button();
            _next.Size = new Size(100, 28);
            _next.Location = new Point(304, 376);
            _next.Click += OnNext;

            _cancel = new Button();
            _cancel.Text = Strings.T("setup.cancel");
            _cancel.Size = new Size(100, 28);
            _cancel.Location = new Point(408, 376);
            _cancel.Click += delegate { Close(); };

            Controls.Add(_banner);
            Controls.Add(_heading);
            Controls.Add(_licensePage);
            Controls.Add(_optionsPage);
            Controls.Add(_donePage);
            Controls.Add(_back);
            Controls.Add(_next);
            Controls.Add(_cancel);

            Show(0);
        }

        private Panel NewPage()
        {
            Panel page = new Panel();
            page.Location = new Point(16, 76);
            page.Size = new Size(488, 290);
            page.Visible = false;
            return page;
        }

        private void Show(int page)
        {
            _page = page;
            _licensePage.Visible = page == 0;
            _optionsPage.Visible = page == 1;
            _donePage.Visible = page == 2;

            switch (page)
            {
                case 0: _heading.Text = Strings.T("setup.licenseTitle"); break;
                case 1: _heading.Text = Strings.T("setup.optionsTitle"); break;
                default: _heading.Text = Strings.AppName; break;
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _back.Visible = _page == 1;
            _cancel.Visible = _page < 2;

            if (_page == 0)
            {
                _next.Text = Strings.T("setup.next");
                _next.Enabled = _accept.Checked; // la licence doit être acceptée
            }
            else if (_page == 1)
            {
                _next.Text = Strings.T("setup.install");
                _next.Enabled = true;
            }
            else
            {
                _next.Text = Strings.T("setup.close");
                _next.Enabled = true;
            }
        }

        private void OnBrowse(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = Strings.T("setup.folder");
                dialog.SelectedPath = _folder.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    _folder.Text = Path.Combine(dialog.SelectedPath, Strings.AppName);
            }
        }

        private void OnNext(object sender, EventArgs e)
        {
            if (_page == 0) { Show(1); return; }
            if (_page == 2) { Close(); return; }

            // Un exécutable en cours d'utilisation ne peut pas être remplacé :
            // mieux vaut le dire que d'échouer avec un message système obscur.
            if (Process.GetProcessesByName("Freeze Ray").Length > 0)
            {
                MessageBox.Show(this, Strings.T("setup.running"), Strings.T("setup.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Install(_folder.Text.Trim());
                _installed = true;
                _doneLabel.Text = Strings.T("setup.done");
                Show(2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Strings.T("setup.error", ex.Message),
                    Strings.T("setup.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Install(string folder)
        {
            Directory.CreateDirectory(folder);

            string exePath = Path.Combine(folder, InstallPaths.ExeName);
            WriteResource(APP_RESOURCE, exePath);
            WriteResource(LICENSE_RESOURCE, Path.Combine(folder, InstallPaths.LicenseName));

            if (_desktop.Checked)
                Shortcuts.Create(InstallPaths.DesktopShortcut, exePath, Strings.AppName);
            if (_startMenu.Checked)
                Shortcuts.Create(InstallPaths.StartMenuShortcut, exePath, Strings.AppName);

            Register(folder, exePath);
        }

        /// <summary>Inscription dans « Applications et fonctionnalités ».</summary>
        private static void Register(string folder, string exePath)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(InstallPaths.UninstallKey))
            {
                if (key == null) return;
                key.SetValue("DisplayName", Strings.AppName);
                key.SetValue("DisplayVersion", Updater.CurrentVersionText);
                key.SetValue("DisplayIcon", exePath);
                key.SetValue("InstallLocation", folder);
                key.SetValue("UninstallString", "\"" + exePath + "\" --uninstall");
                key.SetValue("URLInfoAbout", Updater.ReleasesUrl);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);

                try
                {
                    key.SetValue("EstimatedSize",
                        (int)(new FileInfo(exePath).Length / 1024), RegistryValueKind.DWord);
                }
                catch (IOException)
                {
                }
            }
        }

        private static void WriteResource(string name, string destination)
        {
            using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (source == null)
                    throw new FileNotFoundException("ressource absente : " + name);

                using (FileStream target = File.Create(destination))
                {
                    source.CopyTo(target);
                }
            }
        }

        /// <summary>
        /// La licence est écrite en Markdown et contient une version par langue.
        /// On retire les marques de mise en forme, et on ne présente que la
        /// section correspondant à la langue en cours — lire d'abord un texte
        /// juridique dans une langue étrangère n'aide personne. Repli sur
        /// l'anglais pour les langues que la licence ne traduit pas encore.
        /// </summary>
        private static string ReadLicense()
        {
            string raw = ReadResourceText(LICENSE_RESOURCE);
            if (raw.Length == 0) return string.Empty;

            string wanted = Strings.Current == Language.French ? "Français" : "English";
            StringBuilder header = new StringBuilder();
            StringBuilder section = new StringBuilder();
            StringBuilder fallback = new StringBuilder();
            StringBuilder current = header;

            foreach (string line in raw.Replace("\r\n", "\n").Split('\n'))
            {
                if (line.StartsWith("## "))
                {
                    string name = line.Substring(3).Trim();
                    if (name == wanted) current = section;
                    else if (name == "English") current = fallback;
                    else current = null;
                    continue;
                }

                if (current == null) continue;

                string clean = line.Replace("**", "").Replace("`", "");
                if (clean.StartsWith("---")) continue;
                while (clean.StartsWith("#")) clean = clean.Substring(1);
                current.AppendLine(clean.TrimStart());
            }

            StringBuilder chosen = section.Length > 0 ? section : fallback;
            return (header.ToString().TrimEnd() + "\r\n\r\n" + chosen.ToString().Trim())
                .Replace("\n", "\r\n").Replace("\r\r\n", "\r\n");
        }

        private static string ReadResourceText(string name)
        {
            using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (source == null) return string.Empty;
                using (StreamReader reader = new StreamReader(source, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_installed && _launch.Checked)
            {
                try
                {
                    Process.Start(Path.Combine(_folder.Text.Trim(), InstallPaths.ExeName));
                }
                catch (Exception)
                {
                    // L'installation a réussi : ne pas la signaler comme un échec.
                }
            }
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _banner != null && _banner.Image != null) _banner.Image.Dispose();
            base.Dispose(disposing);
        }
    }
}
