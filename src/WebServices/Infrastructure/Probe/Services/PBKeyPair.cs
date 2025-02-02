﻿using Aiursoft.Scanner.Interfaces;
using System.Security.Cryptography;

namespace Aiursoft.Probe.Services
{
    public class PBKeyPair : ISingletonDependency
    {
        private RSAParameters? _privateKey;
        public RSAParameters GetKey()
        {
            if (_privateKey != null) return _privateKey.Value;
            var provider = new RSACryptoServiceProvider();
            _privateKey = provider.ExportParameters(true);
            return _privateKey.Value;
        }
    }
}
