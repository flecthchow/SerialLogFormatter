using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SerialLogFormatter;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private static readonly string AppVersion = Application.ProductVersion;
    private readonly TextBox _input = MakeEditor();
    private readonly TextBox _output = MakeEditor();
    private readonly TextBox _header = new() { Text = "84 A9 61", Width = 150 };
    private readonly NumericUpDown _bytesPerLine = new() { Minimum = 0, Maximum = 256, Value = 0, Width = 64 };
    private readonly CheckBox _preserveTimestamps = new() { Text = "保留时间戳（单独一行）", Checked = true, AutoSize = true };
    private readonly CheckBox _showInfo = new() { Text = "显示序号和长度", Checked = true, AutoSize = true };
    private readonly CheckBox _uppercase = new() { Text = "大写 HEX", Checked = true, AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.FromArgb(80, 90, 105) };

    public MainForm()
    {
        Text = $"串口日志整理工具 v{AppVersion}";
        MinimumSize = new Size(900, 620);
        Size = new Size(1120, 760);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(245, 247, 250);

        var title = new Label
        {
            Text = $"串口日志整理工具 v{AppVersion}",
            Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.FromArgb(30, 41, 59),
            Margin = new Padding(0, 0, 0, 4)
        };
        var subtitle = new Label
        {
            Text = "粘贴原始日志，按帧头自动分帧、换行和规范化 HEX。数据只在本机处理。",
            AutoSize = true,
            ForeColor = Color.FromArgb(90, 102, 120)
        };

        var settings = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 4)
        };
        settings.Controls.AddRange([
            LabelFor("帧头："), _header,
            LabelFor("  每行字节数（0=整帧一行）："), _bytesPerLine,
            _preserveTimestamps, _showInfo, _uppercase
        ]);

        var format = MakeButton("整理格式", Color.FromArgb(37, 99, 235), Color.White);
        var paste = MakeButton("粘贴并整理", Color.White, Color.FromArgb(37, 99, 235));
        var copy = MakeButton("复制结果", Color.White, Color.FromArgb(30, 41, 59));
        var clear = MakeButton("清空", Color.White, Color.FromArgb(30, 41, 59));
        format.Click += (_, _) => FormatLog();
        paste.Click += (_, _) => { if (Clipboard.ContainsText()) _input.Text = Clipboard.GetText(); FormatLog(); };
        copy.Click += (_, _) => CopyResult();
        clear.Click += (_, _) => { _input.Clear(); _output.Clear(); _status.Text = "已清空"; _input.Focus(); };

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 8) };
        actions.Controls.AddRange([format, paste, copy, clear]);

        var editors = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        editors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        editors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        editors.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editors.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editors.Controls.Add(EditorLabel("原始日志"), 0, 0);
        editors.Controls.Add(EditorLabel("整理结果"), 1, 0);
        editors.Controls.Add(_input, 0, 1);
        editors.Controls.Add(_output, 1, 1);
        _input.Margin = new Padding(0, 5, 7, 0);
        _output.Margin = new Padding(7, 5, 0, 0);
        _output.ReadOnly = true;
        _input.Text = "示例：\r\n[10:21:35.126] TX: 84 A9 61 01 02 03 04 84 A9 61 81 00 FF";

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(22, 18, 22, 14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(title, 0, 0);
        root.Controls.Add(subtitle, 0, 1);
        root.Controls.Add(settings, 0, 2);
        root.Controls.Add(actions, 0, 3);
        root.Controls.Add(editors, 0, 4);
        root.Controls.Add(_status, 0, 5);
        Controls.Add(root);

        AcceptButton = format;
        Shown += (_, _) => { _input.SelectAll(); _input.Focus(); };
    }

    private void FormatLog()
    {
        try
        {
            var result = LogFormatter.Format(_input.Text, _header.Text, (int)_bytesPerLine.Value,
                _preserveTimestamps.Checked, _showInfo.Checked, _uppercase.Checked);
            _output.Text = result.Text;
            _status.Text = result.FrameCount == 0
                ? "没有找到帧。请检查帧头，或确认输入中包含十六进制数据。"
                : $"完成：{result.FrameCount} 帧，共 {result.ByteCount} 字节；无法归入帧的数据 {result.IgnoredByteCount} 字节。";
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "设置有误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void CopyResult()
    {
        if (_output.TextLength == 0) { _status.Text = "当前没有可复制的结果。"; return; }
        Clipboard.SetText(_output.Text);
        _status.Text = "结果已复制到剪贴板。";
    }

    private static TextBox MakeEditor() => new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Dock = DockStyle.Fill,
        AcceptsReturn = true,
        AcceptsTab = true,
        Font = new Font("Consolas", 10.5F),
        BorderStyle = BorderStyle.FixedSingle
    };

    private static Label LabelFor(string text) => new() { Text = text, AutoSize = true, Padding = new Padding(0, 5, 0, 0) };
    private static Label EditorLabel(string text) => new() { Text = text, AutoSize = true, Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold) };
    private static Button MakeButton(string text, Color back, Color fore) => new()
    {
        Text = text,
        AutoSize = true,
        BackColor = back,
        ForeColor = fore,
        FlatStyle = FlatStyle.Flat,
        Padding = new Padding(12, 5, 12, 5),
        Margin = new Padding(0, 0, 9, 0),
        Cursor = Cursors.Hand
    };
}

