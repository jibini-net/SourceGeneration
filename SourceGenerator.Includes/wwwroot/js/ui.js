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
        case "_!open":
        case "_!close":
            var parens = hyphen[1].split('(', 2);
            return {
                type: hyphen[0].substring(2),
                tag: parens[0],
                indexByTag: +parens[1].substring(0, parens[1].length - 1),
                dependent: true
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
        if (replPointer && replPointer.nextSibling && replPointer.nextSibling.nextSibling) {
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
            if (replPointer && replPointer.nextSibling && replPointer.nextSibling.nextSibling) {
                oldEl.replaceWith(replPointer.cloneNode(true));
            } else {
                oldEl.remove();
            }
        }
    }

    for (el = el.previousSibling;
        replPointer && replPointer.nextSibling && replPointer.nextSibling.nextSibling;
        replPointer = replPointer.nextSibling) {
        el.after(replPointer.cloneNode(true));
        el = el.nextSibling;
    }

    while (replPointer && replPointer.nextSibling) {
        replPointer = replPointer.nextSibling;
    }
    if (replPointer) {
        setState(replPointer);
    }
}

function getState() {
    var htmlElement = $("html")[0];
    var stateComment = htmlElement.nextSibling;
    while (stateComment.nextSibling) {
        stateComment = stateComment.nextSibling;
    }

    var txt = document.createElement("textarea");
    txt.innerHTML = stateComment.textContent;
    return JSON.parse(txt.value);
}

function setState(newComment) {
    var htmlElement = $("html")[0];
    var stateComment = htmlElement.nextSibling;
    while (stateComment.nextSibling) {
        stateComment = stateComment.nextSibling;
    }

    stateComment.replaceWith(newComment.cloneNode(true));
}

var dispatchTail = null;

function dispatch(el, action, args) {
    if (!args) {
        args = { };
    }
    var path = findPath(el);

    var tagRenderRequest = {
        State: undefined,
        Path: path
            .slice(1)
            .map(parseData)
            .map((it) => ({
                Tag: it.tag,
                IndexByTag: it.indexByTag,
                Dependent: it.dependent ? true : undefined
            })),
        Pars: args
    };

    var self = path[path.length - 1];
    var selfData = parseData(self);
    var replaceNode = path.reverse().find((it) => !parseData(it).dependent);

    var nextDispatch = async () => await new Promise((res, rej) => {
        tagRenderRequest.State = getState();
        $.ajax({
            url: `view/${selfData.tag}/${action}`,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(tagRenderRequest),
            dataType: "html",
            success: (it) => {
                replace(replaceNode, it);
                res();
            },
            error: function (xhr, error) {
                alert(`${xhr.status} - ${error}`);
                rej(`${xhr.status} - ${error}`);
            }
        });
    });

    return dispatchTail = (dispatchTail
        ? dispatchTail.then(nextDispatch)
        : nextDispatch());
}
