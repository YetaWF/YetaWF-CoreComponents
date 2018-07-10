/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

        private Scheme: string = '';
        private UserInfo: string = '';
        private Domain: string = '';
        private Path: string = '';
        private Hash: string = '';
        private QSEntries: QSEntry[] = [];

        public getScheme(): string {
            return this.Scheme;
        }
        public getHostName(): string {
            return this.Domain;
        }
        public getUserInfo(withAt?: boolean): string {
            return this.UserInfo + (withAt && this.UserInfo.length > 0 ? "@" : "");
        }
        public getDomain(): string {
            return this.Domain;
        }
        public getPath(): string {
            return this.Path;
        }
        public getHash(withHash?:boolean): string {
            return (withHash && this.Hash.length > 0 ? "#" : "") + this.Hash;
        }
        public hasSearch(key: string): boolean {
            key = key.toLowerCase();
            for (var entry of this.QSEntries) {
                if (entry.keyLower == key)
                    return true;
            }
            return false;
        }
        public getSearch(key: string): string {
            key = key.toLowerCase();
            for (var entry of this.QSEntries) {
                if (entry.keyLower == key)
                    return entry.value;
            }
            return '';
        }
        public getSearchObject(): object {
            var o: any = {};
            for (var entry of this.QSEntries) {
                o[entry.key] = entry.value;
            }
            return o;
        }
        public setSearchObject(o: object): void {
            this.QSEntries = [];
            for (var prop in o) {
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
            for (var entry of this.QSEntries) {
                if (qs != '')
                    qs += "&";
                else if (withQuestion)
                    qs += "?";
                qs += entry.key + "=" + entry.value;
            }
            return qs;
        }
        public toUrl() {
            return `${this.getScheme()}//${this.getUserInfo(true)}${this.getDomain()}${this.getPath()}${this.getQuery(true)}${this.getHash(true)}`;
        }
        public parse(url: string): void {

            this.Scheme = '';
            this.UserInfo = '';
            this.Domain = '';
            this.Path = '';
            this.Hash = '';
            this.QSEntries = [];

            // remove hash
            var parts = url.split('#');
            if (parts.length == 0) return;
            url = parts[0];
            if (parts.length > 1)
                this.Hash = parts.slice(1).join();

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
                this.Scheme = parts[0];
                url = parts.slice(1).join('//');
            } else {
                url = parts[0];
            }

            // extract everything left of user info
            parts = url.split('@');
            if (parts.length > 1) {
                this.UserInfo = parts[0];
                url = parts.slice(1).join('@');
            } else
                url = parts[0];

            parts = url.split('/');
            if (parts.length > 1) {
                this.Domain = parts[0];
                this.Path = "/" + parts.slice(1).join('/');
            } else
                this.Path = "/";

            // split up query string
            if (qs.length > 0) {
                var qsArr = qs.split('&');
                for (var qsEntry of qsArr) {
                    var entryParts = qsEntry.split('=');
                    if (entryParts.length > 2)
                        throw `Url has malformed query string entry ${qsEntry}`;
                    this.QSEntries.push({ key: entryParts[0], keyLower: entryParts[0].toLowerCase(), value: entryParts.length > 1 ? entryParts[1] : '' });
                }
            }
        }
    }
}