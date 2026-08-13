using System;
using System.Drawing;
using System.Windows.Forms;

namespace FreezeRay
{
    /// <summary>
    /// Petit logo posé sur la barre de titre d'une fenêtre verrouillée, à la
    /// manière de l'épingle de DeskPin : il signale l'état et un clic dessus
    /// libère la fenêtre.
    ///
    /// C'est une fenêtre à transparence par pixel (WS_EX_LAYERED +
    /// UpdateLayeredWindow) : le logo garde son anticrénelage sur n'importe quel
    /// fond, ce qu'une couleur de transparence ne permettrait pas. Elle
    /// n'accepte jamais le focus, cliquer dessus ne désactive donc pas la
    /// fenêtre visée.
    /// </summary>
    internal sealed class WindowMarker : NativeWindow, IDisposable
    {
        private const int MIN_SIZE = 16;
        private const int MAX_SIZE = 22;

        /// <summary>
        /// Écart entre la marque et le premier bouton système. C'est le réglage
        /// à modifier pour déplacer la marque : plus il est petit, plus elle est
        /// à droite ; en dessous de zéro elle empiéterait sur le bouton Réduire.
        /// </summary>
        private const int BUTTON_GAP = 4;

        /// <summary>
        /// Largeur réelle d'un bouton système rapportée à SM_CXSIZE. Cette
        /// métrique héritée vaut 36 px là où Windows 10 dessine des boutons de
        /// 46 px (mesuré : glyphes centrés tous les 46 px) ; le rapport suit en
        /// revanche correctement la mise à l'échelle de l'affichage.
        /// </summary>
        private const int BUTTON_WIDTH_NUM = 46;
        private const int BUTTON_WIDTH_DEN = 36;

        private readonly IntPtr _target;
        private Rectangle _bounds;
        private bool _shown;
        private bool _disposed;

        /// <summary>Clic sur la marque : l'utilisateur demande la libération.</summary>
        public event Action<IntPtr> Clicked;

        public IntPtr Target { get { return _target; } }

        public WindowMarker(IntPtr target)
        {
            _target = target;
            _bounds = ComputePlacement();

            CreateParams cp = new CreateParams();
            cp.Style = Native.WS_POPUP;
            cp.ExStyle = Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
            cp.X = _bounds.X;
            cp.Y = _bounds.Y;
            cp.Width = _bounds.Width;
            cp.Height = _bounds.Height;
            CreateHandle(cp);

            Render();
            Update();
        }

        /// <summary>
        /// Suit la fenêtre : position, visibilité et plan. Appelé périodiquement
        /// par le programme principal.
        /// </summary>
        public void Update()
        {
            if (_disposed || Handle == IntPtr.Zero) return;

            if (!Native.IsWindow(_target) || !Native.IsWindowVisible(_target)
                || Native.IsIconic(_target))
            {
                Hide();
                return;
            }

            Rectangle placement = ComputePlacement();
            if (placement.Width <= 0)
            {
                Hide();
                return;
            }

            bool resized = placement.Size != _bounds.Size;
            bool moved = placement.Location != _bounds.Location;
            _bounds = placement;

            if (resized) Render(); // UpdateLayeredWindow porte aussi la taille

            ApplyZOrder(moved || resized);
            Show();
        }

        /// <summary>
        /// Maintient la marque immédiatement devant sa cible : elle est ainsi
        /// masquée en même temps qu'elle si une autre fenêtre la recouvre.
        ///
        /// Attention au sens de SetWindowPos : hWndInsertAfter désigne la fenêtre
        /// qui PRÉCÈDE, passer la cible placerait donc la marque derrière elle.
        /// On s'insère à la place derrière le voisin situé juste devant la cible.
        /// Une cible au premier plan impose en outre le rang TOPMOST, sinon le
        /// système refuse de laisser la marque au-dessus.
        /// </summary>
        private void ApplyZOrder(bool geometryChanged)
        {
            bool targetTop = Native.IsTopMost(_target);
            bool selfTop = Native.IsTopMost(Handle);
            IntPtr previous = Native.GetWindow(_target, Native.GW_HWNDPREV);

            bool placementOk = targetTop ? selfTop : (!selfTop && previous == Handle);

            if (placementOk)
            {
                // Rien à refaire côté profondeur : on évite un appel par tick.
                if (geometryChanged)
                {
                    Native.SetWindowPos(Handle, IntPtr.Zero, _bounds.X, _bounds.Y,
                        _bounds.Width, _bounds.Height,
                        Native.SWP_NOACTIVATE | Native.SWP_NOZORDER);
                }
                return;
            }

            if (targetTop != selfTop)
            {
                // Changer de bande (normale / premier plan) est une opération à part.
                Native.SetWindowPos(Handle,
                    targetTop ? Native.HWND_TOPMOST : Native.HWND_NOTOPMOST,
                    0, 0, 0, 0,
                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            }

            IntPtr after;
            if (targetTop) after = Native.HWND_TOPMOST;
            else if (previous != IntPtr.Zero && previous != Handle) after = previous;
            else after = Native.HWND_TOP;

            Native.SetWindowPos(Handle, after, _bounds.X, _bounds.Y,
                _bounds.Width, _bounds.Height, Native.SWP_NOACTIVATE);
        }

        private void Show()
        {
            if (_shown) return;
            Native.ShowWindow(Handle, Native.SW_SHOWNOACTIVATE);
            _shown = true;
        }

        private void Hide()
        {
            if (!_shown) return;
            Native.ShowWindow(Handle, Native.SW_HIDE);
            _shown = false;
        }

        /// <summary>
        /// Cherche la barre de titre pour y loger la marque, à gauche des boutons
        /// système. Les fenêtres à cadre personnalisé (navigateurs, applications
        /// UWP…) ne renseignent pas toujours ces informations : on retombe alors
        /// sur le coin supérieur droit du cadre visible.
        /// </summary>
        private Rectangle ComputePlacement()
        {
            Native.RECT frame;
            if (Native.DwmGetWindowAttribute(_target, Native.DWMWA_EXTENDED_FRAME_BOUNDS,
                    out frame, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.RECT))) != 0)
            {
                if (!Native.GetWindowRect(_target, out frame)) return Rectangle.Empty;
            }
            if (frame.Width <= 0 || frame.Height <= 0) return Rectangle.Empty;

