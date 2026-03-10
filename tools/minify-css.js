const fs = require('fs');
const path = require('path');

const cssDir = path.join(__dirname, '..', 'src', 'WebView', 'Css');

function minify(css) {
    // Extract strings and url() values to protect them from mangling
    var placeholders = [];
    var idx = 0;

    // Protect url(...) contents
    css = css.replace(/url\((['"]?)([^)]*?)\1\)/g, function (match) {
        var key = '___PH' + (idx++) + '___';
        placeholders.push({ key: key, value: match });
        return key;
    });

    // Protect quoted strings (content: "..." etc.)
    css = css.replace(/(["'])(?:(?!\1).|\\\1)*\1/g, function (match) {
        var key = '___PH' + (idx++) + '___';
        placeholders.push({ key: key, value: match });
        return key;
    });

    // Remove comments
    css = css.replace(/\/\*[\s\S]*?\*\//g, '');

    // Collapse whitespace
    css = css.replace(/\s+/g, ' ');

    // Remove spaces around structural characters
    css = css.replace(/\s*([{};,>~+])\s*/g, '$1');

    // Remove spaces around colons (selectors & declarations)
    css = css.replace(/\s*:\s*/g, ':');

    // Remove trailing semicolons before closing braces
    css = css.replace(/;}/g, '}');

    // Restore protected values
    for (var i = placeholders.length - 1; i >= 0; i--) {
        css = css.replace(placeholders[i].key, placeholders[i].value);
    }

    return css.trim();
}

var files = fs.readdirSync(cssDir)
    .filter(function (f) { return f.endsWith('.css') && !f.endsWith('.min.css'); });

var changed = 0;
files.forEach(function (f) {
    var src = fs.readFileSync(path.join(cssDir, f), 'utf8');
    var minName = f.replace('.css', '.min.css');
    var minPath = path.join(cssDir, minName);
    var minified = minify(src);

    var existing = fs.existsSync(minPath) ? fs.readFileSync(minPath, 'utf8') : '';
    if (existing !== minified) {
        fs.writeFileSync(minPath, minified);
        console.log('  ' + f + ' -> ' + minName + ' (' + src.length + ' -> ' + minified.length + ')');
        changed++;
    }
});

if (changed === 0) {
    console.log('  CSS files already up to date.');
}
