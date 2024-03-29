﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace YetaWF.Core.Support {

    public class SessionState {

        private ISession _session;
        public SessionState(HttpContext httpContext) {
            _session = httpContext.Session;
        }
        public object this[string key] {
            get {
                return GetString(key);
            }
        }
        public IEnumerable<string> Keys {
            get {
                return _session.Keys;
            }
        }
        public void Remove(string key) {
            _session.Remove(key);
        }

        public void Clear() {
            _session.Clear();
        }
        public string GetString(string key) {
            return _session.GetString(key) ?? string.Empty;
        }
        public void SetString(string key, string value) {
            _session.SetString(key, value);
        }
        internal byte[] GetBytes(string key) {
            byte[]? btes;
            if (_session.TryGetValue(key, out btes) && btes != null)
                return btes;
            return new byte[] { };
        }
        public void SetBytes(string key, byte[] btes) {
            _session.Set(key, btes);
        }
        public int GetInt(string key, int dflt = 0) {
            return _session.GetInt32(key) ?? dflt;
        }
        public void SetInt(string key, int value) {
            _session.SetInt32(key, value);
        }
        public TYPE? GetObject<TYPE>(string key, TYPE? dflt = default(TYPE)) {
            string value = GetString(key);
            if (value == null) return dflt;
            return JsonConvert.DeserializeObject<TYPE>(value);
        }
        public void SetObject<TYPE>(string key, TYPE value) {
            _session.SetString(key, JsonConvert.SerializeObject(value));
        }
    }
}

