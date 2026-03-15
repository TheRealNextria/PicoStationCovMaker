using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;

namespace PicoStationCovMaker
{
    internal static class PicoWallpaperService
    {
        public static void CreateWallpaperFromPng(string pngPath, string sdRoot)
        {
            if (pngPath == null) throw new ArgumentNullException(nameof(pngPath));
            if (string.IsNullOrWhiteSpace(sdRoot)) throw new ArgumentNullException(nameof(sdRoot));
            if (!Directory.Exists(sdRoot)) throw new DirectoryNotFoundException($"SD root not found: {sdRoot}");

            using var bmp = PicoWallpaperEncoder.LoadAndResizePng(pngPath);
            var (palette, indices) = PicoWallpaperEncoder.QuantizeTo8Bpp(bmp);
            byte[] rawBytes = PicoWallpaperEncoder.BuildRawFile(palette, indices);

            string bgPath = Path.Combine(sdRoot, "background.raw");
            File.WriteAllBytes(bgPath, rawBytes);

            // Update or create config.ini with wallpaper=1
            string iniPath = Path.Combine(sdRoot, "config.ini");
            List<string> lines = File.Exists(iniPath)
                ? new List<string>(File.ReadAllLines(iniPath))
                : new List<string>();

            int idx = lines.FindIndex(l => l.TrimStart().StartsWith("wallpaper=", StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
                lines[idx] = "wallpaper=1";
            else
                lines.Add("wallpaper=1");

            File.WriteAllLines(iniPath, lines);
        }
    }
}
