// Mirrors ../SerialLogFormatter.Tests/Program.cs. Run with: node --test work/SerialLogFormatter.Web.Tests
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { format } from '../SerialLogFormatter.Web/formatter.js';

const cases = [
    ['时间戳与空格', '[10:21:35.126] TX: 84 A9 61 01 02 03 04 [10:21:35.130] RX: 84 A9 61 81 00 FF', '84 A9 61', 2, 13],
    ['连续HEX', '84A9610102030484A9618100FF', '84A961', 2, 13],
    ['逗号分隔', 'noise,84,A9,61,AA,BB,84,A9,61,CC', '84 A9 61', 2, 9]
];

for (const [name, input, header, frames, bytes] of cases) {
    test(name, () => {
        const result = format(input, { header });
        assert.equal(result.frameCount, frames);
        assert.equal(result.byteCount, bytes);
    });
}

test('保留时间戳并单独成行', () => {
    const result = format('[10:21:35.126] TX: 84 A9 61 01 02 [2026-07-13 10:21:36.250] RX: 84 A9 61 03', { header: '84 A9 61' });
    assert.equal(result.text, '[10:21:35.126]\n[001] (5 bytes) 84 A9 61 01 02\n[2026-07-13 10:21:36.250]\n[002] (4 bytes) 84 A9 61 03');
});

test('隐藏时间戳且不混入 HEX', () => {
    const result = format('10:21:35.126 84 A9 61 01', { header: '84 A9 61', preserveTimestamps: false, showInfo: false });
    assert.equal(result.text, '84 A9 61 01');
    assert.equal(result.byteCount, 4);
});

test('每 N 字节换行并对齐', () => {
    const result = format('84 A9 61 01 02 03 04', { header: '84 A9 61', bytesPerLine: 4, uppercase: false });
    assert.equal(result.text, '[001] (7 bytes) 84 a9 61 01\n' + ' '.repeat(18) + '02 03 04');
});

test('帧头为空时报错', () => {
    assert.throws(() => format('84 A9 61', { header: 'xyz' }), /帧头不能为空/);
});

test('帧头前的数据计为无法归入帧', () => {
    const result = format('AA BB 84 A9 61 01', { header: '84 A9 61' });
    assert.equal(result.ignoredByteCount, 2);
    assert.equal(format('AA BB', { header: '84 A9 61' }).ignoredByteCount, 2);
});
