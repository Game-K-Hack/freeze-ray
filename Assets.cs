using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace FreezeRay
{
    /// <summary>
    /// Logo de l'application. Les fichiers sont embarqués dans l'exécutable au
    /// moment de la compilation : Freeze Ray.exe reste utilisable seul, sans le
    /// dossier assets.
    /// </summary>
    internal static class Assets
    {
        private const string ICON_RESOURCE = "FreezeRay.app.ico";
        private const string LOGO_RESOURCE = "FreezeRay.logo.png";

        /// <summary>
        /// Illustration réservée à l'en-tête des paramètres. L'icône du système,
        /// le curseur de désignation et la marque continuent d'utiliser
        /// <see cref="LOGO_RESOURCE"/>, qui reste lisible en très petite taille.
        /// </summary>
        private const string BANNER_RESOURCE = "FreezeRay.banner.png";

        private static readonly Dictionary<int, Icon> _icons = new Dictionary<int, Icon>();
        private static Bitmap _logo;
        private static bool _logoLoaded;
        private static Bitmap _banner;
        private static bool _bannerLoaded;

        /// <summary>Logo en pleine résolution, transparent. Null si absent.</summary>
        public static Bitmap Logo
        {
            get
            {
                if (_logoLoaded) return _logo;
                _logoLoaded = true;
                using (Stream s = Open(LOGO_RESOURCE))
                {
                    if (s != null)
                    {
                        try { _logo = new Bitmap(s); }
                        catch (ArgumentException) { _logo = null; }
                    }
                }
                return _logo;
            }
        }

        /// <summary>
        /// Illustration de l'en-tête des paramètres, à la taille voulue.
        /// Retombe sur le logo ordinaire si la ressource manque.
        /// L'appelant devient propriétaire du bitmap.
        /// </summary>
        public static Bitmap RenderBanner(int size)
        {
            if (_banner == null && !_bannerLoaded)
            {
                _bannerLoaded = true;
                using (Stream s = Open(BANNER_RESOURCE))
                {
                    if (s != null)
                    {
                        try { _banner = new Bitmap(s); }
                        catch (ArgumentException) { _banner = null; }
                    }
                }
            }
            return _banner != null ? Scale(_banner, size) : RenderLogo(size);
        }

        /// <summary>Icône à la taille demandée, en repli sur un dessin minimal.</summary>
        public static Icon GetIcon(int size)
        {
            Icon cached;
            if (_icons.TryGetValue(size, out cached)) return cached;

            Icon icon = null;
            using (Stream s = Open(ICON_RESOURCE))
            {
                if (s != null)
                {
                    try { icon = new Icon(s, size, size); }
                    catch (ArgumentException) { icon = null; }
                }
            }
            if (icon == null) icon = Fallback(size);

            _icons[size] = icon;
            return icon;
        }

        /// <summary>Icône de la zone de notification, à la taille attendue par le système.</summary>
        public static Icon TrayIcon
        {
            get
            {
                Size s = SystemInformation.SmallIconSize;
                return GetIcon(Math.Max(s.Width, 16));
            }
        }

        /// <summary>
        /// Logo réduit à la taille voulue, avec un rééchantillonnage de qualité.
        /// L'appelant devient propriétaire du bitmap.
        /// </summary>
        public static Bitmap RenderLogo(int size)
        {
            Bitmap logo = Logo;
            if (logo != null) return Scale(logo, size);

            Bitmap dst = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(dst))
            using (Icon icon = GetIcon(size))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(icon.ToBitmap(), new Rectangle(0, 0, size, size));
            }
            return dst;
        }

        private static Bitmap Scale(Bitmap source, int size)
        {
            Bitmap dst = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.CompositingMode = CompositingMode.SourceCopy; // conserve l'alpha
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, size, size));
            }
            return dst;
        }

        private static Stream Open(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        }

        /// <summary>Épingle dessinée à la volée si les ressources manquent.</summary>
        private static Icon Fallback(int size)
        {
            using (Bitmap bmp = new Bitmap(size, size))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    float k = size / 32f;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(60, 140, 240)))
                    using (Pen pen = new Pen(Color.FromArgb(60, 140, 240), 3f * k))
                    {
                        g.FillEllipse(brush, 9 * k, 3 * k, 14 * k, 14 * k);
                        g.DrawLine(pen, 16 * k, 16 * k, 16 * k, 29 * k);
                    }
                }
                IntPtr h = bmp.GetHicon();
                using (Icon tmp = Icon.FromHandle(h))
                {
                    return (Icon)tmp.Clone();
                }
            }
        }
    }
}
