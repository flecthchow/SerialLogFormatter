using SerialLogFormatter;

var cases = new[]
{
    ("时间戳与空格", "[10:21:35.126] TX: 84 A9 61 01 02 03 04 [10:21:35.130] RX: 84 A9 61 81 00 FF", "84 A9 61", 2, 13),
    ("连续HEX", "84A9610102030484A9618100FF", "84A961", 2, 13),
    ("逗号分隔", "noise,84,A9,61,AA,BB,84,A9,61,CC", "84 A9 61", 2, 9)
};

foreach (var (name, input, header, frames, bytes) in cases)
{
    var result = LogFormatter.Format(input, header, 0, true, true, true);
    if (result.FrameCount != frames || result.ByteCount != bytes)
        throw new Exception($"{name}失败：实际 {result.FrameCount} 帧/{result.ByteCount} 字节");
    Console.WriteLine($"PASS {name}: {result.FrameCount} 帧/{result.ByteCount} 字节");
}

var timestamped = LogFormatter.Format(
    "[10:21:35.126] TX: 84 A9 61 01 02 [2026-07-13 10:21:36.250] RX: 84 A9 61 03",
    "84 A9 61", 0, true, true, true);
var expectedTimestamped = "[10:21:35.126]\r\n[001] (5 bytes) 84 A9 61 01 02\r\n[2026-07-13 10:21:36.250]\r\n[002] (4 bytes) 84 A9 61 03";
if (timestamped.Text != expectedTimestamped)
    throw new Exception($"保留时间戳失败：\r\n{timestamped.Text}");
Console.WriteLine("PASS 保留时间戳并单独成行");

var hiddenTimestamp = LogFormatter.Format(
    "10:21:35.126 84 A9 61 01", "84 A9 61", 0, false, false, true);
if (hiddenTimestamp.Text != "84 A9 61 01" || hiddenTimestamp.ByteCount != 4)
    throw new Exception($"隐藏时间戳失败：{hiddenTimestamp.Text}");
Console.WriteLine("PASS 隐藏时间戳且不混入 HEX");
