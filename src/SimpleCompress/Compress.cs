namespace SimpleCompress
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using System.Text;

    public static class Compress
    {
        public static void FolderToFile(string srcPath, string dstFilePath) {
            // list out name+hash -> [path]
            // write this to a single file
            // gzip that file

            var tmp = dstFilePath+".tmp";
            var bits = new Dictionary<string, List<string>>();

            // find distinct files
            var files = NativeIO.EnumerateFiles(new PathInfo(srcPath).FullNameUnc, searchOption: SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var name = file.Name;
                var hash = HashOf(file);

                Add(name + "|" + hash, file.FullName, bits);
            }

            // pack everything into a temp file
            if (File.Exists(tmp)) File.Delete(tmp);
            using(var fs = File.OpenWrite(tmp))
                foreach (var key in bits.Keys)
                {
                    var paths = bits[key];
                    var catPaths = Encoding.UTF8.GetBytes(string.Join("|", Filter(paths,srcPath)));

                    WriteLength(catPaths.Length, fs);
                    fs.Write(catPaths, 0, catPaths.Length);

                    var info = NativeIO.ReadFileDetails(new PathInfo(paths[0]));
                    WriteLength((long)info.Length, fs);

                    using (var inf = NativeIO.OpenFileStream(info.PathInfo, FileAccess.Read)) inf.CopyTo(fs);

                    fs.Flush();
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

        static void Add(string key, string value, IDictionary<string, List<string>> container)
        {
            if (!container.ContainsKey(key)) container.Add(key, new List<string>());
            container[key].Add(value);
        }

        static string HashOf(FileDetail file)
        {
            using (var md5 = MD5.Create())
            using (var stream = NativeIO.OpenFileStream(file.PathInfo, FileAccess.Read))
            {
                return Convert.ToBase64String(md5.ComputeHash(stream));
            }
        }
    }
}