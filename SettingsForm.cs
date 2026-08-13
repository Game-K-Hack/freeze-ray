using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FreezeRay
{
    /// <summary>
    /// Fenêtre de réglages. Construite en code plutôt qu'au concepteur : le
    /// projet se compile avec le seul csc fourni par Windows, sans fichiers
    /// .resx ni génération.
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        private readonly Settings _settings;
        private readonly Func<bool> _readAutoStart;
        private readonly Func<bool, bool> _writeAutoStart;
        private readonly Action _onChanged;

        private readonly Label _title;
        private readonly Label _version;
        private readonly GroupBox _generalBox;
        private readonly CheckBox _startWithWindows;
        private readonly CheckBox _releaseOnExit;
        private readonly CheckBox _notifications;
        private readonly Label _notificationsHint;
        private readonly Label _languageLabel;
        private readonly ComboBox _language;
        private readonly GroupBox _updateBox;
        private readonly CheckBox _checkAtStartup;
        private readonly Button _check;
        private readonly Label _updateStatus;
        private readonly Button _close;
        private readonly PictureBox _logo;

        /// <summary>Empêche les gestionnaires de réagir pendant qu'on remplit les contrôles.</summary>
        private bool _loading;

        public SettingsForm(Settings settings, Func<bool> readAutoStart,
                            Func<bool, bool> writeAutoStart, Action onChanged)
        {
            _settings = settings;
            _readAutoStart = readAutoStart;
            _writeAutoStart = writeAutoStart;
            _onChanged = onChanged;

            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(430, 404);
            Icon = Assets.GetIcon(32);

            // Illustration propre à cet en-tête : les autres usages du logo
            // (zone de notification, curseur, marque) restent inchangés.
            _logo = new PictureBox();
            _logo.Image = Assets.RenderBanner(64);
            _logo.SizeMode = PictureBoxSizeMode.AutoSize;
            _logo.Location = new Point(14, 12);

            _title = new Label();
            _title.Text = Strings.AppName;
            _title.Font = new Font(Font.FontFamily, 14f, FontStyle.Bold);
            _title.AutoSize = true;
            _title.Location = new Point(88, 24);

            _version = new Label();
            _version.AutoSize = true;
            _version.ForeColor = SystemColors.GrayText;
            _version.Location = new Point(90, 52);

            _generalBox = new GroupBox();
            _generalBox.Location = new Point(16, 80);
            _generalBox.Size = new Size(398, 168);

            _startWithWindows = new CheckBox();
            _startWithWindows.Location = new Point(14, 26);
            _startWithWindows.AutoSize = true;
            _startWithWindows.CheckedChanged += OnAutoStartChanged;

            _releaseOnExit = new CheckBox();
            _releaseOnExit.Location = new Point(14, 52);
            _releaseOnExit.AutoSize = true;
            _releaseOnExit.CheckedChanged += OnSimpleChanged;

            _notifications = new CheckBox();
            _notifications.Location = new Point(14, 78);
            _notifications.AutoSize = true;
            _notifications.CheckedChanged += OnSimpleChanged;

            _notificationsHint = new Label();
            _notificationsHint.Location = new Point(32, 100);
            _notificationsHint.Size = new Size(350, 16);
            _notificationsHint.ForeColor = SystemColors.GrayText;

            _languageLabel = new Label();
            _languageLabel.Location = new Point(14, 130);
            _languageLabel.AutoSize = true;

            _language = new ComboBox();
            _language.DropDownStyle = ComboBoxStyle.DropDownList;
            _language.Location = new Point(90, 126);
            _language.Size = new Size(160, 21);
            // La liste se remplit depuis le registre de langues : ajouter une
            // traduction ne demande aucune retouche ici.
            foreach (Strings.Entry entry in Strings.All) _language.Items.Add(entry.NativeName);
            _language.SelectedIndexChanged += OnLanguageChanged;

            _generalBox.Controls.Add(_startWithWindows);
            _generalBox.Controls.Add(_releaseOnExit);
            _generalBox.Controls.Add(_notifications);
            _generalBox.Controls.Add(_notificationsHint);
            _generalBox.Controls.Add(_languageLabel);
            _generalBox.Controls.Add(_language);

            _updateBox = new GroupBox();
            _updateBox.Location = new Point(16, 260);
            _updateBox.Size = new Size(398, 96);

            _checkAtStartup = new CheckBox();
            _checkAtStartup.Location = new Point(14, 24);
            _checkAtStartup.AutoSize = true;
            _checkAtStartup.CheckedChanged += OnSimpleChanged;

            _check = new Button();
            _check.Location = new Point(14, 54);
            _check.Size = new Size(210, 26);
            _check.Click += OnCheckClicked;

            _updateStatus = new Label();
            _updateStatus.Location = new Point(232, 60);
            _updateStatus.Size = new Size(150, 16);
            _updateStatus.ForeColor = SystemColors.GrayText;

            _updateBox.Controls.Add(_checkAtStartup);
            _updateBox.Controls.Add(_check);
            _updateBox.Controls.Add(_updateStatus);

            _close = new Button();
            _close.Location = new Point(324, 368);
            _close.Size = new Size(90, 26);
            _close.Click += delegate { Close(); };

            Controls.Add(_logo);
            Controls.Add(_title);
            Controls.Add(_version);
            Controls.Add(_generalBox);
            Controls.Add(_updateBox);
            Controls.Add(_close);

            AcceptButton = _close;
            CancelButton = _close;

            LoadValues();
            ApplyTexts();
        }

        private void LoadValues()
        {
            _loading = true;
            _startWithWindows.Checked = _readAutoStart();
            _releaseOnExit.Checked = _settings.ReleaseAllOnExit;
            _notifications.Checked = _settings.ShowNotifications;
            _language.SelectedIndex = Strings.IndexOf(_settings.Language);
            _checkAtStartup.Checked = _settings.CheckUpdatesAtStartup;
            _loading = false;
        }

        /// <summary>Applique la langue courante à tous les libellés.</summary>
        private void ApplyTexts()
        {
            Text = Strings.AppName + " — " + Strings.T("settings.title");
            _version.Text = Strings.T("settings.version", Updater.CurrentVersionText);
            _generalBox.Text = Strings.T("settings.general");
            _startWithWindows.Text = Strings.T("settings.startWithWindows");
            _releaseOnExit.Text = Strings.T("settings.releaseOnExit");
            _notifications.Text = Strings.T("settings.notifications");
            _notificationsHint.Text = Strings.T("settings.notificationsHint");
            _languageLabel.Text = Strings.T("settings.language");
            _updateBox.Text = Strings.T("settings.updates");
            _checkAtStartup.Text = Strings.T("settings.checkAtStartup");
            _check.Text = Strings.T("settings.check");
            _close.Text = Strings.T("settings.close");
        }

        private void OnSimpleChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            Commit();
        }

        private void OnAutoStartChanged(object sender, EventArgs e)
        {
            if (_loading) return;

            // Le registre peut refuser : on réaligne la case sur son état réel.
            if (!_writeAutoStart(_startWithWindows.Checked))
            {
                _loading = true;
                _startWithWindows.Checked = _readAutoStart();
                _loading = false;
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            Strings.Current = Strings.All[_language.SelectedIndex].Id;
            Commit();
            ApplyTexts();
        }

        private void Commit()
        {
            _settings.ReleaseAllOnExit = _releaseOnExit.Checked;
            _settings.ShowNotifications = _notifications.Checked;
            _settings.Language = Strings.All[_language.SelectedIndex].Id;
            _settings.CheckUpdatesAtStartup = _checkAtStartup.Checked;
            _settings.Save();
            if (_onChanged != null) _onChanged();
        }

        private void OnCheckClicked(object sender, EventArgs e)
        {
            Commit();

            _check.Enabled = false;
            _updateStatus.Text = Strings.T("update.checking");

            Updater.CheckAsync(delegate(UpdateResult result)
            {
                // La réponse arrive depuis un thread de travail.
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((MethodInvoker)delegate { ShowResult(result); });
            });
        }

        private void ShowResult(UpdateResult result)
        {
            _check.Enabled = true;
            _updateStatus.Text = string.Empty;

            switch (result.Status)
            {
                case UpdateStatus.UpToDate:
                    MessageBox.Show(this,
                        Strings.T("update.upToDate", Updater.CurrentVersionText),
                        Strings.T("settings.updates"), MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;

                case UpdateStatus.Available:
                    if (MessageBox.Show(this,
                            Strings.T("update.available", result.LatestVersion),
                            Strings.T("update.availableTitle"), MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        OpenPage(result.PageUrl);
                    }
                    break;

                case UpdateStatus.NoRelease:
                    MessageBox.Show(this, Strings.T("update.noRelease"),
                        Strings.T("settings.updates"), MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;

                case UpdateStatus.Error:
                    MessageBox.Show(this, Strings.T("update.error", result.Error),
                        Strings.T("settings.updates"), MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    break;
            }
        }

        private void OpenPage(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Strings.T("update.error", ex.Message),
                    Strings.T("settings.updates"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Commit();
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _logo != null && _logo.Image != null) _logo.Image.Dispose();
            base.Dispose(disposing);
        }
    }
}
