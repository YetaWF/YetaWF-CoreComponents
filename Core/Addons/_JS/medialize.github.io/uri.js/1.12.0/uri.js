﻿/*! URI.js v1.12.0 http://medialize.github.com/URI.js/ */
/* build contains: URI.js, jquery.URI.js */
(function (h, k) { "object" === typeof exports ? module.exports = k(require("./punycode"), require("./IPv6"), require("./SecondLevelDomains")) : "function" === typeof define && define.amd ? define(["./punycode", "./IPv6", "./SecondLevelDomains"], k) : h.URI = k(h.punycode, h.IPv6, h.SecondLevelDomains, h) })(this, function (h, k, q, u) {
    function d(a, b) { if (!(this instanceof d)) return new d(a, b); void 0 === a && (a = "undefined" !== typeof location ? location.href + "" : ""); this.href(a); return void 0 !== b ? this.absoluteTo(b) : this } function r(a) {
        return a.replace(/([.*+?^=!:${}()|[\]\/\\])/g,
        "\\$1")
    } function w(a) { return void 0 === a ? "Undefined" : String(Object.prototype.toString.call(a)).slice(8, -1) } function m(a) { return "Array" === w(a) } function x(a, b) { var c, d; if (m(b)) { c = 0; for (d = b.length; c < d; c++) if (!x(a, b[c])) return !1; return !0 } var e = w(b); c = 0; for (d = a.length; c < d; c++) if ("RegExp" === e) { if ("string" === typeof a[c] && a[c].match(b)) return !0 } else if (a[c] === b) return !0; return !1 } function y(a, b) {
        if (!m(a) || !m(b) || a.length !== b.length) return !1; a.sort(); b.sort(); for (var c = 0, d = a.length; c < d; c++) if (a[c] !== b[c]) return !1;
        return !0
    } function z(a) { return escape(a) } function g(a) { return encodeURIComponent(a).replace(/[!'()*]/g, z).replace(/\*/g, "%2A") } var f = u && u.URI; d.version = "1.12.0"; var e = d.prototype, s = Object.prototype.hasOwnProperty; d._parts = function () { return { protocol: null, username: null, password: null, hostname: null, urn: null, port: null, path: null, query: null, fragment: null, duplicateQueryParameters: d.duplicateQueryParameters, escapeQuerySpace: d.escapeQuerySpace } }; d.duplicateQueryParameters = !1; d.escapeQuerySpace = !0; d.protocol_expression =
    /^[a-z][a-z0-9.+-]*$/i; d.idn_expression = /[^a-z0-9\.-]/i; d.punycode_expression = /(xn--)/i; d.ip4_expression = /^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$/; d.ip6_expression = /^\s*((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:)))(%.+)?\s*$/;
    d.find_uri_expression = /\b((?:[a-z][\w-]+:(?:\/{1,3}|[a-z0-9%])|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}\/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s`!()\[\]{};:'".,<>?\u00ab\u00bb\u201c\u201d\u2018\u2019]))/ig; d.findUri = { start: /\b(?:([a-z][a-z0-9.+-]*:\/\/)|www\.)/gi, end: /[\s\r\n]|$/, trim: /[`!()\[\]{};:'".,<>?\u00ab\u00bb\u201c\u201d\u201e\u2018\u2019]+$/ }; d.defaultPorts = { http: "80", https: "443", ftp: "21", gopher: "70", ws: "80", wss: "443" }; d.invalid_hostname_characters =
    /[^a-zA-Z0-9\.-]/; d.domAttributes = { a: "href", blockquote: "cite", link: "href", base: "href", script: "src", form: "action", img: "src", area: "href", iframe: "src", embed: "src", source: "src", track: "src", input: "src" }; d.getDomAttribute = function (a) { if (a && a.nodeName) { var b = a.nodeName.toLowerCase(); return "input" === b && "image" !== a.type ? void 0 : d.domAttributes[b] } }; d.encode = g; d.decode = decodeURIComponent; d.iso8859 = function () { d.encode = escape; d.decode = unescape }; d.unicode = function () { d.encode = g; d.decode = decodeURIComponent }; d.characters =
    { pathname: { encode: { expression: /%(24|26|2B|2C|3B|3D|3A|40)/ig, map: { "%24": "$", "%26": "&", "%2B": "+", "%2C": ",", "%3B": ";", "%3D": "=", "%3A": ":", "%40": "@" } }, decode: { expression: /[\/\?#]/g, map: { "/": "%2F", "?": "%3F", "#": "%23" } } }, reserved: { encode: { expression: /%(21|23|24|26|27|28|29|2A|2B|2C|2F|3A|3B|3D|3F|40|5B|5D)/ig, map: { "%3A": ":", "%2F": "/", "%3F": "?", "%23": "#", "%5B": "[", "%5D": "]", "%40": "@", "%21": "!", "%24": "$", "%26": "&", "%27": "'", "%28": "(", "%29": ")", "%2A": "*", "%2B": "+", "%2C": ",", "%3B": ";", "%3D": "=" } } } }; d.encodeQuery =
    function (a, b) { var c = d.encode(a + ""); return b ? c.replace(/%20/g, "+") : c }; d.decodeQuery = function (a, b) { a += ""; try { return d.decode(b ? a.replace(/\+/g, "%20") : a) } catch (c) { return a } }; d.recodePath = function (a) { a = (a + "").split("/"); for (var b = 0, c = a.length; b < c; b++) a[b] = d.encodePathSegment(d.decode(a[b])); return a.join("/") }; d.decodePath = function (a) { a = (a + "").split("/"); for (var b = 0, c = a.length; b < c; b++) a[b] = d.decodePathSegment(a[b]); return a.join("/") }; var n = { encode: "encode", decode: "decode" }, p, t = function (a, b) {
        return function (c) {
            return d[b](c +
            "").replace(d.characters[a][b].expression, function (c) { return d.characters[a][b].map[c] })
        }
    }; for (p in n) d[p + "PathSegment"] = t("pathname", n[p]); d.encodeReserved = t("reserved", "encode"); d.parse = function (a, b) {
        var c; b || (b = {}); c = a.indexOf("#"); -1 < c && (b.fragment = a.substring(c + 1) || null, a = a.substring(0, c)); c = a.indexOf("?"); -1 < c && (b.query = a.substring(c + 1) || null, a = a.substring(0, c)); "//" === a.substring(0, 2) ? (b.protocol = null, a = a.substring(2), a = d.parseAuthority(a, b)) : (c = a.indexOf(":"), -1 < c && (b.protocol = a.substring(0,
        c) || null, b.protocol && !b.protocol.match(d.protocol_expression) ? b.protocol = void 0 : "file" === b.protocol ? a = a.substring(c + 3) : "//" === a.substring(c + 1, c + 3) ? (a = a.substring(c + 3), a = d.parseAuthority(a, b)) : (a = a.substring(c + 1), b.urn = !0))); b.path = a; return b
    }; d.parseHost = function (a, b) {
        var c = a.indexOf("/"), d; -1 === c && (c = a.length); "[" === a.charAt(0) ? (d = a.indexOf("]"), b.hostname = a.substring(1, d) || null, b.port = a.substring(d + 2, c) || null) : a.indexOf(":") !== a.lastIndexOf(":") ? (b.hostname = a.substring(0, c) || null, b.port = null) : (d =
        a.substring(0, c).split(":"), b.hostname = d[0] || null, b.port = d[1] || null); b.hostname && "/" !== a.substring(c).charAt(0) && (c++, a = "/" + a); return a.substring(c) || "/"
    }; d.parseAuthority = function (a, b) { a = d.parseUserinfo(a, b); return d.parseHost(a, b) }; d.parseUserinfo = function (a, b) {
        var c = a.indexOf("/"), l = -1 < c ? a.lastIndexOf("@", c) : a.indexOf("@"); -1 < l && (-1 === c || l < c) ? (c = a.substring(0, l).split(":"), b.username = c[0] ? d.decode(c[0]) : null, c.shift(), b.password = c[0] ? d.decode(c.join(":")) : null, a = a.substring(l + 1)) : (b.username = null,
        b.password = null); return a
    }; d.parseQuery = function (a, b) { if (!a) return {}; a = a.replace(/&+/g, "&").replace(/^\?*&*|&+$/g, ""); if (!a) return {}; for (var c = {}, l = a.split("&"), e = l.length, v, g, f = 0; f < e; f++) v = l[f].split("="), g = d.decodeQuery(v.shift(), b), v = v.length ? d.decodeQuery(v.join("="), b) : null, c[g] ? ("string" === typeof c[g] && (c[g] = [c[g]]), c[g].push(v)) : c[g] = v; return c }; d.build = function (a) {
        var b = ""; a.protocol && (b += a.protocol + ":"); a.urn || !b && !a.hostname || (b += "//"); b += d.buildAuthority(a) || ""; "string" === typeof a.path &&
        ("/" !== a.path.charAt(0) && "string" === typeof a.hostname && (b += "/"), b += a.path); "string" === typeof a.query && a.query && (b += "?" + a.query); "string" === typeof a.fragment && a.fragment && (b += "#" + a.fragment); return b
    }; d.buildHost = function (a) { var b = ""; if (a.hostname) d.ip6_expression.test(a.hostname) ? b = a.port ? b + ("[" + a.hostname + "]:" + a.port) : b + a.hostname : (b += a.hostname, a.port && (b += ":" + a.port)); else return ""; return b }; d.buildAuthority = function (a) { return d.buildUserinfo(a) + d.buildHost(a) }; d.buildUserinfo = function (a) {
        var b =
        ""; a.username && (b += d.encode(a.username), a.password && (b += ":" + d.encode(a.password)), b += "@"); return b
    }; d.buildQuery = function (a, b, c) { var l = "", e, g, f, h; for (g in a) if (s.call(a, g) && g) if (m(a[g])) for (e = {}, f = 0, h = a[g].length; f < h; f++) void 0 !== a[g][f] && void 0 === e[a[g][f] + ""] && (l += "&" + d.buildQueryParameter(g, a[g][f], c), !0 !== b && (e[a[g][f] + ""] = !0)); else void 0 !== a[g] && (l += "&" + d.buildQueryParameter(g, a[g], c)); return l.substring(1) }; d.buildQueryParameter = function (a, b, c) {
        return d.encodeQuery(a, c) + (null !== b ? "=" + d.encodeQuery(b,
        c) : "")
    }; d.addQuery = function (a, b, c) { if ("object" === typeof b) for (var l in b) s.call(b, l) && d.addQuery(a, l, b[l]); else if ("string" === typeof b) void 0 === a[b] ? a[b] = c : ("string" === typeof a[b] && (a[b] = [a[b]]), m(c) || (c = [c]), a[b] = a[b].concat(c)); else throw new TypeError("URI.addQuery() accepts an object, string as the name parameter"); }; d.removeQuery = function (a, b, c) {
        var l; if (m(b)) for (c = 0, l = b.length; c < l; c++) a[b[c]] = void 0; else if ("object" === typeof b) for (l in b) s.call(b, l) && d.removeQuery(a, l, b[l]); else if ("string" ===
        typeof b) if (void 0 !== c) if (a[b] === c) a[b] = void 0; else { if (m(a[b])) { l = a[b]; var e = {}, g, f; if (m(c)) for (g = 0, f = c.length; g < f; g++) e[c[g]] = !0; else e[c] = !0; g = 0; for (f = l.length; g < f; g++) void 0 !== e[l[g]] && (l.splice(g, 1), f--, g--); a[b] = l } } else a[b] = void 0; else throw new TypeError("URI.addQuery() accepts an object, string as the first parameter");
    }; d.hasQuery = function (a, b, c, l) {
        if ("object" === typeof b) { for (var e in b) if (s.call(b, e) && !d.hasQuery(a, e, b[e])) return !1; return !0 } if ("string" !== typeof b) throw new TypeError("URI.hasQuery() accepts an object, string as the name parameter");
        switch (w(c)) { case "Undefined": return b in a; case "Boolean": return a = Boolean(m(a[b]) ? a[b].length : a[b]), c === a; case "Function": return !!c(a[b], b, a); case "Array": return m(a[b]) ? (l ? x : y)(a[b], c) : !1; case "RegExp": return m(a[b]) ? l ? x(a[b], c) : !1 : Boolean(a[b] && a[b].match(c)); case "Number": c = String(c); case "String": return m(a[b]) ? l ? x(a[b], c) : !1 : a[b] === c; default: throw new TypeError("URI.hasQuery() accepts undefined, boolean, string, number, RegExp, Function as the value parameter"); }
    }; d.commonPath = function (a, b) {
        var c =
        Math.min(a.length, b.length), d; for (d = 0; d < c; d++) if (a.charAt(d) !== b.charAt(d)) { d--; break } if (1 > d) return a.charAt(0) === b.charAt(0) && "/" === a.charAt(0) ? "/" : ""; if ("/" !== a.charAt(d) || "/" !== b.charAt(d)) d = a.substring(0, d).lastIndexOf("/"); return a.substring(0, d + 1)
    }; d.withinString = function (a, b, c) {
        c || (c = {}); var l = c.start || d.findUri.start, e = c.end || d.findUri.end, g = c.trim || d.findUri.trim, f = /[a-z0-9-]=["']?$/i; for (l.lastIndex = 0; ;) {
            var h = l.exec(a); if (!h) break; h = h.index; if (c.ignoreHtml) {
                var k = a.slice(Math.max(h - 3, 0),
                h); if (k && f.test(k)) continue
            } var k = h + a.slice(h).search(e), m = a.slice(h, k).replace(g, ""); c.ignore && c.ignore.test(m) || (k = h + m.length, m = b(m, h, k, a), a = a.slice(0, h) + m + a.slice(k), l.lastIndex = h + m.length)
        } l.lastIndex = 0; return a
    }; d.ensureValidHostname = function (a) {
        if (a.match(d.invalid_hostname_characters)) {
            if (!h) throw new TypeError("Hostname '" + a + "' contains characters other than [A-Z0-9.-] and Punycode.js is not available"); if (h.toASCII(a).match(d.invalid_hostname_characters)) throw new TypeError("Hostname '" +
            a + "' contains characters other than [A-Z0-9.-]");
        }
    }; d.noConflict = function (a) { if (a) return a = { URI: this.noConflict() }, URITemplate && "function" == typeof URITemplate.noConflict && (a.URITemplate = URITemplate.noConflict()), k && "function" == typeof k.noConflict && (a.IPv6 = k.noConflict()), SecondLevelDomains && "function" == typeof SecondLevelDomains.noConflict && (a.SecondLevelDomains = SecondLevelDomains.noConflict()), a; u.URI === this && (u.URI = f); return this }; e.build = function (a) {
        if (!0 === a) this._deferred_build = !0; else if (void 0 ===
        a || this._deferred_build) this._string = d.build(this._parts), this._deferred_build = !1; return this
    }; e.clone = function () { return new d(this) }; e.valueOf = e.toString = function () { return this.build(!1)._string }; n = { protocol: "protocol", username: "username", password: "password", hostname: "hostname", port: "port" }; t = function (a) { return function (b, c) { if (void 0 === b) return this._parts[a] || ""; this._parts[a] = b || null; this.build(!c); return this } }; for (p in n) e[p] = t(n[p]); n = { query: "?", fragment: "#" }; t = function (a, b) {
        return function (c,
        d) { if (void 0 === c) return this._parts[a] || ""; null !== c && (c += "", c.charAt(0) === b && (c = c.substring(1))); this._parts[a] = c; this.build(!d); return this }
    }; for (p in n) e[p] = t(p, n[p]); n = { search: ["?", "query"], hash: ["#", "fragment"] }; t = function (a, b) { return function (c, d) { var e = this[a](c, d); return "string" === typeof e && e.length ? b + e : e } }; for (p in n) e[p] = t(n[p][1], n[p][0]); e.pathname = function (a, b) {
        if (void 0 === a || !0 === a) { var c = this._parts.path || (this._parts.hostname ? "/" : ""); return a ? d.decodePath(c) : c } this._parts.path = a ? d.recodePath(a) :
        "/"; this.build(!b); return this
    }; e.path = e.pathname; e.href = function (a, b) {
        var c; if (void 0 === a) return this.toString(); this._string = ""; this._parts = d._parts(); var e = a instanceof d, g = "object" === typeof a && (a.hostname || a.path || a.pathname); a.nodeName && (g = d.getDomAttribute(a), a = a[g] || "", g = !1); !e && g && void 0 !== a.pathname && (a = a.toString()); if ("string" === typeof a) this._parts = d.parse(a, this._parts); else if (e || g) for (c in e = e ? a._parts : a, e) s.call(this._parts, c) && (this._parts[c] = e[c]); else throw new TypeError("invalid input");
        this.build(!b); return this
    }; e.is = function (a) {
        var b = !1, c = !1, e = !1, g = !1, f = !1, h = !1, k = !1, m = !this._parts.urn; this._parts.hostname && (m = !1, c = d.ip4_expression.test(this._parts.hostname), e = d.ip6_expression.test(this._parts.hostname), b = c || e, f = (g = !b) && q && q.has(this._parts.hostname), h = g && d.idn_expression.test(this._parts.hostname), k = g && d.punycode_expression.test(this._parts.hostname)); switch (a.toLowerCase()) {
            case "relative": return m; case "absolute": return !m; case "domain": case "name": return g; case "sld": return f;
            case "ip": return b; case "ip4": case "ipv4": case "inet4": return c; case "ip6": case "ipv6": case "inet6": return e; case "idn": return h; case "url": return !this._parts.urn; case "urn": return !!this._parts.urn; case "punycode": return k
        } return null
    }; var A = e.protocol, B = e.port, C = e.hostname; e.protocol = function (a, b) {
        if (void 0 !== a && a && (a = a.replace(/:(\/\/)?$/, ""), !a.match(d.protocol_expression))) throw new TypeError("Protocol '" + a + "' contains characters other than [A-Z0-9.+-] or doesn't start with [A-Z]"); return A.call(this,
        a, b)
    }; e.scheme = e.protocol; e.port = function (a, b) { if (this._parts.urn) return void 0 === a ? "" : this; if (void 0 !== a && (0 === a && (a = null), a && (a += "", ":" === a.charAt(0) && (a = a.substring(1)), a.match(/[^0-9]/)))) throw new TypeError("Port '" + a + "' contains characters other than [0-9]"); return B.call(this, a, b) }; e.hostname = function (a, b) { if (this._parts.urn) return void 0 === a ? "" : this; if (void 0 !== a) { var c = {}; d.parseHost(a, c); a = c.hostname } return C.call(this, a, b) }; e.host = function (a, b) {
        if (this._parts.urn) return void 0 === a ? "" : this;
        if (void 0 === a) return this._parts.hostname ? d.buildHost(this._parts) : ""; d.parseHost(a, this._parts); this.build(!b); return this
    }; e.authority = function (a, b) { if (this._parts.urn) return void 0 === a ? "" : this; if (void 0 === a) return this._parts.hostname ? d.buildAuthority(this._parts) : ""; d.parseAuthority(a, this._parts); this.build(!b); return this }; e.userinfo = function (a, b) {
        if (this._parts.urn) return void 0 === a ? "" : this; if (void 0 === a) {
            if (!this._parts.username) return ""; var c = d.buildUserinfo(this._parts); return c.substring(0,
            c.length - 1)
        } "@" !== a[a.length - 1] && (a += "@"); d.parseUserinfo(a, this._parts); this.build(!b); return this
    }; e.resource = function (a, b) { var c; if (void 0 === a) return this.path() + this.search() + this.hash(); c = d.parse(a); this._parts.path = c.path; this._parts.query = c.query; this._parts.fragment = c.fragment; this.build(!b); return this }; e.subdomain = function (a, b) {
        if (this._parts.urn) return void 0 === a ? "" : this; if (void 0 === a) {
            if (!this._parts.hostname || this.is("IP")) return ""; var c = this._parts.hostname.length - this.domain().length -
            1; return this._parts.hostname.substring(0, c) || ""
        } c = this._parts.hostname.length - this.domain().length; c = this._parts.hostname.substring(0, c); c = RegExp("^" + r(c)); a && "." !== a.charAt(a.length - 1) && (a += "."); a && d.ensureValidHostname(a); this._parts.hostname = this._parts.hostname.replace(c, a); this.build(!b); return this
    }; e.domain = function (a, b) {
        if (this._parts.urn) return void 0 === a ? "" : this; "boolean" === typeof a && (b = a, a = void 0); if (void 0 === a) {
            if (!this._parts.hostname || this.is("IP")) return ""; var c = this._parts.hostname.match(/\./g);
            if (c && 2 > c.length) return this._parts.hostname; c = this._parts.hostname.length - this.tld(b).length - 1; c = this._parts.hostname.lastIndexOf(".", c - 1) + 1; return this._parts.hostname.substring(c) || ""
        } if (!a) throw new TypeError("cannot set domain empty"); d.ensureValidHostname(a); !this._parts.hostname || this.is("IP") ? this._parts.hostname = a : (c = RegExp(r(this.domain()) + "$"), this._parts.hostname = this._parts.hostname.replace(c, a)); this.build(!b); return this
    }; e.tld = function (a, b) {
        if (this._parts.urn) return void 0 === a ? "" :
        this; "boolean" === typeof a && (b = a, a = void 0); if (void 0 === a) { if (!this._parts.hostname || this.is("IP")) return ""; var c = this._parts.hostname.lastIndexOf("."), c = this._parts.hostname.substring(c + 1); return !0 !== b && q && q.list[c.toLowerCase()] ? q.get(this._parts.hostname) || c : c } if (a) if (a.match(/[^a-zA-Z0-9-]/)) if (q && q.is(a)) c = RegExp(r(this.tld()) + "$"), this._parts.hostname = this._parts.hostname.replace(c, a); else throw new TypeError("TLD '" + a + "' contains characters other than [A-Z0-9]"); else {
            if (!this._parts.hostname ||
            this.is("IP")) throw new ReferenceError("cannot set TLD on non-domain host"); c = RegExp(r(this.tld()) + "$"); this._parts.hostname = this._parts.hostname.replace(c, a)
        } else throw new TypeError("cannot set TLD empty"); this.build(!b); return this
    }; e.directory = function (a, b) {
        if (this._parts.urn) return void 0 === a ? "" : this; if (void 0 === a || !0 === a) {
            if (!this._parts.path && !this._parts.hostname) return ""; if ("/" === this._parts.path) return "/"; var c = this._parts.path.length - this.filename().length - 1, c = this._parts.path.substring(0,
            c) || (this._parts.hostname ? "/" : ""); return a ? d.decodePath(c) : c
        } c = this._parts.path.length - this.filename().length; c = this._parts.path.substring(0, c); c = RegExp("^" + r(c)); this.is("relative") || (a || (a = "/"), "/" !== a.charAt(0) && (a = "/" + a)); a && "/" !== a.charAt(a.length - 1) && (a += "/"); a = d.recodePath(a); this._parts.path = this._parts.path.replace(c, a); this.build(!b); return this
    }; e.filename = function (a, b) {
        if (this._parts.urn) return void 0 === a ? "" : this; if (void 0 === a || !0 === a) {
            if (!this._parts.path || "/" === this._parts.path) return "";
            var c = this._parts.path.lastIndexOf("/"), c = this._parts.path.substring(c + 1); return a ? d.decodePathSegment(c) : c
        } c = !1; "/" === a.charAt(0) && (a = a.substring(1)); a.match(/\.?\//) && (c = !0); var e = RegExp(r(this.filename()) + "$"); a = d.recodePath(a); this._parts.path = this._parts.path.replace(e, a); c ? this.normalizePath(b) : this.build(!b); return this
    }; e.suffix = function (a, b) {
        if (this._parts.urn) return void 0 === a ? "" : this; if (void 0 === a || !0 === a) {
            if (!this._parts.path || "/" === this._parts.path) return ""; var c = this.filename(), e = c.lastIndexOf(".");
            if (-1 === e) return ""; c = c.substring(e + 1); c = /^[a-z0-9%]+$/i.test(c) ? c : ""; return a ? d.decodePathSegment(c) : c
        } "." === a.charAt(0) && (a = a.substring(1)); if (c = this.suffix()) e = a ? RegExp(r(c) + "$") : RegExp(r("." + c) + "$"); else { if (!a) return this; this._parts.path += "." + d.recodePath(a) } e && (a = d.recodePath(a), this._parts.path = this._parts.path.replace(e, a)); this.build(!b); return this
    }; e.segment = function (a, b, c) {
        var d = this._parts.urn ? ":" : "/", e = this.path(), g = "/" === e.substring(0, 1), e = e.split(d); void 0 !== a && "number" !== typeof a &&
        (c = b, b = a, a = void 0); if (void 0 !== a && "number" !== typeof a) throw Error("Bad segment '" + a + "', must be 0-based integer"); g && e.shift(); 0 > a && (a = Math.max(e.length + a, 0)); if (void 0 === b) return void 0 === a ? e : e[a]; if (null === a || void 0 === e[a]) if (m(b)) { e = []; a = 0; for (var f = b.length; a < f; a++) if (b[a].length || e.length && e[e.length - 1].length) e.length && !e[e.length - 1].length && e.pop(), e.push(b[a]) } else { if (b || "string" === typeof b) "" === e[e.length - 1] ? e[e.length - 1] = b : e.push(b) } else b || "string" === typeof b && b.length ? e[a] = b : e.splice(a,
        1); g && e.unshift(""); return this.path(e.join(d), c)
    }; e.segmentCoded = function (a, b, c) { var e, g; "number" !== typeof a && (c = b, b = a, a = void 0); if (void 0 === b) { a = this.segment(a, b, c); if (m(a)) for (e = 0, g = a.length; e < g; e++) a[e] = d.decode(a[e]); else a = void 0 !== a ? d.decode(a) : void 0; return a } if (m(b)) for (e = 0, g = b.length; e < g; e++) b[e] = d.decode(b[e]); else b = "string" === typeof b ? d.encode(b) : b; return this.segment(a, b, c) }; var D = e.query; e.query = function (a, b) {
        if (!0 === a) return d.parseQuery(this._parts.query, this._parts.escapeQuerySpace);
        if ("function" === typeof a) { var c = d.parseQuery(this._parts.query, this._parts.escapeQuerySpace), e = a.call(this, c); this._parts.query = d.buildQuery(e || c, this._parts.duplicateQueryParameters, this._parts.escapeQuerySpace); this.build(!b); return this } return void 0 !== a && "string" !== typeof a ? (this._parts.query = d.buildQuery(a, this._parts.duplicateQueryParameters, this._parts.escapeQuerySpace), this.build(!b), this) : D.call(this, a, b)
    }; e.setQuery = function (a, b, c) {
        var e = d.parseQuery(this._parts.query, this._parts.escapeQuerySpace);
        if ("object" === typeof a) for (var g in a) s.call(a, g) && (e[g] = a[g]); else if ("string" === typeof a) e[a] = void 0 !== b ? b : null; else throw new TypeError("URI.addQuery() accepts an object, string as the name parameter"); this._parts.query = d.buildQuery(e, this._parts.duplicateQueryParameters, this._parts.escapeQuerySpace); "string" !== typeof a && (c = b); this.build(!c); return this
    }; e.addQuery = function (a, b, c) {
        var e = d.parseQuery(this._parts.query, this._parts.escapeQuerySpace); d.addQuery(e, a, void 0 === b ? null : b); this._parts.query =
        d.buildQuery(e, this._parts.duplicateQueryParameters, this._parts.escapeQuerySpace); "string" !== typeof a && (c = b); this.build(!c); return this
    }; e.removeQuery = function (a, b, c) { var e = d.parseQuery(this._parts.query, this._parts.escapeQuerySpace); d.removeQuery(e, a, b); this._parts.query = d.buildQuery(e, this._parts.duplicateQueryParameters, this._parts.escapeQuerySpace); "string" !== typeof a && (c = b); this.build(!c); return this }; e.hasQuery = function (a, b, c) {
        var e = d.parseQuery(this._parts.query, this._parts.escapeQuerySpace);
        return d.hasQuery(e, a, b, c)
    }; e.setSearch = e.setQuery; e.addSearch = e.addQuery; e.removeSearch = e.removeQuery; e.hasSearch = e.hasQuery; e.normalize = function () { return this._parts.urn ? this.normalizeProtocol(!1).normalizeQuery(!1).normalizeFragment(!1).build() : this.normalizeProtocol(!1).normalizeHostname(!1).normalizePort(!1).normalizePath(!1).normalizeQuery(!1).normalizeFragment(!1).build() }; e.normalizeProtocol = function (a) {
        "string" === typeof this._parts.protocol && (this._parts.protocol = this._parts.protocol.toLowerCase(),
        this.build(!a)); return this
    }; e.normalizeHostname = function (a) { this._parts.hostname && (this.is("IDN") && h ? this._parts.hostname = h.toASCII(this._parts.hostname) : this.is("IPv6") && k && (this._parts.hostname = k.best(this._parts.hostname)), this._parts.hostname = this._parts.hostname.toLowerCase(), this.build(!a)); return this }; e.normalizePort = function (a) { "string" === typeof this._parts.protocol && this._parts.port === d.defaultPorts[this._parts.protocol] && (this._parts.port = null, this.build(!a)); return this }; e.normalizePath =
    function (a) {
        if (this._parts.urn || !this._parts.path || "/" === this._parts.path) return this; var b, c = this._parts.path, e = "", g, f; "/" !== c.charAt(0) && (b = !0, c = "/" + c); c = c.replace(/(\/(\.\/)+)|(\/\.$)/g, "/").replace(/\/{2,}/g, "/"); b && (e = c.substring(1).match(/^(\.\.\/)+/) || "") && (e = e[0]); for (; ;) { g = c.indexOf("/.."); if (-1 === g) break; else if (0 === g) { c = c.substring(3); continue } f = c.substring(0, g).lastIndexOf("/"); -1 === f && (f = g); c = c.substring(0, f) + c.substring(g + 3) } b && this.is("relative") && (c = e + c.substring(1)); c = d.recodePath(c);
        this._parts.path = c; this.build(!a); return this
    }; e.normalizePathname = e.normalizePath; e.normalizeQuery = function (a) { "string" === typeof this._parts.query && (this._parts.query.length ? this.query(d.parseQuery(this._parts.query, this._parts.escapeQuerySpace)) : this._parts.query = null, this.build(!a)); return this }; e.normalizeFragment = function (a) { this._parts.fragment || (this._parts.fragment = null, this.build(!a)); return this }; e.normalizeSearch = e.normalizeQuery; e.normalizeHash = e.normalizeFragment; e.iso8859 = function () {
        var a =
        d.encode, b = d.decode; d.encode = escape; d.decode = decodeURIComponent; this.normalize(); d.encode = a; d.decode = b; return this
    }; e.unicode = function () { var a = d.encode, b = d.decode; d.encode = g; d.decode = unescape; this.normalize(); d.encode = a; d.decode = b; return this }; e.readable = function () {
        var a = this.clone(); a.username("").password("").normalize(); var b = ""; a._parts.protocol && (b += a._parts.protocol + "://"); a._parts.hostname && (a.is("punycode") && h ? (b += h.toUnicode(a._parts.hostname), a._parts.port && (b += ":" + a._parts.port)) : b += a.host());
        a._parts.hostname && a._parts.path && "/" !== a._parts.path.charAt(0) && (b += "/"); b += a.path(!0); if (a._parts.query) { for (var c = "", e = 0, g = a._parts.query.split("&"), f = g.length; e < f; e++) { var k = (g[e] || "").split("="), c = c + ("&" + d.decodeQuery(k[0], this._parts.escapeQuerySpace).replace(/&/g, "%26")); void 0 !== k[1] && (c += "=" + d.decodeQuery(k[1], this._parts.escapeQuerySpace).replace(/&/g, "%26")) } b += "?" + c.substring(1) } return b += d.decodeQuery(a.hash(), !0)
    }; e.absoluteTo = function (a) {
        var b = this.clone(), c = ["protocol", "username",
        "password", "hostname", "port"], e, g; if (this._parts.urn) throw Error("URNs do not have any generally defined hierarchical components"); a instanceof d || (a = new d(a)); b._parts.protocol || (b._parts.protocol = a._parts.protocol); if (this._parts.hostname) return b; for (e = 0; g = c[e]; e++) b._parts[g] = a._parts[g]; b._parts.path ? ".." === b._parts.path.substring(-2) && (b._parts.path += "/") : (b._parts.path = a._parts.path, b._parts.query || (b._parts.query = a._parts.query)); "/" !== b.path().charAt(0) && (a = a.directory(), b._parts.path = (a ?
        a + "/" : "") + b._parts.path, b.normalizePath()); b.build(); return b
    }; e.relativeTo = function (a) {
        var b = this.clone().normalize(), c, e, g, f; if (b._parts.urn) throw Error("URNs do not have any generally defined hierarchical components"); a = (new d(a)).normalize(); c = b._parts; e = a._parts; g = b.path(); f = a.path(); if ("/" !== g.charAt(0)) throw Error("URI is already relative"); if ("/" !== f.charAt(0)) throw Error("Cannot calculate a URI relative to another relative URI"); c.protocol === e.protocol && (c.protocol = null); if (c.username ===
        e.username && c.password === e.password && null === c.protocol && null === c.username && null === c.password && c.hostname === e.hostname && c.port === e.port) c.hostname = null, c.port = null; else return b.build(); if (g === f) return c.path = "", b.build(); a = d.commonPath(b.path(), a.path()); if (!a) return b.build(); e = e.path.substring(a.length).replace(/[^\/]*$/, "").replace(/.*?\//g, "../"); c.path = e + c.path.substring(a.length); return b.build()
    }; e.equals = function (a) {
        var b = this.clone(); a = new d(a); var c = {}, e = {}, g = {}, f; b.normalize(); a.normalize();
        if (b.toString() === a.toString()) return !0; c = b.query(); e = a.query(); b.query(""); a.query(""); if (b.toString() !== a.toString() || c.length !== e.length) return !1; c = d.parseQuery(c, this._parts.escapeQuerySpace); e = d.parseQuery(e, this._parts.escapeQuerySpace); for (f in c) if (s.call(c, f)) { if (!m(c[f])) { if (c[f] !== e[f]) return !1 } else if (!y(c[f], e[f])) return !1; g[f] = !0 } for (f in e) if (s.call(e, f) && !g[f]) return !1; return !0
    }; e.duplicateQueryParameters = function (a) { this._parts.duplicateQueryParameters = !!a; return this }; e.escapeQuerySpace =
    function (a) { this._parts.escapeQuerySpace = !!a; return this }; return d
});
(function (h, k) { "object" === typeof exports ? module.exports = k(require("jquery", "./URI")) : "function" === typeof define && define.amd ? define(["jquery", "./URI"], k) : k(h.jQuery, h.URI) })(this, function (h, k) {
    function q(d) { return d.replace(/([.*+?^=!:${}()|[\]\/\\])/g, "\\$1") } function u(d) { var f = d.nodeName.toLowerCase(); return "input" === f && "image" !== d.type ? void 0 : k.domAttributes[f] } function d(d) { return { get: function (f) { return h(f).uri()[d]() }, set: function (f, e) { h(f).uri()[d](e); return e } } } function r(d, f) {
        var e, k, n; if (!u(d) ||
        !f) return !1; e = f.match(z); if (!e || !e[5] && ":" !== e[2] && !m[e[2]]) return !1; n = h(d).uri(); if (e[5]) return n.is(e[5]); if (":" === e[2]) return k = e[1].toLowerCase() + ":", m[k] ? m[k](n, e[4]) : !1; k = e[1].toLowerCase(); return w[k] ? m[e[2]](n[k](), e[4], k) : !1
    } var w = {}, m = {
        "=": function (d, f) { return d === f }, "^=": function (d, f, e) { return !!(d + "").match(RegExp("^" + q(f), "i")) }, "$=": function (d, f, e) { return !!(d + "").match(RegExp(q(f) + "$", "i")) }, "*=": function (d, f, e) { "directory" == e && (d += "/"); return !!(d + "").match(RegExp(q(f), "i")) }, "equals:": function (d,
        f) { return d.equals(f) }, "is:": function (d, f) { return d.is(f) }
    }; h.each("authority directory domain filename fragment hash host hostname href password path pathname port protocol query resource scheme search subdomain suffix tld username".split(" "), function (g, f) { w[f] = !0; h.attrHooks["uri:" + f] = d(f) }); var x = function (d, f) { return h(d).uri().href(f).toString() }; h.each(["src", "href", "action", "uri", "cite"], function (d, f) { h.attrHooks[f] = { set: x } }); h.attrHooks.uri.get = function (d) { return h(d).uri() }; h.fn.uri = function (d) {
        var f =
        this.first(), e = f.get(0), h = u(e); if (!h) throw Error('Element "' + e.nodeName + '" does not have either property: href, src, action, cite'); if (void 0 !== d) { var m = f.data("uri"); if (m) return m.href(d); d instanceof k || (d = k(d || "")) } else { if (d = f.data("uri")) return d; d = k(f.attr(h) || "") } d._dom_element = e; d._dom_attribute = h; d.normalize(); f.data("uri", d); return d
    }; k.prototype.build = function (d) {
        if (this._dom_element) this._string = k.build(this._parts), this._deferred_build = !1, this._dom_element.setAttribute(this._dom_attribute,
        this._string), this._dom_element[this._dom_attribute] = this._string; else if (!0 === d) this._deferred_build = !0; else if (void 0 === d || this._deferred_build) this._string = k.build(this._parts), this._deferred_build = !1; return this
    }; var y, z = /^([a-zA-Z]+)\s*([\^\$*]?=|:)\s*(['"]?)(.+)\3|^\s*([a-zA-Z0-9]+)\s*$/; y = h.expr.createPseudo ? h.expr.createPseudo(function (d) { return function (f) { return r(f, d) } }) : function (d, f, e) { return r(d, e[3]) }; h.expr[":"].uri = y; return {}
});
