using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FreezeRay
{
    /// <summary>
    /// Désinstallation, déclenchée par « Freeze Ray.exe --uninstall ».
    ///
    /// Elle vit dans l'application plutôt que dans un exécutable séparé : le
    /// dossier installé ne porte ainsi qu'un seul binaire, et la désinstallation
    /// ne peut pas se désynchroniser de la version installée.
    /// </summary>
    internal static class Uninstaller
    {
        public static void Run()
        {
            if (MessageBox.Show(Strings.T("uninstall.confirm"), Strings.AppName,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            RemoveShortcut(InstallPaths.DesktopShortcut);
            RemoveShortcut(InstallPaths.StartMenuShortcut);
            RemoveRegistry();

            MessageBox.Show(Strings.T("uninstall.done"), Strings.AppName,
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Un exécutable ne peut pas s'effacer lui-même pendant qu'il tourne :
            // on confie la suppression du dossier à un processus qui attend notre
            // sortie.
            ScheduleFolderRemoval(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static void RemoveShortcut(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
                // Un raccourci verrouillé ne doit pas interrompre le reste.
            }
        }

        private static void RemoveRegistry()
        {
            try
            {
                using (RegistryKey run = Registry.CurrentUser.OpenSubKey(InstallPaths.RunKey, true))
                {
                    if (run != null) run.DeleteValue(InstallPaths.RunValue, false);
                }
                Registry.CurrentUser.DeleteSubKeyTree(InstallPaths.UninstallKey, false);
            }
            catch (Exception)
            {
            }
        }

        private static void ScheduleFolderRemoval(string folder)
        {
            try
            {
                folder = folder.TrimEnd(Path.DirectorySeparatorChar);

                // ping tient lieu de temporisation : timeout exige une console.
                ProcessStartInfo start = new ProcessStartInfo("cmd.exe",
                    "/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"" + folder + "\"");
                start.CreateNoWindow = true;
                start.UseShellExecute = false;
                Process.Start(start);
            }
            catch (Exception)
            {
                // Les raccourcis et l'inscription sont déjà retirés : l'essentiel est fait.
            }
        }
    }
}
