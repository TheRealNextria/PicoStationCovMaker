using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PicoStationCovMaker
{
    public partial class MainForm : Form
    {
        private readonly HashSet<string> _inputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string? _outputFolder;


        private const string DefaultCoverBaseUrl = "https://raw.githubusercontent.com/xlenore/psx-covers/main";

        private string CoverSourceConfigPath => Path.Combine(AppContext.BaseDirectory, "PicoStationCovMaker.config.json");

        private sealed class CoverSourceCfg
        {
            public string? CoverBaseUrl { get; set; }
        }

        private void LoadCoverSourceSettings()
        {
            try
            {
                if (File.Exists(CoverSourceConfigPath))
                {
                    var json = File.ReadAllText(CoverSourceConfigPath);
                    var obj = JsonSerializer.Deserialize<CoverSourceCfg>(json);
                    var url = obj?.CoverBaseUrl;
                    txtCoverBaseUrl.Text = string.IsNullOrWhiteSpace(url) ? DefaultCoverBaseUrl : url.Trim();
                }
                else
                {
                    txtCoverBaseUrl.Text = DefaultCoverBaseUrl;
                }
            }
            catch
            {
                txtCoverBaseUrl.Text = DefaultCoverBaseUrl;
            }
        }

        private void SaveCoverSourceSettings()
        {
            try
            {
                var cfg = new CoverSourceCfg { CoverBaseUrl = (txtCoverBaseUrl.Text ?? "").Trim() };
                var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CoverSourceConfigPath, json);
            }
            catch
            {
                // ignore
            }
        }

        private string GetCoverBaseUrl()
        {
            var s = (txtCoverBaseUrl.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s)) return DefaultCoverBaseUrl;
            return s.TrimEnd('/');
        }

        private const string ConfigFileName = "PicoStationCovMaker.config.json";

        private sealed class AppConfig
        {
            public string? CoverBaseUrl { get; set; }
        }

        private AppConfig _config = new AppConfig();

        private string ConfigPath => Path.Combine(AppContext.BaseDirectory, ConfigFileName);

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _config = new AppConfig { CoverBaseUrl = DefaultCoverBaseUrl };
                    return;
                }

                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                _config = cfg ?? new AppConfig { CoverBaseUrl = DefaultCoverBaseUrl };
                if (string.IsNullOrWhiteSpace(_config.CoverBaseUrl))
                    _config.CoverBaseUrl = DefaultCoverBaseUrl;
            }
            catch
            {
                _config = new AppConfig { CoverBaseUrl = DefaultCoverBaseUrl };
            }
        }

        private void SaveConfig()
        {
            try
            {
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // ignore
            }
        }



        private enum InputKind
        {
            Image,
            Cov,
            Game
        }

        private sealed class FileItemMeta
        {
            public string InputPath { get; }
            public string OutputPath { get; set; }
            public InputKind Kind { get; }
            public string? Serial { get; set; }

            public FileItemMeta(string input, string output, InputKind kind)
            {
                InputPath = input;
                OutputPath = output;
                Kind = kind;
            }
        }

        public MainForm()
        {
			this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
			this.ShowInTaskbar = true;
            InitializeComponent();
            LoadCoverSourceSettings();
            LoadConfig();
            txtCoverBaseUrl.Text = _config.CoverBaseUrl ?? DefaultCoverBaseUrl;
            txtCoverBaseUrl.TextChanged += txtCoverBaseUrl_TextChanged;


            // Hook selection preview (in case Designer didn't)
            lvFiles.SelectedIndexChanged += lvFiles_SelectedIndexChanged;

            // Drag & drop on form + drop area controls if present
            AllowDrop = true;
            DragEnter += Main_DragEnter;
            DragDrop += Main_DragDrop;

            if (dropPanel != null)
            {
                dropPanel.AllowDrop = true;
                dropPanel.DragEnter += Main_DragEnter;
                dropPanel.DragDrop += Main_DragDrop;
            }

            if (dropLabel != null)
            {
                dropLabel.AllowDrop = true;
                dropLabel.DragEnter += Main_DragEnter;
                dropLabel.DragDrop += Main_DragDrop;
            }

            UpdateOutputFolderLabel();
            ClearPreview();
        }

        private static InputKind GetKind(string path)
        {
            var ext = Path.GetExtension(path);
            if (ext.Equals(".cov", StringComparison.OrdinalIgnoreCase)) return InputKind.Cov;
            if (ext.Equals(".cue", StringComparison.OrdinalIgnoreCase) || ext.Equals(".bin", StringComparison.OrdinalIgnoreCase)) return InputKind.Game;
            return InputKind.Image;
        }

        private static bool IsSupportedImage(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tif" or ".tiff" or ".webp";
        }

        private static bool IsSupportedDropInput(string path)
        {
            // Drag&drop: images + .cov only (games should be added via Scan SD)
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return IsSupportedImage(path) || ext == ".cov";
        }

        private static IEnumerable<string> EnumerateDropInputs(string folder)
        {
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories); }
            catch { files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly); }

            foreach (var f in files)
                if (IsSupportedDropInput(f))
                    yield return f;
        }

        private void Main_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Main_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null) return;
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0) return;

            var files = new List<string>();
            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    if (IsSupportedDropInput(p)) files.Add(p);
                }
                else if (Directory.Exists(p))
                {
                    files.AddRange(EnumerateDropInputs(p));
                }
            }

            AddToList(files);
        }

        private void AddToList(IEnumerable<string> files)
        {
            int added = 0;
            foreach (var f in files.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!_inputs.Add(f)) continue;

                var kind = GetKind(f);
                string outPath = kind == InputKind.Image ? ResolveOutputPath(f) : f;
                string status = kind == InputKind.Cov ? "COV (preview)" : "Ready";

                var item = new ListViewItem(new[] { f, outPath, status })
                {
                    Tag = new FileItemMeta(f, outPath, kind)
                };
                lvFiles.Items.Add(item);
                added++;
            }

            Log(added > 0 ? $"Added {added} item(s)." : "No supported files found.");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            lvFiles.Items.Clear();
            _inputs.Clear();
            progressBar.Value = 0;
            ClearPreview();
            Log("Cleared list.");
        }

        private void btnOutputFolder_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select output folder (leave empty to output next to the input files).",
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrWhiteSpace(_outputFolder) && Directory.Exists(_outputFolder))
                dlg.SelectedPath = _outputFolder;

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            _outputFolder = dlg.SelectedPath;
            UpdateOutputFolderLabel();
            RefreshOutputPaths();
            Log($"Output folder set to: {_outputFolder}");
        }

        private void UpdateOutputFolderLabel()
        {
            lblOutputFolder.Text = string.IsNullOrWhiteSpace(_outputFolder)
                ? "Output: (same folder as input)"
                : $"Output: {_outputFolder}";
        }

        private void RefreshOutputPaths()
        {
            foreach (ListViewItem item in lvFiles.Items)
            {
                var meta = (FileItemMeta)item.Tag!;
                if (meta.Kind == InputKind.Image)
                {
                    meta.OutputPath = ResolveOutputPath(meta.InputPath);
                    item.SubItems[1].Text = meta.OutputPath;
                }
            }
        }

        private string ResolveOutputPath(string inputPath)
        {
            string name = Path.GetFileNameWithoutExtension(inputPath) + ".cov";
            if (!string.IsNullOrWhiteSpace(_outputFolder))
                return Path.Combine(_outputFolder!, name);

            return Path.Combine(Path.GetDirectoryName(inputPath)!, name);
        }

        private static string ResolveGameCovPath(string gamePath)
        {
            string name = Path.GetFileNameWithoutExtension(gamePath) + ".cov";
            return Path.Combine(Path.GetDirectoryName(gamePath)!, name);
        }

        private async void btnConvert_Click(object sender, EventArgs e)
        {
            if (lvFiles.Items.Count == 0)
            {
                MessageBox.Show(this, "Drop one or more images first.", "Nothing to convert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnConvert.Enabled = false;
            btnClear.Enabled = false;
            btnOutputFolder.Enabled = false;
            if (btnScanSd != null) btnScanSd.Enabled = false;

            try
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = lvFiles.Items.Count;
                progressBar.Value = 0;

                int ok = 0, fail = 0, skipped = 0;

                for (int i = 0; i < lvFiles.Items.Count; i++)
                {
                    var item = lvFiles.Items[i];
                    var meta = (FileItemMeta)item.Tag!;

                    if (meta.Kind == InputKind.Cov || meta.Kind == InputKind.Game)
                    {
                        item.SubItems[2].Text = meta.Kind == InputKind.Game ? "Skipped (game entry)" : "Skipped (.cov preview only)";
                        skipped++;
                        progressBar.Value = i + 1;
                        continue;
                    }

                    item.SubItems[2].Text = "Converting...";
                    await Task.Yield();

                    try
                    {
                        string outPath = ResolveOutputPath(meta.InputPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

                        ConvertImageToCov(meta.InputPath, outPath);

                        long sz = new FileInfo(outPath).Length;
                        if (sz != 32768)
                            throw new IOException($"Output size was {sz} bytes, expected 32768.");

                        meta.OutputPath = outPath;
                        item.SubItems[1].Text = outPath;
                        item.SubItems[2].Text = "OK (32768 bytes)";
                        ok++;

                        if (item.Selected)
                            PreviewGeneratedCov(meta);
                    }
                    catch (Exception ex)
                    {
                        item.SubItems[2].Text = "ERROR";
                        Log($"ERROR: {meta.InputPath}\r\n  {ex.Message}");
                        fail++;
                    }

                    progressBar.Value = i + 1;
                }

                Log($"Done. OK: {ok}, Failed: {fail}, Skipped: {skipped}");
            }
            finally
            {
                btnConvert.Enabled = true;
                btnClear.Enabled = true;
                btnOutputFolder.Enabled = true;
                if (btnScanSd != null) btnScanSd.Enabled = true;
            }
        }

        private void lvFiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count == 0)
            {
                ClearPreview();
                return;
            }

            var meta = (FileItemMeta)lvFiles.SelectedItems[0].Tag!;

            if (meta.Kind == InputKind.Cov)
            {
                try
                {
                    var fi = new FileInfo(meta.InputPath);
                    SetPreview(DecodeCovToBitmap(meta.InputPath),
                        FormatCovInfoLine(Path.GetFileName(meta.InputPath), fi.Length, fi.Length == 32768));
                }
                catch (Exception ex)
                {
                    ClearPreview();
                    lblPreview.Text = $"Preview error: {ex.Message}";
                }
                return;
            }

            if (meta.Kind == InputKind.Game)
            {
                string cov = ResolveGameCovPath(meta.InputPath);
                if (File.Exists(cov))
                {
                    var fi = new FileInfo(cov);
                    SetPreview(DecodeCovToBitmap(cov), FormatCovInfoLine(Path.GetFileName(cov), fi.Length, fi.Length == 32768));
                }
                else
                {
                    ClearPreview();
                    string serialInfo = string.IsNullOrWhiteSpace(meta.Serial) ? "(serial: unknown)" : $"(serial: {meta.Serial})";
                    lblPreview.Text = $"Game: {Path.GetFileName(meta.InputPath)} — cover missing {serialInfo}";
                }
                return;
            }

            PreviewGeneratedCov(meta);
        }

        private void PreviewGeneratedCov(FileItemMeta meta)
        {
            string covPath = ResolveOutputPath(meta.InputPath);

            try
            {
                if (File.Exists(covPath))
                {
                    var fi = new FileInfo(covPath);
                    SetPreview(DecodeCovToBitmap(covPath), FormatCovInfoLine(Path.GetFileName(covPath), fi.Length, fi.Length == 32768));
                }
                else
                {
                    SetPreview(BuildProcessedPreview(meta.InputPath), $"Preview (processed): {Path.GetFileName(meta.InputPath)} (no .cov yet)");
                }
            }
            catch (Exception ex)
            {
                ClearPreview();
                lblPreview.Text = $"Preview error: {ex.Message}";
            }
        }

        private static string FormatCovInfoLine(string fileName, long sizeBytes, bool ok)
        {
            if (ok)
                return $"Preview (.cov): {fileName} — OK • 128×128 • 16bpp BGR555 • {sizeBytes:N0} bytes";

            return $"Preview (.cov): {fileName} — INVALID SIZE ({sizeBytes:N0} bytes, expected 32,768)";
        }

        private void SetPreview(Image img, string caption)
        {
            ClearPreview();
            picPreview.Image = img;
            lblPreview.Text = caption;
        }

        private void ClearPreview()
        {
            if (picPreview.Image != null)
            {
                picPreview.Image.Dispose();
                picPreview.Image = null;
            }
            lblPreview.Text = "Preview: (none)";
        }

        private static Image LoadImageNoLock(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var ms = new MemoryStream(bytes);
            using var tmp = Image.FromStream(ms);
            return (Image)tmp.Clone();
        }

        private static Bitmap CenterCropToSquare(Image src)
        {
            int w = src.Width;
            int h = src.Height;
            int size = Math.Min(w, h);

            int x = (w - size) / 2;
            int y = (h - size) / 2;

            var dst = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(dst);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.None;

            g.DrawImage(src, new Rectangle(0, 0, size, size), new Rectangle(x, y, size, size), GraphicsUnit.Pixel);
            return dst;
        }

        private static Bitmap ResizeTo128(Image srcSquare)
        {
            var dst = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(dst);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.None;

            g.DrawImage(srcSquare, new Rectangle(0, 0, 128, 128));
            return dst;
        }

        private static Bitmap BuildProcessedPreview(string inputPath)
        {
            using var src = LoadImageNoLock(inputPath);
            using var square = CenterCropToSquare(src);
            using var scaled = ResizeTo128(square);

            var bmp = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.SmoothingMode = SmoothingMode.None;
            g.DrawImage(scaled, 0, 0, 128, 128);
            return bmp;
        }

        private static void ConvertImageToCov(string inputPath, string outputPath)
        {
            using var src = LoadImageNoLock(inputPath);

            using var square = CenterCropToSquare(src);
            using var scaled = ResizeTo128(square);

            using var argb = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(argb))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.None;
                g.DrawImage(scaled, new Rectangle(0, 0, 128, 128));
            }

            var rect = new Rectangle(0, 0, 128, 128);
            var data = argb.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                byte[] outBytes = new byte[32768];

                unsafe
                {
                    byte* scan0 = (byte*)data.Scan0;
                    int stride = data.Stride;

                    int o = 0;
                    for (int y = 0; y < 128; y++)
                    {
                        byte* row = scan0 + (y * stride);
                        for (int x = 0; x < 128; x++)
                        {
                            byte b = row[x * 4 + 0];
                            byte gg = row[x * 4 + 1];
                            byte r = row[x * 4 + 2];

                            ushort b5 = (ushort)(b >> 3);
                            ushort g5 = (ushort)(gg >> 3);
                            ushort r5 = (ushort)(r >> 3);

                            ushort psx = (ushort)((b5 << 10) | (g5 << 5) | r5);

                            outBytes[o++] = (byte)(psx & 0xFF);
                            outBytes[o++] = (byte)((psx >> 8) & 0xFF);
                        }
                    }
                }

                File.WriteAllBytes(outputPath, outBytes);
            }
            finally
            {
                argb.UnlockBits(data);
            }
        }

        private static Bitmap DecodeCovToBitmap(string covPath)
        {
            byte[] data = File.ReadAllBytes(covPath);
            if (data.Length != 32768)
                throw new InvalidDataException($"Invalid .cov size: {data.Length} bytes (expected 32768).");

            var bmp = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, 128, 128);
            var bd = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* dst = (byte*)bd.Scan0;
                    int stride = bd.Stride;

                    int i = 0;
                    for (int y = 0; y < 128; y++)
                    {
                        byte* row = dst + y * stride;
                        for (int x = 0; x < 128; x++)
                        {
                            ushort psx = (ushort)(data[i] | (data[i + 1] << 8));
                            i += 2;

                            int r5 = psx & 0x1F;
                            int g5 = (psx >> 5) & 0x1F;
                            int b5 = (psx >> 10) & 0x1F;

                            byte r8 = (byte)((r5 << 3) | (r5 >> 2));
                            byte g8 = (byte)((g5 << 3) | (g5 >> 2));
                            byte b8 = (byte)((b5 << 3) | (b5 >> 2));

                            int p = x * 4;
                            row[p + 0] = b8;
                            row[p + 1] = g8;
                            row[p + 2] = r8;
                            row[p + 3] = 255;
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }

            return bmp;
        }

        // ---------------- SD Scan (multi-bin safe) ----------------

        private async void btnScanSd_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select the SD card root folder to scan (recursive).",
                UseDescriptionForTitle = true
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            btnConvert.Enabled = false;
            btnClear.Enabled = false;
            btnOutputFolder.Enabled = false;
            btnScanSd.Enabled = false;

            try
            {
                Log($"Scanning SD root: {dlg.SelectedPath}");
                await ScanSdAsync(dlg.SelectedPath);
                Log("SD scan done.");
            }
            catch (Exception ex)
            {
                Log($"Scan error: {ex.Message}");
            }
            finally
            {
                btnConvert.Enabled = true;
                btnClear.Enabled = true;
                btnOutputFolder.Enabled = true;
                btnScanSd.Enabled = true;
            }
        }

        private async Task ScanSdAsync(string root)
        {
            lvFiles.Items.Clear();
            _inputs.Clear();
            progressBar.Value = 0;
            ClearPreview();

            var cueFiles = new List<string>();
            var binFiles = new List<string>();

            foreach (var f in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (ext.Equals(".cue", StringComparison.OrdinalIgnoreCase))
                    cueFiles.Add(f);
                else if (ext.Equals(".bin", StringComparison.OrdinalIgnoreCase))
                    binFiles.Add(f);
            }

            // Rule:
            // - If a directory has at least one .cue => add only the .cue files as entries (ignore bins in that directory).
            // - If a directory has NO .cue => add each .bin as a bin-only game.
            var cueDirs = new HashSet<string>(cueFiles.Select(Path.GetDirectoryName).Where(d => d != null)!, StringComparer.OrdinalIgnoreCase);

            var entries = new List<(string gamePath, string? firstBin)>();

            foreach (var cue in cueFiles)
            {
                entries.Add((cue, TryGetFirstBinFromCue(cue)));
            }

            foreach (var bin in binFiles)
            {
                var dir = Path.GetDirectoryName(bin);
                if (dir != null && cueDirs.Contains(dir))
                    continue; // ignore multi-bin tracks

                entries.Add((bin, bin));
            }

            progressBar.Minimum = 0;
            progressBar.Maximum = entries.Count;
            progressBar.Value = 0;

            for (int idx = 0; idx < entries.Count; idx++)
            {
                var (gamePath, firstBin) = entries[idx];

                string covPath = ResolveGameCovPath(gamePath);
                bool covExists = File.Exists(covPath);

                string? serial = null;
                if (!string.IsNullOrWhiteSpace(firstBin) && File.Exists(firstBin))
                    serial = await Task.Run(() => ExtractPs1SerialFromBin(firstBin));

                string status = covExists
                    ? "SKIPPED • cover exists"
                    : (serial != null ? $"FOUND • serial {serial}" : "FOUND • serial unknown");

                var item = new ListViewItem(new[] { gamePath, covPath, status })
                {
                    Tag = new FileItemMeta(gamePath, covPath, InputKind.Game) { Serial = serial }
                };

                lvFiles.Items.Add(item);
                _inputs.Add(gamePath);

                progressBar.Value = idx + 1;
                if (idx % 25 == 0) await Task.Yield();
            }

            Log($"Scan complete. Entries: {entries.Count}. CUE entries: {cueFiles.Count}. BIN-only: {entries.Count - cueFiles.Count}.");
        }

        private static string? TryGetFirstBinFromCue(string cuePath)
        {
            try
            {
                foreach (var raw in File.ReadLines(cuePath))
                {
                    var line = raw.Trim();
                    if (!line.StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string? filePart = null;

                    int q1 = line.IndexOf('\"');
                    int q2 = q1 >= 0 ? line.IndexOf('\"', q1 + 1) : -1;
                    if (q1 >= 0 && q2 > q1)
                        filePart = line.Substring(q1 + 1, q2 - q1 - 1);
                    else
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2) filePart = parts[1];
                    }

                    if (string.IsNullOrWhiteSpace(filePart))
                        continue;

                    if (!filePart.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string dir = Path.GetDirectoryName(cuePath)!;
                    string candidate = Path.GetFullPath(Path.Combine(dir, filePart));
                    return candidate;
                }
            }
            catch { }
            return null;
        }

        private static string? ExtractPs1SerialFromBin(string binPath)
        {
            const int MaxBytesToScan = 64 * 1024 * 1024;
            const int ChunkSize = 1024 * 1024;
            const int Overlap = 64;

            var rx = new Regex(@"\b(SCUS|SLUS|SLES|SCES|SCPS|SLPS|SCPM|SIPS|SLED)_[0-9]{3}\.[0-9]{2}\b",
                RegexOptions.Compiled);

            using var fs = new FileStream(binPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long remaining = Math.Min(fs.Length, MaxBytesToScan);

            byte[] buf = new byte[ChunkSize + Overlap];
            int keep = 0;

            while (remaining > 0)
            {
                int toRead = (int)Math.Min(ChunkSize, remaining);
                int read = fs.Read(buf, keep, toRead);
                if (read <= 0) break;

                int total = keep + read;
                string s = System.Text.Encoding.Latin1.GetString(buf, 0, total);

                var m = rx.Match(s);
                if (m.Success)
                {
                    string raw = m.Value;
                    return raw.Replace('_', '-').Replace(".", "");
                }

                keep = Math.Min(Overlap, total);
                Buffer.BlockCopy(buf, total - keep, buf, 0, keep);
                remaining -= read;
            }

            return null;
        }

        // ----------------------------------------------------------


        // ---------------- Cover download (by PS1 serial) ----------------

        private async void btnDownloadCovers_Click(object sender, EventArgs e)
        {
            btnConvert.Enabled = false;
            btnClear.Enabled = false;
            btnOutputFolder.Enabled = false;
            btnScanSd.Enabled = false;
            btnDownloadCovers.Enabled = false;

            try
            {
                await DownloadCoversAsync();
            }
            catch (Exception ex)
            {
                Log($"Download covers error: {ex.Message}");
                MessageBox.Show(this, ex.Message, "Download covers error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConvert.Enabled = true;
                btnClear.Enabled = true;
                btnOutputFolder.Enabled = true;
                btnScanSd.Enabled = true;
                btnDownloadCovers.Enabled = true;
            }
        }

        private static string? NormalizeSerial(string? serial)
        {
            if (string.IsNullOrWhiteSpace(serial)) return null;
            serial = serial.Trim().ToUpperInvariant();
            serial = serial.Replace('_', '-').Replace(".", "");
            return serial;
        }

        private string GetCoverCacheDir()
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "cover_cache");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private IEnumerable<string> BuildCoverUrls(string serial)
        {
            string baseUrl = GetCoverBaseUrl();

            yield return $"{baseUrl}/covers/default/{serial}.jpg";
            yield return $"{baseUrl}/covers/default/{serial}.png";
            yield return $"{baseUrl}/covers/3d/{serial}.png";
            yield return $"{baseUrl}/covers/3d/{serial}.jpg";
        }

        private async Task DownloadCoversAsync()
        {
            var gameItems = lvFiles.Items
                .Cast<ListViewItem>()
                .Select(it => (it, meta: it.Tag as FileItemMeta))
                .Where(t => t.meta != null && t.meta.Kind == InputKind.Game)
                .ToList();

            if (gameItems.Count == 0)
            {
                MessageBox.Show(this, "No scanned games found. Use 'Scan SD...' first.", "Download covers",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string cacheDir = GetCoverCacheDir();

            progressBar.Minimum = 0;
            progressBar.Maximum = gameItems.Count;
            progressBar.Value = 0;

            int ok = 0, skipped = 0, noSerial = 0, notFound = 0, fail = 0;

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(25);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PicoStationCovMaker/1.0");

            for (int i = 0; i < gameItems.Count; i++)
            {
                var (item, meta) = gameItems[i];
                progressBar.Value = i + 1;

                string gamePath = meta!.InputPath;
                string covPath = ResolveGameCovPath(gamePath);

                if (File.Exists(covPath))
                {
                    item.SubItems[2].Text = "SKIPPED • cover exists";
                    skipped++;
                    continue;
                }

                string? serial = NormalizeSerial(meta.Serial);
                if (string.IsNullOrWhiteSpace(serial))
                {
                    item.SubItems[2].Text = "NO SERIAL";
                    noSerial++;
                    continue;
                }

                item.SubItems[2].Text = $"Downloading {serial}...";
                await Task.Yield();

                try
                {
                    string? imgPath = await DownloadCoverImageAsync(http, cacheDir, serial);
                    if (imgPath == null)
                    {
                        item.SubItems[2].Text = $"NOT FOUND • {serial}";
                        notFound++;
                        continue;
                    }

                    // Reuse existing conversion pipeline
                    ConvertImageToCov(imgPath, covPath);

                    long sz = new FileInfo(covPath).Length;
                    if (sz != 32768)
                        throw new IOException($"Output size was {sz} bytes, expected 32768.");

                    item.SubItems[1].Text = covPath;
                    item.SubItems[2].Text = $"OK • {serial}";
                    ok++;

                    if (item.Selected)
                        lvFiles_SelectedIndexChanged(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    item.SubItems[2].Text = "ERROR";
                    Log($"Cover ERROR for {serial}: {ex.Message}");
                    fail++;
                }

                if (i % 20 == 0)
                    await Task.Yield();
            }

            Log($"Download covers done. OK: {ok}, Skipped: {skipped}, NoSerial: {noSerial}, NotFound: {notFound}, Failed: {fail}");
        }

        private async Task<string?> DownloadCoverImageAsync(HttpClient http, string cacheDir, string serial)
        {
            foreach (var url in BuildCoverUrls(serial))
            {
                string ext = Path.GetExtension(url);
                string cachePath = Path.Combine(cacheDir, $"{serial}{ext}");

                try
                {
                    if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 0)
                        return cachePath;
                }
                catch { }

                try
                {
                    using var resp = await http.GetAsync(url);
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                        continue;

                    resp.EnsureSuccessStatusCode();

                    byte[] bytes = await resp.Content.ReadAsByteArrayAsync();
                    if (bytes.Length < 512)
                        continue;

                    await File.WriteAllBytesAsync(cachePath, bytes);
                    return cachePath;
                }
                catch
                {
                    // try next url
                }
            }

            return null;
        }

        // ----------------------------------------------------------



        private void txtCoverBaseUrl_TextChanged(object? sender, EventArgs e)
        {
            try
            {
                // Keep both configs in sync if both systems exist in this build.
                if (_config != null)
                {
                    _config.CoverBaseUrl = (txtCoverBaseUrl.Text ?? "").Trim();
                    SaveConfig();
                }
                SaveCoverSourceSettings();
            }
            catch
            {
                // ignore
            }
        }

        private void btnCoverUrlReset_Click(object? sender, EventArgs e)
        {
            try
            {
                txtCoverBaseUrl.Text = DefaultCoverBaseUrl;

                if (_config != null)
                {
                    _config.CoverBaseUrl = DefaultCoverBaseUrl;
                    SaveConfig();
                }

                SaveCoverSourceSettings();
            }
            catch
            {
                // ignore
            }
        }

        private void Log(string msg)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
        }

        private void lblOutputFolder_Click(object sender, EventArgs e)
        {

        }
    }
}
