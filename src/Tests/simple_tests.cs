namespace Tests
{
    using System.IO;
    using NUnit.Framework;
    using SimpleCompress;

    [TestFixture]
    public class simple_tests
    {
        [Test]
        public void compress_a_folder_then_uncompress_results_in_same_files() {
            Compress.FolderToFile(@"C:\Temp\DataToCompress", @"C:\Temp\Compressed.inpkg");
            Decompress.FromFileToFolder(@"C:\Temp\Compressed.inpkg", @"C:\Temp\DataResult");

            // todo: check the new output is identical to the input.

            File.Delete(@"C:\Temp\Compressed.inpkg");
        }

    }
}
