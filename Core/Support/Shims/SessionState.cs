/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
#if MVC6
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
#else
using System.Web;
using System.Web.SessionState;
using System.Linq;
#endif

namespace YetaWF.Core.Support {

    public class SessionState {

        private HttpContext _httpContext;
#if MVC6
        private ISession _session;
#else
        private HttpSessionState _session;
#endif
        public SessionState(HttpContext httpContext) {
            _httpContext = httpContext;
#if MVC6
            _session = httpContext.Session;
#else
            _session = httpContext.Session;
#endif
        }
        public object this[string key] {
            get {
                return GetString(key);
            }
        }
        public IEnumerable<string> Keys {
            get {
#if MVC6
                return _session.Keys;
#else
                return (from string k in _session.Keys select k).ToList();
#endif
            }
        }
        public void Remove(string key) {
            _session.Remove(key);
        }

        public void Clear() {
            _session.Clear();
        }
        public string GetString(string key) {
#if MVC6
            return _session.GetString(key);
#else
            object val = _session[key];
            return val == null ? null : val.ToString();
#endif
        }
        public void SetString(string key, string value) {
#if MVC6
            _session.SetString(key, value);
#else
            _session[key] = value;
#endif
        }
        internal byte[] GetBytes(string key) {
#if MVC6
            byte[] btes;
            if (_session.TryGetValue(key, out btes))
                return btes;
            return new byte[] { };
#else
            return (byte[]) _session[key];
#endif
        }
        public void SetBytes(string key, byte[] btes) {
#if MVC6
            _session.Set(key, btes);
#else
            _session[key] = btes;
#endif
        }
        public int GetInt(string key, int dflt = 0) {
#if MVC6
            return _session.GetInt32(key) ?? dflt;
#else
            object val = _session[key];
            if (val == null) return dflt;
            return (int)val;
#endif
        }
        public void SetInt(string key, int value) {
#if MVC6
            _session.SetInt32(key, value);
#else
            _session[key] = value;
#endif
        }
        public TYPE GetObject<TYPE>(string key, TYPE dflt = default(TYPE)) {
#if MVC6
            string value = GetString(key);
            if (value == null) return dflt;
            return JsonConvert.DeserializeObject<TYPE>(value);
#else
            return (TYPE)_session[key];
#endif
        }
        public void SetObject<TYPE>(string key, TYPE value) {
#if MVC6
            _session.SetString(key, JsonConvert.SerializeObject(value));
#else
            _session[key] = value;
#endif
        }
    }
}

