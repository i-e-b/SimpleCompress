using System.IO;
using NUnit.Framework;
using SimpleCompress;

namespace Tests
{
    [TestFixture, Explicit]
    public class signing_tests{
        [Test]
        public void can_sign_an_archive()
        {
            if (Directory.Exists(@"C:\Temp\DataResult\CsvArchive"))
            {
                Directory.Delete(@"C:\Temp\DataResult\CsvArchive", true);
            }

            Compress.FolderToFile(
                @"C:\Temp\CsvArchive",
                @"C:\Temp\CsvArchive.inpkg",
                
                ".\\certs\\core.example.com.pfx", "password");

            Decompress.FromFileToFolder(
                @"C:\Temp\CsvArchive.inpkg",
                @"C:\Temp\DataResult\CsvArchive",
                ".\\certs\\core.example.com.pfx.cer");
        }

        [Test]
        public void enforce_signature()
        {
            if (Directory.Exists(@"C:\Temp\DataResult\CsvArchive"))
            {
                Directory.Delete(@"C:\Temp\DataResult\CsvArchive", true);
            }

            Compress.FolderToFile(
                @"C:\Temp\CsvArchive",
                @"C:\Temp\CsvArchive.inpkg");

            Assert.Throws<System.Security.SecurityException>(()=>
            Decompress.FromFileToFolder(
                @"C:\Temp\CsvArchive.inpkg",
                @"C:\Temp\DataResult\CsvArchive",
                ".\\certs\\core.example.com.pfx.cer"));
        }
    }
}