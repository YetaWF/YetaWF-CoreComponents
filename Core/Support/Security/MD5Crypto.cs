﻿/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Text;

namespace YetaWF.Core.Security {
    public class MD5Crypto {
        public string StringMD5(string text) {

            System.Security.Cryptography.MD5CryptoServiceProvider md5Obj = new System.Security.Cryptography.MD5CryptoServiceProvider();

            byte[] btes = System.Text.Encoding.ASCII.GetBytes(text);
            btes = md5Obj.ComputeHash(btes);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in btes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
