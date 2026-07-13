# 串口日志整理工具

当前版本：v1.1.0

轻量 Windows 桌面工具。将串口软件抓到的 MCU/DSP HEX 日志粘贴进来，按指定帧头自动分帧和换行。

## 当前功能

- 默认按 `84 A9 61` 分帧，帧头可修改
- 支持 `84 A9 61`、`84A961` 等输入形式
- 识别常见日期和时间戳，可保留并在对应帧前单独显示一行
- 可选整帧一行或每 N 字节换行
- 显示帧序号、长度和统计
- 一键粘贴整理、复制结果、清空

## 构建

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

发布后的程序依赖 .NET 8 Desktop Runtime。
