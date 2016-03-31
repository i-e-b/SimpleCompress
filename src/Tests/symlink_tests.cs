namespace Tests
{
    using NUnit.Framework;
    using SimpleCompress;

    [TestFixture]
    public class symlink_tests
    {
        [Test]
        public void can_get_the_target_of_a_symlink() { 
            const string linkPath = @"W:\Temp\linkTest\cont\symtarget";
            const string expectedTarget = @"W:\Temp\linkTest\target";

            var actualTarget = NativeIO.SymbolicLink.GetTarget(linkPath);

            Assert.That(actualTarget, Is.EqualTo(expectedTarget));
        }

    }
}