using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FreezeRay
{
    /// <summary>
    /// Création de raccourcis .lnk via IShellLink, l'interface COM du shell.
    ///
    /// L'alternative courante — piloter WScript.Shell en liaison tardive — exige
    /// une référence supplémentaire au compilateur ; ces quelques déclarations
    /// gardent le projet compilable avec le seul csc fourni par Windows.
    /// </summary>
    internal static class Shortcuts
    {
        public static void Create(string linkPath, string target, string description)
        {
            IShellLinkW link = (IShellLinkW)new ShellLink();
            link.SetPath(target);
            link.SetWorkingDirectory(System.IO.Path.GetDirectoryName(target));
            link.SetIconLocation(target, 0);
            link.SetDescription(description);
            ((IPersistFile)link).Save(linkPath, true);
        }

        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        // L'ordre des membres reproduit la vtable : aucune méthode ne peut être
        // omise, même celles dont on ne se sert pas.
        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder file,
                         int maxPath, IntPtr findData, int flags);
            void GetIDList(out IntPtr idList);
            void SetIDList(IntPtr idList);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name,
                                int maxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder dir,
                                     int maxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string dir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder args,
                              int maxArgs);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);
            void GetHotkey(out short hotkey);
            void SetHotkey(short hotkey);
            void GetShowCmd(out int showCmd);
            void SetShowCmd(int showCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder icon,
                                 int maxPath, out int index);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string icon, int index);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, int reserved);
            void Resolve(IntPtr hwnd, int flags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
        }

        [ComImport, Guid("0000010B-0000-0000-C000-000000000046"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersistFile
        {
            void GetClassID(out Guid classId); // hérité de IPersist
            [PreserveSig]
            int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, int mode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string fileName,
                      [MarshalAs(UnmanagedType.Bool)] bool remember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);
            void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder fileName);
        }
    }
}
