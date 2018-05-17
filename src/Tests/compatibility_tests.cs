namespace Tests
{
    using NUnit.Framework;
    using SimpleCompress;

    [TestFixture, Explicit]
    public class compatibility_tests
    {
        [Test]
        public void dotnet_archives_can_be_read_by_node() {
            // Compress, then run the node by hand
            Compress.FolderToFile(@"C:\Temp\DataToCompress", @"C:\Temp\from-dotnet.inpkg");
        }

        [Test]
        public void node_archives_can_be_read_by_dotnet()
        {
            // make an archive by hand in node at the path below then run the test
            const string src = @"C:\Temp\from-node.inpkg";

            Decompress.FromFileToFolder(src, @"C:\Temp\DataResult\from-node-result");
        }
    }
}