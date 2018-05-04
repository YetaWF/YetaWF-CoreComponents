/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Security.Cryptography;
using System.Text;
using YetaWF.Core.Support;
#if NETCOREAPP
using System.Xml;
#endif

namespace YetaWF.Core.Security {
    public static class RSACrypto {

        // http://codebetter.com/johnvpetersen/2012/04/02/making-your-asp-net-web-apis-secure/
        // http://blogs.msdn.com/b/alejacma/archive/2008/10/23/how-to-generate-key-pairs-encrypt-and-decrypt-data-with-net-c.aspx

        public static void MakeNewKeys(out string publicKey, out string privateKey) {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider()) {
                rsa.PersistKeyInCsp = false;
#if NETCOREAPP
                privateKey = rsa.TranslateToXmlString(true);
                publicKey = rsa.TranslateToXmlString(false);
#else
                privateKey = rsa.ToXmlString(true);
                publicKey = rsa.ToXmlString(false);
#endif
            }
#if DEBUG
            string Token = "User1Forever";
            string encryptedText;
            RSACrypto.Encrypt(publicKey, Token, out encryptedText);
            string decryptedText;
            RSACrypto.Decrypt(privateKey, encryptedText, out decryptedText);
            if (decryptedText != Token)
                throw new InternalError("Encryption/decryption test failure");
#endif
        }

        public static void Encrypt(string publicKeyXml, string plainText, out string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(publicKeyXml)) throw new Error("The public key is missing");
            try {
                using (RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider()) {
                    rsaProvider.PersistKeyInCsp = false;
#if NETCOREAPP
                    rsaProvider.TranslateFromXmlString(publicKeyXml);
#else
                    rsaProvider.FromXmlString(publicKeyXml);
#endif
                    byte[] plainBytes = Encoding.Unicode.GetBytes(plainText);
                    byte[] encryptedBytes = rsaProvider.Encrypt(plainBytes, false);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte bte in encryptedBytes)
                        sb.Append(string.Format("{0:X2}", bte));
                    encryptedText = sb.ToString();
                }
            } catch (Exception ex) {
                throw new InternalError("Failure encrypting - {0}", ErrorHandling.FormatExceptionMessage(ex));
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
#if NETCOREAPP
                    rsaProvider.TranslateFromXmlString(privateKeyXml);
#else
                    rsaProvider.FromXmlString(privateKeyXml);
#endif
                    byte[] plainBytes  = rsaProvider.Decrypt(encryptedBytes, false);
                    plainText = Encoding.Unicode.GetString(plainBytes);
                }
            } catch (Exception ex) {
                throw new InternalError("Failure decrypting - {0}", ErrorHandling.FormatExceptionMessage(ex));
            }
        }
#if NETCOREAPP
        // https://github.com/dotnet/corefx/issues/23686 fucking snowflakes

        public static void TranslateFromXmlString(this RSA rsa, string xmlString) {
            RSAParameters parameters = new RSAParameters();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue")) {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes) {
                    switch (node.Name) {
                        case "Modulus": parameters.Modulus = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Exponent": parameters.Exponent = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "P": parameters.P = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Q": parameters.Q = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DP": parameters.DP = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DQ": parameters.DQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "InverseQ": parameters.InverseQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "D": parameters.D = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    }
                }
            } else {
                throw new Exception("Invalid XML RSA key.");
            }
            rsa.ImportParameters(parameters);
        }
        public static string TranslateToXmlString(this RSA rsa, bool includePrivateParameters) {
            RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                  parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null,
                  parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null,
                  parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
                  parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
                  parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null,
                  parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null,
                  parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null,
                  parameters.D != null ? Convert.ToBase64String(parameters.D) : null);
        }
#else
#endif
    }
}

