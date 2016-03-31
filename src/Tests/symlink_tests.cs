namespace Tests
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Win32.SafeHandles;
    using NUnit.Framework;

    [TestFixture]
    public class symlink_tests
    {
        [Test]
        public void can_get_the_target_of_a_symlink() { 
            const string linkPath = @"W:\Temp\linkTest\cont\symtarget";
            const string expectedTarget = @"W:\Temp\linkTest\target";

            var actualTarget = SymbolicLink.GetTarget(linkPath);

            Assert.That(actualTarget, Is.EqualTo(expectedTarget));
        }

        /// <remarks>
        /// Refer to http://msdn.microsoft.com/en-us/library/windows/hardware/ff552012%28v=vs.85%29.aspx
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct SymbolicLinkReparseData
        {
            // Not certain about this!
            private const int maxUnicodePathLength = 32000;

            public uint ReparseTag;
            public ushort ReparseDataLength;
            public ushort Reserved;
            public ushort SubstituteNameOffset;
            public ushort SubstituteNameLength;
            public ushort PrintNameOffset;
            public ushort PrintNameLength;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = maxUnicodePathLength)]
            public byte[] PathBuffer;
        }

        public static class SymbolicLink
        {
            private const uint GenericReadAccess = 0x80000000;
            private const uint FileFlagsForOpenReparsePointAndBackupSemantics = 0x02200000;
            private const int ioctlCommandGetReparsePoint = 0x000900A8;
            private const uint OpenExisting = 0x3;
            private const uint PathNotAReparsePointError = 0x80071126;
            private const uint ShareModeAll = 0x7; // Read, Write, Delete
            private const uint SymLinkTag = 0xA000000C;
            private const int TargetIsAFile = 0;
            private const int TargetIsADirectory = 1;

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern SafeFileHandle CreateFile(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern bool DeviceIoControl(
                IntPtr hDevice,
                uint dwIoControlCode,
                IntPtr lpInBuffer,
                int nInBufferSize,
                IntPtr lpOutBuffer,
                int nOutBufferSize,
                out int lpBytesReturned,
                IntPtr lpOverlapped);

            public static void CreateDirectoryLink(string linkPath, string targetPath)
            {
                if (CreateSymbolicLink(linkPath, targetPath, TargetIsADirectory) && Marshal.GetLastWin32Error() == 0) { return; }
                try
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                catch (COMException exception)
                {
                    throw new IOException(exception.Message, exception);
                }
            }

            public static void CreateFileLink(string linkPath, string targetPath)
            {
                if (!CreateSymbolicLink(linkPath, targetPath, TargetIsAFile))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            private static SafeFileHandle getFileHandle(string path)
            {
                return CreateFile(path, GenericReadAccess, ShareModeAll, IntPtr.Zero, OpenExisting,
                    FileFlagsForOpenReparsePointAndBackupSemantics, IntPtr.Zero);
            }

            public static string GetTarget(string path)
            {
                using (var fileHandle = getFileHandle(path))
                {
                    if (fileHandle.IsInvalid)
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }

                    int outBufferSize = Marshal.SizeOf(typeof(SymbolicLinkReparseData));
                    IntPtr outBuffer = IntPtr.Zero;
                    SymbolicLinkReparseData reparseDataBuffer;
                    try
                    {
                        outBuffer = Marshal.AllocHGlobal(outBufferSize);
                        int bytesReturned;
                        bool success = DeviceIoControl(
                            fileHandle.DangerousGetHandle(), ioctlCommandGetReparsePoint, IntPtr.Zero, 0,
                            outBuffer, outBufferSize, out bytesReturned, IntPtr.Zero);

                        fileHandle.Close();

                        if (!success)
                        {
                            if (((uint)Marshal.GetHRForLastWin32Error()) == PathNotAReparsePointError)
                            {
                                return null;
                            }
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                        }

                        reparseDataBuffer = (SymbolicLinkReparseData)Marshal.PtrToStructure(
                            outBuffer, typeof(SymbolicLinkReparseData));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(outBuffer);
                    }

                    return reparseDataBuffer.ReparseTag != SymLinkTag
                        ? null
                        : Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer, reparseDataBuffer.PrintNameOffset, reparseDataBuffer.PrintNameLength);
                }
            }
        }

    }
}