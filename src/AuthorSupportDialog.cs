namespace FileOccupiedRelease;

internal static class AuthorSupportDialog
{
    public static void Show(Form owner, Strings texts)
    {
        using var stream = typeof(AuthorSupportDialog).Assembly.GetManifestResourceStream("FileOccupiedRelease.AuthorQr")!;
        using var image = Image.FromStream(stream);
        using var dialog = new Form
        {
            Text = texts.SupportAuthor,
            Font = owner.Font,
            ForeColor = owner.ForeColor,
            BackColor = owner.BackColor,
            AutoScaleMode = AutoScaleMode.Dpi,
            ClientSize = new Size(404, 468),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowIcon = false,
            ShowInTaskbar = false
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(18, 15, 18, 15), ColumnCount = 1, RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label
        {
            Text = texts.SupportThanks, AutoSize = true, MaximumSize = new Size(360, 0), Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        layout.Controls.Add(new PictureBox
        {
            Image = image, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
            Margin = Padding.Empty, AccessibleName = texts.SupportQr
        }, 0, 1);
        layout.Controls.Add(new Label
        {
            Text = texts.SupportInstructions, AutoSize = true, MaximumSize = new Size(360, 0), Margin = new Padding(0, 10, 0, 8)
        }, 0, 2);

        var close = new Button
        {
            Text = texts.Dismiss, AutoSize = true, MinimumSize = new Size(90, 34),
            Anchor = AnchorStyles.Right, DialogResult = DialogResult.Cancel
        };
        layout.Controls.Add(close, 0, 3);
        dialog.Controls.Add(layout);
        dialog.AcceptButton = close;
        dialog.CancelButton = close;
        dialog.ShowDialog(owner);
    }
}
