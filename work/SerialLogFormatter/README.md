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

## 网页版

`work/SerialLogFormatter.Web` 是纯静态网页版（无需构建），整理逻辑 `formatter.js` 移植自 `Program.cs` 中的 `LogFormatter`，修改时请两边同步。网页版额外支持打开/拖入日志文件、下载结果，设置保存在浏览器本地。

本地预览（ES 模块不能直接双击 `file://` 打开）：

```powershell
python -m http.server 8000 --directory work/SerialLogFormatter.Web
```

测试：

```powershell
npm test
```

部署：推送到 `main` 后 `.github/workflows/pages.yml` 会先跑测试再发布到 GitHub Pages。首次使用需在仓库 Settings → Pages → Build and deployment → Source 选择 **GitHub Actions**。
