using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SimpleCompress
{
    internal static class Crypto
    {
        public const string SignatureMarker = "szX509"; // code below relies on this being 6 bytes of ASCII

        public static byte[] SignData(X509Certificate2 key, Stream stream)
        {
            var provider = (RSACryptoServiceProvider)key.PrivateKey;

            var hash = (HashAlgorithm) CryptoConfig.CreateFromName("SHA1");
            var hashVal = hash.ComputeHash(stream);
            return provider.SignHash(hashVal, "SHA1");
        }

        public static bool VerifyData(X509Certificate2 publicKey, Stream stream, byte[] signature)
        {
            var provider = (RSACryptoServiceProvider)publicKey.PublicKey.Key;

            var hash = (HashAlgorithm) CryptoConfig.CreateFromName("SHA1");
            var hashVal = hash.ComputeHash(stream);
            return provider.VerifyHash(hashVal, "SHA1", signature);
        }
        

        private static bool IsSigned(Stream rawStream)
        {
            var buf = new byte[6];
            var read = rawStream.Read(buf, 0, 6);

            if (read != 6) return false;
            return Encoding.ASCII.GetString(buf) == SignatureMarker;
        }

        private static X509Certificate2 LoadPrivateKey(string fileName, string password)
        {
            return new X509Certificate2(fileName, password);
        }

        private static X509Certificate2 LoadPublicKey(string fileName)
        {
            return new X509Certificate2(fileName);
        }

        /// <summary>
        /// Look for signing marker. If present, check validity.
        /// If not valid, will throw an exception.
        /// If not signed, or data is valid, the file stream will be left at the start of the real data.
        /// </summary>
        /// <param name="archiveStream">File stream of archive</param>
        /// <param name="signingCertPath">Path to the public key cert that the package should match</param>
        public static void TryVerify(FileStream archiveStream, string signingCertPath)
        {
            if (! IsSigned(archiveStream)) {
                if (!string.IsNullOrWhiteSpace(signingCertPath)) throw new SecurityException("Signature validation failed");
                archiveStream.Position = 0;
                return;
            }

            var sigLength = ReadSignature(archiveStream, out byte[] signature);
            var dataStartPos = 6 + 8 + sigLength;

            if (string.IsNullOrWhiteSpace(signingCertPath)) { // skip verification
                archiveStream.Position = dataStartPos;
                return;
            }

            var key = LoadPublicKey(Path.GetFullPath(signingCertPath));
            var ok = VerifyData(key, archiveStream, signature);

            if (!ok) throw new SecurityException("Signature validation failed");

            archiveStream.Position = dataStartPos; // reset to data
        }

        /// <summary>
        /// Create a full header with signature marker, length, and signed hash.
        /// </summary>
        /// <param name="rawFile">The file to be signed</param>
        /// <param name="signingPfxPath">Full path the signing cert with private key</param>
        /// <param name="password">password to private key</param>
        public static byte[] BuildSigningHeader(FileStream rawFile, string signingPfxPath, string password) {
            var key = LoadPrivateKey(Path.GetFullPath(signingPfxPath), password);
            var hash = SignData(key, rawFile);
            
            var headerLength = 6 + 8 + hash.Length;
            
            using (var ms = new MemoryStream(headerLength)){
                var marker = Encoding.ASCII.GetBytes(SignatureMarker);
                var lenBytes = BitConverter.GetBytes((long)hash.Length);
                ms.Write(marker, 0, 6);
                ms.Write(lenBytes, 0, 8);
                ms.Write(hash, 0, hash.Length);

                return ms.ToArray();
            }
        }

        private static int ReadSignature(Stream fs, out byte[] sigBytes)
        {
            var lengthBuffer = new byte[8];
            var len = fs.Read(lengthBuffer, 0, 8);
            if (len != 8) { throw new InvalidDataException("Signature header length missing"); } 

            if (!BitConverter.IsLittleEndian) Array.Reverse(lengthBuffer, 0, lengthBuffer.Length);
            var sigLen = BitConverter.ToInt64(lengthBuffer, 0);
            if (sigLen < 8 || sigLen > 4096) { throw new InvalidDataException("Signature header length seems wrong"); } // this is in bytes, not bits. Just a safety check.

            sigBytes = new byte[sigLen];
            len = fs.Read(sigBytes, 0, (int)sigLen);
            if (len != sigLen) { throw new InvalidDataException("Signature truncated"); } 
            return len;
        }
    }
}