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

        public static string Folder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Strings.AppName);
            }
        }

        public static string FilePath
        {
            get { return Path.Combine(Folder, FILE_NAME); }
        }

        public static Settings Load()
        {
            Settings settings = new Settings();
            try
            {
                if (!File.Exists(FilePath)) return settings;

                foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
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
