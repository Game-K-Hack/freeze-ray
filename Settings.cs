using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FreezeRay
{
    /// <summary>
    /// Réglages persistés dans %APPDATA%\Freeze Ray\settings.ini.
    ///
    /// Un simple fichier clé=valeur : le format se lit et se corrige à la main,
    /// et n'impose aucune dépendance de sérialisation.
    ///
    /// « Démarrer avec Windows » n'y figure pas : cet état appartient au registre,
    /// qui en est la seule source de vérité (voir TrayContext).
    /// </summary>
    internal sealed class Settings
    {
        private const string FILE_NAME = "settings.ini";

        public bool ReleaseAllOnExit { get; set; }
        public bool ShowNotifications { get; set; }
        public Language Language { get; set; }

        /// <summary>Interroger GitHub au lancement de l'application.</summary>
        public bool CheckUpdatesAtStartup { get; set; }

        public Settings()
        {
            ReleaseAllOnExit = true;
            ShowNotifications = true;
            Language = Strings.Detect();
            CheckUpdatesAtStartup = true;
        }

        private static string _folder;

        /// <summary>
        /// Les réglages vivent à côté de l'exécutable : l'installation tient
        /// alors dans un seul dossier, qu'on peut copier sur une clé ou effacer
        /// d'un bloc.
        ///
        /// Repli sur %APPDATA% si cet emplacement n'est pas inscriptible — cas
        /// d'une copie déposée dans Program Files, où un utilisateur standard
        /// n'a pas le droit d'écrire. Sans ce repli, les réglages seraient
        /// silencieusement perdus à chaque fermeture.
        /// </summary>
        public static string Folder
        {
            get
            {
                if (_folder == null)
                {
                    string beside = AppDomain.CurrentDomain.BaseDirectory;
                    _folder = IsWritable(beside) ? beside : RoamingFolder;
                }
                return _folder;
            }
        }

        public static string FilePath
        {
            get { return Path.Combine(Folder, FILE_NAME); }
        }

        /// <summary>Emplacement utilisé avant que les réglages ne suivent l'exécutable.</summary>
        private static string RoamingFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Strings.AppName);
            }
        }

        private static string RoamingFilePath
        {
            get { return Path.Combine(RoamingFolder, FILE_NAME); }
        }

        private static bool IsWritable(string directory)
        {
            try
            {
                string probe = Path.Combine(directory,
                    "." + Guid.NewGuid().ToString("N") + ".tmp");
                using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write,
                           FileShare.None, 1, FileOptions.DeleteOnClose))
                {
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static Settings Load()
        {
            Settings settings = new Settings();
            try
            {
                // Reprise de l'ancien emplacement : sans cela, une installation
                // par-dessus une version précédente repartirait de zéro.
                string source = File.Exists(FilePath) ? FilePath : RoamingFilePath;
                if (!File.Exists(source)) return settings;

                foreach (string line in File.ReadAllLines(source, Encoding.UTF8))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed[0] == '#' || trimmed[0] == ';') continue;

                    int separator = trimmed.IndexOf('=');
                    if (separator <= 0) continue;

                    string key = trimmed.Substring(0, separator).Trim();
                    string value = trimmed.Substring(separator + 1).Trim();

                    switch (key)
                    {
                        case "releaseAllOnExit":
                            settings.ReleaseAllOnExit = ParseBool(value, settings.ReleaseAllOnExit);
                            break;
                        case "showNotifications":
                            settings.ShowNotifications = ParseBool(value, settings.ShowNotifications);
                            break;
                        case "language":
                            settings.Language = Strings.FromCode(value);
                            break;
                        case "checkUpdatesAtStartup":
                            settings.CheckUpdatesAtStartup = ParseBool(value, settings.CheckUpdatesAtStartup);
                            break;
                    }
                }
            }
            catch (IOException)
            {
                // Réglages illisibles : on repart des valeurs par défaut.
            }
            catch (UnauthorizedAccessException)
            {
            }
            return settings;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);

                List<string> lines = new List<string>();
                lines.Add("# " + Strings.AppName + " — réglages");
                lines.Add("releaseAllOnExit=" + (ReleaseAllOnExit ? "true" : "false"));
                lines.Add("showNotifications=" + (ShowNotifications ? "true" : "false"));
                lines.Add("language=" + Strings.CodeOf(Language));
                lines.Add("checkUpdatesAtStartup=" + (CheckUpdatesAtStartup ? "true" : "false"));

                File.WriteAllLines(FilePath, lines.ToArray(), Encoding.UTF8);
            }
            catch (IOException)
            {
                // Sans écriture possible, les réglages restent valables pour la session.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            return fallback;
        }
    }
}
