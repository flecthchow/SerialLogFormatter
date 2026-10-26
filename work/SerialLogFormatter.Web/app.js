import { format } from './formatter.js';

const APP_VERSION = '1.1.0';
const SETTINGS_KEY = 'serial-log-formatter.settings';

const $ = id => document.getElementById(id);
const input = $('input');
const output = $('output');
const status = $('status');
const settings = {
    header: $('header'),
    bytesPerLine: $('bytesPerLine'),
    preserveTimestamps: $('preserveTimestamps'),
    showInfo: $('showInfo'),
    uppercase: $('uppercase')
};

$('version').textContent = `v${APP_VERSION} 网页版`;
loadSettings();

function setStatus(text, kind = '') {
    status.textContent = text;
    status.className = `status ${kind}`;
}

function formatLog() {
    try {
        const result = format(input.value, {
            header: settings.header.value,
            bytesPerLine: Math.max(0, Math.min(256, parseInt(settings.bytesPerLine.value, 10) || 0)),
            preserveTimestamps: settings.preserveTimestamps.checked,
            showInfo: settings.showInfo.checked,
            uppercase: settings.uppercase.checked
        });
        output.value = result.text;
        if (result.frameCount === 0) setStatus('没有找到帧。请检查帧头，或确认输入中包含十六进制数据。', 'error');
        else setStatus(`完成：${result.frameCount} 帧，共 ${result.byteCount} 字节；无法归入帧的数据 ${result.ignoredByteCount} 字节。`, 'ok');
    } catch (ex) {
        setStatus(`设置有误：${ex.message}`, 'error');
        settings.header.focus();
    }
}

async function pasteAndFormat() {
    try {
        input.value = await navigator.clipboard.readText();
        formatLog();
    } catch {
        input.focus();
        input.select();
        setStatus('浏览器不允许读取剪贴板。请在左侧按 Ctrl+V 粘贴，粘贴后会自动整理。', 'error');
    }
}

async function copyResult() {
    if (!output.value) { setStatus('当前没有可复制的结果。', 'error'); return; }
    try {
        await navigator.clipboard.writeText(output.value);
    } catch {
        output.select();
        document.execCommand('copy');
    }
    setStatus('结果已复制到剪贴板。', 'ok');
}

function downloadResult() {
    if (!output.value) { setStatus('当前没有可下载的结果。', 'error'); return; }
    const url = URL.createObjectURL(new Blob([output.value.replace(/\n/g, '\r\n')], { type: 'text/plain;charset=utf-8' }));
    const link = Object.assign(document.createElement('a'), { href: url, download: `formatted-${timestamp()}.txt` });
    link.click();
    URL.revokeObjectURL(url);
}

async function loadFile(file) {
    if (!file) return;
    input.value = await file.text();
    setStatus(`已读取 ${file.name}`);
    formatLog();
}

function timestamp() {
    const d = new Date();
    const pad = n => String(n).padStart(2, '0');
    return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}-${pad(d.getHours())}${pad(d.getMinutes())}${pad(d.getSeconds())}`;
}

// Settings are a per-browser convenience; storage may be unavailable (private mode, blocked site data).
function loadSettings() {
    try {
        const saved = JSON.parse(localStorage.getItem(SETTINGS_KEY) ?? '{}');
        for (const [key, el] of Object.entries(settings)) {
            if (!(key in saved)) continue;
            if (el.type === 'checkbox') el.checked = !!saved[key];
            else el.value = saved[key];
        }
    } catch { /* ignore */ }
}

function saveSettings() {
    try {
        const values = Object.fromEntries(Object.entries(settings)
            .map(([key, el]) => [key, el.type === 'checkbox' ? el.checked : el.value]));
        localStorage.setItem(SETTINGS_KEY, JSON.stringify(values));
    } catch { /* ignore */ }
}

$('format').addEventListener('click', formatLog);
$('paste').addEventListener('click', pasteAndFormat);
$('copy').addEventListener('click', copyResult);
$('download').addEventListener('click', downloadResult);
$('open').addEventListener('click', () => $('file').click());
$('file').addEventListener('change', e => { loadFile(e.target.files[0]); e.target.value = ''; });
$('clear').addEventListener('click', () => {
    input.value = '';
    output.value = '';
    setStatus('已清空');
    input.focus();
});

for (const el of Object.values(settings)) {
    el.addEventListener('change', () => { saveSettings(); if (output.value) formatLog(); });
    if (el.type !== 'checkbox') el.addEventListener('keydown', e => { if (e.key === 'Enter') formatLog(); });
}

// Pasting a whole log into an empty box formats it straight away, like the "粘贴并整理" button.
input.addEventListener('paste', () => {
    const wasEmpty = input.value === '' || input.selectionStart === 0 && input.selectionEnd === input.value.length;
    if (wasEmpty) setTimeout(formatLog);
});

document.addEventListener('keydown', e => {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); formatLog(); }
});

input.addEventListener('dragover', e => { e.preventDefault(); input.classList.add('dragging'); });
input.addEventListener('dragleave', () => input.classList.remove('dragging'));
input.addEventListener('drop', e => {
    e.preventDefault();
    input.classList.remove('dragging');
    loadFile(e.dataTransfer.files[0]);
});

input.select();
input.focus();
