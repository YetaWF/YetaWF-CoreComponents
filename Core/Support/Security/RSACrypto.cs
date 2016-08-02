/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Security.Cryptography;
using System.Text;
using YetaWF.Core.Support;

namespace YetaWF.Core.Security {
    public static class RSACrypto {

        // http://codebetter.com/johnvpetersen/2012/04/02/making-your-asp-net-web-apis-secure/
        // http://blogs.msdn.com/b/alejacma/archive/2008/10/23/how-to-generate-key-pairs-encrypt-and-decrypt-data-with-net-c.aspx

        public static void MakeNewKeys(out string publicKey, out string privateKey) {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.PersistKeyInCsp = false;
                privateKey = rsa.ToXmlString(true);
                publicKey = rsa.ToXmlString(false);
            }
#if DEBUG
            string Token = "User1Forever";
            string encryptedText;
            RSACrypto.Encrypt(publicKey, Token, out encryptedText);
            string decryptedText;
            RSACrypto.Decrypt(privateKey, encryptedText, out decryptedText);
            if (decryptedText != Token)
                throw new InternalError("Encryption/decription test failure");
#endif
        }

        public static void Encrypt(string publicKeyXml, string plainText, out string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(publicKeyXml)) throw new Error("The public key is missing");
            try {
                using (RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider()) {
                    rsaProvider.PersistKeyInCsp = false;
                    rsaProvider.FromXmlString(publicKeyXml);
                    byte[] plainBytes = Encoding.Unicode.GetBytes(plainText);
                    byte[] encryptedBytes = rsaProvider.Encrypt(plainBytes, false);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte bte in encryptedBytes)
                        sb.Append(string.Format("{0:X2}", bte));
                    encryptedText = sb.ToString();
                }
            } catch (Exception ex) {
                throw new InternalError("Failure encrypting - {0}", ex.Message);
            }
        }

        public static void Decrypt(string privateKeyXml, string encryptedText, out string plainText) {
            if (string.IsNullOrWhiteSpace(privateKeyXml)) throw new Error("The private key is missing");
            try {
                int length = encryptedText.Length;
                if (length % 2 != 0) throw new InternalError("Invalid data");
                int bytelen = length / 2;
                byte[] encryptedBytes = new byte[bytelen];
                for (int i = 0 ; i < bytelen ; i++)
                    encryptedBytes[i] = (byte) Convert.ToInt32(encryptedText.Substring(i * 2, 2), 16);

                using (RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider()) {
                    rsaProvider.PersistKeyInCsp = false;
                    rsaProvider.FromXmlString(privateKeyXml);
                    byte[] plainBytes  = rsaProvider.Decrypt(encryptedBytes, false);
                    plainText = Encoding.Unicode.GetString(plainBytes);
                }
            } catch (Exception ex) {
                throw new InternalError("Failure decrypting - {0}", ex.Message);
            }
        }
    }
}
