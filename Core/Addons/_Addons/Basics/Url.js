"use strict";
/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF;
(function (YetaWF) {
    /**
     * Url Parsing (simple parsing to replace uri.js, limited to functionality needed by YetaWF)
     * parses https://user:123@sub.domain.com[:80]/path?querystring#hash
     */
    var Url = /** @class */ (function () {
        function Url() {
            this.Schema = "";
            this.UserInfo = "";
            this.Domain = "";
            this.Port = "";
            this.Path = [];
            this.Hash = "";
            this.QSEntries = [];
        }
        Url.prototype.getSchema = function () {
            return this.Schema;
        };
        Url.prototype.getHostName = function () {
            return this.Domain;
        };
        Url.prototype.getPort = function () {
            return this.Port;
        };
        Url.prototype.getUserInfo = function (withAt) {
            return this.UserInfo + (withAt && this.UserInfo.length > 0 ? "@" : "");
        };
        Url.prototype.getDomain = function () {
            if (this.Port)
                return encodeURIComponent(this.Domain) + ":" + encodeURIComponent(this.Port);
            else
                return encodeURIComponent(this.Domain);
        };
        Url.prototype.getPath = function () {
            var path = "";
            for (var _i = 0, _a = this.Path; _i < _a.length; _i++) {
                var part = _a[_i];
                path += "/" + encodeURIComponent(part);
            }
            return path;
        };
        Url.prototype.getHash = function (withHash) {
            if (this.Hash.length === 0)
                return "";
            return (withHash ? "#" : "") + encodeURIComponent(this.Hash);
        };
        Url.prototype.setHash = function (hash) {
            this.Hash = hash !== null && hash !== void 0 ? hash : "";
        };
        Url.prototype.hasSearch = function (key) {
            key = key.toLowerCase();
            for (var _i = 0, _a = this.QSEntries; _i < _a.length; _i++) {
                var entry = _a[_i];
                if (entry.keyLower === key)
                    return true;
            }
            return false;
        };
        Url.prototype.getSearch = function (key) {
            key = key.toLowerCase();
            for (var _i = 0, _a = this.QSEntries; _i < _a.length; _i++) {
                var entry = _a[_i];
                if (entry.keyLower === key)
                    return entry.value;
            }
            return "";
        };
        Url.prototype.getSearchObject = function () {
            var o = {};
            for (var _i = 0, _a = this.QSEntries; _i < _a.length; _i++) {
                var entry = _a[_i];
                o[entry.key] = entry.value;
            }
            return o;
        };
        Url.prototype.setSearchObject = function (o) {
            this.QSEntries = [];
            // eslint-disable-next-line guard-for-in
            for (var prop in o) {
                this.QSEntries.push({ key: prop, keyLower: prop.toLowerCase(), value: o[prop] });
            }
        };
        Url.prototype.addSearch = function (key, value) {
            this.QSEntries.push({ key: key, keyLower: key.toLowerCase(), value: value == null ? "" : value.toString() });
        };
        Url.prototype.addSearchSimpleObject = function (obj) {
            for (var key in obj) {
                if (obj.hasOwnProperty(key)) {
                    this.addSearch(key, obj[key]);
                }
            }
        };
        Url.prototype.addSearchFromSegments = function (segments) {
            if (segments.startsWith("/")) {
                var parts = segments.split("/");
                if (parts.length > 1) {
                    parts = parts.slice(1);
                    if (parts.length % 2 === 0) {
                        var len = parts.length;
                        for (var i = 0; i < len; i += 2) {
                            this.addSearch(decodeURIComponent(parts[i]), decodeURIComponent(parts[i + 1]));
                        }
                    }
                }
            }
        };
        Url.prototype.addFormInfo = function (tag) {
            var formInfo = $YetaWF.Forms.getFormInfo(tag);
            this.addSearch(YConfigs.Forms.RequestVerificationToken, formInfo.RequestVerificationToken);
            this.addSearch(YConfigs.Basics.ModuleGuid, formInfo.ModuleGuid);
        };
        Url.prototype.removeSearch = function (key) {
            key = key.toLowerCase();
            for (var i = this.QSEntries.length - 1; i >= 0; --i) {
                var entry = this.QSEntries[i];
                if (entry.keyLower === key)
                    this.QSEntries.splice(i, 1);
            }
        };
        Url.prototype.removeAllSearch = function () {
            this.QSEntries = [];
        };
        Url.prototype.getQuery = function (withQuestion) {
            var qs = "";
            for (var _i = 0, _a = this.QSEntries; _i < _a.length; _i++) {
                var entry = _a[_i];
                if (qs !== "")
                    qs += "&";
                else if (withQuestion)
                    qs += "?";
                qs += encodeURIComponent(entry.key) + "=";
                if (entry.value !== null)
                    qs += encodeURIComponent(entry.value);
            }
            return qs;
        };
        Url.prototype.replaceLastPath = function (lastPath) {
            this.Path[this.Path.length - 1] = lastPath;
        };
        Url.prototype.toUrl = function () {
            if (this.Schema.length === 0 && this.UserInfo.length === 0 && this.Domain.length === 0)
                return "".concat(this.getPath()).concat(this.getQuery(true)).concat(this.getHash(true));
            else
                return "".concat(this.getSchema(), "//").concat(this.getUserInfo(true)).concat(this.getDomain()).concat(this.getPath()).concat(this.getQuery(true)).concat(this.getHash(true));
        };
        Url.prototype.toFormData = function () {
            return this.getQuery();
        };
        Url.prototype.parse = function (url) {
            this.Schema = "";
            this.UserInfo = "";
            this.Domain = "";
            this.Port = "";
            this.Path = [];
            this.Hash = "";
            this.QSEntries = [];
            // remove hash
            var parts = url.split("#");
            if (parts.length === 0)
                return;
            url = parts[0];
            if (parts.length > 1)
                this.Hash = decodeURIComponent(parts.slice(1).join());
            // remove qs
            parts = url.split("?");
            url = parts[0];
            var qs = "";
            if (parts.length > 1)
                qs = parts.slice(1).join("?");
            // process path
            // scheme
            parts = url.split("//");
            if (parts.length > 1) {
                this.Schema = parts[0];
                url = parts.slice(1).join("//");
            }
            else {
                url = parts[0];
            }
            // extract everything left of user info
            parts = url.split("@");
            if (parts.length > 1) {
                this.UserInfo = parts[0]; // do not decode because we're not encoding, changes not supported
                url = parts.slice(1).join("@");
            }
            else
                url = parts[0];
            var domain = "";
            parts = url.split("/");
            if (parts.length > 1) {
                domain = parts[0];
                parts = parts.slice(1);
                // eslint-disable-next-line guard-for-in
                for (var i in parts)
                    parts[i] = decodeURIComponent(parts[i]);
                this.Path = parts;
            }
            else
                this.Path = [""];
            if (domain) {
                parts = domain.split(":");
                this.Domain = parts[0];
                if (parts.length > 1)
                    this.Port = parts.slice(1).join("//");
            }
            // split up query string
            if (qs.length > 0) {
                var qsArr = qs.split("&");
                for (var _i = 0, qsArr_1 = qsArr; _i < qsArr_1.length; _i++) {
                    var qsEntry = qsArr_1[_i];
                    var entryParts = qsEntry.split("=");
                    if (entryParts.length > 2)
                        throw "Url has malformed query string entry ".concat(qsEntry);
                    var key = decodeURIComponent(entryParts[0]);
                    this.QSEntries.push({ key: key, keyLower: key.toLowerCase(), value: entryParts.length > 1 ? decodeURIComponent(entryParts[1]) : "" });
                }
            }
        };
        return Url;
    }());
    YetaWF.Url = Url;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=Url.js.map
