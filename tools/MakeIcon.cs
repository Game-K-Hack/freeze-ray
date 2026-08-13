// Génère assets/app.ico (multi-résolutions) à partir de assets/icon.png.
// À relancer uniquement si le logo change :
//   %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:MakeIcon.exe ^
//       /r:System.Drawing.dll tools\MakeIcon.cs
//   MakeIcon.exe
//
// Un .ico ne contenant qu'une image 256x256 oblige Windows à la réduire lui-même
// pour la zone de notification ou la barre de titre, avec un rendu flou. On
// pré-calcule donc chaque taille utile avec un rééchantillonnage de qualité.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class MakeIcon
{
    private static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

    private static int Main(string[] args)
    {
        string root = args.Length > 0
            ? args[0]
            : Path.GetDirectoryName(Path.GetDirectoryName(
                  System.Reflection.Assembly.GetExecutingAssembly().Location));
        string source = Path.Combine(root, @"assets\icon.png");
        string output = Path.Combine(root, @"assets\app.ico");

        if (!File.Exists(source))
        {
            Console.Error.WriteLine("Introuvable : " + source);
            return 1;
        }

        using (Bitmap src = new Bitmap(source))
        using (FileStream fs = File.Create(output))
        using (BinaryWriter w = new BinaryWriter(fs))
        {
            byte[][] payloads = new byte[Sizes.Length][];
            for (int i = 0; i < Sizes.Length; i++)
            {
                using (Bitmap scaled = Resize(src, Sizes[i]))
                {
                    // Le format PNG n'est reconnu dans un .ico qu'à partir de
                    // Vista et n'a d'intérêt qu'en 256 : ailleurs, DIB classique.
                    payloads[i] = Sizes[i] >= 256 ? EncodePng(scaled) : EncodeDib(scaled);
                }
            }

            w.Write((ushort)0);              // réservé
            w.Write((ushort)1);              // type : icône
            w.Write((ushort)Sizes.Length);

            int offset = 6 + 16 * Sizes.Length;
            for (int i = 0; i < Sizes.Length; i++)
            {
                w.Write((byte)(Sizes[i] >= 256 ? 0 : Sizes[i])); // 0 signifie 256
                w.Write((byte)(Sizes[i] >= 256 ? 0 : Sizes[i]));
                w.Write((byte)0);            // palette
                w.Write((byte)0);            // réservé
                w.Write((ushort)1);          // plans
                w.Write((ushort)32);         // bits par pixel
                w.Write(payloads[i].Length);
                w.Write(offset);
                offset += payloads[i].Length;
            }

            foreach (byte[] payload in payloads) w.Write(payload);
        }

        Console.WriteLine("Écrit : " + output);
        return 0;
    }

    private static Bitmap Resize(Bitmap src, int size)
    {
        Bitmap dst = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(dst))
        {
            g.CompositingMode = CompositingMode.SourceCopy; // preserve la transparence
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(src, new Rectangle(0, 0, size, size));
        }
        return dst;
    }

    private static byte[] EncodePng(Bitmap bmp)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    /// <summary>
    /// BITMAPINFOHEADER + pixels BGRA de bas en haut + masque AND vide (la
    /// transparence vient du canal alpha).
    /// </summary>
    private static byte[] EncodeDib(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        int maskStride = ((w + 31) / 32) * 4;
        byte[] data = new byte[40 + w * h * 4 + maskStride * h];

        using (BinaryWriter bw = new BinaryWriter(new MemoryStream(data)))
        {
            bw.Write(40);            // biSize
            bw.Write(w);             // biWidth
            bw.Write(h * 2);         // biHeight : XOR + AND
            bw.Write((ushort)1);     // biPlanes
            bw.Write((ushort)32);    // biBitCount
            bw.Write(0);             // biCompression = BI_RGB
            bw.Write(w * h * 4 + maskStride * h);
            bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);

            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                byte[] row = new byte[w * 4];
                for (int y = h - 1; y >= 0; y--) // de bas en haut
                {
                    System.Runtime.InteropServices.Marshal.Copy(
                        new IntPtr(bd.Scan0.ToInt64() + (long)y * bd.Stride), row, 0, row.Length);
                    bw.Write(row);
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
        }
        return data;
    }
}
