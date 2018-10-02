"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
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
            // tslint:disable-next-line:forin
            for (var prop in o) {
                this.QSEntries.push({ key: prop, keyLower: prop.toLowerCase(), value: o[prop] });
            }
        };
        Url.prototype.addSearch = function (key, value) {
            this.QSEntries.push({ key: key, keyLower: key.toLowerCase(), value: value.toString() });
        };
        Url.prototype.addSearchSimpleObject = function (obj) {
            for (var key in obj) {
                if (obj.hasOwnProperty(key)) {
                    this.addSearch(key, obj[key]);
                }
            }
        };
        Url.prototype.addFormInfo = function (tag, counter) {
            var formInfo = $YetaWF.Forms.getFormInfo(tag, undefined, counter);
            this.addSearch(YConfigs.Forms.RequestVerificationToken, formInfo.RequestVerificationToken);
            this.addSearch(YConfigs.Forms.UniqueIdPrefix, formInfo.UniqueIdPrefix);
            this.addSearch(YConfigs.Basics.ModuleGuid, formInfo.ModuleGuid);
            this.addSearch(YConfigs.Basics.Link_CharInfo, formInfo.CharInfo);
        };
        Url.prototype.removeSearch = function (key) {
            key = key.toLowerCase();
            for (var i = this.QSEntries.length - 1; i >= 0; --i) {
                var entry = this.QSEntries[i];
                if (entry.keyLower === key)
                    this.QSEntries.splice(i, 1);
            }
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
        Url.prototype.toUrl = function () {
            if (this.Schema.length === 0 && this.UserInfo.length === 0 && this.Domain.length === 0)
                return "" + this.getPath() + this.getQuery(true) + this.getHash(true);
            else
                return this.getSchema() + "//" + this.getUserInfo(true) + this.getDomain() + this.getPath() + this.getQuery(true) + this.getHash(true);
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
                        throw "Url has malformed query string entry " + qsEntry;
                    var key = decodeURIComponent(entryParts[0]);
                    this.QSEntries.push({ key: key, keyLower: key.toLowerCase(), value: entryParts.length > 1 ? decodeURIComponent(entryParts[1]) : "" });
                }
            }
        };
        return Url;
    }());
    YetaWF.Url = Url;
})(YetaWF || (YetaWF = {}));
