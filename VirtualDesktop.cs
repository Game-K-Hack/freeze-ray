using System;
using System.Runtime.InteropServices;

namespace KeepScreen
{
    /// <summary>
    /// Acces aux interfaces COM non documentees du shell Windows 10/11 qui gerent
    /// les bureaux virtuels. C'est la meme mecanique que le menu contextuel
    /// "Afficher cette fenetre sur tous les bureaux" de la vue des taches.
    /// </summary>
    internal static class VirtualDesktop
    {
        private static readonly Guid CLSID_ImmersiveShell =
            new Guid("C2F03A33-21F5-47FA-B4BB-156362A2F239");
        private static readonly Guid CLSID_VirtualDesktopPinnedApps =
            new Guid("B5A399E7-1C87-46B8-88E9-FC5747B171BD");
        private static readonly Guid IID_IApplicationViewCollection =
            new Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5");
        private static readonly Guid IID_IVirtualDesktopPinnedApps =
            new Guid("4CE81583-1E4C-4632-A621-07A53543148F");

        private static IApplicationViewCollection _views;
        private static IVirtualDesktopPinnedApps _pinnedApps;

        /// <summary>Message d'erreur si l'init COM a echoue, sinon null.</summary>
        public static string InitError { get; private set; }

        public static bool Available
        {
            get
            {
                EnsureInit();
                return _views != null && _pinnedApps != null;
            }
        }

        private static void EnsureInit()
        {
            if (_views != null && _pinnedApps != null) return;
            try
            {
                Type shellType = Type.GetTypeFromCLSID(CLSID_ImmersiveShell);
                object shell = Activator.CreateInstance(shellType);
                IServiceProvider10 provider = (IServiceProvider10)shell;

                Guid svc = IID_IApplicationViewCollection;
                Guid iid = IID_IApplicationViewCollection;
                _views = (IApplicationViewCollection)provider.QueryService(ref svc, ref iid);

                svc = CLSID_VirtualDesktopPinnedApps;
                iid = IID_IVirtualDesktopPinnedApps;
                _pinnedApps = (IVirtualDesktopPinnedApps)provider.QueryService(ref svc, ref iid);

                InitError = null;
            }
            catch (Exception ex)
            {
                _views = null;
                _pinnedApps = null;
                InitError = ex.Message;
            }
        }

        private static IApplicationView GetView(IntPtr hwnd)
        {
            EnsureInit();
            if (_views == null) return null;
            IApplicationView view;
            int hr = _views.GetViewForHwnd(hwnd, out view);
            return hr == 0 ? view : null;
        }

        public static bool IsPinned(IntPtr hwnd)
        {
            IApplicationView view = GetView(hwnd);
            if (view == null) return false;
            bool pinned;
            return _pinnedApps.IsViewPinned(view, out pinned) == 0 && pinned;
        }

        public static bool Pin(IntPtr hwnd)
        {
            IApplicationView view = GetView(hwnd);
            return view != null && _pinnedApps.PinView(view) == 0;
        }

        public static bool Unpin(IntPtr hwnd)
        {
            IApplicationView view = GetView(hwnd);
            return view != null && _pinnedApps.UnpinView(view) == 0;
        }

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IServiceProvider10
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object QueryService(ref Guid service, ref Guid riid);
        }

        // Interface opaque : on ne fait que transporter le pointeur d'une
        // methode a l'autre, aucun membre n'a besoin d'etre declare.
        [ComImport, Guid("372E1D3B-38D3-42E4-A15B-8AB2B178F513"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IApplicationView
        {
        }

        // L'ordre des methodes reproduit la vtable : les trois premieres sont
        // declarees uniquement pour que GetViewForHwnd tombe au bon offset.
        [ComImport, Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IApplicationViewCollection
        {
            [PreserveSig]
            int GetViews(out IntPtr array);
            [PreserveSig]
            int GetViewsByZOrder(out IntPtr array);
            [PreserveSig]
            int GetViewsByAppUserModelId([MarshalAs(UnmanagedType.HString)] string id, out IntPtr array);
            [PreserveSig]
            int GetViewForHwnd(IntPtr hwnd, out IApplicationView view);
        }

        [ComImport, Guid("4CE81583-1E4C-4632-A621-07A53543148F"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IVirtualDesktopPinnedApps
        {
            [PreserveSig]
            int IsAppIdPinned([MarshalAs(UnmanagedType.HString)] string appId,
                              [MarshalAs(UnmanagedType.Bool)] out bool pinned);
            [PreserveSig]
            int PinAppID([MarshalAs(UnmanagedType.HString)] string appId);
            [PreserveSig]
            int UnpinAppID([MarshalAs(UnmanagedType.HString)] string appId);
            [PreserveSig]
            int IsViewPinned(IApplicationView view,
                             [MarshalAs(UnmanagedType.Bool)] out bool pinned);
            [PreserveSig]
            int PinView(IApplicationView view);
            [PreserveSig]
            int UnpinView(IApplicationView view);
        }
    }
}
