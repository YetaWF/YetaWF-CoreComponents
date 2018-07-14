/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// jquery-free

namespace YetaWF {

    export interface QSEntry {
        key: string;
        keyLower: string;
        value: string;
    }

    /**
     * Url Parsing (simple parsing to replace uri.js, limited to functionality needed by YetaWF)
     * parses https://user:123@sub.domain.com[:80]/path?querystring#hash
     */
    export class Url {

        private Schema: string = '';
        private UserInfo: string = '';
        private Domain: string = '';
        private Path: string[] = [];
        private Hash: string = '';
        private QSEntries: QSEntry[] = [];

        public getSchema(): string {
            return this.Schema;
        }
        public getHostName(): string {
            return this.Domain;
        }
        public getUserInfo(withAt?: boolean): string {
            return this.UserInfo + (withAt && this.UserInfo.length > 0 ? "@" : "");
        }
        public getDomain(): string {
            return encodeURIComponent(this.Domain);
        }
        public getPath(): string {
            var path = '';
            for (let part of this.Path) {
                path += '/' + encodeURIComponent(part);
            }
            return path;
        }
        public getHash(withHash?: boolean): string {
            if (this.Hash.length == 0) return '';
            return (withHash ? "#" : "") + encodeURIComponent(this.Hash);
        }
        public hasSearch(key: string): boolean {
            key = key.toLowerCase();
            for (let entry of this.QSEntries) {
                if (entry.keyLower == key)
                    return true;
            }
            return false;
        }
        public getSearch(key: string): string {
            key = key.toLowerCase();
            for (let entry of this.QSEntries) {
                if (entry.keyLower == key)
                    return entry.value;
            }
            return '';
        }
        public getSearchObject(): object {
            var o: any = {};
            for (let entry of this.QSEntries) {
                o[entry.key] = entry.value;
            }
            return o;
        }
        public setSearchObject(o: object): void {
            this.QSEntries = [];
            for (let prop in o) {
                this.QSEntries.push({ key: prop, keyLower: prop.toLowerCase(), value: o[prop] });
            }
        }
        public addSearch(key: string, value: string): void {
            this.QSEntries.push({ key: key, keyLower: key.toLowerCase(), value: value });
        }
        public removeSearch(key: string): void {
            key = key.toLowerCase();
            for (var i = this.QSEntries.length - 1; i >= 0; --i) {
                var entry = this.QSEntries[i];
                if (entry.keyLower == key)
                    this.QSEntries.splice(i, 1);
            }
        }
        public getQuery(withQuestion?: boolean): string {
            var qs: string = '';
            for (let entry of this.QSEntries) {
                if (qs != '')
                    qs += "&";
                else if (withQuestion)
                    qs += "?";
                qs += encodeURIComponent(entry.key) + "=" + encodeURIComponent(entry.value);
            }
            return qs;
        }
        public toUrl(): string {
            if (this.Schema.length == 0 && this.UserInfo.length == 0 && this.Domain.length == 0)
                return `${this.getPath()}${this.getQuery(true)}${this.getHash(true)}`;
            else
                return `${this.getSchema()}//${this.getUserInfo(true)}${this.getDomain()}${this.getPath()}${this.getQuery(true)}${this.getHash(true)}`;
        }
        public parse(url: string): void {

            this.Schema = '';
            this.UserInfo = '';
            this.Domain = '';
            this.Path = [];
            this.Hash = '';
            this.QSEntries = [];

            // remove hash
            var parts = url.split('#');
            if (parts.length == 0) return;
            url = parts[0];
            if (parts.length > 1)
                this.Hash = decodeURIComponent(parts.slice(1).join());

            // remove qs
            parts = url.split('?');
            url = parts[0];
            var qs = '';
            if (parts.length > 1)
                qs = parts.slice(1).join('?');

            // process path

            // scheme
            parts = url.split('//');
            if (parts.length > 1) {
                this.Schema = parts[0];
                url = parts.slice(1).join('//');
            } else {
                url = parts[0];
            }

            // extract everything left of user info
            parts = url.split('@');
            if (parts.length > 1) {
                this.UserInfo = parts[0]; // do not decode because we're not encoding, changes not supported
                url = parts.slice(1).join('@');
            } else
                url = parts[0];

            parts = url.split('/');
            if (parts.length > 1) {
                this.Domain = decodeURIComponent(parts[0]);
                parts = parts.slice(1);
                for (let i in parts)
                    parts[i] = decodeURIComponent(parts[i]);
                this.Path = parts;
            } else
                this.Path = [''];

            // split up query string
            if (qs.length > 0) {
                var qsArr = qs.split('&');
                for (let qsEntry of qsArr) {
                    var entryParts = qsEntry.split('=');
                    if (entryParts.length > 2)
                        throw `Url has malformed query string entry ${qsEntry}`;
                    var key = decodeURIComponent(entryParts[0]);
                    this.QSEntries.push({ key: key, keyLower: key.toLowerCase(), value: entryParts.length > 1 ? decodeURIComponent(entryParts[1]) : '' });
                }
            }
        }
    }
}