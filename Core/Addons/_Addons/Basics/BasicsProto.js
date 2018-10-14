/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// string.startsWith
if (typeof String.prototype.startsWith != 'function') {
    String.prototype.startsWith = function (str) {
        return this.indexOf(str) == 0;
    };
}
// string endsWith
if (typeof String.prototype.endsWith != 'function') {
    String.prototype.endsWith = function (str) {
        return this.indexOf(str) == this.length - str.length;
    };
}

// string.isValidInt - check for valid int
String.prototype.isValidInt = function (start, end) { // http://stackoverflow.com/questions/10834796/validate-that-a-string-is-a-positive-integer
    var n = ~~Number(this);
    return String(n) == this && n >= start && (end == undefined || n <= end);
}

// String.format
String.prototype.format = function () {
    var args = arguments;
    return this.replace(/{(\d+)}/g, function (match, number) {
        return typeof args[number] != 'undefined'
            ? args[number]
            : match
        ;
    });
};

// Element.prototype.matches Polyfill
if (!Element.prototype.matches) {
    Element.prototype.matches =
        Element.prototype.matchesSelector ||
        Element.prototype.mozMatchesSelector ||
        Element.prototype.msMatchesSelector ||
        Element.prototype.oMatchesSelector ||
        Element.prototype.webkitMatchesSelector ||
        function (s) {
            var matches = (this.document || this.ownerDocument).querySelectorAll(s),
                i = matches.length;
            while (--i >= 0 && matches.item(i) !== this) { }
            return i > -1;
        };
}

// from:https://github.com/jserz/js_piece/blob/master/DOM/ChildNode/remove()/remove().md
(function (arr) {
    arr.forEach(function (item) {
        if (item.hasOwnProperty('remove')) {
            return;
        }
        Object.defineProperty(item, 'remove', {
            configurable: true,
            enumerable: true,
            writable: true,
            value: function remove() {
                if (this.parentNode !== null)
                    this.parentNode.removeChild(this);
            }
        });
    });
})([Element.prototype, CharacterData.prototype, DocumentType.prototype]);

if (!Number.MAX_SAFE_INTEGER) {
    Number.MAX_SAFE_INTEGER = Math.pow(2, 53) - 1; // 9007199254740991
}
if (!Number.MIN_SAFE_INTEGER) {
    Number.MIN_SAFE_INTEGER = - Math.pow(2, 53) - 1; // -9007199254740991
}