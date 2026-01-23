using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private string? _currentPreviewCovPath;
        // Sorting: toggle missing-serial items to top when clicking Status column
        private bool _missingSerialFirstEnabled;
        private int _nextOriginalIndex;
        private bool _coverUrlInternalUpdate;


        // Base repo (kept for the config textbox); actual downloads use the 3 fixed paths below.
        private const string DefaultCoverBaseUrl = "https://raw.githubusercontent.com/megavolt85/picostation-covers/main";

        private string CoverSourceConfigPath => Path.Combine(AppContext.BaseDirectory, "PicoStationCovMaker.config.json");

        private sealed class CoverSourceCfg
        {
            public string? CoverBaseUrl { get; set; }
        }


        private void UpdatePngquantAvailability()
        {
            try
            {
                string pngquantPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "pngquant.exe");
                bool hasPngquant = File.Exists(pngquantPath);

                // Convert requires pngquant.exe (disable button if missing)
                if (btnConvert != null)
                btnConvert.Enabled = hasPngquant;
                // 8bpp mode requires pngquant.exe (no fallback)
                if (rb8bpp != null)
                {
                    rb8bpp.Enabled = hasPngquant;

                    // If user had 8bpp selected but pngquant is missing, force 16bpp.
                    if (!hasPngquant && rb8bpp.Checked && rb16bpp != null)
                    rb16bpp.Checked = true;
                }
            }
            catch
            {
                // If anything goes wrong, fail safe: disable 8bpp.
                if (rb8bpp != null)
                {
                    rb8bpp.Enabled = false;
                    if (rb8bpp.Checked && rb16bpp != null)
                    rb16bpp.Checked = true;
                }
                if (btnConvert != null)
                btnConvert.Enabled = false;
            }
        }

        private void LoadCoverSourceSettings()
        {
            // Legacy helper (older builds stored only one URL).
            // Current build uses the same config file but supports a dropdown (multiple URLs).
            try
            {
                if (cmbCoverBaseUrl == null) return;

                string selected = DefaultCoverBaseUrl;
                var list = new List<string>();

                if (File.Exists(CoverSourceConfigPath))
                {
                    var json = File.ReadAllText(CoverSourceConfigPath);

                    // Try current schema first
                    try
                    {
                        var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                        if (cfg != null)
                        {
                            if (cfg.CoverBaseUrls != null) list.AddRange(cfg.CoverBaseUrls);
                            if (!string.IsNullOrWhiteSpace(cfg.CoverBaseUrl)) selected = cfg.CoverBaseUrl.Trim();
                        }
                    }
                    catch
                    {
                        // Fallback to legacy schema
                        var obj = JsonSerializer.Deserialize<CoverSourceCfg>(json);
                        var url = obj?.CoverBaseUrl;
                        if (!string.IsNullOrWhiteSpace(url)) selected = url.Trim();
                    }
                }

                if (list.Count == 0) list.Add(DefaultCoverBaseUrl);
                if (!list.Any(s => string.Equals(s?.Trim(), selected, StringComparison.OrdinalIgnoreCase)))
                    list.Insert(0, selected);

                PopulateCoverUrlDropdown(list, selected);
            }
            catch
            {
                PopulateCoverUrlDropdown(new List<string> { DefaultCoverBaseUrl }, DefaultCoverBaseUrl);
            }
        }

        private void SaveCoverSourceSettings()
        {
            // Legacy helper (older builds stored only one URL).
            // Current build persists via SaveConfig().
            try
            {
                if (_config != null)
                {
                    SaveConfig();
                }
            }
            catch
            {
                // ignore
            }
        }

        private string GetCoverBaseUrl()
        {
            var s = (cmbCoverBaseUrl?.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s)) return DefaultCoverBaseUrl;
            return s.TrimEnd('/');
        }

        private const string ConfigFileName = "PicoStationCovMaker.config.json";

        private sealed class AppConfig
        {
            // Selected URL
            public string? CoverBaseUrl { get; set; }
            // Dropdown history / presets
            public List<string>? CoverBaseUrls { get; set; }
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

            public int OriginalIndex { get; set; }

            public FileItemMeta(string input, string output, InputKind kind)
            {
                InputPath = input;
                OutputPath = output;
                Kind = kind;
            }
        }



        private enum SerialPromptAction
        {
            Ok,
            Skip,
            CancelAll
        }

        private SerialPromptAction PromptSerialForImage(string imagePath, out string? serialDash)
        {
            serialDash = null;

            using var dlg = new Form();
            dlg.Text = "Enter PS1 Serial";
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.ShowInTaskbar = false;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.ClientSize = new Size(520, 220);

            var lbl = new Label
            {
                AutoSize = false,
                Left = 170,
                Top = 15,
                Width = 330,
                Height = 45,
                Text = $"Enter serial for:\r\n{Path.GetFileName(imagePath)}"
            };

            var pic = new PictureBox
            {
                Left = 15,
                Top = 15,
                Width = 140,
                Height = 140,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            Bitmap? thumb = null;
            try
            {
                using var src = LoadImageNoLock(imagePath);
                using var square = CenterCropToSquare(src);
                using var scaled = ResizeTo128(square);
                thumb = new Bitmap(scaled);
                pic.Image = thumb;
            }
            catch
            {
                // If thumbnail fails, we still allow serial entry.
            }

            var tb = new TextBox
            {
                Left = 170,
                Top = 80,
                Width = 330,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var hint = new Label
            {
                AutoSize = false,
                Left = 170,
                Top = 110,
                Width = 330,
                Height = 30,
                Text = "Example: SLUS-01206 (leave empty to skip)"
            };

            var btnOk = new Button { Text = "OK", Left = 170, Top = 160, Width = 90, DialogResult = DialogResult.OK };
            var btnSkip = new Button { Text = "Skip", Left = 270, Top = 160, Width = 90, DialogResult = DialogResult.Ignore };
            var btnCancelAll = new Button { Text = "Cancel All", Left = 370, Top = 160, Width = 130, DialogResult = DialogResult.Cancel };

            dlg.Controls.Add(lbl);
            dlg.Controls.Add(pic);
            dlg.Controls.Add(tb);
            dlg.Controls.Add(hint);
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnSkip);
            dlg.Controls.Add(btnCancelAll);

            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancelAll;

            var dr = dlg.ShowDialog(this);

            try
            {
                pic.Image = null;
                thumb?.Dispose();
            }
            catch { }

            if (dr == DialogResult.Cancel)
                return SerialPromptAction.CancelAll;

            if (dr == DialogResult.Ignore)
                return SerialPromptAction.Skip;

            var raw = (tb.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return SerialPromptAction.Skip;

            serialDash = NormalizeSerial(raw) ?? raw.ToUpperInvariant();
            return SerialPromptAction.Ok;
        }
        public MainForm()
        {
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            this.ShowInTaskbar = true;
            InitializeComponent();
            UpdatePngquantAvailability();
            LoadCoverSourceSettings();
            LoadConfig();
            // Populate dropdown from config (and keep legacy loader working)
            var urls = (_config.CoverBaseUrls ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
            if (urls.Count == 0) urls.Add(DefaultCoverBaseUrl);

            var selected = string.IsNullOrWhiteSpace(_config.CoverBaseUrl) ? DefaultCoverBaseUrl : _config.CoverBaseUrl.Trim();
            if (!urls.Any(u => string.Equals(u, selected, StringComparison.OrdinalIgnoreCase)))
                urls.Insert(0, selected);

            PopulateCoverUrlDropdown(urls, selected);

            if (cmbCoverBaseUrl != null)
            {
                // Commit the URL only when the user confirms (Enter) or selects an item.
                // Saving + repopopulating on each keystroke causes caret jumps ("typing backwards").
                cmbCoverBaseUrl.KeyDown += cmbCoverBaseUrl_KeyDown;
                cmbCoverBaseUrl.SelectionChangeCommitted += cmbCoverBaseUrl_SelectionChangeCommitted;
                cmbCoverBaseUrl.DropDown += cmbCoverBaseUrl_DropDown;
            }


            // Hook selection preview (in case Designer didn't)
            lvFiles.SelectedIndexChanged += lvFiles_SelectedIndexChanged;
            lvFiles.ColumnClick += lvFiles_ColumnClick;

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
            bool cancelAll = false;

            foreach (var f in files.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (cancelAll) break;
                if (_inputs.Contains(f)) continue;

                var kind = GetKind(f);

                // For images: ask serial per file (PicoStation expects a footer; leave empty to skip).
                string? serialDash = null;
                if (kind == InputKind.Image)
                {
                    var action = PromptSerialForImage(f, out serialDash);
                    if (action == SerialPromptAction.CancelAll)
                    {
                        cancelAll = true;
                        break;
                    }
                    if (action != SerialPromptAction.Ok)
                        serialDash = null;
                }

                if (!_inputs.Add(f)) continue;

                string outPath = kind == InputKind.Image ? ResolveOutputPath(f) : f;
                string status = kind == InputKind.Cov ? "COV (preview)" : "Ready";

                var meta = new FileItemMeta(f, outPath, kind);
                if (kind == InputKind.Image)
                    meta.Serial = serialDash;


                meta.OriginalIndex = _nextOriginalIndex++;

                var item = new ListViewItem(new[] { f, outPath, status })
                {
                    Tag = meta
                };

                lvFiles.Items.Add(item);
                added++;
            }

            if (_missingSerialFirstEnabled && added > 0)
                ApplyMissingSerialFirstSort();

            Log(added > 0 ? $"Added {added} item(s)." : "No supported files found.");
        }


        private void btnClear_Click(object sender, EventArgs e)
        {
            lvFiles.Items.Clear();
            _inputs.Clear();
            _nextOriginalIndex = 0;
            _missingSerialFirstEnabled = false;
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
                        // 8bpp-only
                        ConvertImageToCov8bpp(meta.InputPath, outPath);

                        // Always ensure the 16-byte serial footer area exists (16912 total).
                        var covBytes = File.ReadAllBytes(outPath);
                        covBytes = EnsureFooter16912(covBytes, meta.Serial);
                        File.WriteAllBytes(outPath, covBytes);

                        long sz = covBytes.Length;
                        if (sz != 16912)
                            throw new IOException($"Output size was {sz} bytes, expected 16912.");

                        item.SubItems[2].Text = $"OK ({sz} bytes)";
                        meta.OutputPath = outPath;
                        item.SubItems[1].Text = outPath;
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
                    FormatCovInfoLine(Path.GetFileName(meta.InputPath), fi.Length), meta.InputPath);
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
                    SetPreview(DecodeCovToBitmap(cov), FormatCovInfoLine(Path.GetFileName(cov), fi.Length));
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
        private void lvFiles_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (lvFiles == null || lvFiles.Columns == null || lvFiles.Columns.Count == 0) return;

            // Only toggle sorting when the user clicks the Status column
            int statusColIndex = -1;
            try
            {
                if (colStatus != null)
                    statusColIndex = lvFiles.Columns.IndexOf(colStatus);
            }
            catch { statusColIndex = -1; }

            if (statusColIndex < 0)
                statusColIndex = 2; // fallback (Input, Output, Status)

            if (e.Column != statusColIndex)
                return;

            ToggleMissingSerialFirstSort();
        }

        private void ToggleMissingSerialFirstSort()
        {
            _missingSerialFirstEnabled = !_missingSerialFirstEnabled;

            if (_missingSerialFirstEnabled)
                ApplyMissingSerialFirstSort();
            else
                RestoreOriginalOrder();
        }

        private void ApplyMissingSerialFirstSort()
        {
            if (lvFiles == null) return;

            lvFiles.BeginUpdate();
            try
            {
                // Ensure grouping is fully disabled (prevents the blue 'Default' group)
                foreach (ListViewItem it in lvFiles.Items)
                    it.Group = null;
                lvFiles.Groups.Clear();
                lvFiles.ShowGroups = false;

                lvFiles.ListViewItemSorter = new MissingSerialFirstComparer();
                lvFiles.Sort();
            }
            finally
            {
                lvFiles.EndUpdate();
            }
        }

        private void RestoreOriginalOrder()
        {
            if (lvFiles == null) return;

            lvFiles.BeginUpdate();
            try
            {
                foreach (ListViewItem it in lvFiles.Items)
                    it.Group = null;
                lvFiles.Groups.Clear();
                lvFiles.ShowGroups = false;

                lvFiles.ListViewItemSorter = new OriginalOrderComparer();
                lvFiles.Sort();
                lvFiles.ListViewItemSorter = null;
            }
            finally
            {
                lvFiles.EndUpdate();
            }
        }

        private static bool IsMissingSerial(object? tag)
        {
            if (tag is not FileItemMeta m) return true;
            var s = (m.Serial ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(s) || s.Equals("unknown", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class MissingSerialFirstComparer : System.Collections.IComparer
        {
            public int Compare(object? x, object? y)
            {
                var a = x as ListViewItem;
                var b = y as ListViewItem;
                if (a == null || b == null) return 0;

                bool amiss = IsMissingSerial(a.Tag);
                bool bmiss = IsMissingSerial(b.Tag);

                if (amiss != bmiss)
                    return amiss ? -1 : 1;

                int ai = (a.Tag as FileItemMeta)?.OriginalIndex ?? int.MaxValue;
                int bi = (b.Tag as FileItemMeta)?.OriginalIndex ?? int.MaxValue;
                return ai.CompareTo(bi);
            }
        }

        private sealed class OriginalOrderComparer : System.Collections.IComparer
        {
            public int Compare(object? x, object? y)
            {
                var a = x as ListViewItem;
                var b = y as ListViewItem;
                if (a == null || b == null) return 0;

                int ai = (a.Tag as FileItemMeta)?.OriginalIndex ?? int.MaxValue;
                int bi = (b.Tag as FileItemMeta)?.OriginalIndex ?? int.MaxValue;
                return ai.CompareTo(bi);
            }
        }


        private void PreviewGeneratedCov(FileItemMeta meta)
        {
            string covPath = ResolveOutputPath(meta.InputPath);

            try
            {
                if (File.Exists(covPath))
                {
                    var fi = new FileInfo(covPath);
                    SetPreview(DecodeCovToBitmap(covPath), FormatCovInfoLine(Path.GetFileName(covPath), fi.Length));
                }
                else
                {
                    SetPreview(BuildProcessedPreview(meta.InputPath), $"Processed: {Path.GetFileName(meta.InputPath)} (no .cov yet)");
                }
            }
            catch (Exception ex)
            {
                ClearPreview();
                lblPreview.Text = $"Preview error: {ex.Message}";
            }
        }

        private static string FormatCovInfoLine(string fileName, long sizeBytes)
        {
            if (sizeBytes == 32768)
            return "UNSUPPORTED legacy 16bpp (32768 bytes)";
            if (sizeBytes == 16896 || sizeBytes == 16912)
            return $"Preview (.cov): {fileName} — OK • 128×128 • 8bpp indexed + palette • {sizeBytes:N0} bytes";

            return $"Preview (.cov): {fileName} — INVALID SIZE ({sizeBytes:N0} bytes)";
        }


        private void SetPreview(Image img, string caption, string? covPathForPalette = null)
        {
            _currentPreviewCovPath = covPathForPalette;
            ClearPreviewImageOnly();
            picPreview.Image = img;
            lblPreview.Text = caption;
            UpdateExportPaletteButton();
        }

        private void ClearPreview()
        {
            _currentPreviewCovPath = null;
            ClearPreviewImageOnly();
            lblPreview.Text = "Preview: (none)";
            UpdateExportPaletteButton();
        }

        private void ClearPreviewImageOnly()
        {
            if (picPreview.Image != null)
            {
                picPreview.Image.Dispose();
                picPreview.Image = null;
            }
        }

        private void UpdateExportPaletteButton()
        {
            try
            {
                if (btnExportPalette == null)
                return;

                if (string.IsNullOrWhiteSpace(_currentPreviewCovPath) || !File.Exists(_currentPreviewCovPath))
                {
                    btnExportPalette.Enabled = false;
                    return;
                }

                var fi = new FileInfo(_currentPreviewCovPath);
                btnExportPalette.Enabled = fi.Length == 16896 || fi.Length == 16912;
            }
            catch
            {
                if (btnExportPalette != null) btnExportPalette.Enabled = false;
            }
        }

        private void btnExportPalette_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentPreviewCovPath) || !File.Exists(_currentPreviewCovPath))
                {
                    MessageBox.Show(this, "No .cov is currently previewed.", "Export palette", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var covBytes = File.ReadAllBytes(_currentPreviewCovPath);
                if (covBytes.Length != 16896 && covBytes.Length != 16912)
                {
                    MessageBox.Show(this,
                    "Palette export is only available for 8bpp .cov files (expected 16,896 bytes: 512-byte palette + 16,384-byte indices).",
                    "Export palette",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                    return;
                }

                var palBytes = BuildRiffPalFromCovPalette(covBytes);

                using var sfd = new SaveFileDialog();
                sfd.Title = "Export palette (.pal)";
                sfd.Filter = "RIFF Palette (*.pal)|*.pal|All files (*.*)|*.*";
                sfd.AddExtension = true;
                sfd.OverwritePrompt = true;

                var baseName = Path.GetFileNameWithoutExtension(_currentPreviewCovPath);
                sfd.FileName = baseName + ".pal";

                if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

                File.WriteAllBytes(sfd.FileName, palBytes);
                Log($"Exported palette: {sfd.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export palette", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static byte[] BuildRiffPalFromCovPalette(byte[] covBytes)
        {
            // covBytes[0..511] = 256 * 16-bit PS1 RGB555 little-endian
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write((uint)0); // placeholder
            bw.Write(Encoding.ASCII.GetBytes("PAL "));
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write((uint)(256 * 4));

            for (int i = 0; i < 256; i++)
            {
                int lo = covBytes[i * 2];
                int hi = covBytes[i * 2 + 1];
                ushort v = (ushort)(lo | (hi << 8));

                int r5 = v & 0x1F;
                int g5 = (v >> 5) & 0x1F;
                int b5 = (v >> 10) & 0x1F;

                byte r8 = (byte)((r5 << 3) | (r5 >> 2));
                byte g8 = (byte)((g5 << 3) | (g5 >> 2));
                byte b8 = (byte)((b5 << 3) | (b5 >> 2));

                bw.Write(r8);
                bw.Write(g8);
                bw.Write(b8);
                bw.Write((byte)0);
            }

            // Fix RIFF size
            bw.Flush();
            long len = ms.Length;
            ms.Position = 4;
            bw.Write((uint)(len - 8));
            bw.Flush();
            return ms.ToArray();
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




        private static (Color[] paletteRgba, byte[] indices) ReadIndexedPng8(string path, int expectedW, int expectedH)
        {
            // Minimal PNG decoder for 8-bit indexed (color type 3), non-interlaced.
            // Needed because System.Drawing does not reliably preserve PNG tRNS alpha for paletted images.
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            byte[] sig = br.ReadBytes(8);
            byte[] pngSig = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (sig.Length != 8 || !sig.SequenceEqual(pngSig))
            throw new InvalidDataException("Not a PNG file: " + path);

            int width = 0, height = 0;
            byte bitDepth = 0, colorType = 0, interlace = 0;
            byte[]? plte = null;
            byte[]? trns = null;
            using var idat = new MemoryStream();

            while (fs.Position < fs.Length)
            {
                uint len = ReadBE32(br);
                string type = new string(br.ReadChars(4));
                byte[] data = br.ReadBytes((int)len);
                br.ReadUInt32(); // CRC (ignored)

                if (type == "IHDR")
                {
                    width = (int)ReadBE32(data, 0);
                    height = (int)ReadBE32(data, 4);
                    bitDepth = data[8];
                    colorType = data[9];
                    interlace = data[12];
                }
                else if (type == "PLTE")
                {
                    plte = data;
                }
                else if (type == "tRNS")
                {
                    trns = data;
                }
                else if (type == "IDAT")
                {
                    idat.Write(data, 0, data.Length);
                }
                else if (type == "IEND")
                {
                    break;
                }
            }

            if (width != expectedW || height != expectedH)
            throw new InvalidDataException($"pngquant output is not {expectedW}x{expectedH}.");
            if (colorType != 3 || bitDepth != 8)
            throw new InvalidDataException("pngquant output is not an 8-bit indexed PNG (color type 3, bit depth 8).");
            if (interlace != 0)
            throw new InvalidDataException("Interlaced PNG not supported.");
            if (plte == null || plte.Length % 3 != 0)
            throw new InvalidDataException("PNG missing PLTE chunk.");

            int palCount = plte.Length / 3;
            var palette = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                byte r = 0, g = 0, b = 0, a = 255;
                if (i < palCount)
                {
                    r = plte[i * 3 + 0];
                    g = plte[i * 3 + 1];
                    b = plte[i * 3 + 2];
                    if (trns != null && i < trns.Length)
                    a = trns[i];
                }
                else
                {
                    if (trns != null && i < trns.Length)
                    a = trns[i];
                }
                palette[i] = Color.FromArgb(a, r, g, b);
            }

            // Decompress IDAT (zlib)
            idat.Position = 0;
            using var z = new System.IO.Compression.ZLibStream(idat, System.IO.Compression.CompressionMode.Decompress);
            byte[] raw = new byte[(expectedW + 1) * expectedH];
            int read = 0;
            while (read < raw.Length)
            {
                int n = z.Read(raw, read, raw.Length - read);
                if (n <= 0) break;
                read += n;
            }
            if (read != raw.Length)
            throw new InvalidDataException("Failed to decompress PNG image data.");

            // Unfilter
            byte[] indices = new byte[expectedW * expectedH];
            int bpp = 1; // bytes per pixel for indexed8
            int src = 0;
            int dst = 0;
            byte[] prev = new byte[expectedW];
            byte[] cur = new byte[expectedW];

            for (int y = 0; y < expectedH; y++)
            {
                byte filter = raw[src++];
                Buffer.BlockCopy(raw, src, cur, 0, expectedW);
                src += expectedW;

                switch (filter)
                {
                    case 0: // None
                    break;
                    case 1: // Sub
                    for (int x = 0; x < expectedW; x++)
                    {
                        byte left = (x >= bpp) ? cur[x - bpp] : (byte)0;
                        cur[x] = (byte)(cur[x] + left);
                    }
                    break;
                    case 2: // Up
                    for (int x = 0; x < expectedW; x++)
                    cur[x] = (byte)(cur[x] + prev[x]);
                    break;
                    case 3: // Average
                    for (int x = 0; x < expectedW; x++)
                    {
                        byte left = (x >= bpp) ? cur[x - bpp] : (byte)0;
                        byte up = prev[x];
                        cur[x] = (byte)(cur[x] + ((left + up) >> 1));
                    }
                    break;
                    case 4: // Paeth
                    for (int x = 0; x < expectedW; x++)
                    {
                        byte a = (x >= bpp) ? cur[x - bpp] : (byte)0;
                        byte b = prev[x];
                        byte c = (x >= bpp) ? prev[x - bpp] : (byte)0;
                        cur[x] = (byte)(cur[x] + PaethPredictor(a, b, c));
                    }
                    break;
                    default:
                    throw new InvalidDataException("Unsupported PNG filter: " + filter);
                }

                Buffer.BlockCopy(cur, 0, indices, dst, expectedW);
                dst += expectedW;

                // swap prev/cur
                var tmp = prev; prev = cur; cur = tmp;
            }

            return (palette, indices);
        }

        private static byte PaethPredictor(byte a, byte b, byte c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }

        private static uint ReadBE32(BinaryReader br)
        {
            var b = br.ReadBytes(4);
            if (b.Length != 4) throw new EndOfStreamException();
            return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        }

        private static uint ReadBE32(byte[] buf, int ofs)
        {
            return ((uint)buf[ofs] << 24) | ((uint)buf[ofs + 1] << 16) | ((uint)buf[ofs + 2] << 8) | buf[ofs + 3];
        }

        private static ushort To5(byte v)
        {
            // Match convertImage.py rounding: ((v * 31) + 127) // 255
            return (ushort)(((v * 31) + 127) / 255);
        }
        private static void ConvertImageToCov8bpp(string inputPath, string outputPath)
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string pngquantPath = Path.Combine(exeDir, "Tools", "pngquant.exe");
            if (!File.Exists(pngquantPath))
            throw new FileNotFoundException("pngquant.exe not found. Place it at: " + pngquantPath);

            using var src = LoadImageNoLock(inputPath);
            using var square = CenterCropToSquare(src);
            using var scaled = ResizeTo128(square);

            string tempDir = Path.Combine(Path.GetTempPath(), "PicoStationCovMaker");
            Directory.CreateDirectory(tempDir);
            string tmpResized = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + "_resized.png");
            string tmp8bpp = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + "_8bpp.png");

            try
            {
                // Match the external pipeline: keep alpha in the PNG fed to pngquant.
                scaled.Save(tmpResized, ImageFormat.Png);

                var psi = new ProcessStartInfo
                {
                    FileName = pngquantPath,
                    Arguments = $"--force --strip --posterize 3 --speed 1 --output \"{tmp8bpp}\" \"{tmpResized}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = tempDir
                };

                using (var p = Process.Start(psi)!)
                {
                    string stdout = p.StandardOutput.ReadToEnd();
                    string stderr = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    if (p.ExitCode != 0 || !File.Exists(tmp8bpp))
                    throw new InvalidOperationException("pngquant failed.\n" + stderr + (string.IsNullOrWhiteSpace(stdout) ? "" : ("\n" + stdout)));
                }

                // Read PLTE + tRNS reliably (System.Drawing is unreliable for indexed alpha).
                var (paletteRgba, indices) = ReadIndexedPng8(tmp8bpp, 128, 128);

                byte[] outBytes = new byte[16896];

                // Palette conversion exactly like convertImage.py (spicyjpeg):
                // - alpha < 32  => 0x0000 (transparent)
                // - alpha >= 32 => STP bit set
                // - alpha >= 224 => solid (no STP)
                // - solid black (0x0000) at alpha>=224 => 0x0421
                for (int i = 0; i < 256; i++)
                {
                    Color c = paletteRgba[i];

                    ushort r5 = To5(c.R);
                    ushort g5 = To5(c.G);
                    ushort b5 = To5(c.B);
                    ushort solid = (ushort)(r5 | (g5 << 5) | (b5 << 10));
                    ushort semi = (ushort)(solid | 0x8000);

                    byte a = c.A;
                    ushort psx = 0x0000;
                    if (a >= 32)
                    psx = semi;
                    if (a >= 224)
                    {
                        psx = solid;
                        if (psx == 0x0000)
                        psx = 0x0421;
                    }

                    int o = i * 2;
                    outBytes[o + 0] = (byte)(psx & 0xFF);
                    outBytes[o + 1] = (byte)((psx >> 8) & 0xFF);
                }

                Buffer.BlockCopy(indices, 0, outBytes, 512, 128 * 128);
                File.WriteAllBytes(outputPath, outBytes);
            }
            finally
            {
                try { if (File.Exists(tmpResized)) File.Delete(tmpResized); } catch { }
                try { if (File.Exists(tmp8bpp)) File.Delete(tmp8bpp); } catch { }
            }
        }



        private sealed class OctreeQuantizer
        {
            private sealed class Node
            {
                public bool IsLeaf;
                public int PixelCount;
                public int R, G, B;
                public Node?[] Children = new Node?[8];
                public Node? NextReducible;
                public int PaletteIndex;
            }

            private readonly int _maxColors;
            private int _leafCount;
            private readonly Node _root = new Node();
            private readonly Node?[] _reducible = new Node?[9]; // levels 0-8
            private List<Color> _palette = new List<Color>(256);

            public OctreeQuantizer(int maxColors)
            {
                _maxColors = Math.Max(2, Math.Min(256, maxColors));
            }

            public void AddColor(byte r, byte g, byte b)
            {
                AddColor(_root, r, g, b, 0);
                while (_leafCount > _maxColors)
                Reduce();
            }

            private void AddColor(Node node, byte r, byte g, byte b, int level)
            {
                if (node.IsLeaf)
                {
                    node.PixelCount++;
                    node.R += r;
                    node.G += g;
                    node.B += b;
                    return;
                }

                int idx = GetIndex(r, g, b, level);
                var child = node.Children[idx];
                if (child == null)
                {
                    child = new Node();
                    node.Children[idx] = child;

                    if (level == 8)
                    {
                        child.IsLeaf = true;
                        _leafCount++;
                    }
                    else
                    {
                        child.NextReducible = _reducible[level];
                        _reducible[level] = child;
                    }
                }

                AddColor(child, r, g, b, level + 1);
            }

            private static int GetIndex(byte r, byte g, byte b, int level)
            {
                int shift = 7 - level;
                int ri = (r >> shift) & 1;
                int gi = (g >> shift) & 1;
                int bi = (b >> shift) & 1;
                return (ri << 2) | (gi << 1) | bi;
            }

            private void Reduce()
            {
                int level = 8;
                while (level > 0 && _reducible[level] == null)
                level--;

                var node = _reducible[level];
                if (node == null) return;

                _reducible[level] = node.NextReducible;

                int r = 0, g = 0, b = 0, count = 0;
                int childrenLeaves = 0;

                for (int i = 0; i < 8; i++)
                {
                    var child = node.Children[i];
                    if (child == null) continue;

                    r += child.R;
                    g += child.G;
                    b += child.B;
                    count += child.PixelCount;

                    if (child.IsLeaf) childrenLeaves++;
                    else childrenLeaves += CountLeaves(child);

                    node.Children[i] = null;
                }

                node.IsLeaf = true;
                node.R = r;
                node.G = g;
                node.B = b;
                node.PixelCount = count;

                _leafCount -= (childrenLeaves - 1);
            }

            private static int CountLeaves(Node node)
            {
                if (node.IsLeaf) return 1;
                int n = 0;
                for (int i = 0; i < 8; i++)
                {
                    var c = node.Children[i];
                    if (c != null) n += CountLeaves(c);
                }
                return n;
            }

            public List<Color> BuildPalette()
            {
                _palette = new List<Color>(_leafCount);
                BuildPalette(_root);
                if (_palette.Count == 0)
                _palette.Add(Color.Black);
                return _palette;
            }

            private void BuildPalette(Node node)
            {
                if (node.IsLeaf)
                {
                    int count = Math.Max(1, node.PixelCount);
                    int r = node.R / count;
                    int g = node.G / count;
                    int b = node.B / count;
                    node.PaletteIndex = _palette.Count;
                    _palette.Add(Color.FromArgb(r, g, b));
                    return;
                }

                for (int i = 0; i < 8; i++)
                {
                    var child = node.Children[i];
                    if (child != null) BuildPalette(child);
                }
            }

            public byte GetPaletteIndex(byte r, byte g, byte b)
            {
                return (byte)GetPaletteIndex(_root, r, g, b, 0);
            }

            private int GetPaletteIndex(Node node, byte r, byte g, byte b, int level)
            {
                if (node.IsLeaf)
                return node.PaletteIndex;

                int idx = GetIndex(r, g, b, level);
                var child = node.Children[idx];
                if (child == null)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        child = node.Children[i];
                        if (child != null) break;
                    }
                    if (child == null) return 0;
                }

                return GetPaletteIndex(child, r, g, b, level + 1);
            }
        }

        private static Bitmap DecodeCovToBitmap(string covPath)
        {
            byte[] data = File.ReadAllBytes(covPath);

            // 8bpp .cov: palette (512 bytes) + indices (16384 bytes) = 16896
            // Optional serial appended (+16 bytes) = 16912
            if (data.Length == 16896 || data.Length == 16912)
            return Decode8bppCovToBitmap(data);

            if (data.Length == 32768)
            throw new InvalidDataException($"Unsupported legacy 16bpp .cov: {data.Length} bytes. This build supports only 16896/16912-byte 8bpp .cov files.");

            throw new InvalidDataException($"Invalid .cov size: {data.Length} bytes (expected 16896 or 16912).");
        }


        private static Bitmap Decode8bppCovToBitmap(byte[] data)
        {
            // palette: 256 * 2 bytes, then 128*128 indices
            var pal = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                int o = i * 2;
                ushort psx = (ushort)(data[o] | (data[o + 1] << 8));

                int r5 = psx & 0x1F;
                int g5 = (psx >> 5) & 0x1F;
                int b5 = (psx >> 10) & 0x1F;

                byte r8 = (byte)((r5 << 3) | (r5 >> 2));
                byte g8 = (byte)((g5 << 3) | (g5 >> 2));
                byte b8 = (byte)((b5 << 3) | (b5 >> 2));

                // Hardware-like preview:
                // - 0x0000 is treated as fully transparent
                // - STP bit (0x8000) is treated as semi-transparent
                // - otherwise opaque
                byte a8 = 255;
                if (psx == 0x0000)
                    a8 = 0;
                else if ((psx & 0x8000) != 0)
                    a8 = 128;

                pal[i] = Color.FromArgb(a8, r8, g8, b8);
            }

            // Preview-only cleanup:
            // We want CD/disc covers to preview without the surrounding square background,
            // but we must NOT break 3D/front covers where the corner background color can
            // be a real part of the artwork (dithering/edges).
            //
            // Strategy:
            // 1) Detect a dominant "corner background" palette index.
            // 2) Only if the image looks like a DISC (center hole/background present),
            //    flood-fill from the image edges through pixels close to that background
            //    color and make ONLY the edge-connected background transparent.
            //
            // This avoids the "white dots" (holes) caused by making a palette index
            // transparent globally, and it keeps 3D/front covers intact.
            byte cornerBgIndex;
            double cornerBgRatio;
            GetDominantCornerIndex(data, out cornerBgIndex, out cornerBgRatio);
            bool hasCornerBackground = cornerBgRatio >= 0.90;
            bool isDisc = hasCornerBackground && IsLikelyDiscByIndices(data, cornerBgIndex);

            var bmp = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, 128, 128);
            var bd = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                int stride = bd.Stride;
                byte[] row = new byte[stride];

                int src = 512; // start of index data
                for (int y = 0; y < 128; y++)
                {
                    int ofs = 0;
                    for (int x = 0; x < 128; x++)
                    {
                        byte idx = data[src++];
                        Color c = pal[idx];

                        // BGRA
                        row[ofs + 0] = c.B;
                        row[ofs + 1] = c.G;
                        row[ofs + 2] = c.R;
                        row[ofs + 3] = c.A;
                        ofs += 4;
                    }

                    IntPtr rowPtr = IntPtr.Add(bd.Scan0, y * stride);
                    System.Runtime.InteropServices.Marshal.Copy(row, 0, rowPtr, stride);
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }

            if (isDisc)
            {
                TryClearEdgeConnectedBackground(bmp, pal[cornerBgIndex]);
            }

            return bmp;
        }

        private static bool IsLikelyDiscByIndices(byte[] covData, byte bgIndex)
        {
            // covData: [512 bytes palette][16384 bytes indices]
            // Heuristic for disc-like images:
            // - Corners are background (already ensured by caller)
            // - CENTER region contains a noticeable amount of background (disc hole)
            // - INNER ring contains much less background (disc art)
            try
            {
                const int W = 128;
                const int idxStart = 512;
                int CountBgInRect(int x0, int y0, int w, int h)
                {
                    int bg = 0;
                    for (int y = y0; y < y0 + h; y++)
                    {
                        int row = idxStart + y * W;
                        for (int x = x0; x < x0 + w; x++)
                        {
                            if (covData[row + x] == bgIndex) bg++;
                        }
                    }
                    return bg;
                }

                // Center hole sample: 16x16 around the center.
                int centerBg = CountBgInRect(56, 56, 16, 16);
                int centerTotal = 16 * 16;
                double centerRatio = (double)centerBg / centerTotal;

                // Inner ring sample (where disc art should dominate): 32x32 around the center.
                int innerBg = CountBgInRect(48, 48, 32, 32);
                int innerTotal = 32 * 32;
                double innerRatio = (double)innerBg / innerTotal;

                // Disc-like if the small center has quite a bit of background,
                // but the larger inner area is not mostly background.
                return centerRatio >= 0.20 && innerRatio <= 0.55;
            }
            catch
            {
                return false;
            }
        }

        private static void TryClearEdgeConnectedBackground(Bitmap bmp, Color bg)
        {
            // Preview-only: flood-fill from edges through pixels close to "bg" and set alpha=0.
            // This removes the square background behind disc covers WITHOUT creating holes
            // inside the disc due to dithering.
            try
            {
                if (bmp.Width != 128 || bmp.Height != 128) return;

                const int W = 128;
                const int H = 128;
                const int tol = 12; // per-channel tolerance

                bool CloseToBg(byte r, byte g, byte b)
                    => Math.Abs(r - bg.R) <= tol && Math.Abs(g - bg.G) <= tol && Math.Abs(b - bg.B) <= tol;

                var rect = new Rectangle(0, 0, W, H);
                var bd = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                try
                {
                    int stride = bd.Stride;
                    byte[] px = new byte[stride * H];
                    System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);

                    var visited = new bool[W * H];
                    var q = new Queue<int>();

                    int Idx(int x, int y) => y * W + x;
                    int Ofs(int x, int y) => y * stride + x * 4;

                    void Enq(int x, int y)
                    {
                        if ((uint)x >= W || (uint)y >= H) return;
                        int id = Idx(x, y);
                        if (visited[id]) return;
                        int o = Ofs(x, y);
                        byte b = px[o + 0], g = px[o + 1], r = px[o + 2];
                        if (!CloseToBg(r, g, b)) return;
                        visited[id] = true;
                        q.Enqueue(id);
                    }

                    // seed edges
                    for (int x = 0; x < W; x++) { Enq(x, 0); Enq(x, H - 1); }
                    for (int y = 0; y < H; y++) { Enq(0, y); Enq(W - 1, y); }

                    if (q.Count == 0) return;

                    while (q.Count > 0)
                    {
                        int id = q.Dequeue();
                        int x = id % W;
                        int y = id / W;
                        int o = Ofs(x, y);
                        // set alpha=0
                        px[o + 3] = 0;

                        Enq(x - 1, y);
                        Enq(x + 1, y);
                        Enq(x, y - 1);
                        Enq(x, y + 1);
                    }

                    System.Runtime.InteropServices.Marshal.Copy(px, 0, bd.Scan0, px.Length);
                }
                finally
                {
                    bmp.UnlockBits(bd);
                }
            }
            catch
            {
                // never break preview
            }
        }

        private static void GetDominantCornerIndex(byte[] covData, out byte dominantIndex, out double ratio)
        {
            // corners sampled in 8x8 blocks (4 corners)
            // cov indices start at offset 512
            int[] counts = new int[256];
            int total = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int tl = 512 + (y * 128 + x);
                    int tr = 512 + (y * 128 + (127 - x));
                    int bl = 512 + ((127 - y) * 128 + x);
                    int br = 512 + ((127 - y) * 128 + (127 - x));

                    counts[covData[tl]]++;
                    counts[covData[tr]]++;
                    counts[covData[bl]]++;
                    counts[covData[br]]++;
                    total += 4;
                }
            }

            int bestIdx = 0;
            int bestCount = counts[0];
            for (int i = 1; i < 256; i++)
            {
                if (counts[i] > bestCount)
                {
                    bestCount = counts[i];
                    bestIdx = i;
                }
            }

            dominantIndex = (byte)bestIdx;
            ratio = total == 0 ? 0.0 : (double)bestCount / total;
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
            _nextOriginalIndex = 0;
            _missingSerialFirstEnabled = false;
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

                // Fallback: some images do not contain a visible SLUS/SLES/etc. string.
                // If no serial is found, compute a fingerprint (PVD CRC32) and try to resolve
                // it via PicoStationCovMaker.serialmap.json placed next to the EXE.
                bool serialFromMap = false;
                string? mapTitle = null;
                string? mapNotes = null;
                string? pvdCrcHex = null;

                if (string.IsNullOrWhiteSpace(serial))
                {
                    pvdCrcHex = TryComputePvdCrc32Hex(gamePath, firstBin);
                    if (!string.IsNullOrWhiteSpace(pvdCrcHex))
                    {
                        if (TryResolveSerialFromSerialMap(pvdCrcHex!, out var s2, out mapTitle, out mapNotes))
                        {
                            serial = s2;
                            serialFromMap = true;
                            if (!string.IsNullOrWhiteSpace(mapNotes))
                                Log($"Serial map hit: {Path.GetFileName(gamePath)} => {serial} (PVD CRC32 {pvdCrcHex}) — {mapNotes}");
                            else
                                Log($"Serial map hit: {Path.GetFileName(gamePath)} => {serial} (PVD CRC32 {pvdCrcHex})");
                        }
                        else
                        {
                            Log($"No serial in BIN and no serialmap match for {Path.GetFileName(gamePath)} (PVD CRC32 {pvdCrcHex}).");
                        }
                    }
                    else
                    {
                        Log($"No serial in BIN and could not compute PVD CRC32 for {Path.GetFileName(gamePath)}.");
                    }
                }

                string status = covExists
                ? "SKIPPED • cover exists"
                : (serial != null
                    ? (serialFromMap
                        ? $"FOUND • serial {serial} • {TruncTitle(mapTitle)} (JSON)"
                        : $"FOUND • serial {serial}")
                    : (string.IsNullOrWhiteSpace(pvdCrcHex)
                        ? "FOUND • serial unknown"
                        : $"FOUND • serial unknown • PVD {pvdCrcHex}"));

                var meta = new FileItemMeta(gamePath, covPath, InputKind.Game)
                {
                    Serial = serial,
                    OriginalIndex = _nextOriginalIndex++
                };

                var item = new ListViewItem(new[] { gamePath, covPath, status })
                {
                    Tag = meta
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

        private static string TruncTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "";
            title = title.Trim();
            return title.Length <= 40 ? title : title.Substring(0, 37) + "...";
        }

        private static bool TryResolveSerialFromSerialMap(string pvdCrc32Hex, out string? serialDash, out string? title, out string? notes)
        {
            serialDash = null;
            title = null;
            notes = null;

            try
            {
                string mapPath = Path.Combine(AppContext.BaseDirectory, "PicoStationCovMaker.serialmap.json");
                if (!File.Exists(mapPath))
                    return false;

                using var fs = File.OpenRead(mapPath);
                using var doc = JsonDocument.Parse(fs);

                if (!doc.RootElement.TryGetProperty("pvd_crc32", out var pvdObj) || pvdObj.ValueKind != JsonValueKind.Object)
                    return false;

                if (!pvdObj.TryGetProperty(pvdCrc32Hex.ToUpperInvariant(), out var entry) || entry.ValueKind != JsonValueKind.Object)
                    return false;

                if (entry.TryGetProperty("serial", out var sEl) && sEl.ValueKind == JsonValueKind.String)
                    serialDash = sEl.GetString();

                if (entry.TryGetProperty("title", out var tEl) && tEl.ValueKind == JsonValueKind.String)
                    title = tEl.GetString();

                if (entry.TryGetProperty("notes", out var nEl) && nEl.ValueKind == JsonValueKind.String)
                    notes = nEl.GetString();

                return !string.IsNullOrWhiteSpace(serialDash);
            }
            catch
            {
                return false;
            }
        }

        private static string? TryComputePvdCrc32Hex(string gamePath, string? firstBin)
        {
            try
            {
                // We support BIN/CUE only.
                // PVD = ISO9660 Primary Volume Descriptor, typically at logical block 16.
                // PS1 BIN is usually raw 2352/sector; user-data within sector depends on MODE1/2352 vs MODE2/2352.

                if (gamePath.EndsWith(".cue", StringComparison.OrdinalIgnoreCase) && File.Exists(gamePath))
                {
                    if (!TryParseCueForDataTrack(gamePath, out var cueBinPath, out var mode, out var index01Lba))
                        return null;

                    if (string.IsNullOrWhiteSpace(cueBinPath) || !File.Exists(cueBinPath))
                        return null;

                    int userDataOffset = mode.Equals("MODE1/2352", StringComparison.OrdinalIgnoreCase) ? 16 : 24; // MODE2/2352 default
                    long sector2352 = 2352;
                    long pvdLba = (long)index01Lba + 16;
                    long fileOfs = pvdLba * sector2352 + userDataOffset;

                    byte[] buf = new byte[2048];
                    using var fs = File.OpenRead(cueBinPath);
                    if (fs.Length < fileOfs + buf.Length) return null;
                    fs.Position = fileOfs;
                    int read = fs.Read(buf, 0, buf.Length);
                    if (read != buf.Length) return null;
                    uint crc = Crc32.Compute(buf);
                    return crc.ToString("X8");
                }

                // BIN-only entry: best-effort.
                if (string.IsNullOrWhiteSpace(firstBin) || !File.Exists(firstBin))
                    return null;

                // Try MODE2/2352 first (most common PS1), then MODE1/2352.
                foreach (int userDataOffset in new[] { 24, 16 })
                {
                    long pvdLba = 16;
                    long fileOfs = pvdLba * 2352L + userDataOffset;
                    byte[] buf = new byte[2048];
                    using var fs = File.OpenRead(firstBin);
                    if (fs.Length < fileOfs + buf.Length) continue;
                    fs.Position = fileOfs;
                    int read = fs.Read(buf, 0, buf.Length);
                    if (read != buf.Length) continue;
                    uint crc = Crc32.Compute(buf);
                    return crc.ToString("X8");
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryParseCueForDataTrack(string cuePath, out string? binPath, out string mode, out int index01Lba)
        {
            binPath = null;
            mode = "MODE2/2352";
            index01Lba = 0;

            try
            {
                string cueDir = Path.GetDirectoryName(cuePath)!;
                string? currentFile = null;
                bool inFirstTrack = false;

                foreach (var raw in File.ReadLines(cuePath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;

                    if (line.StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
                    {
                        currentFile = ExtractCueFilePath(line);
                        if (!string.IsNullOrWhiteSpace(currentFile))
                        {
                            string candidate = Path.GetFullPath(Path.Combine(cueDir, currentFile));
                            binPath = candidate;
                        }
                        continue;
                    }

                    if (line.StartsWith("TRACK", StringComparison.OrdinalIgnoreCase))
                    {
                        // Example: TRACK 01 MODE2/2352
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            inFirstTrack = parts[1] == "01";
                            if (inFirstTrack)
                            {
                                mode = parts[2].ToUpperInvariant();
                                if (mode != "MODE1/2352" && mode != "MODE2/2352")
                                    mode = "MODE2/2352";
                            }
                        }
                        continue;
                    }

                    if (inFirstTrack && line.StartsWith("INDEX 01", StringComparison.OrdinalIgnoreCase))
                    {
                        // INDEX 01 mm:ss:ff (75 frames/sec)
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            index01Lba = CueTimeToLba(parts[2]);
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static string? ExtractCueFilePath(string fileLine)
        {
            try
            {
                int q1 = fileLine.IndexOf('"');
                int q2 = q1 >= 0 ? fileLine.IndexOf('"', q1 + 1) : -1;
                if (q1 >= 0 && q2 > q1)
                    return fileLine.Substring(q1 + 1, q2 - q1 - 1);

                var parts = fileLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) return parts[1];
            }
            catch { }
            return null;
        }

        private static int CueTimeToLba(string mmssff)
        {
            // mm:ss:ff with 75 frames/sec
            try
            {
                var p = mmssff.Split(':');
                if (p.Length != 3) return 0;
                int mm = int.Parse(p[0]);
                int ss = int.Parse(p[1]);
                int ff = int.Parse(p[2]);
                return (mm * 60 + ss) * 75 + ff;
            }
            catch
            {
                return 0;
            }
        }

        private static class Crc32
        {
            private static readonly uint[] Table = BuildTable();

            private static uint[] BuildTable()
            {
                const uint poly = 0xEDB88320u;
                var t = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint c = i;
                    for (int k = 0; k < 8; k++)
                        c = (c & 1) != 0 ? (poly ^ (c >> 1)) : (c >> 1);
                    t[i] = c;
                }
                return t;
            }

            public static uint Compute(byte[] data)
            {
                uint c = 0xFFFFFFFFu;
                for (int i = 0; i < data.Length; i++)
                    c = Table[(c ^ data[i]) & 0xFF] ^ (c >> 8);
                return c ^ 0xFFFFFFFFu;
            }
        }

        private static string? ExtractPs1SerialFromBin(string binPath)
        {
            const int MaxBytesToScan = 64 * 1024 * 1024;
            const int ChunkSize = 1024 * 1024;
            const int Overlap = 64;

	            // Support both classic disc IDs like "SLUS_012.06" and packed/product codes like "SLUSP012.06".
	            // Normalize to UI format: "SLUS-01206".
	            var rx = new Regex(@"(?<![A-Z0-9])(SCUS|SLUS|SLES|SCES|SCPS|SLPS|SCPM|SIPS|SLED|SLPM|SCED)(?:[_P-]?)([0-9]{3})\.([0-9]{2})(?![0-9])",
                RegexOptions.Compiled);

            // Support alternate split IDs like "SLUS_00.220" (2+3 digits).
            // Normalize to UI format: "SLUS-00220".
            var rxAltSplit = new Regex(@"(?<![A-Z0-9])(SCUS|SLUS|SLES|SCES|SCPS|SLPS|SCPM|SIPS|SLED|SLPM|SCED)(?:[_P-]?)([0-9]{2})\.([0-9]{3})(?![0-9])",
                RegexOptions.Compiled);


	            // Support LSP serials like "LSP20033.001".
	            // Normalize to UI format: "LSP-200330" (use 5 digits before the dot + first digit after the dot).
	            var rxLsp = new Regex(@"(?<![A-Z0-9])LSP([0-9]{5})\.([0-9]{3})(?![0-9])", RegexOptions.Compiled);

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

	            var mLsp = rxLsp.Match(s);
	            if (mLsp.Success)
	            {
	                string digits = mLsp.Groups[1].Value + mLsp.Groups[2].Value.Substring(0, 1);
	                return $"LSP-{digits}";
	            }

                

                var mAlt = rxAltSplit.Match(s);
                if (mAlt.Success)
                {
                    string prefix = mAlt.Groups[1].Value;
                    string digits = mAlt.Groups[2].Value + mAlt.Groups[3].Value;
                    return $"{prefix}-{digits}";
                }

                var m = rx.Match(s);
                if (m.Success)
	                {
	                    string prefix = m.Groups[1].Value;
	                    string digits = m.Groups[2].Value + m.Groups[3].Value;
	                    return $"{prefix}-{digits}";
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

        private static byte[] EnsureSerialFooter(byte[] covData, string serialDash)
        {
            // Ensure output is 16912 bytes: 16896 base + 16-byte ASCII serial footer.
            // Footer contains serial in the same dash format shown in the UI (e.g. SLUS-01206), padded with 0x00.
            if (covData == null) throw new ArgumentNullException(nameof(covData));
            if (string.IsNullOrWhiteSpace(serialDash)) return covData;

            string s = serialDash.Trim().ToUpperInvariant();
            byte[] serialBytes = Encoding.ASCII.GetBytes(s);
            int copyLen = Math.Min(serialBytes.Length, 16);

            byte[] outBytes;
            if (covData.Length == 16912)
            {
                outBytes = covData;
            }
            else
            {
                outBytes = new byte[16912];
                int baseLen = Math.Min(covData.Length, 16896);
                Buffer.BlockCopy(covData, 0, outBytes, 0, baseLen);
            }

            // Write footer (last 16 bytes)
            int footerOff = 16912 - 16;
            Array.Clear(outBytes, footerOff, 16);
            Buffer.BlockCopy(serialBytes, 0, outBytes, footerOff, copyLen);

            return outBytes;
        }


        private static byte[] EnsureFooter16912(byte[] covData, string? serialDash)
        {
            // PicoStation expects a 16-byte footer area for the serial.
            // If serialDash is empty, we still pad with 0x00 so the loader does not read image bytes as serial.
            if (covData == null) throw new ArgumentNullException(nameof(covData));

            byte[] outBytes;
            if (covData.Length == 16912)
            {
                outBytes = covData;
            }
            else
            {
                outBytes = new byte[16912];
                int baseLen = Math.Min(covData.Length, 16896);
                Buffer.BlockCopy(covData, 0, outBytes, 0, baseLen);
            }

            int footerOff = 16912 - 16;
            Array.Clear(outBytes, footerOff, 16);

            var sNorm = NormalizeSerial(serialDash);
            if (!string.IsNullOrWhiteSpace(sNorm))
            {
                byte[] serialBytes = Encoding.ASCII.GetBytes(sNorm!);
                int copyLen = Math.Min(serialBytes.Length, 16);
                Buffer.BlockCopy(serialBytes, 0, outBytes, footerOff, copyLen);
            }

            return outBytes;
        }

        private enum CoverDownloadType
        {
            Cover,
            Cover3D,
            Disc

        }

        private CoverDownloadType GetSelectedCoverDownloadType()
        {
            // Designer sets rbDlCover.Checked = true by default
            if (rbDl3d != null && rbDl3d.Checked) return CoverDownloadType.Cover3D;
            if (rbDlCd != null && rbDlCd.Checked) return CoverDownloadType.Disc;
            return CoverDownloadType.Cover;
        }

        private string GetCoverDownloadBaseUrl(CoverDownloadType t)
        {
            // User can change the base URL via the dropdown.
            // Support either:
            //  - repo root:  https://.../main
            //  - already pointing at covers: https://.../main/covers/default/
            string root = GetCoverBaseUrl();
            int coversIdx = root.IndexOf("/covers/", StringComparison.OrdinalIgnoreCase);
            if (coversIdx >= 0)
            {
                root = root.Substring(0, coversIdx);
            }

            string sub = t switch
            {
                CoverDownloadType.Cover => "default",
                CoverDownloadType.Cover3D => "3d",
                CoverDownloadType.Disc => "cd",
                _ => "default"
            };

            return $"{root.TrimEnd('/')}/covers/{sub}/";
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

            var dlType = GetSelectedCoverDownloadType();
            string baseUrl = GetCoverDownloadBaseUrl(dlType);

            bool overwriteExisting = chkDlOverwrite != null && chkDlOverwrite.Checked;

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

                if (File.Exists(covPath) && !overwriteExisting)
                {
                    item.SubItems[2].Text = "SKIPPED • .cov exists";
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

                item.SubItems[2].Text = $"Downloading {dlType} • {serial}...";
                await Task.Yield();

                try
                {
                    // Try .cov first (PicoStation-ready). If not found, try .png/.jpg and convert locally.
                    string? foundExt = null;
                    string? foundUrl = null;
                    byte[]? downloaded = null;

                    foreach (var ext in new[] { ".cov", ".png", ".jpg" })
                    {
                        string candidateUrl = $"{baseUrl}{serial}{ext}";
                        using var resp = await http.GetAsync(candidateUrl);

                        if (resp.StatusCode == HttpStatusCode.NotFound)
                            continue;

                        resp.EnsureSuccessStatusCode();

                        var bytes = await resp.Content.ReadAsByteArrayAsync();
                        if (bytes == null || bytes.Length == 0)
                            throw new IOException($"Downloaded file was empty. URL: {candidateUrl}");

                        foundExt = ext;
                        foundUrl = candidateUrl;
                        downloaded = bytes;
                        break;
                    }

                    if (foundExt == null || downloaded == null || foundUrl == null)
                    {
                        item.SubItems[2].Text = $"NOT FOUND • {dlType} • {serial}";
                        notFound++;
                        continue;
                    }

                    if (foundExt.Equals(".cov", StringComparison.OrdinalIgnoreCase))
                    {
                        // Expected sizes:
                        //  - 8bpp: 512 palette + 16384 indices = 16896 bytes (or 16912 with 16-byte serial)
                        if (downloaded.Length != 16896 && downloaded.Length != 16912)
                            throw new IOException($"Unexpected .cov size {downloaded.Length} bytes. Expected 16896 or 16912. URL: {foundUrl}");

                        var data = EnsureSerialFooter(downloaded, serial);
                        File.WriteAllBytes(covPath, data);
                    }
                    else
                    {
                        // Downloaded an image; convert using the same pipeline as manual conversion.
                        string tempDir = Path.Combine(Path.GetTempPath(), "PicoStationCovMaker");
                        Directory.CreateDirectory(tempDir);
                        string tmpImg = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + foundExt);

                        try
                        {
                            File.WriteAllBytes(tmpImg, downloaded);

                            Directory.CreateDirectory(Path.GetDirectoryName(covPath)!);
                            ConvertImageToCov8bpp(tmpImg, covPath);

                            // Always ensure the 16-byte serial footer area exists (16912 total).
                            var covBytes = File.ReadAllBytes(covPath);
                            covBytes = EnsureFooter16912(covBytes, serial);
                            File.WriteAllBytes(covPath, covBytes);
                        }
                        finally
                        {
                            try { if (File.Exists(tmpImg)) File.Delete(tmpImg); } catch { }
                        }
                    }

                    item.SubItems[1].Text = covPath;
                    item.SubItems[2].Text = $"OK • {dlType} • {serial}";
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

        // ----------------------------------------------------------



        private void cmbCoverBaseUrl_KeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode != Keys.Enter)
                    return;

                // Commit on Enter only (prevents caret jumps / "typing backwards").
                e.SuppressKeyPress = true;
                CommitCoverBaseUrl();

                // Optionally move focus away so the user sees it "committed".
                // (Not required; harmless if it fails.)
                try { btnDownloadCovers?.Focus(); } catch { }
            }
            catch
            {
                // ignore
            }
        }

        private void cmbCoverBaseUrl_SelectionChangeCommitted(object? sender, EventArgs e)
        {
            try
            {
                if (_coverUrlInternalUpdate)
                    return;

                // IMPORTANT: use SelectedItem (Text can still be the previous value at this moment)
                // which caused the dropdown to "stick" on the first URL.
                string? selected = cmbCoverBaseUrl?.SelectedItem?.ToString();
                CommitCoverBaseUrl(selected);
            }
            catch
            {
                // ignore
            }
        }

        // Shared commit helper used by Enter + selection.
        private void CommitCoverBaseUrl(string? overrideUrl = null)
        {
            if (_coverUrlInternalUpdate)
                return;

            // Keep config in sync and persist only when the user confirms.
            if (_config != null)
            {
                string cur = (overrideUrl ?? (cmbCoverBaseUrl?.Text ?? "")).Trim();
                if (string.IsNullOrWhiteSpace(cur)) cur = DefaultCoverBaseUrl;

                _config.CoverBaseUrl = cur;
                _config.CoverBaseUrls ??= new List<string>();
                if (!_config.CoverBaseUrls.Any(u => string.Equals(u?.Trim(), cur, StringComparison.OrdinalIgnoreCase)))
                    _config.CoverBaseUrls.Insert(0, cur);

                SaveConfig();
                RefreshCoverUrlDropdownFromConfig(cur);
            }

            SaveCoverSourceSettings();
        }

        private void btnCoverUrlReset_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cmbCoverBaseUrl != null)
                    cmbCoverBaseUrl.Text = DefaultCoverBaseUrl;

                if (_config != null)
                {
                    _config.CoverBaseUrl = DefaultCoverBaseUrl;
                    _config.CoverBaseUrls ??= new List<string>();
                    if (!_config.CoverBaseUrls.Any(u => string.Equals(u?.Trim(), DefaultCoverBaseUrl, StringComparison.OrdinalIgnoreCase)))
                        _config.CoverBaseUrls.Insert(0, DefaultCoverBaseUrl);
                    SaveConfig();

                    RefreshCoverUrlDropdownFromConfig(DefaultCoverBaseUrl);
                }

                SaveCoverSourceSettings();
            }
            catch
            {
                // ignore
            }
        }

        private void cmbCoverBaseUrl_DropDown(object? sender, EventArgs e)
        {
            // Refresh from disk when the user opens the dropdown.
            // This makes external edits to PicoStationCovMaker.config.json visible immediately.
            try
            {
                if (_coverUrlInternalUpdate)
                    return;

                LoadConfig();

                string selected = (cmbCoverBaseUrl?.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(selected))
                    selected = string.IsNullOrWhiteSpace(_config.CoverBaseUrl) ? DefaultCoverBaseUrl : _config.CoverBaseUrl!.Trim();

                _config.CoverBaseUrls ??= new List<string>();
                if (!_config.CoverBaseUrls.Any(u => string.Equals(u?.Trim(), selected, StringComparison.OrdinalIgnoreCase)))
                    _config.CoverBaseUrls.Insert(0, selected);

                RefreshCoverUrlDropdownFromConfig(selected);
            }
            catch
            {
                // ignore
            }
        }

        private void RefreshCoverUrlDropdownFromConfig(string selected)
        {
            if (cmbCoverBaseUrl == null) return;

            var list = (_config.CoverBaseUrls ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count == 0)
                list.Add(DefaultCoverBaseUrl);

            if (!list.Any(u => string.Equals(u, selected, StringComparison.OrdinalIgnoreCase)))
                list.Insert(0, selected);

            PopulateCoverUrlDropdown(list, selected);
        }

        private void PopulateCoverUrlDropdown(List<string> urls, string selected)
        {
            if (cmbCoverBaseUrl == null) return;

            _coverUrlInternalUpdate = true;
            cmbCoverBaseUrl.BeginUpdate();
            try
            {
                cmbCoverBaseUrl.Items.Clear();
                foreach (var u in urls.Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    var t = u.Trim();
                    if (cmbCoverBaseUrl.Items.Cast<object>().Any(x => string.Equals(x?.ToString(), t, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    cmbCoverBaseUrl.Items.Add(t);
                }

                cmbCoverBaseUrl.Text = string.IsNullOrWhiteSpace(selected) ? DefaultCoverBaseUrl : selected;
            }
            finally
            {
                cmbCoverBaseUrl.EndUpdate();
                _coverUrlInternalUpdate = false;
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
