using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace FileOccupiedRelease;

internal sealed class MainWindow : Form
{
    private static readonly Color Ink = Color.FromArgb(22, 42, 53);
    private static readonly Color Muted = Color.FromArgb(69, 92, 105);
    private static readonly Color Accent = Color.FromArgb(0, 101, 100);
    private readonly Label title = new() { AutoSize = true, Font = new Font("Segoe UI", 23, FontStyle.Bold) };
    private readonly Label pathLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 14, 0) };
    private readonly Label dropTitle = new()
    {
        Dock = DockStyle.Top, Height = 38, TextAlign = ContentAlignment.BottomCenter,
        Font = new Font("Segoe UI", 13, FontStyle.Bold)
    };
    private readonly Label dropHint = new() { Dock = DockStyle.Fill, ForeColor = Muted, TextAlign = ContentAlignment.TopCenter };
    private readonly CheckBox recursive = new() { AutoSize = true, Checked = true, Anchor = AnchorStyles.Left, Margin = new Padding(13, 0, 0, 0) };
    private readonly Label selected = new() { AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Muted, Margin = new Padding(10, 0, 4, 0) };
    private readonly TextBox path = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 7, 8, 0) };
    private readonly Button browse = MakeButton();
    private readonly Button clear = MakeButton();
    private readonly Button scan = MakeButton();
    private readonly Button close = MakeButton();
    private readonly Button force = MakeButton();
    private readonly Button language = MakeButton();
    private readonly Label status = new() { Dock = DockStyle.Fill, ForeColor = Muted, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ListView results = new()
    {
        Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false,
        HideSelection = false, Margin = Padding.Empty, HeaderStyle = ColumnHeaderStyle.Nonclickable
    };

    private bool chinese = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh";
    private Strings Texts => chinese ? Strings.Chinese : Strings.English;
    private Func<Strings, string> statusText = strings => strings.Ready;
    private CancellationTokenSource? scanCancellation;
    private bool releasing;

    public MainWindow(string? initialPath)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        ForeColor = Ink;
        BackColor = Color.FromArgb(240, 246, 246);
        MinimumSize = new Size(670, 410);
        ClientSize = new Size(795, 530);
        AllowDrop = true;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24, 18, 24, 16), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var header = MakeRow(2);
        header.Controls.Add(title, 0, 0);
        header.Controls.Add(language, 1, 0);
        layout.Controls.Add(header, 0, 0);
        var input = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, RowCount = 2,
            BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White,
            Padding = new Padding(13, 10, 13, 12), Margin = new Padding(0, 12, 0, 0)
        };
        input.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        input.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        input.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        input.Controls.Add(pathLabel, 0, 0);
        input.Controls.Add(path, 1, 0);
        input.Controls.Add(browse, 2, 0);
        input.Controls.Add(recursive, 3, 0);

        var drop = new Panel
        {
            Dock = DockStyle.Fill, Height = 75, Margin = new Padding(0, 10, 0, 0), Padding = new Padding(2),
            BackColor = Color.FromArgb(246, 250, 250)
        };
        drop.Controls.Add(dropHint);
        drop.Controls.Add(dropTitle);
        drop.Paint += (_, e) =>
        {
            using var border = new Pen(Color.FromArgb(117, 158, 160)) { DashStyle = DashStyle.Dash };
            e.Graphics.DrawRectangle(border, 0, 0, drop.ClientSize.Width - 1, drop.ClientSize.Height - 1);
        };
        input.Controls.Add(drop, 0, 1);
        input.SetColumnSpan(drop, 4);
        layout.Controls.Add(input, 0, 1);

        var actions = MakeRow(3, equalWidth: true);
        actions.Margin = new Padding(0, 11, 0, 11);
        scan.Margin = new Padding(0, 0, 5, 0);
        close.Margin = new Padding(5, 0, 5, 0);
        force.Margin = new Padding(5, 0, 0, 0);
        foreach (var button in new[] { scan, close, force })
        {
            button.Dock = DockStyle.Fill;
            button.MinimumSize = new Size(90, 40);
        }
        actions.Controls.Add(scan, 0, 0);
        actions.Controls.Add(close, 1, 0);
        actions.Controls.Add(force, 2, 0);
        layout.Controls.Add(actions, 0, 2);
        scan.BackColor = Accent;
        scan.ForeColor = Color.White;
        scan.FlatAppearance.BorderColor = Accent;
        scan.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 80, 79);
        scan.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 65, 63);

        results.Columns.Add("");
        results.Columns.Add("", 74);
        results.Columns.Add("", 110);
        results.Columns.Add("", 105);
        layout.Controls.Add(results, 0, 3);
        results.Resize += (_, _) => ResizeColumns();

        var footer = MakeRow(3);
        footer.Margin = new Padding(0, 10, 0, 0);
        footer.Controls.Add(status, 0, 0);
        footer.Controls.Add(selected, 1, 0);
        footer.Controls.Add(clear, 2, 0);
        layout.Controls.Add(footer, 0, 4);
        force.ForeColor = Color.FromArgb(151, 43, 38);

        language.Click += (_, _) => { chinese = !chinese; ApplyLanguage(); };
        var menu = new ContextMenuStrip();
        var chooseFiles = menu.Items.Add("");
        var chooseFolder = menu.Items.Add("");
        chooseFiles.Click += (_, _) => ChooseFile();
        chooseFolder.Click += (_, _) => ChooseFolder();
        browse.ContextMenuStrip = menu;
        browse.Click += (_, _) => menu.Show(browse, new Point(0, browse.Height));
        clear.Click += (_, _) => { path.Clear(); InvalidateResults(); };
        path.TextChanged += (_, _) => InvalidateResults();
        recursive.CheckedChanged += (_, _) => InvalidateResults();
        results.SelectedIndexChanged += (_, _) => UpdateActions();
        scan.Click += async (_, _) =>
        {
            if (scanCancellation is null)
                await ScanAsync();
            else
            {
                scanCancellation.Cancel();
                SetStatus(strings => strings.Cancelling);
                UpdateActions();
            }
        };
        close.Click += async (_, _) => await ReleaseAsync(forceEnd: false);
        force.Click += async (_, _) => await ReleaseAsync(forceEnd: true);
        foreach (var dropTarget in new Control[] { this, drop, dropTitle, dropHint })
        {
            dropTarget.AllowDrop = true;
            dropTarget.DragEnter += (_, e) =>
                e.Effect = !IsBusy && e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
            dropTarget.DragDrop += async (_, e) =>
            {
                if (!IsBusy && e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
                    await ScanPathAsync(string.Join(" | ", paths));
            };
        }
        FormClosing += (_, _) => scanCancellation?.Cancel();

        AcceptButton = scan;
        ApplyLanguage();
        if (!string.IsNullOrWhiteSpace(initialPath))
            Shown += async (_, _) => await ScanPathAsync(initialPath);
        Shown += (_, _) => ResizeColumns();
    }

    private bool IsBusy => scanCancellation is not null || releasing;
    private LockOwner? SelectedOwner => results.SelectedItems.Count == 1 ? results.SelectedItems[0].Tag as LockOwner : null;

    private void ApplyLanguage()
    {
        Text = title.Text = Texts.Title;
        language.Text = chinese ? "English" : "简体中文";
        pathLabel.Text = path.AccessibleName = Texts.Path;
        path.PlaceholderText = Texts.Ready;
        browse.Text = Texts.Browse;
        browse.ContextMenuStrip!.Items[0].Text = Texts.File;
        browse.ContextMenuStrip.Items[1].Text = Texts.Folder;
        recursive.Text = Texts.Recursive;
        dropTitle.Text = Texts.Drop;
        dropHint.Text = Texts.DropHint;
        clear.Text = Texts.Clear;
        results.AccessibleName = Texts.Results;
        results.Columns[0].Text = Texts.Process;
        results.Columns[1].Text = Texts.Pid;
        results.Columns[2].Text = Texts.Kind;
        results.Columns[3].Text = Texts.State;
        foreach (ListViewItem item in results.Items)
        {
            var owner = (LockOwner)item.Tag!;
            item.SubItems[2].Text = ProcessType(owner);
            item.SubItems[3].Text = Texts.InUse;
        }
        close.Text = Texts.Close;
        force.Text = Texts.Force;
        status.Text = statusText(Texts);
        UpdateActions();
    }

    private void SetStatus(Func<Strings, string> message)
    {
        statusText = message;
        status.Text = message(Texts);
    }

    private void UpdateActions()
    {
        path.Enabled = browse.Enabled = recursive.Enabled = !IsBusy;
        clear.Enabled = !IsBusy && path.TextLength > 0;
        selected.Text = string.Format(Texts.Selection, results.SelectedItems.Count);
        scan.Text = scanCancellation is null ? Texts.Scan : Texts.Cancel;
        scan.Enabled = scanCancellation is not null
            ? !scanCancellation.IsCancellationRequested
            : !releasing && !string.IsNullOrWhiteSpace(path.Text);
        scan.BackColor = scan.Enabled ? Accent : Color.FromArgb(222, 237, 236);
        scan.FlatAppearance.BorderColor = scan.Enabled ? Accent : Color.FromArgb(176, 199, 199);
        close.Enabled = force.Enabled = !IsBusy && SelectedOwner?.CanClose == true;
    }

    private async void ChooseFile()
    {
        using var dialog = new OpenFileDialog { Title = Texts.File, CheckFileExists = true, Multiselect = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            await ScanPathAsync(string.Join(" | ", dialog.FileNames));
    }

    private async void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = Texts.Folder, UseDescriptionForTitle = true, ShowNewFolderButton = false };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            await ScanPathAsync(dialog.SelectedPath);
    }

    private Task ScanPathAsync(string value)
    {
        path.Text = value;
        return ScanAsync();
    }

    private string[] ReadPaths() => path.Text.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(value => value.Trim('"')).Where(value => value.Length > 0).ToArray();

    private async Task ScanAsync(Func<Strings, string>? priorResult = null)
    {
        var paths = ReadPaths();
        if (paths.Length == 0)
        {
            SetStatus(strings => strings.MissingPath);
            return;
        }
        bool includeSubfolders = recursive.Checked;
        using var cancellation = new CancellationTokenSource();
        scanCancellation = cancellation;
        results.Items.Clear();
        SetStatus(strings => strings.Scanning);
        UpdateActions();

        try
        {
            var owners = await Task.Run(() => LockScanner.Scan(paths, includeSubfolders, cancellation.Token), cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (IsDisposed)
                return;

            results.BeginUpdate();
            try
            {
                foreach (var owner in owners)
                {
                    string pid = owner.Pid.ToString(CultureInfo.InvariantCulture);
                    var item = new ListViewItem([
                        string.IsNullOrWhiteSpace(owner.Name) ? pid : owner.Name, pid, ProcessType(owner), Texts.InUse
                    ]) { Tag = owner };
                    results.Items.Add(item);
                }
            }
            finally
            {
                results.EndUpdate();
            }

            SetStatus(strings =>
            {
                var count = owners.Length == 0 ? strings.Empty : string.Format(strings.Count, owners.Length);
                return priorResult is null ? count : $"{priorResult(strings)} {count}";
            });
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
                SetStatus(strings => strings.Cancelled);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or ArgumentException or NotSupportedException)
        {
            if (!IsDisposed)
                ShowError(error);
        }
        finally
        {
            scanCancellation = null;
            if (!IsDisposed)
                UpdateActions();
        }
    }

    private async Task ReleaseAsync(bool forceEnd)
    {
        var owner = SelectedOwner;
        if (owner is null || !owner.CanClose || IsBusy)
            return;

        if (forceEnd && MessageBox.Show(this, string.Format(Texts.Confirm, owner.Name, owner.Pid), Texts.Title,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        var paths = ReadPaths();
        bool includeSubfolders = recursive.Checked;
        releasing = true;
        UpdateActions();
        SetStatus(strings => strings.Releasing);
        try
        {
            var result = await Task.Run<ReleaseResult?>(() =>
            {
                var activeOwner = LockScanner.Scan(paths, includeSubfolders, CancellationToken.None)
                    .FirstOrDefault(item => item.Pid == owner.Pid && item.StartTime == owner.StartTime);
                return activeOwner is null ? null : ProcessRelease.Release(activeOwner, forceEnd);
            });
            if (IsDisposed)
                return;
            Func<Strings, string> message = strings => result switch
            {
                null => strings.NoLongerUsingPath,
                ReleaseResult.Closed => strings.Closed,
                ReleaseResult.Gone => strings.Gone,
                ReleaseResult.Protected => strings.Protected,
                ReleaseResult.NoWindow => strings.NoWindow,
                _ => strings.StillRunning
            };
            if (result is null or ReleaseResult.Closed or ReleaseResult.Gone)
                await ScanAsync(message);
            else
                SetStatus(message);
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException or UnauthorizedAccessException
            or IOException or ArgumentException or NotSupportedException)
        {
            if (!IsDisposed)
                ShowError(error);
        }
        finally
        {
            releasing = false;
            if (!IsDisposed)
                UpdateActions();
        }
    }

    private void ShowError(Exception error) => SetStatus(strings => error switch
    {
        ArgumentException or FileNotFoundException or DirectoryNotFoundException => strings.MissingPath,
        UnauthorizedAccessException or Win32Exception { NativeErrorCode: 5 } => strings.AccessDenied,
        Win32Exception native => string.Format(strings.WindowsError, native.NativeErrorCode),
        _ => strings.Error
    });

    private void InvalidateResults()
    {
        results.Items.Clear();
        SetStatus(strings => strings.Ready);
        UpdateActions();
    }

    private string ProcessType(LockOwner owner) => owner.ApplicationType switch
    {
        1 or 2 => Texts.App,
        3 => Texts.Service,
        4 or 1000 => Texts.System,
        5 => Texts.Console,
        _ => Texts.Unknown
    };

    private void ResizeColumns()
    {
        results.Columns[1].Width = LogicalToDeviceUnits(74);
        results.Columns[2].Width = LogicalToDeviceUnits(110);
        results.Columns[3].Width = LogicalToDeviceUnits(105);
        results.Columns[0].Width = Math.Max(0, results.ClientSize.Width - LogicalToDeviceUnits(289) - SystemInformation.VerticalScrollBarWidth);
    }

    private static TableLayoutPanel MakeRow(int columns, bool equalWidth = false)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = columns, RowCount = 1, Margin = Padding.Empty
        };
        for (var index = 0; index < columns; index++)
        {
            row.ColumnStyles.Add(equalWidth ? new ColumnStyle(SizeType.Percent, 100f / columns)
                : index == 0 ? new ColumnStyle(SizeType.Percent, 100) : new ColumnStyle(SizeType.AutoSize));
        }
        return row;
    }

    private static Button MakeButton()
    {
        var button = new Button
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlatStyle = FlatStyle.Flat,
            BackColor = Color.White, Padding = new Padding(9, 3, 9, 3),
            Margin = new Padding(5, 0, 0, 0), MinimumSize = new Size(90, 34)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(160, 183, 188);
        return button;
    }
}
