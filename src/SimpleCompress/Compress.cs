namespace SimpleCompress
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Root class for compressing folders into single files
    /// </summary>
    public static class Compress
    {
        /// <summary>
        /// Read all files under the source path into a single destination file.
        /// </summary>
        /// <param name="srcPath">Full path of source directory</param>
        /// <param name="dstFilePath">Full path of destination file</param>
        public static void FolderToFile(string srcPath, string dstFilePath) {
            // list out name+hash -> [path]
            // write this to a single file
            // gzip that file

            var tmp = dstFilePath+".tmp";
            var filePaths = new Dictionary<string, PathList>();
            var symLinks = new Dictionary<string, string>(); // link path -> target path

            // find distinct files
            var files = NativeIO.EnumerateFiles(new PathInfo(srcPath).FullNameUnc, (symPath, targetPath)=>{
                if (IsSubpath(srcPath, targetPath))
                {
                    symLinks.Add(symPath, targetPath);
                    return false;
                }
                return true;
            }, searchOption: SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var hash = HashOf(file);

                Add(hash, file, filePaths);
            }

            // pack everything into a temp file
            if (File.Exists(tmp)) File.Delete(tmp);
            using (var fs = File.OpenWrite(tmp))
            {
                // Write data files
                foreach (var fileKey in filePaths.Keys)
                {
                    var pathList = filePaths[fileKey];
                    var catPaths = Encoding.UTF8.GetBytes(string.Join("|", Filter(pathList.Paths, srcPath)));

                    // Write <MD5:16 bytes>
                    fs.Write(pathList.HashData, 0, 16);

                    // Write <length:8 bytes><paths:utf8 str>
                    WriteLength(catPaths.Length, fs);
                    fs.Write(catPaths, 0, catPaths.Length);

                    var info = NativeIO.ReadFileDetails(new PathInfo(pathList.Paths[0]));

                    // Write <length:8 bytes><data:byte array>
                    WriteLength((long)info.Length, fs);
                    using (var inf = NativeIO.OpenFileStream(info.PathInfo, FileAccess.Read)) inf.CopyTo(fs);

                    fs.Flush();
                }

                // Write symbolic links
                foreach (var linkSrc in symLinks.Keys)
                {
                    var linkTarget = symLinks[linkSrc];
                    var linkData = Encoding.UTF8.GetBytes(string.Join("|", Filter(new[] { linkSrc, linkTarget }, srcPath)));

                    // Write <zeros:16 bytes>
                    WriteLength(0, fs);
                    WriteLength(0, fs);

                    // Write <length:8 bytes><path pair:utf8 str>, path pair is 'src|target'
                    WriteLength(linkData.Length, fs);
                    fs.Write(linkData, 0, linkData.Length);

                    // Write <length:8 bytes>, always zero (there is not file content in a link)
                    WriteLength(0, fs);
                }
            }

            // Compress the file
            if (File.Exists(dstFilePath)) File.Delete(dstFilePath);
            using (var compressing = new GZipStream(File.OpenWrite(dstFilePath), CompressionLevel.Optimal))
            using (var cat = File.OpenRead(tmp))
            {
                cat.CopyTo(compressing, 65536);
                compressing.Flush();
            }

            // Kill the temp file
            File.Delete(tmp);
        }

        static bool IsSubpath(string srcPath, string symPath)
        {
            Func<string,string> crop = p => (p.StartsWith(@"\\?\")) ? (p.Substring(4)) : (p);
            return crop(symPath).StartsWith(crop(srcPath), StringComparison.Ordinal);
        }

        static string[] Filter(IReadOnlyList<string> paths, string srcPath)
        {
            var outp = new string[paths.Count];
            for (var i = 0; i < outp.Length; i++) {
                var path = paths[i];
                if (path.StartsWith(srcPath)) outp[i] = path.Substring(srcPath.Length);
                else outp[i] = path;
            }
            return outp;
        }

        static void WriteLength(long length, Stream fs)
        {
            var bytes = BitConverter.GetBytes(length);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes, 0, bytes.Length);
            fs.Write(bytes, 0, 8);
        }

        static void Add(byte[] hash, FileDetail file, IDictionary<string, PathList> container)
        {
            var key = file.Name + "|" + (Convert.ToBase64String(hash));
            if (!container.ContainsKey(key)) container.Add(key, new PathList(hash, file.FullName));
            else container[key].Add(file.FullName);
        }

        static byte[] HashOf(FileDetail file)
        {
            using (var md5 = MD5.Create())
            using (var stream = NativeIO.OpenFileStream(file.PathInfo, FileAccess.Read))
            {
                return (md5.ComputeHash(stream));
            }
        }
    }
}