public readonly record struct FormatResult(string Text, int FrameCount, int ByteCount, int IgnoredByteCount);

public static partial class LogFormatter
{
    // Bracketed timestamps are matched first so their original wrapper is retained in the output.
    [GeneratedRegex(@"\[[^\]\r\n]*(?:\d{1,2}:\d{2}:\d{2}|\d{4}[-/]\d{1,2}[-/]\d{1,2})[^\]\r\n]*\]|(?<![0-9A-Fa-f])(?:\d{4}[-/]\d{1,2}[-/]\d{1,2}[ T])?\d{1,2}:\d{2}:\d{2}(?:[.,]\d{1,6})?", RegexOptions.Compiled)]
    private static partial Regex TimestampRegex();
    [GeneratedRegex(@"(?i)(?<![0-9a-f])[0-9a-f]{2}(?![0-9a-f])|(?<![0-9A-Za-z])[0-9A-Fa-f]{6,}(?![0-9A-Za-z])", RegexOptions.Compiled)]
    private static partial Regex HexTokenRegex();

    public static FormatResult Format(string input, string headerText, int bytesPerLine, bool preserveTimestamps, bool showInfo, bool uppercase)
    {
        var header = ExtractBytes(headerText, true);
        if (header.Count == 0) throw new ArgumentException("帧头不能为空，请输入类似 84 A9 61 的十六进制字节。");

        var source = input ?? string.Empty;
        var timestamps = TimestampRegex().Matches(source)
            .Select(match => new TimestampToken(match.Index, match.Index + match.Length, match.Value.Trim()))
            .ToList();
        var bytes = ExtractByteTokens(source, timestamps);
        var starts = FindFrameStarts(bytes, header);
        if (starts.Count == 0) return new FormatResult(string.Empty, 0, 0, bytes.Count);

        var sb = new StringBuilder();
        var total = 0;
        var timestampIndex = 0;
        for (var frameIndex = 0; frameIndex < starts.Count; frameIndex++)
        {
            var start = starts[frameIndex];
            var end = frameIndex + 1 < starts.Count ? starts[frameIndex + 1] : bytes.Count;
            var frame = bytes.GetRange(start, end - start);
            total += frame.Count;

            if (frameIndex > 0) sb.AppendLine();
            if (preserveTimestamps)
            {
                while (timestampIndex < timestamps.Count && timestamps[timestampIndex].Start <= frame[0].Position)
                {
                    sb.AppendLine(timestamps[timestampIndex].Text);
                    timestampIndex++;
                }
            }
            else
            {
                while (timestampIndex < timestamps.Count && timestamps[timestampIndex].Start <= frame[0].Position)
                    timestampIndex++;
            }
            if (showInfo) sb.Append($"[{frameIndex + 1:000}] ({frame.Count} bytes) ");

            var width = bytesPerLine <= 0 ? int.MaxValue : bytesPerLine;
            for (var i = 0; i < frame.Count; i++)
            {
                if (i > 0 && i % width == 0)
                {
                    sb.AppendLine();
                    if (showInfo) sb.Append("                  ");
                }
                else if (i > 0) sb.Append(' ');
                sb.Append(frame[i].Value.ToString(uppercase ? "X2" : "x2"));
            }
        }

        return new FormatResult(sb.ToString(), starts.Count, total, starts[0]);
    }

    private static List<byte> ExtractBytes(string text, bool allowLongTokens)
    {
        var result = new List<byte>();
        foreach (Match match in HexTokenRegex().Matches(text))
        {
            var value = match.Value;
            if (value.Length == 2)
            {
                result.Add(Convert.ToByte(value, 16));
            }
            else if (allowLongTokens && value.Length % 2 == 0)
            {
                for (var i = 0; i < value.Length; i += 2)
                    result.Add(Convert.ToByte(value.Substring(i, 2), 16));
            }
        }
        return result;
    }

    private static List<ByteToken> ExtractByteTokens(string text, List<TimestampToken> timestamps)
    {
        var result = new List<ByteToken>();
        var timestampIndex = 0;
        foreach (Match match in HexTokenRegex().Matches(text))
        {
            while (timestampIndex < timestamps.Count && timestamps[timestampIndex].End <= match.Index)
                timestampIndex++;
            if (timestampIndex < timestamps.Count && match.Index >= timestamps[timestampIndex].Start && match.Index < timestamps[timestampIndex].End)
                continue;

            var value = match.Value;
            if (value.Length == 2)
            {
                result.Add(new ByteToken(Convert.ToByte(value, 16), match.Index));
            }
            else if (value.Length % 2 == 0)
            {
                for (var i = 0; i < value.Length; i += 2)
                    result.Add(new ByteToken(Convert.ToByte(value.Substring(i, 2), 16), match.Index + i));
            }
        }
        return result;
    }

    private static List<int> FindFrameStarts(List<ByteToken> data, List<byte> header)
    {
        var starts = new List<int>();
        for (var i = 0; i <= data.Count - header.Count; i++)
        {
            var matches = true;
            for (var j = 0; j < header.Count; j++)
                if (data[i + j].Value != header[j]) { matches = false; break; }
            if (!matches) continue;
            starts.Add(i);
            i += header.Count - 1;
        }
        return starts;
    }

    private readonly record struct ByteToken(byte Value, int Position);
    private readonly record struct TimestampToken(int Start, int End, string Text);
}
