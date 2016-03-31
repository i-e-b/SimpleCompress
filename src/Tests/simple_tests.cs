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
            Decompress.FromFileToFolder(@"W:\Temp\Compressed.inpkg", 
@"W:\Temp\DataResult\Pseudopseudohypoparathyroidism\Pneumonoultramicroscopicsilicovolcanoconiosis\Floccinaucinihilipilification\Antidisestablishmentarianism\Honorificabilitudinitatibus\Donau­dampf­schiffahrts­elektrizitäten­haupt­betriebs­werk­bau­unter­beamten­gesellschaft");

            // todo: check the new output is identical to the input.

            File.Delete(@"W:\Temp\Compressed.inpkg");
        }

        [Test]
        public void symbolic_links_are_merged_if_external_and_relinked_if_internal() {
            Compress.FolderToFile(@"W:\Temp\linkTest", @"W:\Temp\linktest.inpkg");
            Decompress.FromFileToFolder(@"W:\Temp\linktest.inpkg",
@"W:\Temp\DataResult\linkResult");

            // todo: check the new output is identical to the input.

            File.Delete(@"W:\Temp\linktest.inpkg");
        }


    }
}
