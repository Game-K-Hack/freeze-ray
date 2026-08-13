using System;
using System.IO;

namespace FreezeRay
{
    /// <summary>
    /// Emplacements partagés par l'installeur et par la désinstallation.
    ///
    /// L'installation se fait par utilisateur, dans %LOCALAPPDATA%\Programs, et
    /// non dans Program Files. Deux raisons : aucune élévation n'est demandée,
    /// et surtout le dossier reste inscriptible — c'est la condition pour que le
    /// fichier de réglages puisse vivre à côté de l'application, comme voulu.
    /// </summary>
    internal static class InstallPaths
    {
        public const string ExeName = "Freeze Ray.exe";
        public const string LicenseName = "LICENSE.md";
        public const string ShortcutName = "Freeze Ray.lnk";

        /// <summary>Clé d'inscription dans « Applications et fonctionnalités ».</summary>
        public const string UninstallKey =
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FreezeRay";

        public const string RunKey =
            @"Software\Microsoft\Windows\CurrentVersion\Run";

        public const string RunValue = "Freeze Ray";

        public static string DefaultFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", Strings.AppName);
            }
        }

        public static string DesktopShortcut
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    ShortcutName);
            }
        }

        public static string StartMenuShortcut
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    ShortcutName);
            }
        }
    }
}