            Native.TITLEBARINFO info = new Native.TITLEBARINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.TITLEBARINFO));
            info.rgstate = new int[6];

            int captionTop = frame.Top;
            int captionHeight = Native.GetSystemMetrics(Native.SM_CYCAPTION);
            int right = frame.Right;
            int buttons = 3;

            if (Native.GetTitleBarInfo(_target, ref info) && IsSaneCaption(info.rcTitleBar, frame))
            {
                captionTop = info.rcTitleBar.Top;
                captionHeight = info.rcTitleBar.Height;
                right = Math.Min(info.rcTitleBar.Right, frame.Right);

                buttons = 0;
                for (int i = 2; i <= 5; i++) // 2 réduire, 3 agrandir, 4 aide, 5 fermer
                {
                    if ((info.rgstate[i] & Native.STATE_SYSTEM_INVISIBLE) == 0) buttons++;
                }
                if (buttons == 0) buttons = 1;
            }

            int size = Math.Max(MIN_SIZE, Math.Min(MAX_SIZE, captionHeight - 6));
            int buttonWidth = Native.GetSystemMetrics(Native.SM_CXSIZE)
                              * BUTTON_WIDTH_NUM / BUTTON_WIDTH_DEN;
            int x = right - buttons * buttonWidth - BUTTON_GAP - size;
            int y = captionTop + (captionHeight - size) / 2;

            // Ne jamais sortir du cadre de la fenêtre visée.
            if (x < frame.Left) x = frame.Left;
            if (y < frame.Top) y = frame.Top;

            return new Rectangle(x, y, size, size);
        }

        private static bool IsSaneCaption(Native.RECT caption, Native.RECT frame)
        {
            return caption.Height >= 16 && caption.Height <= 64
                   && caption.Width > 32
                   && caption.Top >= frame.Top - 8
                   && caption.Top <= frame.Top + frame.Height / 2;
        }

        /// <summary>Pousse le logo dans la fenêtre superposée, alpha compris.</summary>
        private void Render()
        {
            int size = _bounds.Width;
            if (size <= 0) return;

            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            IntPtr memDc = Native.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                using (Bitmap bmp = Assets.RenderLogo(size))
                {
                    // Un fond entièrement transparent produit le bitmap à alpha
                    // prémultiplié qu'attend UpdateLayeredWindow.
                    hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                }
                oldBitmap = Native.SelectObject(memDc, hBitmap);

                Native.SIZE dim = new Native.SIZE();
                dim.Cx = size;
                dim.Cy = size;
                Native.POINT src = new Native.POINT();
                Native.POINT dst = new Native.POINT();
                dst.X = _bounds.X;
                dst.Y = _bounds.Y;

                Native.BLENDFUNCTION blend = new Native.BLENDFUNCTION();
                blend.BlendOp = Native.AC_SRC_OVER;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = Native.AC_SRC_ALPHA;

                Native.UpdateLayeredWindow(Handle, screenDc, ref dst, ref dim,
                    memDc, ref src, 0, ref blend, Native.ULW_ALPHA);
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero) Native.SelectObject(memDc, oldBitmap);
                if (hBitmap != IntPtr.Zero) Native.DeleteObject(hBitmap);
                Native.DeleteDC(memDc);
                Native.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Native.WM_MOUSEACTIVATE:
                    // Ne jamais voler le focus a la fenetre visee.
                    m.Result = new IntPtr(Native.MA_NOACTIVATE);
                    return;

                case Native.WM_SETCURSOR:
                    Native.SetCursor(Cursors.Hand.Handle);
                    m.Result = new IntPtr(1);
                    return;

                case Native.WM_LBUTTONUP:
                    if (Clicked != null) Clicked(_target);
                    return;
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clicked = null;
            if (Handle != IntPtr.Zero) DestroyHandle();
        }
    }
}
