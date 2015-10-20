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
            Compress.FolderToFile(@"W:\Temp\DataToCompress", @"W:\Temp\Compressed.inpkg");
            Decompress.FromFileToFolder(@"W:\Temp\Compressed.inpkg", @"W:\Temp\inetpub\Sigma Systems\CatalogServices\SigmaCatalogServices\Sigma\ThisIsAReallyLongPath\DataResult");

            // todo: check the new output is identical to the input.

            File.Delete(@"W:\Temp\Compressed.inpkg");
        }

    }
}
