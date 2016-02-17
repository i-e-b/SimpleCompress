﻿namespace SimpleCompress
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    public static class Decompress
    {
        public static void FromFileToFolder(string srcFilePath, string dstPath) { 
            // decompress to temp file, 
            // run through the stream, writing the data out to the first file, then copy to all duplicates

            var tmp = srcFilePath + ".tmp";

            // decompress file
            if (File.Exists(tmp)) File.Delete(tmp);
            using (var compressing = new GZipStream(File.OpenRead(srcFilePath), CompressionMode.Decompress))
            using (var cat = File.OpenWrite(tmp))
            {
                compressing.CopyTo(cat, 65536);
                cat.Flush();
            }

            // scan the file
            using (var fs = File.OpenRead(tmp)) {
                byte[] fileHash;
                long pathsLength;
                // <MD5:16 bytes><length:8 bytes>>
                while (ReadPathLength(fs, out pathsLength, out fileHash))
                {
                    // get all target paths
                    long fileLength;
                    // <paths:utf8 str>
                    var subPaths = ReadUtf8(fs, pathsLength).Split('|');
                    // <length:8 bytes>
                    if (!ReadFileLength(fs, out fileLength)) throw new Exception("Malformed file: no length for data");

                    // read source into first file
                    var firstPath = dstPath+subPaths[0];
                    PutFolder(firstPath);
                    // <data:byte array>
                    CopyLength(fs, firstPath, fileLength);
                    if (subPaths.Length == 1) continue;

                    // copy first file into all other locations
                    var srcInfo = new PathInfo(firstPath);
                    for (int i = 1; i < subPaths.Length; i++) {
                        var thisPath = dstPath + subPaths[i];
                        PutFolder(thisPath);
                        if (!NativeIO.CopyFile(srcInfo, new PathInfo(thisPath))) throw new Exception("Failed to write "+ thisPath);
                    }
                }
            }

            // cleanup temp file
            File.Delete(tmp);
        }

        static void PutFolder(string path)
        {
            var pinfo = new PathInfo(path);
            if (pinfo.Parent != null) NativeIO.CreateDirectory(new PathInfo(path).Parent, recursive:true);
        }

        static void CopyLength(Stream fs, string dstFilePath, long fileLength)
        {
            const int bufSz = 65536;
            var remain = fileLength;
            var buffer = new byte[bufSz];
            using (var fout = NativeIO.OpenFileStream(new PathInfo(dstFilePath), FileAccess.Write, FileMode.CreateNew)) {
                int len;
                while (remain > bufSz) {
                    len = fs.Read(buffer, 0, bufSz);
                    if (len != bufSz) throw new Exception("Malformed file: data truncated");
                    fout.Write(buffer, 0, bufSz);
                    remain -= bufSz;
                }

                if (remain == 0) return;

                len = fs.Read(buffer, 0, (int)remain);
                if (len != remain) throw new Exception("Malformed file: data truncated at end");
                fout.Write(buffer, 0, (int)remain);
            }
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