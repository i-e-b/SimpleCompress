namespace SimpleCompress
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Root class for expanding a single archive to a directory
    /// </summary>
    public static class Decompress
    {
        /// <summary>
        /// Expand an archive file to a directory
        /// </summary>
        /// <param name="srcFilePath">Full path to the archive file</param>
        /// <param name="dstPath">Full path to the destination directory that will contain resulting file. This directory will be created if it doesn't exist.</param>
        /// <param name="signingCertPath">Optional: Full path to a public key certificate file. If not supplied, any signatures are ignored</param>
        public static void FromFileToFolder(string srcFilePath, string dstPath, string signingCertPath = null) {
            // decompress to temp file, 
            // run through the stream, writing the data out to the first file, then copy to all duplicates

            var tmp = srcFilePath + ".tmp";

            // First decompress contents
            if (File.Exists(tmp)) File.Delete(tmp);
            using (var compressing = new GZipStream(File.OpenRead(srcFilePath), CompressionMode.Decompress))
            using (var cat = File.Open(tmp, FileMode.Create, FileAccess.Write))
            {
                compressing.CopyTo(cat, 65536);
                cat.Flush();
            }

            // Next, scan the file and copy data to paths
            using (var fs = File.OpenRead(tmp)) {
                Crypto.TryVerify(fs, signingCertPath);

                byte[] fileHash;
                long pathsLength;
                // <MD5:16 bytes><length:8 bytes>>
                while (ReadPathLength(fs, out pathsLength, out fileHash))
                {
                    // get all target paths
                    long fileLength;
                    // <paths:utf8 str>
                    var subPaths = ReadUtf8(fs, pathsLength).Split('|').Select(p => dstPath + p).ToArray();
                    // <length:8 bytes>
                    if (!ReadFileLength(fs, out fileLength)) throw new Exception("Malformed file: no length for data");

                    if (fileLength == 0 && IsZero(fileHash))
                    {
                        if (subPaths.Length != 2) throw new Exception("Malformed file: symbolic link did not have a single source and target");
                        NativeIO.SymbolicLink.CreateDirectoryLink(subPaths[0], subPaths[1]);
                    }
                    else
                    {
                        WriteFileToAllPaths(subPaths, fs, fileLength, fileHash);
                    }
                }
            }

            // Finally, cleanup temp file
            File.Delete(tmp);
        }

        static bool IsZero(IEnumerable<byte> fileHash)
        {
            return fileHash.All(b => b == 0);
        }

        static void WriteFileToAllPaths(IList<string> subPaths, Stream fs, long fileLength, IEnumerable<byte> expectedHash)
        {
            // read source into first file
            var firstPath = subPaths[0];
            PutFolder(firstPath);
            // <data:byte array>
            CopyLength(fs, firstPath, fileLength, expectedHash);
            if (subPaths.Count == 1)
            {
                return;
            }

            // copy first file into all other locations
            var srcInfo = new PathInfo(firstPath);
            for (int i = 1; i < subPaths.Count; i++)
            {
                var thisPath = subPaths[i];
                PutFolder(thisPath);
                if (!NativeIO.CopyFile(srcInfo, new PathInfo(thisPath)))
                {
                    throw new Exception("Failed to write " + thisPath);
                }
            }
        }

        static void PutFolder(string path)
        {
            var pinfo = new PathInfo(path);
            if (pinfo.Parent != null) NativeIO.CreateDirectory(new PathInfo(path).Parent, recursive:true);
        }

        static void CopyLength(Stream fs, string dstFilePath, long fileLength, IEnumerable<byte> expectedHash)
        {
            const int bufSz = 65536;
            var remain = fileLength;
            var buffer = new byte[bufSz];
            using (var md5 = MD5.Create())
            using (var fout = NativeIO.OpenFileStream(new PathInfo(dstFilePath), FileAccess.Write, FileMode.CreateNew))
            {
                int len;
                while (remain > bufSz)
                {
                    len = fs.Read(buffer, 0, bufSz);
                    if (len != bufSz) throw new Exception("Malformed file: data truncated");
                    md5.TransformBlock(buffer, 0, len, null, 0);
                    fout.Write(buffer, 0, bufSz);
                    remain -= bufSz;
                }

                if (remain != 0)
                {
                    len = fs.Read(buffer, 0, (int)remain);
                    if (len != remain) throw new Exception("Malformed file: data truncated at end");
                    md5.TransformBlock(buffer, 0, (int)remain, null, 0);
                    fout.Write(buffer, 0, (int)remain);
                }

                md5.TransformFinalBlock(new byte[0], 0, 0);
                if (!HashesEqual(expectedHash, md5.Hash)) { throw new Exception("Damaged archive: File at " + dstFilePath + " failed a checksum"); }
            }
        }

        static bool HashesEqual(IEnumerable<byte> expectedHash, IEnumerable<byte> hash)
        {
            return expectedHash.SequenceEqual(hash);
        }

        static string ReadUtf8(Stream fs, long pathsLength)
        {
            var bytes = new byte[pathsLength];
            var len = fs.Read(bytes, 0, (int)pathsLength);
            if (len != pathsLength) throw new Exception("Malformed file: too short in paths");

            return Encoding.UTF8.GetString(bytes);
        }

        static bool ReadPathLength(Stream fs, out long pathsLength, out byte[] md5Hash)
        {
            md5Hash = null;
            pathsLength = 0;

            var md5Buffer = new byte[16];
            var len = fs.Read(md5Buffer, 0, 16);
            if (len != 16) { return false; }

            var lengthBuffer = new byte[8];
            len = fs.Read(lengthBuffer, 0, 8);
            if (len != 8) { return false; }

            md5Hash = md5Buffer; // there should be no endian issues on the MD5 hash.
            if (!BitConverter.IsLittleEndian) Array.Reverse(lengthBuffer, 0, lengthBuffer.Length);
            pathsLength = BitConverter.ToInt64(lengthBuffer, 0);
            return true;
        }

        static bool ReadFileLength(Stream fs, out long pathsLength)
        {
            pathsLength = 0;

            var lengthBuffer = new byte[8];
            var len = fs.Read(lengthBuffer, 0, 8);
            if (len != 8) { return false; }

            if (!BitConverter.IsLittleEndian) Array.Reverse(lengthBuffer, 0, lengthBuffer.Length);
            pathsLength = BitConverter.ToInt64(lengthBuffer, 0);
            return true;
        }
    }
}