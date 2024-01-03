function parseData(el) {
    if (!el.textContent) {
        return undefined;
    }
    var hyphen = el.textContent.split('-', 2);
    switch (hyphen[0]) {
        case "_open":
        case "_close":
            var parens = hyphen[1].split('(', 2);
            return {
                type: hyphen[0].substring(1),
                tag: parens[0],
                indexByTag: +parens[1].substring(0, parens[1].length - 1)
            };
            break;
        default:
            return undefined;
    }
}

function findPath(el) {
    var depth = 0;
    while (el.previousSibling) {
        el = el.previousSibling;
        if (el.nodeType == 8) {
            var data = parseData(el);
            if (!data) {
                continue;
            }

            switch (data.type) {
                case "open":
                    if (depth-- == 0) {
                        return findPath(el).concat([el]);
                    }
                    break;

                case "close":
                    depth++;
                    break;
            }
        }
    }
    return el.parentElement
        ? findPath(el.parentElement)
        : [];
}

function replace(startEl, html) {
    var parse = Range.prototype.createContextualFragment.bind(document.createRange());
    var doc = parse(html);
    var replPointer = doc.firstChild;

    var startData = parseData(startEl);
    var el = startEl.nextSibling;

    for (var depth = 0; depth >= 0;) {
        if (replPointer) {
            replPointer = replPointer.nextSibling;
        }

        var data = parseData(el);
        if (data && data.tag == startData.tag && data.indexByTag == startData.indexByTag) {
            switch (data.type) {
                case "open":
                    depth++;
                    break;

                case "close":
                    depth--;
                    break;
            }
        }

        if (depth >= 0) {
            var oldEl = el;
            el = el.nextSibling
            if (replPointer && replPointer.nextSibling) {
                oldEl.replaceWith(replPointer.cloneNode(true));
            } else {
                oldEl.remove();
            }
        }
    }

    for (el = el.previousSibling; replPointer && replPointer.nextSibling; replPointer = replPointer.nextSibling) {
        el.after(replPointer.cloneNode(true));
        el = el.nextSibling;
    }
}

function test(el) {
    var path = findPath(el);
    replace(path[path.length - 1], "<!-- --><h1>Hello, world!</h1><!-- -->");
}
