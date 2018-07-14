/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
// jquery-free

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

// string compare that considers null == ""
function StringYCompare(str1, str2) { //$$$ this is crap
    if (!str1 && !str2) return true;
    return str1 == str2;
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