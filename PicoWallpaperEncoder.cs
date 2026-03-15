using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PicoStationCovMaker
{
    internal static class PicoWallpaperEncoder
    {
        public static Bitmap LoadAndResizePng(string pngPath)
        {
            if (pngPath == null) throw new ArgumentNullException(nameof(pngPath));

            using var src = (Bitmap)Image.FromFile(pngPath);
            var dest = new Bitmap(320, 240, PixelFormat.Format24bppRgb);

            using (var g = Graphics.FromImage(dest))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(src, new Rectangle(0, 0, 320, 240));
            }

            return dest;
        }

        public static (Color[] Palette, byte[] Indices) QuantizeTo8Bpp(Bitmap bmp320x240)
        {
            if (bmp320x240 == null) throw new ArgumentNullException(nameof(bmp320x240));
            if (bmp320x240.Width != 320 || bmp320x240.Height != 240)
                throw new ArgumentException("Bitmap must be 320x240.", nameof(bmp320x240));

            // Build a fixed 256-color cube palette: 8x8x4 (R,G,B)
            var palette = new Color[256];
            for (int rIndex = 0; rIndex < 8; rIndex++)
            {
                for (int gIndex = 0; gIndex < 8; gIndex++)
                {
                    for (int bIndex = 0; bIndex < 4; bIndex++)
                    {
                        int idx = (rIndex << 5) | (gIndex << 2) | bIndex;
                        int r = (int)(rIndex * 255.0 / 7.0);
                        int g = (int)(gIndex * 255.0 / 7.0);
                        int b = (int)(bIndex * 255.0 / 3.0);
                        palette[idx] = Color.FromArgb(r, g, b);
                    }
                }
            }

            int width = bmp320x240.Width;
            int height = bmp320x240.Height;
            var indices = new byte[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = bmp320x240.GetPixel(x, y);
                    int rIndex = (c.R * 7 + 127) / 255;
                    int gIndex = (c.G * 7 + 127) / 255;
                    int bIndex = (c.B * 3 + 127) / 255;

                    if (rIndex < 0) rIndex = 0; else if (rIndex > 7) rIndex = 7;
                    if (gIndex < 0) gIndex = 0; else if (gIndex > 7) gIndex = 7;
                    if (bIndex < 0) bIndex = 0; else if (bIndex > 3) bIndex = 3;

                    int idx = (rIndex << 5) | (gIndex << 2) | bIndex;
                    indices[y * width + x] = (byte)idx;
                }
            }

            // Avoid using palette index 0 for actual pixels because the hardware treats it as transparent.
            // Copy color 0 into slot 1, and remap any index-0 pixels to 1 so black stays visible.
            if (palette.Length > 1)
            {
                palette[1] = palette[0];
            }

            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] == 0)
                    indices[i] = 1;
            }

            return (palette, indices);
        }

        public static byte[] BuildRawFile(Color[] palette, byte[] indices)
        {
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (indices == null) throw new ArgumentNullException(nameof(indices));
            if (indices.Length != 320 * 240)
                throw new ArgumentException("Indices array must be 320x240.", nameof(indices));

            const int width = 320;
            const int height = 240;
            const int leftWidth = 256;
            const int rightWidth = 64;

            // 1) Palette to 512 bytes (256 * 2 bytes, PSX-style RGB555)
            var palBytes = new byte[512];
            for (int i = 0; i < 256; i++)
            {
                Color c = i < palette.Length ? palette[i] : Color.Black;

                int r5 = (c.R * 31 + 127) / 255;
                int g5 = (c.G * 31 + 127) / 255;
                int b5 = (c.B * 31 + 127) / 255;

                if (r5 < 0) r5 = 0; else if (r5 > 31) r5 = 31;
                if (g5 < 0) g5 = 0; else if (g5 > 31) g5 = 31;
                if (b5 < 0) b5 = 0; else if (b5 > 31) b5 = 31;

                int val = (b5 << 10) | (g5 << 5) | r5;

                // Avoid fully black (0) palette entries because hardware treats that as transparent.
                if (val == 0)
                {
                    // Use a very dark non-zero color instead (matches what reference tool does roughly).
                    val = (4 << 10); // B5 = 4, R5 = G5 = 0 -> nearly black but not transparent
                }

                palBytes[i * 2] = (byte)(val & 0xFF);
                palBytes[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
            }

            // 2) Split indices into left (256x240) and right (64x240) blocks
            var left = new byte[leftWidth * height];
            var right = new byte[rightWidth * height];

            for (int y = 0; y < height; y++)
            {
                int srcRowStart = y * width;
                int leftRowStart = y * leftWidth;
                int rightRowStart = y * rightWidth;

                Buffer.BlockCopy(indices, srcRowStart, left, leftRowStart, leftWidth);
                Buffer.BlockCopy(indices, srcRowStart + leftWidth, right, rightRowStart, rightWidth);
            }

            // 3) Concatenate palette + left + right
            var output = new byte[512 + left.Length + right.Length];
            Buffer.BlockCopy(palBytes, 0, output, 0, 512);
            Buffer.BlockCopy(left, 0, output, 512, left.Length);
            Buffer.BlockCopy(right, 0, output, 512 + left.Length, right.Length);

            return output;
        }


        public static Bitmap LoadRawWallpaper(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var all = System.IO.File.ReadAllBytes(path);
            const int width = 320;
            const int height = 240;
            const int leftWidth = 256;
            const int rightWidth = 64;
            int expectedPixels = width * height;
            int expectedSize = 512 + expectedPixels;
            if (all.Length < expectedSize)
                throw new InvalidOperationException($"Wallpaper RAW too small: {all.Length} bytes (expected at least {expectedSize}).");

            // 1) Palette: first 512 bytes, 256 entries * 2 bytes, PSX-style RGB555
            var palette = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                int lo = all[i * 2];
                int hi = all[i * 2 + 1];
                int val = lo | (hi << 8);

                int r5 = val & 0x1F;
                int g5 = (val >> 5) & 0x1F;
                int b5 = (val >> 10) & 0x1F;

                int r = (int)(r5 * 255.0 / 31.0);
                int g = (int)(g5 * 255.0 / 31.0);
                int b = (int)(b5 * 255.0 / 31.0);

                palette[i] = Color.FromArgb(r, g, b);
            }

            // 2) Pixel indices: left block (256x240) then right block (64x240)
            int pixelOffset = 512;
            int leftSize = leftWidth * height;
            int rightSize = rightWidth * height;
            if (all.Length < pixelOffset + leftSize + rightSize)
                throw new InvalidOperationException("Wallpaper RAW does not contain full left+right blocks.");

            var left = new byte[leftSize];
            var right = new byte[rightSize];
            Buffer.BlockCopy(all, pixelOffset, left, 0, leftSize);
            Buffer.BlockCopy(all, pixelOffset + leftSize, right, 0, rightSize);

            var indices = new byte[expectedPixels];
            for (int y = 0; y < height; y++)
            {
                int destRowStart = y * width;
                int leftRowStart = y * leftWidth;
                int rightRowStart = y * rightWidth;

                Buffer.BlockCopy(left, leftRowStart, indices, destRowStart, leftWidth);
                Buffer.BlockCopy(right, rightRowStart, indices, destRowStart + leftWidth, rightWidth);
            }

            // 3) Build 24bpp bitmap
            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte idx = indices[y * width + x];
                    Color c = palette[idx];
                    bmp.SetPixel(x, y, c);
                }
            }

            return bmp;
        }
    }
}
