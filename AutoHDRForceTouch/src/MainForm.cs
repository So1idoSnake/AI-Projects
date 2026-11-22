using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoHDRForceTouch
{
    internal sealed class MainForm : Form
    {
        private static readonly Color WindowColor = Color.FromArgb(18, 22, 28);
        private static readonly Color SurfaceColor = Color.FromArgb(32, 38, 48);
        private static readonly Color SurfaceHoverColor = Color.FromArgb(43, 51, 64);
        private static readonly Color AccentColor = Color.FromArgb(0, 120, 215);
        private static readonly Color MutedTextColor = Color.FromArgb(177, 187, 201);

        private readonly RegistryService _registryService;
        private readonly ToolTip _pathToolTip;
        private readonly Panel _optionsPanel;
        private readonly Panel _successPanel;
        private readonly Label _fileNameLabel;
        private readonly Label _pathLabel;
        private readonly Button _autoHdrYesButton;
        private readonly Button _autoHdrNoButton;
        private readonly Button _tenBitYesButton;
        private readonly Button _tenBitNoButton;
        private readonly Button _applyButton;
        private readonly Button _cancelButton;
        private readonly Label _successMessageLabel;
        private readonly Label _successDetailsLabel;
        private readonly Button _doneButton;

        private string _selectedExecutablePath;
        private bool _autoHdrEnabled = true;
        private bool _tenBitEnabled = true;
        private bool _pickerShown;

        internal MainForm(RegistryService registryService)
        {
            if (registryService == null)
            {
                throw new ArgumentNullException("registryService");
            }

            _registryService = registryService;
            _pathToolTip = new ToolTip();

            Text = "Auto HDR Force";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            // Use the same embedded artwork in the title bar and taskbar that
            // Windows Explorer and game-library launchers read from the EXE.
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            ShowIcon = true;
            ClientSize = new Size(520, 590);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = WindowColor;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            _optionsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = WindowColor,
                Padding = new Padding(28, 24, 28, 24)
            };

            TableLayoutPanel optionsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = WindowColor,
                ColumnCount = 1,
                RowCount = 9,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            optionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

            Label titleLabel = new Label
            {
                Text = "Confirm options",
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = Padding.Empty
            };

            TableLayoutPanel fileLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 4, 0, 8)
            };
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _fileNameLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Font = new Font(Font.FontFamily, 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 205, 255),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = Padding.Empty
            };
            _pathLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = MutedTextColor,
                TextAlign = ContentAlignment.TopLeft,
                Margin = Padding.Empty
            };
            fileLayout.Controls.Add(_fileNameLabel, 0, 0);
            fileLayout.Controls.Add(_pathLabel, 0, 1);

            Label autoHdrLabel = CreateSectionLabel("Enable Auto HDR for this game?");
            TableLayoutPanel autoHdrButtons = CreateSegmentLayout();
            _autoHdrYesButton = CreateSegmentButton("Yes");
            _autoHdrNoButton = CreateSegmentButton("No");
            _autoHdrYesButton.Click += delegate { SetAutoHdrEnabled(true); };
            _autoHdrNoButton.Click += delegate { SetAutoHdrEnabled(false); };
            autoHdrButtons.Controls.Add(_autoHdrYesButton, 0, 0);
            autoHdrButtons.Controls.Add(_autoHdrNoButton, 1, 0);

            Label tenBitLabel = CreateSectionLabel("Enable 10-bit color?");
            TableLayoutPanel tenBitButtons = CreateSegmentLayout();
            _tenBitYesButton = CreateSegmentButton("Yes");
            _tenBitNoButton = CreateSegmentButton("No");
            _tenBitYesButton.Click += delegate { SetTenBitEnabled(true); };
            _tenBitNoButton.Click += delegate { SetTenBitEnabled(false); };
            tenBitButtons.Controls.Add(_tenBitYesButton, 0, 0);
            tenBitButtons.Controls.Add(_tenBitNoButton, 1, 0);

            _applyButton = CreateActionButton("Apply", true);
            _applyButton.Click += ApplyButtonClick;
            _cancelButton = CreateActionButton("Cancel", false);
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.Click += delegate { Close(); };

            optionsLayout.Controls.Add(titleLabel, 0, 0);
            optionsLayout.Controls.Add(fileLayout, 0, 1);
            optionsLayout.Controls.Add(autoHdrLabel, 0, 2);
            optionsLayout.Controls.Add(autoHdrButtons, 0, 3);
            optionsLayout.Controls.Add(tenBitLabel, 0, 4);
            optionsLayout.Controls.Add(tenBitButtons, 0, 5);
            optionsLayout.Controls.Add(new Panel(), 0, 6);
            optionsLayout.Controls.Add(_applyButton, 0, 7);
            optionsLayout.Controls.Add(_cancelButton, 0, 8);
            _optionsPanel.Controls.Add(optionsLayout);

            _successPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = WindowColor,
                Padding = new Padding(28, 42, 28, 28),
                Visible = false
            };

            TableLayoutPanel successLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Margin = Padding.Empty
            };
            successLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            successLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
            successLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            successLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            successLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            successLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
            successLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 4F));

            CheckMarkControl checkMark = new CheckMarkControl
            {
                Anchor = AnchorStyles.None,
                Size = new Size(92, 92),
                Margin = Padding.Empty
            };
            Label successTitle = new Label
            {
                Text = "Success",
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 20F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = Padding.Empty
            };
            _successMessageLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 11F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = Padding.Empty
            };
            _successDetailsLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = MutedTextColor,
                TextAlign = ContentAlignment.TopCenter,
                Margin = new Padding(8, 10, 8, 10)
            };
            _doneButton = CreateActionButton("Done", true);
            _doneButton.Click += delegate { Close(); };

            successLayout.Controls.Add(checkMark, 0, 0);
            successLayout.Controls.Add(successTitle, 0, 1);
            successLayout.Controls.Add(_successMessageLabel, 0, 2);
            successLayout.Controls.Add(_successDetailsLabel, 0, 3);
            successLayout.Controls.Add(_doneButton, 0, 4);
            _successPanel.Controls.Add(successLayout);

            Controls.Add(_successPanel);
            Controls.Add(_optionsPanel);
            AcceptButton = _applyButton;
            CancelButton = _cancelButton;
            Shown += MainFormShown;

            SetAutoHdrEnabled(true);
            SetTenBitEnabled(true);
        }

        private static Label CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.BottomLeft,
                Margin = Padding.Empty
            };
        }

        private static TableLayoutPanel CreateSegmentLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 8),
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return layout;
        }

        private static Button CreateSegmentButton(string text)
        {
            return new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(102, 112, 126) },
                BackColor = SurfaceColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
                Cursor = Cursors.Hand,
                Margin = text == "Yes" ? new Padding(0, 0, 6, 0) : new Padding(6, 0, 0, 0),
                UseVisualStyleBackColor = false,
                TabStop = true
            };
        }

        private static Button CreateActionButton(string text, bool primary)
        {
            Color background = primary ? AccentColor : SurfaceColor;
            return new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance =
                {
                    BorderSize = primary ? 0 : 1,
                    BorderColor = Color.FromArgb(102, 112, 126),
                    MouseOverBackColor = primary ? Color.FromArgb(20, 134, 232) : SurfaceHoverColor,
                    MouseDownBackColor = primary ? Color.FromArgb(0, 100, 185) : Color.FromArgb(51, 60, 74)
                },
                BackColor = background,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 8, 0, 4),
                UseVisualStyleBackColor = false
            };
        }

        private void MainFormShown(object sender, EventArgs e)
        {
            if (_pickerShown)
            {
                return;
            }

            _pickerShown = true;
            BeginInvoke(new Action(ShowExecutablePicker));
        }

        private void ShowExecutablePicker()
        {
            using (OpenFileDialog picker = new OpenFileDialog())
            {
                picker.Title = "Choose the game executable";
                picker.Filter = "Applications (*.exe)|*.exe";
                picker.DefaultExt = "exe";
                picker.AddExtension = true;
                picker.CheckFileExists = true;
                picker.CheckPathExists = true;
                picker.DereferenceLinks = true;
                picker.Multiselect = false;
                picker.RestoreDirectory = true;
                picker.SupportMultiDottedExtensions = true;

                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    Close();
                    return;
                }

                _selectedExecutablePath = Path.GetFullPath(picker.FileName);
                _fileNameLabel.Text = Path.GetFileName(_selectedExecutablePath);
                _pathLabel.Text = _selectedExecutablePath;
                _pathToolTip.SetToolTip(_fileNameLabel, _selectedExecutablePath);
                _pathToolTip.SetToolTip(_pathLabel, _selectedExecutablePath);
                _applyButton.Enabled = true;
                _applyButton.Focus();
            }
        }

        private void SetAutoHdrEnabled(bool enabled)
        {
            _autoHdrEnabled = enabled;
            StyleSegmentPair(_autoHdrYesButton, _autoHdrNoButton, enabled);

            _tenBitYesButton.Enabled = enabled;
            _tenBitNoButton.Enabled = enabled;
            if (!enabled)
            {
                // Ten-bit upgrade is part of the Auto HDR behavior string, so it
                // cannot remain selected independently when Auto HDR is disabled.
                SetTenBitEnabled(false);
            }
            else
            {
                StyleSegmentPair(_tenBitYesButton, _tenBitNoButton, _tenBitEnabled);
            }
        }

        private void SetTenBitEnabled(bool enabled)
        {
            _tenBitEnabled = enabled && _autoHdrEnabled;
            StyleSegmentPair(_tenBitYesButton, _tenBitNoButton, _tenBitEnabled);
        }

        private static void StyleSegmentPair(Button yesButton, Button noButton, bool yesSelected)
        {
            StyleSegmentButton(yesButton, yesSelected);
            StyleSegmentButton(noButton, !yesSelected);
        }

        private static void StyleSegmentButton(Button button, bool selected)
        {
            button.BackColor = selected ? AccentColor : SurfaceColor;
            button.FlatAppearance.BorderColor = selected ? AccentColor : Color.FromArgb(102, 112, 126);
        }

        private async void ApplyButtonClick(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(_selectedExecutablePath) || !File.Exists(_selectedExecutablePath))
            {
                MessageBox.Show(
                    this,
                    "The selected executable no longer exists. Close the app and select it again.",
                    "Executable not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            bool enableAutoHdr = _autoHdrEnabled;
            bool enableTenBit = _tenBitEnabled;
            SetWorkingState(true);

            try
            {
                RegistryApplyResult result = await Task.Run(
                    delegate { return _registryService.Apply(_selectedExecutablePath, enableAutoHdr, enableTenBit); });

                if (result == null || !result.Success)
                {
                    string message = result == null
                        ? "The registry operation did not return a result."
                        : result.Message;
                    ShowApplyError(message);
                    return;
                }

                ShowSuccess(result);
            }
            catch (Exception exception)
            {
                ShowApplyError(exception.Message);
            }
            finally
            {
                if (_optionsPanel.Visible)
                {
                    SetWorkingState(false);
                }
            }
        }

        private void SetWorkingState(bool working)
        {
            // Prevent closing the UI while the background operation is between
            // backup and read-back verification.
            ControlBox = !working;
            _applyButton.Enabled = !working;
            _cancelButton.Enabled = !working;
            _autoHdrYesButton.Enabled = !working;
            _autoHdrNoButton.Enabled = !working;
            _tenBitYesButton.Enabled = !working && _autoHdrEnabled;
            _tenBitNoButton.Enabled = !working && _autoHdrEnabled;
            UseWaitCursor = working;
            _applyButton.Text = working ? "Applying..." : "Apply";
        }

        private void ShowApplyError(string message)
        {
            MessageBox.Show(
                this,
                String.IsNullOrWhiteSpace(message) ? "Auto HDR could not be updated." : message,
                "Auto HDR was not changed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void ShowSuccess(RegistryApplyResult result)
        {
            UseWaitCursor = false;
            ControlBox = true;
            _successMessageLabel.Text = String.IsNullOrWhiteSpace(result.Message)
                ? "The requested Auto HDR settings were applied."
                : result.Message;

            string details = Path.GetFileName(_selectedExecutablePath);
            if (!String.IsNullOrWhiteSpace(result.BackupPath))
            {
                details += Environment.NewLine + Environment.NewLine + "Backup: " + result.BackupPath;
                _pathToolTip.SetToolTip(_successDetailsLabel, result.BackupPath);
            }
            _successDetailsLabel.Text = details;

            _optionsPanel.Visible = false;
            _successPanel.Visible = true;
            _successPanel.BringToFront();
            AcceptButton = _doneButton;
            CancelButton = _doneButton;
            _doneButton.Focus();
        }

        private sealed class CheckMarkControl : Control
        {
            internal CheckMarkControl()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint,
                    true);
                BackColor = WindowColor;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Rectangle circle = new Rectangle(5, 5, Width - 10, Height - 10);
                using (Brush brush = new SolidBrush(Color.FromArgb(20, 166, 75)))
                {
                    e.Graphics.FillEllipse(brush, circle);
                }

                Point[] check =
                {
                    new Point((int)(Width * 0.27), (int)(Height * 0.51)),
                    new Point((int)(Width * 0.43), (int)(Height * 0.67)),
                    new Point((int)(Width * 0.74), (int)(Height * 0.34))
                };
                using (Pen pen = new Pen(Color.White, Math.Max(5F, Width / 16F)))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    e.Graphics.DrawLines(pen, check);
                }
            }
        }
    }
}
