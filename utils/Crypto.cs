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

        public static void Init()
        {
            CSP = new(2048);

            privateKey = CSP.ToXmlString(true);
            publicKey = CSP.ToXmlString(false);
        }
    }
}
