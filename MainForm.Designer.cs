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
            colOutput = new ColumnHeader();
            colStatus = new ColumnHeader();
            bottomPanel = new Panel();
            progressBar = new ProgressBar();
            txtLog = new TextBox();
            topPanel = new Panel();
            btnConvert = new Button();
            btnClear = new Button();
            btnOutputFolder = new Button();
            btnScanSd = new Button();
            btnDownloadCovers = new Button();
            lblCoverBaseUrl = new Label();
            txtCoverBaseUrl = new TextBox();
            btnCoverUrlReset = new Button();
            lblOutputFolder = new Label();
            splitContainer = new SplitContainer();
            splitRight = new SplitContainer();
            previewPanel = new Panel();
            picPreview = new PictureBox();
            lblPreview = new Label();
            dropPanel.SuspendLayout();
            bottomPanel.SuspendLayout();
            topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitRight).BeginInit();
            splitRight.Panel1.SuspendLayout();
            splitRight.Panel2.SuspendLayout();
            splitRight.SuspendLayout();
            previewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPreview).BeginInit();
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
            dropPanel.Size = new Size(492, 506);
            dropPanel.TabIndex = 0;
            // 
            // dropLabel
            // 
            dropLabel.Dock = DockStyle.Fill;
            dropLabel.Font = new Font("Segoe UI", 12F);
            dropLabel.Location = new Point(12, 12);
            dropLabel.Name = "dropLabel";
            dropLabel.Size = new Size(466, 480);
            dropLabel.TabIndex = 0;
            dropLabel.Text = "Drop images / folders / .cov files here\r\n\r\nOutput: 128×128 raw 16bpp BGR555 (.cov)\r\n(32,768 bytes, little-endian)\r\n\r\nTip: drop an existing .cov to preview it.";
            dropLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lvFiles
            // 
            lvFiles.Columns.AddRange(new ColumnHeader[] { colInput, colOutput, colStatus });
            lvFiles.Dock = DockStyle.Fill;
            lvFiles.FullRowSelect = true;
            lvFiles.GridLines = true;
            lvFiles.HideSelection = true;
            lvFiles.Location = new Point(0, 0);
            lvFiles.Name = "lvFiles";
            lvFiles.Size = new Size(875, 258);
            lvFiles.TabIndex = 1;
            lvFiles.UseCompatibleStateImageBehavior = false;
            lvFiles.View = View.Details;
            // 
            // colInput
            // 
            colInput.Text = "Input";
            colInput.Width = 250;
            // 
            // colOutput
            // 
            colOutput.Text = "Output";
            colOutput.Width = 300;
            // 
            // colStatus
            // 
            colStatus.Text = "Status";
            colStatus.Width = 120;
            // 
            // bottomPanel
            // 
            bottomPanel.Controls.Add(progressBar);
            bottomPanel.Controls.Add(txtLog);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Location = new Point(0, 584);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(12, 8, 12, 12);
            bottomPanel.Size = new Size(1371, 229);
            bottomPanel.TabIndex = 2;
            // 
            // progressBar
            // 
            progressBar.Dock = DockStyle.Top;
            progressBar.Location = new Point(12, 8);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(1347, 18);
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
            txtLog.Size = new Size(1347, 209);
            txtLog.TabIndex = 1;
            // 
            // topPanel
            // 
            topPanel.Controls.Add(btnConvert);
            topPanel.Controls.Add(btnClear);
            topPanel.Controls.Add(btnOutputFolder);
            topPanel.Controls.Add(btnScanSd);
            topPanel.Controls.Add(btnDownloadCovers);
            topPanel.Controls.Add(lblCoverBaseUrl);
            topPanel.Controls.Add(txtCoverBaseUrl);
            topPanel.Controls.Add(btnCoverUrlReset);
            topPanel.Controls.Add(lblOutputFolder);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Padding = new Padding(12);
            topPanel.Size = new Size(1371, 78);
            topPanel.TabIndex = 3;
            // 
            // btnConvert
            // 
            btnConvert.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConvert.Location = new Point(1284, 10);
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
            btnClear.Location = new Point(1203, 10);
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
            btnOutputFolder.Location = new Point(1122, 10);
            btnOutputFolder.Name = "btnOutputFolder";
            btnOutputFolder.Size = new Size(75, 25);
            btnOutputFolder.TabIndex = 1;
            btnOutputFolder.Text = "Output...";
            btnOutputFolder.UseVisualStyleBackColor = true;
            btnOutputFolder.Click += btnOutputFolder_Click;
            // 
            // btnScanSd
            // 
            btnScanSd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnScanSd.Location = new Point(1041, 10);
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
            btnDownloadCovers.Location = new Point(960, 10);
            btnDownloadCovers.Name = "btnDownloadCovers";
            btnDownloadCovers.Size = new Size(75, 25);
            btnDownloadCovers.TabIndex = 5;
            btnDownloadCovers.Text = "Covers...";
            btnDownloadCovers.UseVisualStyleBackColor = true;
            btnDownloadCovers.Click += btnDownloadCovers_Click;
            // 
            // lblCoverBaseUrl
            // 
            lblCoverBaseUrl.AutoSize = true;
            lblCoverBaseUrl.Location = new Point(12, 48);
            lblCoverBaseUrl.Name = "lblCoverBaseUrl";
            lblCoverBaseUrl.Size = new Size(103, 15);
            lblCoverBaseUrl.TabIndex = 6;
            lblCoverBaseUrl.Text = "Cover source URL:";
            // 
            // txtCoverBaseUrl
            // 
            txtCoverBaseUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtCoverBaseUrl.Location = new Point(115, 45);
            txtCoverBaseUrl.Name = "txtCoverBaseUrl";
            txtCoverBaseUrl.Size = new Size(1083, 23);
            txtCoverBaseUrl.TabIndex = 7;
            // 
            // btnCoverUrlReset
            // 
            btnCoverUrlReset.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCoverUrlReset.Location = new Point(1203, 45);
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
            lblOutputFolder.Size = new Size(700, 54);
            lblOutputFolder.TabIndex = 0;
            lblOutputFolder.Text = "Output:";
            lblOutputFolder.TextAlign = ContentAlignment.MiddleLeft;
            lblOutputFolder.Visible = false;
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 78);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(dropPanel);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(splitRight);
            splitContainer.Size = new Size(1371, 506);
            splitContainer.SplitterDistance = 492;
            splitContainer.TabIndex = 4;
            // 
            // splitRight
            // 
            splitRight.Dock = DockStyle.Fill;
            splitRight.Location = new Point(0, 0);
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
            splitRight.Size = new Size(875, 506);
            splitRight.SplitterDistance = 258;
            splitRight.TabIndex = 5;
            // 
            // previewPanel
            // 
            previewPanel.BorderStyle = BorderStyle.FixedSingle;
            previewPanel.Controls.Add(picPreview);
            previewPanel.Controls.Add(lblPreview);
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.Location = new Point(0, 0);
            previewPanel.Name = "previewPanel";
            previewPanel.Padding = new Padding(10);
            previewPanel.Size = new Size(875, 244);
            previewPanel.TabIndex = 0;
            // 
            // picPreview
            // 
            picPreview.Dock = DockStyle.Fill;
            picPreview.Location = new Point(10, 30);
            picPreview.Name = "picPreview";
            picPreview.Size = new Size(853, 202);
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.TabIndex = 1;
            picPreview.TabStop = false;
            // 
            // lblPreview
            // 
            lblPreview.AutoEllipsis = true;
            lblPreview.Dock = DockStyle.Top;
            lblPreview.Location = new Point(10, 10);
            lblPreview.Name = "lblPreview";
            lblPreview.Size = new Size(853, 20);
            lblPreview.TabIndex = 0;
            lblPreview.Text = "Preview: (none)";
            lblPreview.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1371, 813);
            Controls.Add(splitContainer);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
            MinimumSize = new Size(900, 550);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PicoStation COV Maker";
            dropPanel.ResumeLayout(false);
            bottomPanel.ResumeLayout(false);
            bottomPanel.PerformLayout();
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            splitRight.Panel1.ResumeLayout(false);
            splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitRight).EndInit();
            splitRight.ResumeLayout(false);
            previewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picPreview).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel dropPanel;
        private System.Windows.Forms.Label dropLabel;
        private System.Windows.Forms.ListView lvFiles;
        private System.Windows.Forms.ColumnHeader colInput;
        private System.Windows.Forms.ColumnHeader colOutput;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnOutputFolder;
        private System.Windows.Forms.Button btnScanSd;
        private System.Windows.Forms.Button btnDownloadCovers;
        private System.Windows.Forms.Label lblCoverBaseUrl;
        private System.Windows.Forms.TextBox txtCoverBaseUrl;
        private System.Windows.Forms.Button btnCoverUrlReset;
        private System.Windows.Forms.Label lblOutputFolder;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.SplitContainer splitRight;
        private System.Windows.Forms.Panel previewPanel;
        private System.Windows.Forms.PictureBox picPreview;
        private System.Windows.Forms.Label lblPreview;
    }
}
