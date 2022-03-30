/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Text;

namespace YetaWF.Core.Security {

    public class MD5Crypto {

        public string StringMD5(string text) {

            using (System.Security.Cryptography.MD5 md5Obj = System.Security.Cryptography.MD5.Create()) {

                byte[] btes = System.Text.Encoding.ASCII.GetBytes(text);
                btes = md5Obj.ComputeHash(btes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in btes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
