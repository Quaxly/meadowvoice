using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace meadowvoice
{
    internal static class Crypto
    {
        public static RSACryptoServiceProvider CSP;
        public static string privateKey;
        public static string publicKey;

        public static bool Available => CSP != null;

        public static void Init()
        {
            CSP = new(2048);

            privateKey = CSP.ToXmlString(true);
            publicKey = CSP.ToXmlString(false);
        }

        internal static byte[] Decrypt(byte[] data)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.FromXmlString(privateKey);
                return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
            }
        }

        internal static byte[] Encrypt(byte[] data, string key)
        {
            using(RSA rsa = RSA.Create())
            {
                rsa.FromXmlString(key);
                return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
            }
        }
    }
}
