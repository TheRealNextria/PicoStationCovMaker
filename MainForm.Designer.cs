#nullable disable

namespace PicoStationCovMaker

{

    partial class MainForm

    {

        private System.ComponentModel.IContainer components = null;



        protected override void Dispose(bool disposing)

        {

            if (disposing && (components != null))

            {

                components.Dispose();

            }

            base.Dispose(disposing);

        }



        #region Windows Form Designer generated code



        private void InitializeComponent()

        {
            dropPanel = new Panel();
            dropLabel = new Label();
            lvFiles = new ListView();
            colInput = new ColumnHeader();
            colStatus = new ColumnHeader();
            bottomPanel = new Panel();
            progressBar = new ProgressBar();
            txtLog = new TextBox();
            topPanel = new Panel();
            btnConvert = new Button();
            btnClear = new Button();
            btnOutputFolder = new Button();
            btnOpenSd = new Button();
            btnScanSd = new Button();
            btnDownloadCovers = new Button();
            btnCalcPvd = new Button();
            grpDownloadType = new GroupBox();
            label2 = new Label();
            lblFolderCoverType = new Label();
            cmbFolderCoverType = new ComboBox();
            lblGameCoverType = new Label();
            cmbGameCoverType = new ComboBox();
            chkDlOverwrite = new CheckBox();
            lblCoverBaseUrl = new Label();
            cmbCoverBaseUrl = new ComboBox();
            btnCoverUrlReset = new Button();
            lblOutputFolder = new Label();
            splitContainer = new SplitContainer();
            tabMain = new TabControl();
            tabConvert = new TabPage();
            splitRight = new SplitContainer();
            previewPanel = new Panel();
            previewGrid = new TableLayoutPanel();
            picPreviewFolder = new PictureBox();
            picPreview = new PictureBox();
            previewHeader = new Panel();
            lblPreview = new Label();
            btnExportPalette = new Button();
            tabMenuColors = new TabPage();
            menuColorsHost = new Panel();
            pnlMenuPreview = new Panel();
            menuControls = new Panel();
            cmbMenuTarget = new ComboBox();
            trkR = new TrackBar();
            trkG = new TrackBar();
            trkB = new TrackBar();
            btnMenuPick = new Button();
            btnMenuSave = new Button();
            btnMenuReload = new Button();
            btnWallpaper = new Button();
            lblMenuHex = new Label();
            dropPanel.SuspendLayout();
            bottomPanel.SuspendLayout();
            topPanel.SuspendLayout();
            grpDownloadType.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            tabMain.SuspendLayout();
            tabConvert.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitRight).BeginInit();
            splitRight.Panel1.SuspendLayout();
            splitRight.Panel2.SuspendLayout();
            splitRight.SuspendLayout();
            previewPanel.SuspendLayout();
            previewGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPreviewFolder).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picPreview).BeginInit();
            previewHeader.SuspendLayout();
            tabMenuColors.SuspendLayout();
            menuColorsHost.SuspendLayout();
            menuControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkR).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkG).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkB).BeginInit();
            SuspendLayout();
            // 
            // dropPanel
            // 
            dropPanel.BorderStyle = BorderStyle.FixedSingle;
            dropPanel.Controls.Add(dropLabel);
            dropPanel.Dock = DockStyle.Fill;
            dropPanel.Location = new Point(0, 0);
            dropPanel.Name = "dropPanel";
            dropPanel.Padding = new Padding(12);
            dropPanel.Size = new Size(465, 648);
            dropPanel.TabIndex = 0;
            // 
            // dropLabel
            // 
            dropLabel.Dock = DockStyle.Fill;
            dropLabel.Font = new Font("Segoe UI", 12F);
            dropLabel.Location = new Point(12, 12);
            dropLabel.Name = "dropLabel";
            dropLabel.Size = new Size(439, 622);
            dropLabel.TabIndex = 0;
            dropLabel.Text = "Drop images / folders / .cov files here\r\n\r\nOutput: 128x128 8bpp indexed + palette (.cov)\r\n(16,896 bytes; 16,912 bytes with serial)\r\n\r\nTip: drop an existing .cov to preview it.";
            dropLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lvFiles
            // 
            lvFiles.Columns.AddRange(new ColumnHeader[] { colInput, colStatus });
            lvFiles.Dock = DockStyle.Fill;
            lvFiles.FullRowSelect = true;
            lvFiles.GridLines = true;
            lvFiles.HideSelection = true;
            lvFiles.Location = new Point(0, 0);
            lvFiles.Name = "lvFiles";
            lvFiles.Size = new Size(1201, 310);
            lvFiles.TabIndex = 1;
            lvFiles.UseCompatibleStateImageBehavior = false;
            lvFiles.View = View.Details;
            // 
            // colInput
            // 
            colInput.Text = "Input";
            colInput.Width = 600;
            // 
            // colStatus
            // 
            colStatus.Text = "Status";
            colStatus.Width = 300;
            // 
            // bottomPanel
            // 
            bottomPanel.Controls.Add(progressBar);
            bottomPanel.Controls.Add(txtLog);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Location = new Point(0, 764);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(12, 8, 12, 12);
            bottomPanel.Size = new Size(1684, 229);
            bottomPanel.TabIndex = 2;
            // 
            // progressBar
            // 
            progressBar.Dock = DockStyle.Top;
            progressBar.Location = new Point(12, 8);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(1660, 18);
            progressBar.TabIndex = 0;
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Location = new Point(12, 8);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1660, 209);
            txtLog.TabIndex = 1;
            // 
            // topPanel
            // 
            topPanel.Controls.Add(btnConvert);
            topPanel.Controls.Add(btnClear);
            topPanel.Controls.Add(btnOutputFolder);
            topPanel.Controls.Add(btnOpenSd);
            topPanel.Controls.Add(btnScanSd);
            topPanel.Controls.Add(btnDownloadCovers);
            topPanel.Controls.Add(btnCalcPvd);
            topPanel.Controls.Add(grpDownloadType);
            topPanel.Controls.Add(lblCoverBaseUrl);
            topPanel.Controls.Add(cmbCoverBaseUrl);
            topPanel.Controls.Add(btnCoverUrlReset);
            topPanel.Controls.Add(lblOutputFolder);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Padding = new Padding(12);
            topPanel.Size = new Size(1684, 116);
            topPanel.TabIndex = 3;
            // 
            // btnConvert
            // 
            btnConvert.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConvert.Location = new Point(1597, 10);
            btnConvert.Name = "btnConvert";
            btnConvert.Size = new Size(75, 25);
            btnConvert.TabIndex = 3;
            btnConvert.Text = "Convert";
            btnConvert.UseVisualStyleBackColor = true;
            btnConvert.Click += btnConvert_Click;
            // 
            // btnClear
            // 
            btnClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClear.Location = new Point(1516, 10);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(75, 25);
            btnClear.TabIndex = 2;
            btnClear.Text = "Clear";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // btnOutputFolder
            // 
            btnOutputFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOutputFolder.Location = new Point(1598, 41);
            btnOutputFolder.Name = "btnOutputFolder";
            btnOutputFolder.Size = new Size(75, 25);
            btnOutputFolder.TabIndex = 1;
            btnOutputFolder.Text = "Output...";
            btnOutputFolder.UseVisualStyleBackColor = true;
            btnOutputFolder.Click += btnOutputFolder_Click;
            // 
            // btnOpenSd
            // 
            btnOpenSd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenSd.Location = new Point(1420, 10);
            btnOpenSd.Name = "btnOpenSd";
            btnOpenSd.Size = new Size(90, 25);
            btnOpenSd.TabIndex = 1000;
            btnOpenSd.Text = "Open SD";
            btnOpenSd.UseVisualStyleBackColor = true;
            btnOpenSd.Click += btnOpenSd_Click;
            // 
            // btnScanSd
            // 
            btnScanSd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnScanSd.Location = new Point(1339, 10);
            btnScanSd.Name = "btnScanSd";
            btnScanSd.Size = new Size(75, 25);
            btnScanSd.TabIndex = 4;
            btnScanSd.Text = "Scan SD...";
            btnScanSd.UseVisualStyleBackColor = true;
            btnScanSd.Click += btnScanSd_Click;
            // 
            // btnDownloadCovers
            // 
            btnDownloadCovers.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDownloadCovers.Location = new Point(1221, 10);
            btnDownloadCovers.Name = "btnDownloadCovers";
            btnDownloadCovers.Size = new Size(112, 25);
            btnDownloadCovers.TabIndex = 5;
            btnDownloadCovers.Text = "Download Covers";
            btnDownloadCovers.UseVisualStyleBackColor = true;
            btnDownloadCovers.Click += btnDownloadCovers_Click;
            // 
            // btnCalcPvd
            // 
            btnCalcPvd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCalcPvd.Location = new Point(1221, 46);
            btnCalcPvd.Name = "btnCalcPvd";
            btnCalcPvd.Size = new Size(160, 25);
            btnCalcPvd.TabIndex = 999;
            btnCalcPvd.Text = "Calculate PVD CRC32";
            btnCalcPvd.UseVisualStyleBackColor = true;
            btnCalcPvd.Click += btnCalcPvd_Click;
            // 
            // grpDownloadType
            // 
            grpDownloadType.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            grpDownloadType.Controls.Add(label2);
            grpDownloadType.Controls.Add(lblFolderCoverType);
            grpDownloadType.Controls.Add(cmbFolderCoverType);
            grpDownloadType.Controls.Add(lblGameCoverType);
            grpDownloadType.Controls.Add(cmbGameCoverType);
            grpDownloadType.Controls.Add(chkDlOverwrite);
            grpDownloadType.Location = new Point(787, 1);
            grpDownloadType.Name = "grpDownloadType";
            grpDownloadType.Size = new Size(428, 79);
            grpDownloadType.TabIndex = 10;
            grpDownloadType.TabStop = false;
            grpDownloadType.Enter += grpDownloadType_Enter_1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(18, 16);
            label2.Name = "label2";
            label2.Size = new Size(91, 15);
            label2.TabIndex = 8;
            label2.Text = "Download Type:";
            label2.Click += label1_Click;
            // 
            // lblFolderCoverType
            // 
            lblFolderCoverType.AutoSize = true;
            lblFolderCoverType.Location = new Point(155, 19);
            lblFolderCoverType.Name = "lblFolderCoverType";
            lblFolderCoverType.Size = new Size(43, 15);
            lblFolderCoverType.TabIndex = 4;
            lblFolderCoverType.Text = "Folder:";
            // 
            // cmbFolderCoverType
            // 
            cmbFolderCoverType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFolderCoverType.FormattingEnabled = true;
            cmbFolderCoverType.Items.AddRange(new object[] { "Default", "3D", "CD" });
            cmbFolderCoverType.Location = new Point(204, 16);
            cmbFolderCoverType.Name = "cmbFolderCoverType";
            cmbFolderCoverType.Size = new Size(120, 23);
            cmbFolderCoverType.TabIndex = 5;
            // 
            // lblGameCoverType
            // 
            lblGameCoverType.AutoSize = true;
            lblGameCoverType.Location = new Point(157, 50);
            lblGameCoverType.Name = "lblGameCoverType";
            lblGameCoverType.Size = new Size(41, 15);
            lblGameCoverType.TabIndex = 6;
            lblGameCoverType.Text = "Game:";
            // 
            // cmbGameCoverType
            // 
            cmbGameCoverType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGameCoverType.FormattingEnabled = true;
            cmbGameCoverType.Items.AddRange(new object[] { "Default", "3D", "CD" });
            cmbGameCoverType.Location = new Point(204, 47);
            cmbGameCoverType.Name = "cmbGameCoverType";
            cmbGameCoverType.Size = new Size(120, 23);
            cmbGameCoverType.TabIndex = 7;
            // 
            // chkDlOverwrite
            // 
            chkDlOverwrite.AutoSize = true;
            chkDlOverwrite.Location = new Point(345, 18);
            chkDlOverwrite.Name = "chkDlOverwrite";
            chkDlOverwrite.Size = new Size(77, 19);
            chkDlOverwrite.TabIndex = 3;
            chkDlOverwrite.Text = "Overwrite";
            chkDlOverwrite.UseVisualStyleBackColor = true;
            // 
            // lblCoverBaseUrl
            // 
            lblCoverBaseUrl.AutoSize = true;
            lblCoverBaseUrl.Location = new Point(12, 89);
            lblCoverBaseUrl.Name = "lblCoverBaseUrl";
            lblCoverBaseUrl.Size = new Size(103, 15);
            lblCoverBaseUrl.TabIndex = 6;
            lblCoverBaseUrl.Text = "Cover source URL:";
            // 
            // cmbCoverBaseUrl
            // 
            cmbCoverBaseUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbCoverBaseUrl.FormattingEnabled = true;
            cmbCoverBaseUrl.Location = new Point(121, 86);
            cmbCoverBaseUrl.Name = "cmbCoverBaseUrl";
            cmbCoverBaseUrl.Size = new Size(1389, 23);
            cmbCoverBaseUrl.TabIndex = 7;
            // 
            // btnCoverUrlReset
            // 
            btnCoverUrlReset.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCoverUrlReset.Location = new Point(1516, 86);
            btnCoverUrlReset.Name = "btnCoverUrlReset";
            btnCoverUrlReset.Size = new Size(75, 23);
            btnCoverUrlReset.TabIndex = 8;
            btnCoverUrlReset.Text = "Reset";
            btnCoverUrlReset.UseVisualStyleBackColor = true;
            btnCoverUrlReset.Click += btnCoverUrlReset_Click;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoEllipsis = true;
            lblOutputFolder.Dock = DockStyle.Left;
            lblOutputFolder.Location = new Point(12, 12);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(55, 92);
            lblOutputFolder.TabIndex = 0;
            lblOutputFolder.TextAlign = ContentAlignment.MiddleLeft;
            lblOutputFolder.Visible = false;
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 116);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(dropPanel);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(tabMain);
            splitContainer.Size = new Size(1684, 648);
            splitContainer.SplitterDistance = 465;
            splitContainer.TabIndex = 4;
            // 
            // tabMain
            // 
            tabMain.Controls.Add(tabConvert);
            tabMain.Controls.Add(tabMenuColors);
            tabMain.Dock = DockStyle.Fill;
            tabMain.Location = new Point(0, 0);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(1215, 648);
            tabMain.TabIndex = 5;
            // 
            // tabConvert
            // 
            tabConvert.Controls.Add(splitRight);
            tabConvert.Location = new Point(4, 24);
            tabConvert.Name = "tabConvert";
            tabConvert.Padding = new Padding(3);
            tabConvert.Size = new Size(1207, 620);
            tabConvert.TabIndex = 0;
            tabConvert.Text = "Convert / Preview";
            tabConvert.UseVisualStyleBackColor = true;
            // 
            // splitRight
            // 
            splitRight.Dock = DockStyle.Fill;
            splitRight.Location = new Point(3, 3);
            splitRight.Name = "splitRight";
            splitRight.Orientation = Orientation.Horizontal;
            // 
            // splitRight.Panel1
            // 
            splitRight.Panel1.Controls.Add(lvFiles);
            // 
            // splitRight.Panel2
            // 
            splitRight.Panel2.Controls.Add(previewPanel);
            splitRight.Size = new Size(1201, 614);
            splitRight.SplitterDistance = 310;
            splitRight.TabIndex = 5;
            // 
            // previewPanel
            // 
            previewPanel.BorderStyle = BorderStyle.FixedSingle;
            previewPanel.Controls.Add(previewGrid);
            previewPanel.Controls.Add(previewHeader);
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.Location = new Point(0, 0);
            previewPanel.Name = "previewPanel";
            previewPanel.Padding = new Padding(10);
            previewPanel.Size = new Size(1201, 300);
            previewPanel.TabIndex = 0;
            // 
            // previewGrid
            // 
            previewGrid.ColumnCount = 2;
            previewGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            previewGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            previewGrid.Controls.Add(picPreviewFolder, 0, 0);
            previewGrid.Controls.Add(picPreview, 1, 0);
            previewGrid.Dock = DockStyle.Fill;
            previewGrid.Location = new Point(10, 34);
            previewGrid.Name = "previewGrid";
            previewGrid.RowCount = 1;
            previewGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            previewGrid.Size = new Size(1179, 254);
            previewGrid.TabIndex = 1;
            // 
            // picPreviewFolder
            // 
            picPreviewFolder.Dock = DockStyle.Fill;
            picPreviewFolder.Location = new Point(0, 0);
            picPreviewFolder.Margin = new Padding(0, 0, 5, 0);
            picPreviewFolder.Name = "picPreviewFolder";
            picPreviewFolder.Size = new Size(584, 254);
            picPreviewFolder.SizeMode = PictureBoxSizeMode.Zoom;
            picPreviewFolder.TabIndex = 0;
            picPreviewFolder.TabStop = false;
            // 
            // picPreview
            // 
            picPreview.Dock = DockStyle.Fill;
            picPreview.Location = new Point(594, 0);
            picPreview.Margin = new Padding(5, 0, 0, 0);
            picPreview.Name = "picPreview";
            picPreview.Size = new Size(585, 254);
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.TabIndex = 1;
            picPreview.TabStop = false;
            // 
            // previewHeader
            // 
            previewHeader.Controls.Add(lblPreview);
            previewHeader.Controls.Add(btnExportPalette);
            previewHeader.Dock = DockStyle.Top;
            previewHeader.Location = new Point(10, 10);
            previewHeader.Name = "previewHeader";
            previewHeader.Size = new Size(1179, 24);
            previewHeader.TabIndex = 2;
            // 
            // lblPreview
            // 
            lblPreview.AutoEllipsis = true;
            lblPreview.Dock = DockStyle.Fill;
            lblPreview.Location = new Point(0, 0);
            lblPreview.Name = "lblPreview";
            lblPreview.Size = new Size(1049, 24);
            lblPreview.TabIndex = 0;
            lblPreview.Text = "Preview: (none)";
            lblPreview.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnExportPalette
            // 
            btnExportPalette.Dock = DockStyle.Right;
            btnExportPalette.Location = new Point(1049, 0);
            btnExportPalette.Name = "btnExportPalette";
            btnExportPalette.Size = new Size(130, 24);
            btnExportPalette.TabIndex = 1;
            btnExportPalette.Text = "Export palette (.pal)";
            btnExportPalette.UseVisualStyleBackColor = true;
            btnExportPalette.Click += btnExportPalette_Click;
            // 
            // tabMenuColors
            // 
            tabMenuColors.Controls.Add(menuColorsHost);
            tabMenuColors.Location = new Point(4, 24);
            tabMenuColors.Name = "tabMenuColors";
            tabMenuColors.Padding = new Padding(3);
            tabMenuColors.Size = new Size(1207, 620);
            tabMenuColors.TabIndex = 1;
            tabMenuColors.Text = "Menu Colors";
            tabMenuColors.UseVisualStyleBackColor = true;
            // 
            // menuColorsHost
            // 
            menuColorsHost.BorderStyle = BorderStyle.FixedSingle;
            menuColorsHost.Controls.Add(pnlMenuPreview);
            menuColorsHost.Controls.Add(menuControls);
            menuColorsHost.Dock = DockStyle.Fill;
            menuColorsHost.Location = new Point(3, 3);
            menuColorsHost.Name = "menuColorsHost";
            menuColorsHost.Size = new Size(1201, 614);
            menuColorsHost.TabIndex = 0;
            // 
            // pnlMenuPreview
            // 
            pnlMenuPreview.Dock = DockStyle.Fill;
            pnlMenuPreview.Location = new Point(0, 0);
            pnlMenuPreview.Name = "pnlMenuPreview";
            pnlMenuPreview.Size = new Size(1199, 540);
            pnlMenuPreview.TabIndex = 0;
            // 
            // menuControls
            // 
            menuControls.Controls.Add(cmbMenuTarget);
            menuControls.Controls.Add(trkR);
            menuControls.Controls.Add(trkG);
            menuControls.Controls.Add(trkB);
            menuControls.Controls.Add(btnMenuPick);
            menuControls.Controls.Add(btnMenuSave);
            menuControls.Controls.Add(btnMenuReload);
            menuControls.Controls.Add(btnWallpaper);
            menuControls.Controls.Add(lblMenuHex);
            menuControls.Dock = DockStyle.Bottom;
            menuControls.Location = new Point(0, 540);
            menuControls.Name = "menuControls";
            menuControls.Padding = new Padding(10);
            menuControls.Size = new Size(1199, 72);
            menuControls.TabIndex = 1;
            // 
            // cmbMenuTarget
            // 
            cmbMenuTarget.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMenuTarget.Location = new Point(10, 10);
            cmbMenuTarget.Name = "cmbMenuTarget";
            cmbMenuTarget.Size = new Size(140, 23);
            cmbMenuTarget.TabIndex = 0;
            // 
            // trkR
            // 
            trkR.Location = new Point(165, 6);
            trkR.Maximum = 255;
            trkR.Name = "trkR";
            trkR.Size = new Size(200, 45);
            trkR.TabIndex = 1;
            trkR.TickStyle = TickStyle.None;
            // 
            // trkG
            // 
            trkG.Location = new Point(370, 6);
            trkG.Maximum = 255;
            trkG.Name = "trkG";
            trkG.Size = new Size(200, 45);
            trkG.TabIndex = 2;
            trkG.TickStyle = TickStyle.None;
            // 
            // trkB
            // 
            trkB.Location = new Point(575, 6);
            trkB.Maximum = 255;
            trkB.Name = "trkB";
            trkB.Size = new Size(200, 45);
            trkB.TabIndex = 3;
            trkB.TickStyle = TickStyle.None;
            // 
            // btnMenuPick
            // 
            btnMenuPick.Location = new Point(785, 8);
            btnMenuPick.Name = "btnMenuPick";
            btnMenuPick.Size = new Size(70, 25);
            btnMenuPick.TabIndex = 4;
            btnMenuPick.Text = "Pick...";
            btnMenuPick.UseVisualStyleBackColor = true;
            // 
            // btnMenuSave
            // 
            btnMenuSave.Location = new Point(860, 8);
            btnMenuSave.Name = "btnMenuSave";
            btnMenuSave.Size = new Size(70, 25);
            btnMenuSave.TabIndex = 5;
            btnMenuSave.Text = "Save";
            btnMenuSave.UseVisualStyleBackColor = true;
            // 
            // btnMenuReload
            // 
            btnMenuReload.Location = new Point(935, 8);
            btnMenuReload.Name = "btnMenuReload";
            btnMenuReload.Size = new Size(70, 25);
            btnMenuReload.TabIndex = 6;
            btnMenuReload.Text = "Reload";
            btnMenuReload.UseVisualStyleBackColor = true;
            // 
            // btnWallpaper
            // 
            btnWallpaper.Location = new Point(1010, 8);
            btnWallpaper.Name = "btnWallpaper";
            btnWallpaper.Size = new Size(90, 25);
            btnWallpaper.TabIndex = 7;
            btnWallpaper.Text = "Wallpaper...";
            btnWallpaper.UseVisualStyleBackColor = true;
            // 
            // lblMenuHex
            // 

            lblMenuHex.AutoSize = true;
            lblMenuHex.Location = new Point(10, 42);
            lblMenuHex.Name = "lblMenuHex";
            lblMenuHex.Size = new Size(50, 15);
            lblMenuHex.TabIndex = 7;
            lblMenuHex.Text = "#000000";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1684, 993);
            Controls.Add(splitContainer);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
            MinimumSize = new Size(1700, 1032);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PicoStation COV Maker";
            dropPanel.ResumeLayout(false);
            bottomPanel.ResumeLayout(false);
            bottomPanel.PerformLayout();
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            grpDownloadType.ResumeLayout(false);
            grpDownloadType.PerformLayout();
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            tabMain.ResumeLayout(false);
            tabConvert.ResumeLayout(false);
            splitRight.Panel1.ResumeLayout(false);
            splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitRight).EndInit();
            splitRight.ResumeLayout(false);
            previewPanel.ResumeLayout(false);
            previewGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picPreviewFolder).EndInit();
            ((System.ComponentModel.ISupportInitialize)picPreview).EndInit();
            previewHeader.ResumeLayout(false);
            tabMenuColors.ResumeLayout(false);
            menuColorsHost.ResumeLayout(false);
            menuControls.ResumeLayout(false);
            menuControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkR).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkG).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkB).EndInit();
            ResumeLayout(false);

        }



        #endregion



        private System.Windows.Forms.Panel dropPanel;

        private System.Windows.Forms.Label dropLabel;

        private System.Windows.Forms.ListView lvFiles;

        private System.Windows.Forms.ColumnHeader colInput;
        private System.Windows.Forms.ColumnHeader colStatus;

        private System.Windows.Forms.Panel bottomPanel;

        private System.Windows.Forms.ProgressBar progressBar;

        private System.Windows.Forms.TextBox txtLog;

        private System.Windows.Forms.Panel topPanel;

        private System.Windows.Forms.Button btnConvert;

        private System.Windows.Forms.Button btnClear;

        private System.Windows.Forms.Button btnOutputFolder;

        private System.Windows.Forms.Button btnScanSd;
        private System.Windows.Forms.Button btnOpenSd;

        private System.Windows.Forms.Button btnDownloadCovers;
        private System.Windows.Forms.Button btnCalcPvd;

        private System.Windows.Forms.GroupBox grpDownloadType;

        private System.Windows.Forms.CheckBox chkDlOverwrite;

        private System.Windows.Forms.Label lblFolderCoverType;

        private System.Windows.Forms.ComboBox cmbFolderCoverType;

        private System.Windows.Forms.Label lblGameCoverType;

        private System.Windows.Forms.ComboBox cmbGameCoverType;

        private System.Windows.Forms.Label lblCoverBaseUrl;

        private System.Windows.Forms.ComboBox cmbCoverBaseUrl;

        private System.Windows.Forms.Button btnCoverUrlReset;

                private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabConvert;
        private System.Windows.Forms.TabPage tabMenuColors;
        private System.Windows.Forms.Panel menuColorsHost;
        private System.Windows.Forms.Panel pnlMenuPreview;
        private System.Windows.Forms.Panel menuControls;
        private System.Windows.Forms.ComboBox cmbMenuTarget;
        private System.Windows.Forms.TrackBar trkR;
        private System.Windows.Forms.TrackBar trkG;
        private System.Windows.Forms.TrackBar trkB;
        private System.Windows.Forms.Button btnMenuPick;
        private System.Windows.Forms.Button btnMenuSave;
        private System.Windows.Forms.Button btnMenuReload;
        private System.Windows.Forms.Button btnWallpaper;
        private System.Windows.Forms.Label lblMenuHex;
private System.Windows.Forms.SplitContainer splitContainer;

        private System.Windows.Forms.SplitContainer splitRight;

        private System.Windows.Forms.Panel previewPanel;

        private System.Windows.Forms.Panel previewHeader;

        private System.Windows.Forms.Button btnExportPalette;

        private System.Windows.Forms.TableLayoutPanel previewGrid;
        private System.Windows.Forms.PictureBox picPreviewFolder;
        private System.Windows.Forms.PictureBox picPreview;

        private System.Windows.Forms.Label lblPreview;
        private Label label2;
        private Label lblOutputFolder;
    }

}