// Port of LogFormatter in ../SerialLogFormatter/Program.cs. Keep the two implementations in sync.

// Bracketed timestamps are matched first so their original wrapper is retained in the output.
const TIMESTAMP_REGEX = /\[[^\]\r\n]*(?:\d{1,2}:\d{2}:\d{2}|\d{4}[-\/]\d{1,2}[-\/]\d{1,2})[^\]\r\n]*\]|(?<![0-9A-Fa-f])(?:\d{4}[-\/]\d{1,2}[-\/]\d{1,2}[ T])?\d{1,2}:\d{2}:\d{2}(?:[.,]\d{1,6})?/g;
const HEX_TOKEN_REGEX = /(?<![0-9a-f])[0-9a-f]{2}(?![0-9a-f])|(?<![0-9A-Za-z])[0-9A-Fa-f]{6,}(?![0-9A-Za-z])/gi;
const INFO_INDENT = ' '.repeat(18);

/**
 * @param {string} input
 * @param {{header: string, bytesPerLine?: number, preserveTimestamps?: boolean, showInfo?: boolean, uppercase?: boolean, newline?: string}} options
 * @returns {{text: string, frameCount: number, byteCount: number, ignoredByteCount: number}}
 */
export function format(input, {
    header: headerText,
    bytesPerLine = 0,
    preserveTimestamps = true,
    showInfo = true,
    uppercase = true,
    newline = '\n'
}) {
    const header = extractBytes(headerText ?? '');
    if (header.length === 0) throw new Error('帧头不能为空，请输入类似 84 A9 61 的十六进制字节。');

    const source = input ?? '';
    const timestamps = [...source.matchAll(TIMESTAMP_REGEX)]
        .map(match => ({ start: match.index, end: match.index + match[0].length, text: match[0].trim() }));
    const bytes = extractByteTokens(source, timestamps);
    const starts = findFrameStarts(bytes, header);
    if (starts.length === 0) return { text: '', frameCount: 0, byteCount: 0, ignoredByteCount: bytes.length };

    const out = [];
    const width = bytesPerLine > 0 ? bytesPerLine : Infinity;
    let total = 0;
    let timestampIndex = 0;
    for (let frameIndex = 0; frameIndex < starts.length; frameIndex++) {
        const start = starts[frameIndex];
        const end = frameIndex + 1 < starts.length ? starts[frameIndex + 1] : bytes.length;
        const frame = bytes.slice(start, end);
        total += frame.length;

        if (frameIndex > 0) out.push(newline);
        while (timestampIndex < timestamps.length && timestamps[timestampIndex].start <= frame[0].position) {
            if (preserveTimestamps) out.push(timestamps[timestampIndex].text, newline);
            timestampIndex++;
        }
        if (showInfo) out.push(`[${String(frameIndex + 1).padStart(3, '0')}] (${frame.length} bytes) `);

        for (let i = 0; i < frame.length; i++) {
            if (i > 0 && i % width === 0) {
                out.push(newline);
                if (showInfo) out.push(INFO_INDENT);
            } else if (i > 0) out.push(' ');
            const hex = frame[i].value.toString(16).padStart(2, '0');
            out.push(uppercase ? hex.toUpperCase() : hex);
        }
    }

    return { text: out.join(''), frameCount: starts.length, byteCount: total, ignoredByteCount: starts[0] };
}

function extractBytes(text) {
    const result = [];
    for (const match of text.matchAll(HEX_TOKEN_REGEX)) {
        const value = match[0];
        if (value.length % 2 !== 0) continue;
        for (let i = 0; i < value.length; i += 2)
            result.push(parseInt(value.substring(i, i + 2), 16));
    }
    return result;
}

function extractByteTokens(text, timestamps) {
    const result = [];
    let timestampIndex = 0;
    for (const match of text.matchAll(HEX_TOKEN_REGEX)) {
        while (timestampIndex < timestamps.length && timestamps[timestampIndex].end <= match.index)
            timestampIndex++;
        if (timestampIndex < timestamps.length && match.index >= timestamps[timestampIndex].start && match.index < timestamps[timestampIndex].end)
            continue;

        const value = match[0];
        if (value.length % 2 !== 0) continue;
        for (let i = 0; i < value.length; i += 2)
            result.push({ value: parseInt(value.substring(i, i + 2), 16), position: match.index + i });
    }
    return result;
}

function findFrameStarts(data, header) {
    const starts = [];
    for (let i = 0; i <= data.length - header.length; i++) {
        let matches = true;
        for (let j = 0; j < header.length; j++)
            if (data[i + j].value !== header[j]) { matches = false; break; }
        if (!matches) continue;
        starts.push(i);
        i += header.length - 1;
    }
    return starts;
}
