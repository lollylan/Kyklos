// Holt Liniensymbole aus Tabler Icons (MIT) oder Lucide (ISC) und gibt sie als Einträge für Icons.Vectors aus.
//
//   node tools/import-icons.mjs tabler:bone=knochen lucide:baby=saeugling ...
//
// Ausgabe: C#-Zeilen `{ "knochen", "M..." },` – SVG-Elemente (path, circle, ellipse, rect, line, polyline, polygon)
// werden in einen einzigen Pfad im 24er-Raster umgerechnet, mit ausgeschriebenen Befehlen, damit WPF ihn sicher liest.

const SOURCES = {
    tabler: n => `https://cdn.jsdelivr.net/npm/@tabler/icons@latest/icons/outline/${n}.svg`,
    lucide: n => `https://cdn.jsdelivr.net/npm/lucide-static@latest/icons/${n}.svg`
};

const ARGS = { m: 2, l: 2, h: 1, v: 1, c: 6, s: 4, q: 4, t: 2, a: 7, z: 0 };

function fmt(x) {
    const s = (Math.round(x * 1000) / 1000).toString();
    return s === '-0' ? '0' : s;
}

// Zerlegt SVG-Pfaddaten nach der SVG-Grammatik (auch "1.5.5", "-1-2" und zusammengezogene Bogen-Flags wie "a1 1 0 011 1").
function normalizePath(d) {
    const out = [];
    let i = 0, cmd = null;
    const skip = () => { while (i < d.length && /[\s,]/.test(d[i])) i++; };
    const num = () => {
        skip();
        const m = /^[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?/.exec(d.slice(i));
        if (!m) throw new Error(`Zahl erwartet bei ${i}: ${d.slice(i, i + 12)}`);
        i += m[0].length;
        return parseFloat(m[0]);
    };
    const flag = () => { skip(); const f = d[i++]; if (f !== '0' && f !== '1') throw new Error('Bogen-Flag erwartet'); return +f; };
    while (true) {
        skip();
        if (i >= d.length) break;
        if (/[a-zA-Z]/.test(d[i])) cmd = d[i++];
        else if (!cmd) throw new Error('Pfad beginnt nicht mit einem Befehl');
        const lower = cmd.toLowerCase();
        if (lower === 'z') { out.push(cmd); cmd = null; continue; }
        const vals = lower === 'a' ? [num(), num(), num(), flag(), flag(), num(), num()] : Array.from({ length: ARGS[lower] }, num);
        // Ein relatives m am Anfang ist in SVG absolut; im zusammengefügten Pfad hinge es sonst am Ende der vorigen Form.
        out.push((!out.length && cmd === 'm' ? 'M' : cmd) + ' ' + vals.map(fmt).join(' '));
        // Weitere Koordinaten nach einem M gelten als L.
        if (cmd === 'M') cmd = 'L'; else if (cmd === 'm') cmd = 'l';
    }
    return out.join(' ');
}

function attrs(tag) {
    const a = {};
    for (const m of tag.matchAll(/([\w-]+)="([^"]*)"/g)) a[m[1]] = m[2];
    return a;
}

function circle(cx, cy, rx, ry) {
    return `M ${fmt(cx + rx)} ${fmt(cy)} A ${fmt(rx)} ${fmt(ry)} 0 1 1 ${fmt(cx - rx)} ${fmt(cy)} A ${fmt(rx)} ${fmt(ry)} 0 1 1 ${fmt(cx + rx)} ${fmt(cy)} Z`;
}

function rect(x, y, w, h, rx, ry) {
    if (!rx && !ry) return `M ${fmt(x)} ${fmt(y)} H ${fmt(x + w)} V ${fmt(y + h)} H ${fmt(x)} Z`;
    rx = Math.min(rx || ry, w / 2); ry = Math.min(ry || rx, h / 2);
    const A = (ex, ey) => `A ${fmt(rx)} ${fmt(ry)} 0 0 1 ${fmt(ex)} ${fmt(ey)}`;
    return [`M ${fmt(x + rx)} ${fmt(y)}`, `H ${fmt(x + w - rx)}`, A(x + w, y + ry), `V ${fmt(y + h - ry)}`, A(x + w - rx, y + h),
            `H ${fmt(x + rx)}`, A(x, y + h - ry), `V ${fmt(y + ry)}`, A(x + rx, y), 'Z'].join(' ');
}

function points(p, close) {
    const n = p.trim().split(/[\s,]+/).map(Number);
    let s = `M ${fmt(n[0])} ${fmt(n[1])}`;
    for (let k = 2; k < n.length; k += 2) s += ` L ${fmt(n[k])} ${fmt(n[k + 1])}`;
    return close ? s + ' Z' : s;
}

function convert(svg, label) {
    const parts = [];
    for (const m of svg.matchAll(/<(path|circle|ellipse|rect|line|polyline|polygon)\b([^>]*)\/?>/g)) {
        const a = attrs(m[2]);
        if (a.stroke === 'none') continue; // Tablers unsichtbarer Rahmen
        if (a.fill && a.fill !== 'none') console.error(`Hinweis: ${label} hat eine gefüllte Form – sie wird nur als Kontur gezeichnet.`);
        const f = k => parseFloat(a[k] || 0);
        switch (m[1]) {
            case 'path': parts.push(normalizePath(a.d)); break;
            case 'circle': parts.push(circle(f('cx'), f('cy'), f('r'), f('r'))); break;
            case 'ellipse': parts.push(circle(f('cx'), f('cy'), f('rx'), f('ry'))); break;
            case 'rect': parts.push(rect(f('x'), f('y'), f('width'), f('height'), f('rx'), f('ry'))); break;
            case 'line': parts.push(`M ${fmt(f('x1'))} ${fmt(f('y1'))} L ${fmt(f('x2'))} ${fmt(f('y2'))}`); break;
            case 'polyline': parts.push(points(a.points, false)); break;
            case 'polygon': parts.push(points(a.points, true)); break;
        }
    }
    if (!parts.length) throw new Error('keine Formen gefunden');
    return parts.join(' ');
}

const specs = process.argv.slice(2);
if (!specs.length) {
    console.error('Aufruf: node tools/import-icons.mjs tabler:<name>=<id> lucide:<name>=<id> ...');
    process.exit(1);
}
let failed = 0;
for (const spec of specs) {
    const m = /^(tabler|lucide):([\w-]+)=(\w+)$/.exec(spec);
    if (!m) { console.error(`Ungültig: ${spec}`); failed++; continue; }
    const [, src, name, id] = m;
    try {
        const res = await fetch(SOURCES[src](name));
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        console.log(`            { "${id}", "${convert(await res.text(), spec)}" }, // ${src}:${name}`);
    } catch (e) {
        console.error(`Fehler bei ${spec}: ${e.message}`);
        failed++;
    }
}
process.exit(failed ? 1 : 0);
