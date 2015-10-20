namespace SimpleCompress
{
    /*
     * 
     * This gigantic mess is a cut down version of https://github.com/i-e-b/tinyQuickIO
     * Which enables handling of 32k length paths, where .Net is limited to ~250
     * 
     */

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    using Microsoft.Win32.SafeHandles;
    using System.Security.Principal;
    using System.Security.AccessControl;
    using System.Collections.ObjectModel;
    using System.Linq;


    enum Win32SecurityObjectType
    {
        SeUnknownObjectType = 0x0,
        SeFileObject = 0x1,
        SeService = 0x2,
        SePrinter = 0x3,
        SeRegistryKey = 0x4,
        SeLmshare = 0x5,
        SeKernelObject = 0x6,
        SeWindowObject = 0x7,
        SeDsObject = 0x8,
        SeDsObjectAll = 0x9,
        SeProviderDefinedObject = 0xa,
        SeWmiguidObject = 0xb,
        SeRegistryWow6432Key = 0xc
    }

    static class Win32SafeNativeMethods
    {
        #region advapi32.dll
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        internal static extern uint GetSecurityDescriptorLength(IntPtr byteArray);



        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint GetNamedSecurityInfo(
            string unicodePath,
            Win32SecurityObjectType securityObjectType,
            Win32FileSystemEntrySecurityInformation securityInfo,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor);


        #endregion

        #region kernel32.dll

        /// <summary>
        /// Sets the last all times for files or directories
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetFileTime", ExactSpelling = true)]
        internal static extern Int32 SetAllFileTimes(SafeFileHandle fileHandle, ref long lpCreationTime, ref long lpLastAccessTime, ref long lpLastWriteTime);

        /// <summary>
        /// Sets the last creation time for files or directories
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetFileTime", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetCreationFileTime(SafeFileHandle hFile, ref long lpCreationTime, IntPtr lpLastAccessTime, IntPtr lpLastWriteTime);

        /// <summary>
        /// Sets the last acess time for files or directories
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetFileTime", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetLastAccessFileTime(SafeFileHandle hFile, IntPtr lpCreationTime, ref long lpLastAccessTime, IntPtr lpLastWriteTime);

        /// <summary>
        /// Sets the last write time for files or directories
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetFileTime", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetLastWriteFileTime(SafeFileHandle hFile, IntPtr lpCreationTime, IntPtr lpLastAccessTime, ref long lpLastWriteTime);

        /// <summary>
        /// Create directory
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateDirectory(string fullName, IntPtr securityAttributes);

        /// <summary>
        /// Creates a file / directory or opens an handle for an existing file.
        /// <b>If you want to get an handle for an existing folder use <see cref="OpenReadWriteFileSystemEntryHandle"/> with ( 0x02000000 ) as attribute and FileMode ( 0x40000000 | 0x80000000 )</b>
        /// Otherwise it you'll get an invalid handle
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile(
            string fullName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// Use this to open an handle for an existing file or directory to change for example the timestamps
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateFile")]
        internal static extern SafeFileHandle OpenReadWriteFileSystemEntryHandle(
            string fullName,
            uint dwAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)]FileMode dwMode,
            uint dwAttribute,
            IntPtr hTemplateFile);

        /// <summary>
        /// Finds first file of given path
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Win32FileHandle FindFirstFile(string fullName, [In, Out] Win32FindData win32FindData);

        /// <summary>
        /// Finds next file of current handle
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool FindNextFile(Win32FileHandle findFileHandle, [In, Out, MarshalAs(UnmanagedType.LPStruct)] Win32FindData win32FindData);

        /// <summary>
        /// Moves a directory
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool MoveFile(string fullNameSource, string fullNameTarget);

        /// <summary>
        /// Copy file
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CopyFile(string fullNameSource, string fullNameTarget, bool failOnExists);

        /// <summary>
        /// Removes a file.
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteFile(string fullName);

        /// <summary>
        /// Removes a file.
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveDirectory(string fullName);

        /// <summary>
        /// Set File Attributes
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetFileAttributes(string fullName, uint fileAttributes);

        /// <summary>
        /// Gets Attributes of given path
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint GetFileAttributes(string fullName);

        /// <summary>
        /// Close Hnalde
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern bool FindClose(SafeHandle fileHandle);

        /// <summary>
        /// Free unmanaged memory
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LocalFree(IntPtr handle);

        #endregion

        #region netapi32.dll
        /// <summary>
        /// Enumerate shares (NT)
        /// </summary>
        /// <remarks>http://msdn.microsoft.com/en-us/library/windows/desktop/bb525387(v=vs.85).aspx</remarks>
        [DllImport("netapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetShareEnum(
            string lpServerName,
            int dwLevel,
            out IntPtr lpBuffer,
            int dwPrefMaxLen,
            out int entriesRead,
            out int totalEntries,
            ref int hResume);

        [DllImport("netapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetApiBufferFree(IntPtr lpBuffer);
        #endregion
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    [BestFitMapping(false)]
    public class Win32FindData
    {
        /// <summary>
        /// File Attributes
        /// </summary>
        public FileAttributes dwFileAttributes;

        /// <summary>
        /// Last Creation Time (Low DateTime)
        /// </summary>
        public UInt32 ftCreationTime_dwLowDateTime;

        /// <summary>
        /// Last Creation Time (High DateTime)
        /// </summary>
        public UInt32 ftCreationTime_dwHighDateTime;

        /// <summary>
        /// Last Access Time (Low DateTime)
        /// </summary>
        public UInt32 ftLastAccessTime_dwLowDateTime;

        /// <summary>
        /// Last Access Time (High DateTime)
        /// </summary>
        public UInt32 ftLastAccessTime_dwHighDateTime;

        /// <summary>
        /// Last Write Time (Low DateTime)
        /// </summary>
        public UInt32 ftLastWriteTime_dwLowDateTime;

        /// <summary>
        /// Last Write Time (High DateTime)
        /// </summary>
        public UInt32 ftLastWriteTime_dwHighDateTime;

        /// <summary>
        /// File Size High
        /// </summary>
        public UInt32 nFileSizeHigh;

        /// <summary>
        /// File Size Low
        /// </summary>
        public UInt32 nFileSizeLow;

        /// <summary>
        /// Reserved
        /// </summary>
        public Int32 dwReserved0;

        /// <summary>
        /// Reserved
        /// </summary>
        public int dwReserved1;

        /// <summary>
        /// File name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        /// <summary>
        /// Alternate File Name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;

        /// <summary>
        /// Creates a new Instance
        /// </summary>
        public static Win32FindData New
        {
            get
            {
                return new Win32FindData();
            }
        }

        /// <summary>
        /// Returns the total size in bytes
        /// </summary>
        /// <returns></returns>
        public UInt64 CalculateBytes()
        {
            return ((UInt64)nFileSizeHigh << 32 | nFileSizeLow);
        }


        internal static DateTime ConvertDateTime(UInt32 high, UInt32 low)
        {
            return DateTime.FromFileTimeUtc((((Int64)high) << 0x20) | low);
        }

        /// <summary>
        /// Gets last write time based on UTC
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastWriteTimeUtc()
        {
            return ConvertDateTime(ftLastWriteTime_dwHighDateTime, ftLastWriteTime_dwLowDateTime);
        }

        /// <summary>
        /// Gets last access time based on UTC
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastAccessTimeUtc()
        {
            return ConvertDateTime(ftLastAccessTime_dwHighDateTime, ftLastAccessTime_dwLowDateTime);
        }

        /// <summary>
        /// Gets the creation time based on UTC
        /// </summary>
        /// <returns></returns>
        public DateTime GetCreationTimeUtc()
        {
            return ConvertDateTime(ftCreationTime_dwHighDateTime, ftCreationTime_dwLowDateTime);
        }


    }
    [Flags]
    internal enum Win32FileSystemEntrySecurityInformation : uint
    {
        OwnerSecurityInformation = 1,
        GroupSecurityInformation = 2,

        DaclSecurityInformation = 4,
        SaclSecurityInformation = 8,

        UnprotectedSaclSecurityInformation = 0x10000000,
        UnprotectedDaclSecurityInformation = 0x20000000,

        ProtectedSaclSecurityInformation = 0x40000000,
        ProtectedDaclSecurityInformation = 0x80000000
    }

    /// <summary>
    /// Provides a class for Win32 safe handle implementations
    /// </summary>
    internal sealed class Win32FileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the Win32ApiFileHandle class, specifying whether the handle is to be reliably released.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal Win32FileHandle()
            : base(true)
        {
        }

        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            if (!(IsInvalid || IsClosed))
            {
                return Win32SafeNativeMethods.FindClose(this);
            }
            return (IsInvalid || IsClosed);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Win32ApiFileHandle class specifying whether to perform a normal dispose operation. 
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!(IsInvalid || IsClosed))
            {
                Win32SafeNativeMethods.FindClose(this);
            }
            base.Dispose(disposing);
        }
    }
    /// <summary>
    /// Performs operations for files or directories and path information. 
    /// </summary>
    public static class PathTools
    {
        public const String RegularLocalPathPrefix = @"";
        public const String RegularSharePathPrefix = @"\\";
        public static readonly Int32 RegularSharePathPrefixLength = RegularSharePathPrefix.Length;
        public const String UncLocalPathPrefix = @"\\?\";
        public const String UncSharePathPrefix = @"\\?\UNC\";
        public static readonly Int32 UncSharePathPrefixLength = UncSharePathPrefix.Length;

        /// <summary>
        /// Converts unc path to regular path
        /// </summary>
        public static String ToRegularPath(String anyFullname)
        {
            // First: Check for UNC QuickIOShareInfo
            if (anyFullname.StartsWith(UncSharePathPrefix, StringComparison.Ordinal))
            {
                return ToShareRegularPath(anyFullname); // Convert
            }
            if (anyFullname.StartsWith(UncLocalPathPrefix, StringComparison.Ordinal))
            {
                return ToLocalRegularPath(anyFullname); // Convert
            }
            return anyFullname;
        }

        /// <summary>
        /// Converts an unc path to a local regular path
        /// </summary>
        /// <param name="uncLocalPath">Unc Path</param>
        /// <example>\\?\C:\temp\file.txt >> C:\temp\file.txt</example>
        /// <returns>Local Regular Path</returns>
        public static String ToLocalRegularPath(String uncLocalPath)
        {
            return uncLocalPath.Substring(UncLocalPathPrefix.Length);
        }

        /// <summary>
        /// Converts an unc path to a share regular path
        /// </summary>
        /// <param name="uncSharePath">Unc Path</param>
        /// <example>\\?\UNC\server\share >> \\server\share</example>
        /// <returns>QuickIOShareInfo Regular Path</returns>
        public static String ToShareRegularPath(String uncSharePath)
        {
            return RegularSharePathPrefix + uncSharePath.Substring(UncSharePathPrefix.Length);
        }

        /// <summary>
        /// Gets name of file or directory
        /// </summary>
        /// <param name="fullName">Path</param>
        /// <returns>Name of file or directory</returns>
        public static String GetName(String fullName)
        {
            var path = TrimTrailingSepartor(fullName);
            var sepPosition = path.LastIndexOf(Path.DirectorySeparatorChar);

            return sepPosition == -1 ? path : path.Substring(sepPosition + 1);
        }

        /// <summary>
        /// Removes Last <see cref="Path.DirectorySeparatorChar "/>
        /// </summary>
        private static String TrimTrailingSepartor(String path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Invalid Chars are: " &lt; &gt; | and all chars lower than ASCII value 32
        /// </summary>
        /// <remarks>Ignores Unix File Systems</remarks>
        /// <param name="path">Path to check</param>
        public static void ThrowIfPathContainsInvalidChars(String path)
        {
            if (path.Any(currentChar => currentChar < 32 || currentChar == '\"' || currentChar == '<' || currentChar == '>' || currentChar == '|'))
            {
                throw new Exception("Path contains invalid characters" + path);
            }
        }

        /// <summary>
        /// Combines given path elements
        /// </summary>
        /// <param name="pathElements">Path elements to combine</param>
        /// <returns>Combined Path</returns>
        public static String Combine(params String[] pathElements)
        {
            if (pathElements == null || pathElements.Length == 0)
            {
                throw new ArgumentNullException("pathElements", "Cannot be null or empty");
            }

            // Verify not required; System.IO.Path.Combine calls internal path invalid char verifier

            // First Element
            var combinedPath = pathElements[0];

            // Other elements
            for (var i = 1; i < pathElements.Length; i++)
            {
                var el = pathElements[i];

                // Combine
                combinedPath = Path.Combine(combinedPath, el);
            }

            return combinedPath;
        }

        /// <summary>
        /// Returns true if path is local regular path such as 'C:\folder\folder\file.txt'
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>True if path is local regular path</returns>
        public static Boolean IsLocalRegularPath(String path)
        {
            return (path.Length >= 3 && Char.IsLetter(path[0]) && path[1] == ':' && path[2] == Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Returns true if path is local UNC path such as '\\?\C:\folder\folder\file.txt'
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>True if path is local UNC path</returns>
        public static Boolean IsLocalUncPath(String path)
        {
            return (path.Length >= 7 && path[0] == '\\' && path[1] == '\\' && (path[2] == '?' || path[2] == '.') && path[3] == '\\' && IsLocalRegularPath(path.Substring(4)));
        }

        /// <summary>
        /// Returns true if path is share regular path such as '\\server\share\folder\file.txt'
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>True if path is share regular path</returns>
        public static Boolean IsShareRegularPath(String path)
        {
            if (!path.StartsWith(RegularSharePathPrefix, StringComparison.Ordinal))
            {
                return false;
            }
            if (path.StartsWith(UncSharePathPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var pathElements = path.Substring(RegularSharePathPrefixLength).Split('\\');
            return (pathElements.Length >= 2);
        }

        /// <summary>
        /// Returns true if path is share UNC path such as '\\?\UNC\server\share\folder\file.txt'
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>True if path is share UNC path</returns>
        public static Boolean IsShareUncPath(String path)
        {
            if (!path.StartsWith(UncSharePathPrefix))
            {
                return false;
            }

            var pathElements = path.Substring(UncSharePathPrefixLength).Split('\\');
            return (pathElements.Length >= 2);
        }

        /// <summary>
        /// Try to parse path
        /// </summary>
        /// <param name="path">Path to parse</param>
        /// <param name="parsePathResult">result</param>
        /// <param name="supportRelativePath">true to support relative path</param>
        /// <returns>True on success. <paramref name="parsePathResult"/> is set.</returns>
        public static Boolean TryParsePath(String path, out PathResult parsePathResult, bool supportRelativePath = true)
        {
            if (TryParseLocalRegularPath(path, out parsePathResult))
            {
                return true;
            }
            if (TryParseLocalUncPath(path, out parsePathResult))
            {
                return true;
            }
            if (TryParseShareRegularPath(path, out parsePathResult))
            {
                return true;
            }
            if (TryParseShareUncPath(path, out parsePathResult))
            {
                return true;
            }

            if (supportRelativePath && TryParseLocalRegularPath(Path.GetFullPath(path), out parsePathResult))
            {
                return true;
            }

            return false;
        }

        public static PathResult ParsePath(string path, bool supportRelativePath = true)
        {
            PathResult result;
            if (!TryParsePath(path, out result))
            {
                throw new Exception("Invalid path at " + path);
            }

            return result;
        }


        /// <summary>
        /// Returns true if specified <paramref name="path"/> is local regular path and returns result due to <paramref name="parsePathResult"/>
        /// </summary>
        /// <param name="path">Local path to parse</param>
        /// <param name="parsePathResult"><see cref="PathResult"/></param>
        /// <returns>True if parse succeeded and <paramref name="parsePathResult"/> is filled</returns>
        public static Boolean TryParseLocalRegularPath(String path, out PathResult parsePathResult)
        {
            if (!IsLocalRegularPath(path))
            {
                parsePathResult = null;
                return false;
            }

            parsePathResult = new PathResult { PathLocation = LocalOrShare.Local, PathType = UncOrRegular.Regular };

            if (path.Length == 3)
            {
                parsePathResult.IsRoot = true;
                parsePathResult.ParentPath = null;
                parsePathResult.RootPath = null;
                parsePathResult.Name = null;
                parsePathResult.FullNameUnc = UncLocalPathPrefix + path;
                parsePathResult.FullName = path;
            }
            else
            {
                parsePathResult.IsRoot = false;
                parsePathResult.FullName = path.TrimEnd(Path.DirectorySeparatorChar);
                parsePathResult.FullNameUnc = UncLocalPathPrefix + parsePathResult.FullName;
                parsePathResult.ParentPath = parsePathResult.FullName.Substring(0, parsePathResult.FullName.LastIndexOf(Path.DirectorySeparatorChar));
                parsePathResult.RootPath = path.Substring(0, 3);

                parsePathResult.Name = parsePathResult.FullName.Substring(parsePathResult.FullName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }

            return true;
        }

        /// <summary>
        /// Returns true if specified <paramref name="path"/> is local UNC path and returns result due to <paramref name="parsePathResult"/>
        /// </summary>
        /// <param name="path">Local UNC path to parse</param>
        /// <param name="parsePathResult"><see cref="PathResult"/></param>
        /// <returns>True if parse succeeded and <paramref name="parsePathResult"/> is filled</returns>
        public static Boolean TryParseLocalUncPath(String path, out PathResult parsePathResult)
        {
            if (!IsLocalUncPath(path))
            {
                parsePathResult = null;
                return false;
            }

            parsePathResult = new PathResult { PathLocation = LocalOrShare.Local, PathType = UncOrRegular.UNC };

            if (path.Length == 7)
            {
                parsePathResult.IsRoot = true;
                parsePathResult.ParentPath = null;
                parsePathResult.RootPath = null;

                parsePathResult.FullNameUnc = path;
                parsePathResult.FullName = path.Substring(4);
                parsePathResult.Name = null;
            }
            else
            {
                parsePathResult.IsRoot = false;
                parsePathResult.FullNameUnc = path.TrimEnd(Path.DirectorySeparatorChar);
                parsePathResult.FullName = parsePathResult.FullNameUnc.Substring(4);

                parsePathResult.ParentPath = parsePathResult.FullName.Substring(0, parsePathResult.FullName.LastIndexOf(Path.DirectorySeparatorChar));
                parsePathResult.RootPath = path.Substring(4, 3);

                parsePathResult.Name = parsePathResult.FullName.Substring(parsePathResult.FullName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }

            return true;
        }

        /// <summary>
        /// Returns true if specified <paramref name="path"/> is share regular path and returns result due to <paramref name="parsePathResult"/>
        /// </summary>
        /// <param name="path">QuickIOShareInfo regular path to parse</param>
        /// <param name="parsePathResult"><see cref="PathResult"/></param>
        /// <returns>True if parse succeeded and <paramref name="parsePathResult"/> is filled</returns>
        public static Boolean TryParseShareRegularPath(String path, out PathResult parsePathResult)
        {
            if (!IsShareRegularPath(path))
            {
                parsePathResult = null;
                return false;
            }

            parsePathResult = new PathResult { PathLocation = LocalOrShare.Share, PathType = UncOrRegular.Regular };

            var cleanedPath = path.TrimEnd('\\');

            var pathElements = cleanedPath.Substring(RegularSharePathPrefixLength).Split('\\');

            var server = pathElements[0];
            var name = pathElements[1];

            var rootPath = RegularSharePathPrefix + server + @"\" + name;

            var completePath = rootPath;
            for (int i = 2; i < pathElements.Length; i++)
            {
                completePath += "\\" + pathElements[i];
            }

            // set
            parsePathResult.IsRoot = (cleanedPath == rootPath);

            if (parsePathResult.IsRoot)
            {
                parsePathResult.ParentPath = null;
                parsePathResult.RootPath = null;
                parsePathResult.Name = null;
                parsePathResult.FullNameUnc = UncSharePathPrefix + server + @"\" + name;
                parsePathResult.FullName = RegularSharePathPrefix + server + @"\" + name;
            }
            else
            {
                parsePathResult.FullName = cleanedPath;
                parsePathResult.FullNameUnc = UncSharePathPrefix + cleanedPath.Substring(2);
                parsePathResult.ParentPath = completePath.Substring(0, completePath.LastIndexOf(Path.DirectorySeparatorChar));
                parsePathResult.RootPath = rootPath;

                parsePathResult.Name = pathElements[pathElements.Length - 1];
            }

            return true;
        }

        /// <summary>
        /// Returns true if specified <paramref name="path"/> is share UNC path and returns result due to <paramref name="parsePathResult"/>
        /// </summary>
        /// <param name="path">QuickIOShareInfo UNC path to parse</param>
        /// <param name="parsePathResult"><see cref="PathResult"/></param>
        /// <returns>True if parse succeeded and <paramref name="parsePathResult"/> is filled</returns>
        public static Boolean TryParseShareUncPath(String path, out PathResult parsePathResult)
        {
            if (!IsShareUncPath(path))
            {
                parsePathResult = null;
                return false;
            }

            parsePathResult = new PathResult { PathLocation = LocalOrShare.Share, PathType = UncOrRegular.UNC };

            var cleanedPath = path.TrimEnd('\\');

            var pathElements = cleanedPath.Substring(UncSharePathPrefixLength).Split('\\');

            var server = pathElements[0];
            var name = pathElements[1];

            var completeRelativePath = server + @"\" + name;
            for (int i = 2; i < pathElements.Length; i++)
            {
                completeRelativePath += "\\" + pathElements[i];
            }

            // set
            parsePathResult.IsRoot = (cleanedPath == (UncSharePathPrefix + server + @"\" + name));

            if (parsePathResult.IsRoot)
            {
                parsePathResult.ParentPath = null;
                parsePathResult.RootPath = null;
                parsePathResult.Name = null;
                parsePathResult.FullNameUnc = UncSharePathPrefix + server + @"\" + name;
                parsePathResult.FullName = RegularSharePathPrefix + server + @"\" + name;
            }
            else
            {
                parsePathResult.FullName = RegularSharePathPrefix + completeRelativePath;
                parsePathResult.FullNameUnc = UncSharePathPrefix + completeRelativePath;
                parsePathResult.ParentPath = RegularSharePathPrefix + completeRelativePath.Substring(0, completeRelativePath.LastIndexOf(Path.DirectorySeparatorChar));
                parsePathResult.RootPath = RegularSharePathPrefix + server + @"\" + name;

                parsePathResult.Name = pathElements[pathElements.Length - 1];
            }

            return true;
        }
    }
    public class PathResult
    {
        /// <summary>
        /// Full root path
        /// </summary>
        /// <example><b>C:\folder\parent\file.txt</b> returns <b>C:\</b></example>
        /// <remarks>Returns null if source path is Root</remarks>
        public String RootPath { get; internal set; }

        /// <summary>
        /// Full parent path
        /// </summary>
        /// <example><b>C:\folder\parent\file.txt</b> returns <b>C:\folder\parent</b></example>
        /// <remarks>Returns null if source path is Root</remarks>
        public String ParentPath { get; internal set; }

        /// <summary>
        /// Name of file or directory
        /// </summary>
        /// <example><b>C:\folder\parent\file.txt</b> returns <b>file.txt</b></example>
        /// <example><b>C:\folder\parent</b> returns <b>parent</b></example>
        /// <remarks>Returns null if source path is Root</remarks>
        public String Name { get; internal set; }

        /// <summary>
        /// True if source path is root
        /// </summary>
        public Boolean IsRoot { get; internal set; }

        /// <summary>
        /// Full path without trailing directory separtor char
        /// </summary>
        public String FullName { get; internal set; }

        /// <summary>
        /// Full UNC path without trailing directory separtor char
        /// </summary>
        public string FullNameUnc { get; internal set; }

        /// <summary>
        /// <see cref="UncOrRegular"/>
        /// </summary>
        public UncOrRegular PathType { get; internal set; }

        /// <summary>
        /// <see cref="LocalOrShare"/>
        /// </summary>
        public LocalOrShare PathLocation { get; internal set; }

    }
    /// <summary>
    /// Provides properties and instance method for paths
    /// </summary>
    public sealed class PathInfo
    {
        /// <summary>
        /// Creates the path information container
        /// </summary>
        /// <param name="anyFullname">Full path to the file or directory (regular or unc)</param>
        public PathInfo(String anyFullname)
            : this(anyFullname, PathTools.GetName(anyFullname))
        {
            PathTools.ThrowIfPathContainsInvalidChars(anyFullname);
        }

        /// <summary>
        /// Creates the path information container
        /// </summary>
        /// <param name="anyFullname">Full path to the file or directory (regular or unc). Relative path will be recognized as local regular path.</param>
        /// <param name="name">Name of file or directory</param>
        public PathInfo(String anyFullname, String name)
        {
            PathResult parsePathResult;
            if (!PathTools.TryParsePath(anyFullname, out parsePathResult))
            {
                // Unknown path
                throw new Exception("Unable to parse path " + anyFullname);
            }

            TransferParseResult(parsePathResult);

            Name = name;
        }

        /// <summary>
        /// Transfers properties from result to current instance
        /// </summary>
        /// <param name="parsePathResult"></param>
        private void TransferParseResult(PathResult parsePathResult)
        {
            FullNameUnc = parsePathResult.FullNameUnc;
            FullName = parsePathResult.FullName;
            ParentFullName = parsePathResult.ParentPath;
            RootFullName = parsePathResult.RootPath;
            IsRoot = parsePathResult.IsRoot;
            PathLocation = parsePathResult.PathLocation;
            PathType = parsePathResult.PathType;

            if (PathLocation == LocalOrShare.Local)
            {
                var testRoot = IsRoot ? FullName : RootFullName;

                if (!Array.Exists(Environment.GetLogicalDrives(), drve => drve.Equals(testRoot, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new Exception("UnsupportedDriveType " + testRoot);
                }
            }

        }

        /// <summary>
        /// Path to file or directory (regular format)
        /// </summary>
        public String FullName { get; private set; }

        /// <summary>
        /// Path to file or directory (unc format)
        /// </summary>
        public String FullNameUnc { get; private set; }

        /// <summary>
        /// Name of file or directory
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// <see cref="PathType"/>
        /// </summary>
        public UncOrRegular PathType { get; set; }

        /// <summary>
        /// Fullname of Root. null if current path is root.
        /// </summary>
        public string RootFullName { get; set; }

        /// <summary>
        /// Fullname of Parent. null if current path is root.
        /// </summary>
        public string ParentFullName { get; set; }

        /// <summary>
        /// Parent Directory
        /// </summary>
        public PathInfo Parent
        {
            get { return (ParentFullName == null ? null : new PathInfo(ParentFullName)); }
        }

        /// <summary>
        /// <see cref="LocalOrShare"/> of current path
        /// </summary>
        public LocalOrShare PathLocation { get; private set; }

        /// <summary>
        /// FindData
        /// </summary>
        internal Win32FindData FindData
        {
            get
            {
                if (IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide owner access");
                }
                return _findData ?? (_findData = NativeIO.GetFindDataFromPath(this));
            }
            set
            {
                _findData = value;
            }
        }
        private Win32FindData _findData;

        /// <summary>
        /// Attributes. Cached.
        /// </summary>
        /// <exception cref="NotSupportedException">if path is root</exception>
        public FileAttributes Attributes
        {
            get
            {
                if (IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide attributes");
                }
                return FindData.dwFileAttributes;
            }
        }

        /// <summary>
        /// Returns true if current path is root
        /// </summary>
        public bool IsRoot { get; private set; }

        /// <summary>
        /// Returns Root or null if current path is root
        /// </summary>
        public PathInfo Root
        {
            get { return (RootFullName == null ? null : new PathInfo(RootFullName)); }
        }

        /// <summary>
        /// Returns true if path exists.
        /// </summary>
        /// <returns></returns>
        public Boolean Exists
        {
            get
            {
                return NativeIO.Exists(this);
            }
        }

        /// <summary>
        /// Returns current <see cref="FileSecurity"/>
        /// </summary>
        /// <returns><see cref="FileSecurity"/></returns>
        public FileSecurity GetFileSystemSecurity()
        {
            return new FileSecurity(this);
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="NTAccount"/></returns>
        public NTAccount GetOwner()
        {
            if (IsRoot)
            {
                throw new NotSupportedException("Root directory does not provide owner access");
            }
            return GetOwnerIdentifier().Translate(typeof(NTAccount)) as NTAccount;
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="IdentityReference"/></returns>
        public IdentityReference GetOwnerIdentifier()
        {
            if (IsRoot)
            {
                throw new NotSupportedException("Root directory does not provide owner access");
            }
            return GetFileSystemSecurity().FileSystemSecurityInformation.GetOwner(typeof(SecurityIdentifier));
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        public void SetOwner(NTAccount newOwner)
        {
            if (IsRoot)
            {
                throw new NotSupportedException("Root directory does not provide owner access");
            }
            GetFileSystemSecurity().FileSystemSecurityInformation.SetOwner(newOwner.Translate(typeof(SecurityIdentifier)));
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        public void SetOwner(IdentityReference newOwersIdentityReference)
        {
            if (IsRoot)
            {
                throw new NotSupportedException("Root directory does not provide owner access");
            }
            GetFileSystemSecurity().FileSystemSecurityInformation.SetOwner(newOwersIdentityReference);
        }
    }

    public abstract class MetadataBase
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="uncResultPath"></param>
        protected MetadataBase(string uncResultPath)
        {
            FullNameUnc = uncResultPath;
        }

        /// <summary>
        /// Transfers data from find data
        /// </summary>
        internal void SetFindData(Win32FindData win32FindData)
        {
            LastWriteTimeUtc = win32FindData.GetLastWriteTimeUtc();
            LastAccessTimeUtc = win32FindData.GetLastAccessTimeUtc();
            CreationTimeUtc = win32FindData.GetCreationTimeUtc();

            Name = win32FindData.cFileName;

            Attributes = win32FindData.dwFileAttributes;
        }

        /// <summary>
        /// Name of file or directory
        /// </summary>
        public String Name { get; private set; }
        /// <summary>
        /// Path to file or directory (unc format)
        /// </summary>
        public String FullNameUnc { get; private set; }

        #region FileTimes
        /// <summary>
        /// Gets the creation time (UTC)
        /// </summary>
        public DateTime CreationTimeUtc { get; private set; }
        /// <summary>
        /// Gets the creation time
        /// </summary>
        public DateTime CreationTime
        {
            get
            {
                return LastWriteTimeUtc.ToLocalTime();
            }
        }

        /// <summary>
        /// Gets the time (UTC) of last access. 
        /// </summary>
        public DateTime LastAccessTimeUtc { get; private set; }
        /// <summary>
        /// Gets the time that the  file was last accessed
        /// </summary>
        public DateTime LastAccessTime
        {
            get
            {
                return LastAccessTimeUtc.ToLocalTime();
            }
        }

        /// <summary>
        /// Gets the time (UTC) was last written to
        /// </summary>
        public DateTime LastWriteTimeUtc { get; private set; }
        /// <summary>
        /// Gets the time the file was last written to.
        /// </summary>
        public DateTime LastWriteTime
        {
            get
            {
                return LastWriteTimeUtc.ToLocalTime();
            }
        }
        #endregion

        /// <summary>
        /// File Attributes
        /// </summary>
        public FileAttributes Attributes { get; internal set; }

        /// <summary>
        /// Returns a new instance of <see cref="PathInfo"/> of the current path
        /// </summary>
        /// <returns><see cref="PathInfo"/></returns>
        public PathInfo ToPathInfo()
        {
            return new PathInfo(FullNameUnc);
        }
    }

    /// <summary>
    /// File metadata information
    /// </summary>
    public sealed class FileMetadata : MetadataBase
    {
        /// <summary>
        /// Creates instance of <see cref="FileMetadata"/>
        /// </summary>
        /// <param name="uncResultPath">UNC Path of current file</param>
        /// <param name="win32FindData">Win32FindData of current file</param>
        internal FileMetadata(string uncResultPath, Win32FindData win32FindData)
            : base(uncResultPath)
        {
            SetFindData(win32FindData);

            Bytes = win32FindData.CalculateBytes();
        }

        /// <summary>
        /// Size of the file. 
        /// </summary>
        public UInt64 Bytes { get; private set; }
    }

    /// <summary>
    /// Directory metadata information
    /// </summary>
    public sealed class DirectoryMetadata : MetadataBase
    {
        /// <summary>
        /// Creates instance of <see cref="DirectoryMetadata"/>
        /// </summary>
        /// <param name="win32FindData">Win32FindData of current directory</param>
        /// <param name="subDirs">Directories in current directory</param>
        /// <param name="subFiles">Files in current directory</param>
        /// <param name="uncFullname">UNC Path of current directory</param>
        internal DirectoryMetadata(string uncFullname, Win32FindData win32FindData, IList<DirectoryMetadata> subDirs, IList<FileMetadata> subFiles)
            : base(uncFullname)
        {
            Directories = new ReadOnlyCollection<DirectoryMetadata>(subDirs);
            Files = new ReadOnlyCollection<FileMetadata>(subFiles);

            SetFindData(win32FindData);
        }

        /// <summary>
        /// Directories in current directory
        /// </summary>
        public ReadOnlyCollection<DirectoryMetadata> Directories { get; internal set; }

        /// <summary>
        /// Files in current directory
        /// </summary>
        public ReadOnlyCollection<FileMetadata> Files { get; internal set; }

        UInt64? _bytes;
        /// <summary>
        /// Size of the file. 
        /// </summary>
        public UInt64 Bytes
        {
            get
            {
                if (_bytes != null)
                {
                    return (UInt64)_bytes;
                }
                _bytes = Directories.Aggregate<DirectoryMetadata, ulong>(0, (current, t) => current + +t.Bytes) + Files.Aggregate(_bytes, (current, t) => current + +t.Bytes);

                return _bytes ?? 0;
            }
        }
    }



    public enum LocalOrShare
    {
        Local,
        Share
    }

    public enum UncOrRegular
    {
        Regular,
        UNC
    }

    public enum FileOrDirectory
    {
        File = 0,
        Directory = 1
    }

    public enum SuppressExceptions
    {
        None,
        SuppressAllExceptions
    }

    public enum AdminOrNormal
    {
        Admin = 2,
        Normal = 1
    }
    public class FileSecurity
    {
        /// <summary>
        /// Creates new instance of <see cref="FileSecurity"/> for specified path.
        /// Current Windows Identtiy is used.
        /// </summary>
        /// <param name="pathInfo"></param>
        public FileSecurity(PathInfo pathInfo)
            : this(pathInfo, WindowsIdentity.GetCurrent())
        {

        }

        /// <summary>
        /// Supply the path to the file or directory and a user or group. 
        /// Access checks are done
        /// during instantiation to ensure we always have a valid object
        /// </summary>
        /// <param name="pathInfo"></param>
        /// <param name="principal"></param>
        public FileSecurity(PathInfo pathInfo, WindowsIdentity principal)
        {
            if (pathInfo == null)
            {
                throw new ArgumentNullException("pathInfo");
            }
            if (principal == null)
            {
                throw new ArgumentNullException("principal");
            }

            PathInfo = pathInfo;
            WindowsIdentity = principal;

            Refresh();
        }

        /// <summary>
        /// Refreshes the Information
        /// </summary>
        public void Refresh()
        {
            GetSecurityFromFileSystem();
            ReadSecurityInformation();
        }

        /// <summary>
        /// Affected Windows IDentity
        /// </summary>
        public WindowsIdentity WindowsIdentity { get; private set; }

        /// <summary>
        /// Affected path
        /// </summary>
        public PathInfo PathInfo { get; private set; }

        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedAppendData { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedChangePermissions { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedCreateDirectories { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedCreateFiles { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedDelete { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedDeleteSubdirectoriesAndFiles { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedExecuteFile { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedFullControl { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedListDirectory { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedModify { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedRead { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedReadAndExecute { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedReadAttributes { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedReadData { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedReadExtendedAttributes { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedReadPermissions { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedSynchronize { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedTakeOwnership { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedTraverse { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedWrite { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedWriteAttributes { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedWriteData { get; private set; }
        /// <summary>
        /// Returns true if specified right level is denied
        /// </summary>
        public bool IsDeniedWriteExtendedAttributes { get; private set; }

        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedAppendData { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedChangePermissions { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedCreateDirectories { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedCreateFiles { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedDelete { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedDeleteSubdirectoriesAndFiles { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedExecuteFile { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedFullControl { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedListDirectory { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedModify { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedRead { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedReadAndExecute { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedReadAttributes { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedReadData { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedReadExtendedAttributes { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedReadPermissions { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedSynchronize { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedTakeOwnership { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedTraverse { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedWrite { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedWriteAttributes { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedWriteData { get; private set; }
        /// <summary>
        /// Returns true if specified right level is explizit allowed
        /// </summary>
        public bool IsAllowedWriteExtendedAttributes { get; private set; }

        /// <summary>
        /// Ermittelt, ob etwas hinzugefügt werden kann (Dateien)
        /// </summary>
        public bool CanAppendData
        {
            get { return !IsDeniedAppendData && IsAllowedAppendData; }
        }

        /// <summary>
        /// Ermittelt, ob die Rechte verändert werden dürfen
        /// </summary>
        public bool CanChangePermissions
        {
            get { return !IsDeniedChangePermissions && IsAllowedChangePermissions; }
        }

        /// <summary>
        /// Ermittelt, ob neue Ordner hinzugefügt werden dürfen (Ordner)
        /// </summary>
        public bool CanCreateDirectories
        {
            get { return !IsDeniedCreateDirectories && IsAllowedCreateDirectories; }
        }

        /// <summary>
        /// Ermittelt, ob neue Dateien hinzugefügt werden dürfen (Ordner)
        /// </summary>
        public bool CanCreateFiles
        {
            get { return !IsDeniedCreateFiles && IsAllowedCreateFiles; }
        }

        /// <summary>
        /// Ermittelt, ob etwas gelöscht werden darf (Ordner)
        /// </summary>
        public bool CanDelete
        {
            get { return !IsDeniedDelete && IsAllowedDelete; }
        }

        /// <summary>
        /// Ermittelt, ob darunterliegende Ordner und Dateien gelöscht werden dürfen (Ordner)
        /// </summary>
        public bool CanDeleteSubdirectoriesAndFiles
        {
            get
            {
                return !IsDeniedDeleteSubdirectoriesAndFiles &&
                       IsAllowedDeleteSubdirectoriesAndFiles;
            }
        }

        /// <summary>
        /// Ermittelt, ob eine Datei ausgeführt werden darf (Dateien)
        /// </summary>
        public bool CanExecuteFile
        {
            get { return !IsDeniedExecuteFile && IsAllowedExecuteFile; }
        }

        /// <summary>
        /// Ermittelt, ob die vollständige Kontrolle gewährt ist
        /// </summary>
        public bool CanFullControl
        {
            get { return !IsDeniedFullControl && IsAllowedFullControl; }
        }

        /// <summary>
        /// Ermittelt, ob die Ordner aufgelistet werden dürfen (Ordner)
        /// </summary>
        public bool CanListDirectory
        {
            get { return !IsDeniedListDirectory && IsAllowedListDirectory; }
        }

        /// <summary>
        /// Ermittelt, ob etwas verändert werden darf
        /// </summary>
        public bool CanModify
        {
            get { return !IsDeniedModify && IsAllowedModify; }
        }

        /// <summary>
        /// Ermittelt, ob etwas gelesen werden darf
        /// </summary>
        public bool CanRead
        {
            get { return !IsDeniedRead && IsAllowedRead; }
        }

        /// <summary>
        /// Ermittelt, ob gelesen und ausgeführt werden darf
        /// </summary>
        public bool CanReadAndExecute
        {
            get { return !IsDeniedReadAndExecute && IsAllowedReadAndExecute; }
        }

        /// <summary>
        /// Ermittelt, ob die Attribute gelesen werden dürfen
        /// </summary>
        public bool CanReadAttributes
        {
            get { return !IsDeniedReadAttributes && IsAllowedReadAttributes; }
        }
        /// <summary>
        /// Ermittelt, ob Daten gelesen werden dürfen
        /// </summary>
        public bool CanReadData
        {
            get { return !IsDeniedReadData && IsAllowedReadData; }
        }

        /// <summary>
        /// Ermittelt, ob die erweiterten Attribute gelesen werden dürfen
        /// </summary>
        public bool CanReadExtendedAttributes
        {
            get
            {
                return !IsDeniedReadExtendedAttributes &&
                       IsAllowedReadExtendedAttributes;
            }
        }
        /// <summary>
        /// Ermittelt, ob die Rechte gelesen werden dürfen
        /// </summary>
        public bool CanReadPermissions
        {
            get { return !IsDeniedReadPermissions && IsAllowedReadPermissions; }
        }

        /// <summary>
        /// Ermittelt, ob synchronisiert werden darf
        /// </summary>
        public bool CanSynchronize
        {
            get { return !IsDeniedSynchronize && IsAllowedSynchronize; }
        }
        /// <summary>
        /// Ermittelt, ob der Besitzerstatus eingenommen werden darf
        /// </summary>
        public bool CanTakeOwnership
        {
            get { return !IsDeniedTakeOwnership && IsAllowedTakeOwnership; }
        }

        /// <summary>
        /// Ermittelt, ob ???????
        /// </summary>
        public bool CanTraverse
        {
            get { return !IsDeniedTraverse && IsAllowedTraverse; }
        }

        /// <summary>
        /// Ermittelt, ob geschrieben werden darf
        /// </summary>
        public bool CanWrite
        {
            get { return !IsDeniedWrite && IsAllowedWrite; }
        }

        /// <summary>
        /// Ermittelt, ob Attribute verändert werden dürfen
        /// </summary>
        public bool CanWriteAttributes
        {
            get { return !IsDeniedWriteAttributes && IsAllowedWriteAttributes; }
        }

        /// <summary>
        /// Ermittelt, ob Daten geschrieben werden dürfen
        /// </summary>
        public bool CanWriteData
        {
            get { return !IsDeniedWriteData && IsAllowedWriteData; }
        }

        /// <summary>
        /// Ermittelt, ob erweiterte Attribute geschrieben werden dürfen
        /// </summary>
        public bool CanWriteExtendedAttributes
        {
            get
            {
                return !IsDeniedWriteExtendedAttributes && IsAllowedWriteExtendedAttributes;
            }
        }

        #region Initial Read
        /// <summary>
        /// Reads the security information of <see cref="FileSystemSecurityInformation"/>
        /// </summary>
        private void ReadSecurityInformation()
        {
            // Receive File System ACL Information
            var acl = FileSystemSecurityInformation.GetAccessRules(true, true, typeof(SecurityIdentifier));

            ReceiveUserIdentityRules(acl);
            ReceivGroupIdentityRules(acl);
        }

        /// <summary>
        /// Processes the authentication data of a Windows Group
        /// </summary>
        /// <param name="acl"><see cref="AuthorizationRuleCollection"/></param>
        private void ReceivGroupIdentityRules(AuthorizationRuleCollection acl)
        {
            // Only Handle Groups
            if (WindowsIdentity.Groups == null)
            {
                return;
            }

            foreach (var identity in WindowsIdentity.Groups)
            {
                for (var i = 0; i < acl.Count; i++)
                {
                    var rule = (FileSystemAccessRule)acl[i];
                    HandleFileSystemAccessRule(rule, identity);
                }
            }
        }

        /// <summary>
        /// Processes the authentication data of a Windows identity
        /// </summary>
        /// <param name="rule">FileSystemAccessRule</param>
        /// <param name="identity"></param>
        private void HandleFileSystemAccessRule(FileSystemAccessRule rule, IdentityReference identity)
        {
            if (rule == null)
            {
                return;
            }

            // Ignore all other users
            if (identity.Equals(rule.IdentityReference))
            {
                HandleAccessControlType(rule);
            }
        }

        /// <summary>
        /// Processes the authentication data of a Windows user
        /// </summary>
        /// <param name="acl"><see cref="AuthorizationRuleCollection"/></param>
        private void ReceiveUserIdentityRules(AuthorizationRuleCollection acl)
        {
            if (WindowsIdentity.User == null)
            {
                return;
            }

            for (var i = 0; i < acl.Count; i++)
            {
                var rule = (FileSystemAccessRule)acl[i];
                HandleFileSystemAccessRule(rule, WindowsIdentity.User);
            }
        }

        /// <summary>
        /// Handles the access rights. Differentiates between allowed and denied rights
        /// </summary>
        private void HandleAccessControlType(FileSystemAccessRule rule)
        {
            switch (rule.AccessControlType)
            {
                case AccessControlType.Allow:
                    HandleAllowedAccessRule(rule);
                    break;
                case AccessControlType.Deny:
                    HandleDeniedAccessRule(rule);
                    break;
            }
        }

        /// <summary>
        /// Processed the permitted rights
        /// </summary>
        private void HandleAllowedAccessRule(FileSystemAccessRule rule)
        {
            IsAllowedAppendData = Contains(FileSystemRights.AppendData, rule);
            IsAllowedChangePermissions = Contains(FileSystemRights.ChangePermissions, rule);
            IsAllowedCreateDirectories = Contains(FileSystemRights.CreateDirectories, rule);
            IsAllowedCreateFiles = Contains(FileSystemRights.CreateFiles, rule);
            IsAllowedDelete = Contains(FileSystemRights.Delete, rule);
            IsAllowedDeleteSubdirectoriesAndFiles = Contains(FileSystemRights.DeleteSubdirectoriesAndFiles, rule);
            IsAllowedExecuteFile = Contains(FileSystemRights.ExecuteFile, rule);
            IsAllowedFullControl = Contains(FileSystemRights.FullControl, rule);
            IsAllowedListDirectory = Contains(FileSystemRights.ListDirectory, rule);
            IsAllowedModify = Contains(FileSystemRights.Modify, rule);
            IsAllowedRead = Contains(FileSystemRights.Read, rule);
            IsAllowedReadAndExecute = Contains(FileSystemRights.ReadAndExecute, rule);
            IsAllowedReadAttributes = Contains(FileSystemRights.ReadAttributes, rule);
            IsAllowedReadData = Contains(FileSystemRights.ReadData, rule);
            IsAllowedReadExtendedAttributes = Contains(FileSystemRights.ReadExtendedAttributes, rule);
            IsAllowedReadPermissions = Contains(FileSystemRights.ReadPermissions, rule);
            IsAllowedSynchronize = Contains(FileSystemRights.Synchronize, rule);
            IsAllowedTakeOwnership = Contains(FileSystemRights.TakeOwnership, rule);
            IsAllowedTraverse = Contains(FileSystemRights.Traverse, rule);
            IsAllowedWrite = Contains(FileSystemRights.Write, rule);
            IsAllowedWriteAttributes = Contains(FileSystemRights.WriteAttributes, rule);
            IsAllowedWriteData = Contains(FileSystemRights.WriteData, rule);
            IsAllowedWriteExtendedAttributes = Contains(FileSystemRights.WriteExtendedAttributes, rule);
        }

        /// <summary>
        /// Processed the denied rights
        /// </summary>
        private void HandleDeniedAccessRule(FileSystemAccessRule rule)
        {
            IsDeniedAppendData = Contains(FileSystemRights.AppendData, rule);
            IsDeniedChangePermissions = Contains(FileSystemRights.ChangePermissions, rule);
            IsDeniedCreateDirectories = Contains(FileSystemRights.CreateDirectories, rule);
            IsDeniedCreateFiles = Contains(FileSystemRights.CreateFiles, rule);
            IsDeniedDelete = Contains(FileSystemRights.Delete, rule);
            IsDeniedDeleteSubdirectoriesAndFiles = Contains(FileSystemRights.DeleteSubdirectoriesAndFiles, rule);
            IsDeniedExecuteFile = Contains(FileSystemRights.ExecuteFile, rule);
            IsDeniedFullControl = Contains(FileSystemRights.FullControl, rule);
            IsDeniedListDirectory = Contains(FileSystemRights.ListDirectory, rule);
            IsDeniedModify = Contains(FileSystemRights.Modify, rule);
            IsDeniedRead = Contains(FileSystemRights.Read, rule);
            IsDeniedReadAndExecute = Contains(FileSystemRights.ReadAndExecute, rule);
            IsDeniedReadAttributes = Contains(FileSystemRights.ReadAttributes, rule);
            IsDeniedReadData = Contains(FileSystemRights.ReadData, rule);
            IsDeniedReadExtendedAttributes = Contains(FileSystemRights.ReadExtendedAttributes, rule);
            IsDeniedReadPermissions = Contains(FileSystemRights.ReadPermissions, rule);
            IsDeniedSynchronize = Contains(FileSystemRights.Synchronize, rule);
            IsDeniedTakeOwnership = Contains(FileSystemRights.TakeOwnership, rule);
            IsDeniedTraverse = Contains(FileSystemRights.Traverse, rule);
            IsDeniedWrite = Contains(FileSystemRights.Write, rule);
            IsDeniedWriteAttributes = Contains(FileSystemRights.WriteAttributes, rule);
            IsDeniedWriteData = Contains(FileSystemRights.WriteData, rule);
            IsDeniedWriteExtendedAttributes = Contains(FileSystemRights.WriteExtendedAttributes, rule);
        }


        /// <summary>
        /// Returs the if <paramref name="right"/> is in <sparamref name="rule"/>
        /// </summary>
        public Boolean Contains(FileSystemRights right, FileSystemAccessRule rule)
        {
            return (((Int32)right & (Int32)rule.FileSystemRights) == (Int32)right);
        }

        /// <summary>
        /// Get the File Information and set's the result to <see cref="FileSystemSecurityInformation"/> on success.
        /// Also set's owner and owner's domain
        /// </summary>
        /// <returns>true on success. Use native win32 exception to get further error information</returns>
        private void GetSecurityFromFileSystem()
        {
            var sidHandle = new IntPtr();
            try
            {
                FileSystemSecurityInformation = ReceiveFileSystemSecurityInformation(out sidHandle);
            }
            finally
            {
                Win32SafeNativeMethods.LocalFree(sidHandle);
            }
        }

        public static Boolean ContainsFileAttribute(FileAttributes source, FileAttributes attr)
        {
            return (source & attr) != 0;
        }


        public static void NativeExceptionMapping(String path, Int32 errorCode)
        {
            if (errorCode == 0)
            {
                return;
            }

            var affectedPath = PathTools.ToRegularPath(path);

            throw new Exception("Error on '" + affectedPath + "': See InnerException for details.", new Win32Exception(errorCode));
        }

        /// <summary>
        /// Gets the security information of specified handle from file system
        /// </summary>
        /// <param name="sidHandle">Handle to get file security information</param>
        /// <returns><see cref="CommonObjectSecurity"/>Result</returns>
        private CommonObjectSecurity ReceiveFileSystemSecurityInformation(out IntPtr sidHandle)
        {
            var zeroHandle = new IntPtr();
            var pSecurityDescriptor = new IntPtr();

            try
            {
                var namedSecInfoResult = Win32SafeNativeMethods.GetNamedSecurityInfo(PathInfo.FullNameUnc, Win32SecurityObjectType.SeFileObject,
                    Win32FileSystemEntrySecurityInformation.OwnerSecurityInformation | Win32FileSystemEntrySecurityInformation.DaclSecurityInformation,
                    out sidHandle, out zeroHandle, out zeroHandle, out zeroHandle, out pSecurityDescriptor);
                var win32Error = Marshal.GetLastWin32Error();
                // Cancel if call failed

                if (namedSecInfoResult != 0)
                {
                    NativeExceptionMapping(PathInfo.FullName, win32Error);
                }

                var securityDescriptorLength = Win32SafeNativeMethods.GetSecurityDescriptorLength(pSecurityDescriptor);
                var securityDescriptorDataArray = new byte[securityDescriptorLength];
                Marshal.Copy(pSecurityDescriptor, securityDescriptorDataArray, 0, (int)securityDescriptorLength);

                CommonObjectSecurity securityInfo;
                if (ContainsFileAttribute(PathInfo.Attributes, FileAttributes.Directory))
                {
                    securityInfo = new DirectorySecurity();
                    securityInfo.SetSecurityDescriptorBinaryForm(securityDescriptorDataArray);
                }
                else
                {
                    securityInfo = new System.Security.AccessControl.FileSecurity();
                    securityInfo.SetSecurityDescriptorBinaryForm(securityDescriptorDataArray);
                }

                return securityInfo;
            }
            finally
            {
                Win32SafeNativeMethods.LocalFree(zeroHandle);
                Win32SafeNativeMethods.LocalFree(pSecurityDescriptor);
            }
        }


        #endregion

        /// <summary>
        /// File System Security Information
        /// </summary>
        public CommonObjectSecurity FileSystemSecurityInformation { get; private set; }
    }
    
    public abstract class FileDetailBase
    {
        private DateTime _creationTimeUtc;
        private DateTime _lastAccessTimeUtc;
        private DateTime _lastWriteTimeUtc;

        public static FileAttributes ForceFileAttributesExistance(FileAttributes source, FileAttributes attr, bool existance)
        {
            var source1 = source | attr;
            var source2 = source & attr;
            return existance ? source1 : source2;
        }

        /// <summary>
        /// True if file is readonly. Cached.
        /// </summary>
        public Boolean IsReadOnly
        {
            get
            {
                return (Attributes & FileAttributes.ReadOnly) != 0;
            }
            set
            {
                ForceFileAttributesExistance(Attributes, FileAttributes.ReadOnly, value);
                NativeIO.SetAttributes(PathInfo, Attributes);
            }
        }

        /// <summary>
        /// PathInfo Container
        /// </summary>
        public PathInfo PathInfo { get; protected internal set; }

        /// <summary>
        /// Initializes a new instance of the QuickIOAbstractBase class, which acts as a wrapper for a file path.
        /// </summary>
        /// <param name="pathInfo"><see cref="PathInfo"/></param>
        /// <param name="findData"><see cref="Win32FindData"/></param>
        internal FileDetailBase(PathInfo pathInfo, Win32FindData findData)
        {
            FindData = findData;
            PathInfo = pathInfo;
            if (findData != null)
            {
                Attributes = findData.dwFileAttributes;
            }
        }

        /// <summary>
        /// Name of file or directory
        /// </summary>
        public String Name { get { return PathInfo.Name; } }

        /// <summary>
        /// Full path of the directory or file.
        /// </summary>
        public String FullName { get { return PathInfo.FullName; } }
        /// <summary>
        /// Full path of the directory or file (unc format)
        /// </summary>
        public String FullNameUnc { get { return PathInfo.FullNameUnc; } }

        /// <summary>
        /// Fullname of Parent.
        /// </summary>
        public String ParentFullName { get { return PathInfo.ParentFullName; } }
        /// <summary>
        /// Parent. 
        /// </summary>
        public PathInfo Parent { get { return PathInfo.Parent; } }

        public FileSecurity GetFileSystemSecurity()
        {
            return PathInfo.GetFileSystemSecurity();
        }

        /// <summary>
        /// Fullname of Root. null if current path is root.
        /// </summary>
        public String RootFullName { get { return PathInfo.RootFullName; } }
        /// <summary>
        /// Returns Root or null if current path is root
        /// </summary>
        public PathInfo Root { get { return PathInfo.Root; } }


        /// <summary>
        /// Attributes (Cached Value)
        /// </summary>
        public FileAttributes Attributes { get; protected internal set; }

        #region UNC Times
        /// <summary>
        /// Gets the creation time (UTC)
        /// </summary>
        public DateTime CreationTimeUtc
        {
            get
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                return _creationTimeUtc;
            }
            protected set
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                _creationTimeUtc = value;
            }
        }

        /// <summary>
        /// Gets the time (UTC) of last access. 
        /// </summary>
        public DateTime LastAccessTimeUtc
        {
            get
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                return _lastAccessTimeUtc;
            }
            protected set
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                _lastAccessTimeUtc = value;
            }
        }

        /// <summary>
        /// Gets the time (UTC) was last written to
        /// </summary>
        public DateTime LastWriteTimeUtc
        {
            get
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                return _lastWriteTimeUtc;
            }
            protected set
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                _lastWriteTimeUtc = value;
            }
        }

        #endregion

        #region LocalTime
        /// <summary>
        /// Gets the creation time
        /// </summary>
        public DateTime CreationTime { get { return CreationTimeUtc.ToLocalTime(); } }

        /// <summary>
        /// Gets the time that the  file was last accessed
        /// </summary>
        public DateTime LastAccessTime { get { return LastAccessTimeUtc.ToLocalTime(); } }

        /// <summary>
        /// Gets the time the file was last written to.
        /// </summary>
        public DateTime LastWriteTime { get { return LastWriteTimeUtc.ToLocalTime(); } }
        #endregion

        public override string ToString()
        {
            return FullName;
        }

        /// <summary>
        /// Win32ApiFindData bag
        /// </summary>
        internal Win32FindData FindData { get; private set; }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="NTAccount"/></returns>
        public NTAccount GetOwner()
        {
            return PathInfo.GetOwner();
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="IdentityReference"/></returns>
        public IdentityReference GetOwnerIdentifier()
        {
            return PathInfo.GetOwnerIdentifier();
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="IdentityReference"/></returns>
        public void SetOwner(NTAccount newOwner)
        {
            PathInfo.SetOwner(newOwner);
        }

        /// <summary>
        /// Sets the owner
        /// </summary>
        public void SetOwner(IdentityReference newOwersIdentityReference)
        {
            PathInfo.SetOwner(newOwersIdentityReference);
        }
    }
    /// <summary>
    /// Provides properties and instance methods for directories
    /// </summary>
    public sealed class DirectoryDetail : FileDetailBase
    {


        /// <summary>
        /// Create new instance of <see cref="DirectoryDetail"/>
        /// </summary>
        public DirectoryDetail(String path) : this(new PathInfo(path)) { }

        /// <summary>
        /// Create new instance of <see cref="DirectoryDetail"/>
        /// </summary>
        public DirectoryDetail(PathInfo pathInfo) : this(pathInfo, pathInfo.IsRoot ? null : NativeIO.GetFindDataFromPath(pathInfo)) { }

        /// <summary>
        /// Creates the folder information on the basis of the path and the handles
        /// </summary>
        /// <param name="pathInfo"><see cref="PathInfo"/></param>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        internal DirectoryDetail(PathInfo pathInfo, Win32FindData win32FindData) :
            base(pathInfo, win32FindData)
        {
            if (win32FindData != null)
            {
                RetriveDateTimeInformation(win32FindData);
            }
        }

        /// <summary>
        /// Determines the time stamp of the given <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        private void RetriveDateTimeInformation(Win32FindData win32FindData)
        {
            LastWriteTimeUtc = win32FindData.GetLastWriteTimeUtc();
            LastAccessTimeUtc = win32FindData.GetLastAccessTimeUtc();
            CreationTimeUtc = win32FindData.GetCreationTimeUtc();
        }
    }

    public sealed class FileDetail : FileDetailBase
    {
        /// <summary>
        /// Create new instance of <see cref="FileDetail"/>
        /// </summary>
        public FileDetail(String path) : this(new PathInfo(path)) { }

        /// <summary>
        /// Create new instance of <see cref="FileDetail"/>
        /// </summary>
        public FileDetail(PathInfo pathInfo) : this(pathInfo, NativeIO.GetFindDataFromPath(pathInfo)) { }

        /// <summary>
        /// Creates the file information on the basis of the path and <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="fullName">Full path to the file</param>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        internal FileDetail(String fullName, Win32FindData win32FindData)
            : this(new PathInfo(fullName), win32FindData)
        {
            RetriveDateTimeInformation(win32FindData);
            CalculateSize(win32FindData);
        }

        /// <summary>
        /// Creates the file information on the basis of the path and <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="pathInfo">Full path to the file</param>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        internal FileDetail(PathInfo pathInfo, Win32FindData win32FindData)
            : base(pathInfo, win32FindData)
        {
            RetriveDateTimeInformation(win32FindData);
            CalculateSize(win32FindData);
        }


        /// <summary>
        /// Size of the file. Cached.
        /// </summary>
        public UInt64 Bytes { get; private set; }

        /// <summary>
        /// Size of the file (returns <see cref="Bytes"/>).
        /// </summary>
        public UInt64 Length { get { return Bytes; } }


        /// <summary>
        /// Determines the time stamp of the given <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        private void RetriveDateTimeInformation(Win32FindData win32FindData)
        {
            LastWriteTimeUtc = win32FindData.GetLastWriteTimeUtc();
            LastAccessTimeUtc = win32FindData.GetLastAccessTimeUtc();
            CreationTimeUtc = win32FindData.GetCreationTimeUtc();
        }

        /// <summary>
        /// Calculates the size of the file from the handle
        /// </summary>
        /// <param name="win32FindData"></param>
        private void CalculateSize(Win32FindData win32FindData)
        {
            Bytes = win32FindData.CalculateBytes();
        }
    }

    [FileIOPermission(SecurityAction.Demand, AllFiles = FileIOPermissionAccess.AllAccess)]
    public static class NativeIO
    {
        /// <summary>End of enumeration indicator in Win32</summary>
        public const Int32 ERROR_NO_MORE_FILES = 18;

        public static Boolean ContainsFileAttribute(FileAttributes source, FileAttributes attr) { return (source & attr) != 0; }

        public static void NativeExceptionMapping(String path, Int32 errorCode)
        {
            if (errorCode == 0)
            {
                return;
            }

            string affectedPath = PathTools.ToRegularPath(path);

            throw new Exception("Error on '" + affectedPath + "': See InnerException for details.", new Win32Exception(errorCode));
        }

        internal static FileOrDirectory DetermineFileSystemEntry(PathInfo pathInfo)
        {
            var findData = GetFindDataFromPath(pathInfo);

            return !ContainsFileAttribute(findData.dwFileAttributes, FileAttributes.Directory) ? FileOrDirectory.File : FileOrDirectory.Directory;
        }

        /// <summary>
        /// Opens a <see cref="FileStream"/> for access at the given path. Ensure stream is correctly disposed.
        /// </summary>
        public static FileStream OpenFileStream(PathInfo pathInfo, FileAccess fileAccess, FileMode fileOption = FileMode.Open, FileShare shareMode = FileShare.Read, Int32 buffer = 0)
        {
            var fileHandle = Win32SafeNativeMethods.CreateFile(pathInfo.FullNameUnc, fileAccess, shareMode, IntPtr.Zero, fileOption, 0, IntPtr.Zero);
            var win32Error = Marshal.GetLastWin32Error();
            if (fileHandle.IsInvalid)
            {
                NativeExceptionMapping(pathInfo.FullName, win32Error); // Throws an exception
            }

            return buffer > 0 ? new FileStream(fileHandle, fileAccess, buffer) : new FileStream(fileHandle, fileAccess);
        }

        /// <summary> Removes a file by UNC path </summary>
        /// <param name="path">Path to the file to remove</param>
        public static void DeleteFileUnc(String path)
        {
            bool result = Win32SafeNativeMethods.DeleteFile(path);
            int win32Error = Marshal.GetLastWin32Error();
            if (!result)
            {
                NativeExceptionMapping(path, win32Error);
            }
        }

        /// <summary>
        ///     Removes a file.
        /// </summary>
        /// <param name="pathInfo">PathInfo of the file to remove</param>
        /// <exception cref="FileNotFoundException">This error is fired if the specified file to remove does not exist.</exception>
        public static void DeleteFile(PathInfo pathInfo)
        {
            RemoveAttribute(pathInfo, FileAttributes.ReadOnly);
            DeleteFileUnc(pathInfo.FullNameUnc);
        }

        /// <summary>
        /// Deletes all files in the given directory.
        /// <para>If recursive flag is `true` all subdirectories and files will be removed.</para>
        /// If recursive flag is `false` the method will fail if the target is not empty.
        /// </summary>
        /// <param name="directoryInfo">Info of directory to clear</param>
        /// <param name="recursive">If <paramref name="recursive"/> is true then all subfolders are also deleted.</param>
        /// <remarks>Function loads every file and attribute. Alls read-only flags will be removed before removing.</remarks>
        public static void DeleteDirectory(DirectoryDetail directoryInfo, bool recursive = false)
        {
            // Contents
            if (recursive)
            {
                // search all contents
                var subFiles = FindPaths(directoryInfo.FullNameUnc, pathFormatReturn: UncOrRegular.UNC);
                foreach (var item in subFiles) { DeleteFileUnc(item); }

                var subDirs = EnumerateDirectories(directoryInfo.PathInfo);
                foreach (var subDir in subDirs) { DeleteDirectory(subDir, true); }
            }

            // Remove specified
            var removed = Win32SafeNativeMethods.RemoveDirectory(directoryInfo.FullNameUnc);
            var win32Error = Marshal.GetLastWin32Error();
            if (!removed)
            {
                NativeExceptionMapping(directoryInfo.FullName, win32Error);
            }
        }

        /// <summary>
        /// Remove a file attribute
        /// </summary>
        /// <param name="pathInfo">Affected target</param>
        /// <param name="attribute">Attribute to remove</param>
        /// <returns>true if removed. false if not exists in attributes</returns>
        public static Boolean RemoveAttribute(PathInfo pathInfo, FileAttributes attribute)
        {
            if ((pathInfo.Attributes & attribute) != attribute) { return false; }
            var attributes = pathInfo.Attributes;
            attributes &= ~attribute;
            SetAttributes(pathInfo, attributes);
            return true;
        }

        /// <summary>
        ///     Adds a file attribute
        /// </summary>
        /// <param name="pathInfo">Affected target</param>
        /// <param name="attribute">Attribute to add</param>
        /// <returns>true if added. false if already exists in attributes</returns>
        public static Boolean AddAttribute(PathInfo pathInfo, FileAttributes attribute)
        {
            if ((pathInfo.Attributes & attribute) == attribute) { return false; }
            var attributes = pathInfo.Attributes;
            attributes |= attribute;
            SetAttributes(pathInfo, attributes);
            return true;
        }

        /// <summary>
        /// Creates a new directory. If <paramref name="recursive" /> is false, the parent directory must exists.
        /// </summary>
        /// <param name="pathInfo">
        /// Complete path to create
        /// </param>
        /// <param name="recursive">If <paramref name="recursive" /> is false, the parent directory must exist.</param>
        public static void CreateDirectory(PathInfo pathInfo, bool recursive = false)
        {
            if (recursive)
            {
                var parent = pathInfo.Parent;
                if (parent.IsRoot)
                {
                    // Root
                    if (!parent.Exists)
                    {
                        throw new Exception("Root path does not exists. You cannot create a root this way. " + parent.FullName);
                    }
                }
                else if (!parent.Exists)
                {
                    CreateDirectory(parent, true);
                }
            }

            if (pathInfo.Exists)
            {
                return;
            }

            bool created = Win32SafeNativeMethods.CreateDirectory(pathInfo.FullNameUnc, IntPtr.Zero);
            int win32Error = Marshal.GetLastWin32Error();
            if (!created)
            {
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Gets the <see cref="Win32FindData" /> from the passed path.
        /// </summary>
        /// <param name="pathInfo">Path</param>
        /// <param name="pathFindData"><seealso cref="Win32FindData" />. Will be null if path does not exist.</param>
        /// <returns>true if path is valid and <see cref="Win32FindData" /> is set</returns>
        /// <remarks>
        ///     <see>
        ///         <cref>QuickIOCommon.NativeExceptionMapping</cref>
        ///     </see>
        ///     if invalid handle found.
        /// </remarks>
        public static bool TryGetFindDataFromPath(PathInfo pathInfo, out Win32FindData pathFindData)
        {
            var win32FindData = new Win32FindData();
            int win32Error;


            using (var fileHandle = FindFirstSafeFileHandle(pathInfo.FullNameUnc, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    NativeExceptionMapping(pathInfo.FullName, win32Error);
                }

                // Ignore . and .. directories
                if (!IsSystemDirectoryEntry(win32FindData))
                {
                    pathFindData = win32FindData;
                    return true;
                }
            }

            pathFindData = null;
            return false;
        }

        static Boolean IsSystemDirectoryEntry(Win32FindData win32FindData)
        {
            if (win32FindData.cFileName.Length >= 3)
            {
                return false;
            }

            return (win32FindData.cFileName == "." || win32FindData.cFileName == "..");
        }

        /// <summary>
        ///     Returns the <see cref="SafeFileHandle" /> and fills <see cref="Win32FindData" /> from the passes path.
        /// </summary>
        /// <param name="path">Path to the file system entry</param>
        /// <param name="win32FindData"></param>
        /// <param name="win32Error">Last error code. 0 if no error occurs</param>
        /// <returns>
        ///     <see cref="SafeFileHandle" />
        /// </returns>
        static Win32FileHandle FindFirstSafeFileHandle(string path, Win32FindData win32FindData, out Int32 win32Error)
        {
            var result = Win32SafeNativeMethods.FindFirstFile(path, win32FindData);
            win32Error = Marshal.GetLastWin32Error();

            return result;
        }

        /// <summary>
        ///     Reurns true if passed path exists
        /// </summary>
        /// <param name="pathInfo">Path to check</param>
        public static Boolean Exists(PathInfo pathInfo)
        {
            uint attributes = Win32SafeNativeMethods.GetFileAttributes(pathInfo.FullNameUnc);
            return !Equals(attributes, 0xffffffff);
        }

        /// <summary>
        ///     Returns the <see cref="Win32FindData" /> from specified <paramref name="pathInfo" />
        /// </summary>
        /// <param name="pathInfo">Path to the file system entry</param>
        /// <returns>
        ///     <see cref="Win32FindData" />
        /// </returns>
        public static Win32FindData GetFindDataFromPath(PathInfo pathInfo)
        {
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(pathInfo.FullNameUnc, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    NativeExceptionMapping(pathInfo.FullName, win32Error);
                }

                // Ignore . and .. directories
                if (!IsSystemDirectoryEntry(win32FindData))
                {
                    return win32FindData;
                }
            }

            throw new Exception("PathNotFound " + pathInfo.FullName);
        }

        /// <summary>
        ///     Returns the <see cref="SafeFileHandle" /> and fills <see cref="Win32FindData" /> from the passes path.
        /// </summary>
        /// <param name="path">Path to the file system entry</param>
        /// <returns>
        ///     <see cref="SafeFileHandle" />
        /// </returns>
        internal static SafeFileHandle OpenReadWriteFileSystemEntryHandle(string path)
        {
            return Win32SafeNativeMethods.OpenReadWriteFileSystemEntryHandle(path, (0x40000000 | 0x80000000), FileShare.Read | FileShare.Write | FileShare.Delete, IntPtr.Zero, FileMode.Open, (0x02000000), IntPtr.Zero);
        }

        /// <summary>
        ///     Determined metadata of directory
        /// </summary>
        /// <param name="uncDirectoryPath">Path of the directory</param>
        /// <param name="findData">
        ///     <see cref="Win32FindData" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns><see cref="DirectoryMetadata" /> started with the given directory</returns>
        internal static DirectoryMetadata EnumerateDirectoryMetadata(String uncDirectoryPath, Win32FindData findData, SuppressExceptions enumerateOptions)
        {
            // Results
            var subFiles = new List<FileMetadata>();
            var subDirs = new List<DirectoryMetadata>();

            // Match for start of search
            string currentPath = PathTools.Combine(uncDirectoryPath, "*");

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    if (win32Error != ERROR_NO_MORE_FILES)
                    {
                        NativeExceptionMapping(uncDirectoryPath, win32Error);
                    }

                    if (EnumerationHandleInvalidFileHandle(uncDirectoryPath, enumerateOptions, win32Error))
                    {
                        return null;
                    }
                }

                // Add any matching non-system results to the output
                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    var uncResultPath = PathTools.Combine(uncDirectoryPath, win32FindData.cFileName);

                    // if it's a file, add to the collection
                    if (!ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        subFiles.Add(new FileMetadata(uncResultPath, win32FindData));
                    }
                    else
                    {
                        subDirs.Add(EnumerateDirectoryMetadata(uncResultPath, win32FindData, enumerateOptions));
                    }
                    // Create new FindData object for next result

                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }

            return new DirectoryMetadata(uncDirectoryPath, findData, subDirs, subFiles);
        }

        /// <summary>
        ///     Determined all subfolders of a directory
        /// </summary>
        /// <param name="pathInfo">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns><see cref="DirectoryDetail" /> collection of subfolders</returns>
        internal static IEnumerable<DirectoryDetail> EnumerateDirectories(PathInfo pathInfo, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None)
        {
            // Match for start of search
            string currentPath = PathTools.Combine(pathInfo.FullNameUnc, pattern);

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    if (win32Error != ERROR_NO_MORE_FILES)
                    {
                        NativeExceptionMapping(pathInfo.FullName, win32Error);
                    }

                    if (EnumerationHandleInvalidFileHandle(pathInfo.FullName, enumerateOptions, win32Error))
                    {
                        yield return null;
                    }
                }

                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    string resultPath = PathTools.Combine(pathInfo.FullName, win32FindData.cFileName);

                    // Check for Directory
                    if (ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        yield return new DirectoryDetail(new PathInfo(resultPath), win32FindData);

                        // SubFolders?!
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            foreach (var match in EnumerateDirectories(new PathInfo(resultPath, win32FindData.cFileName), pattern, searchOption, enumerateOptions))
                            {
                                yield return match;
                            }
                        }
                    }
                    // Create new FindData object for next result
                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }
        }

        /// <summary>
        ///     Returns the handle by given path and finddata
        /// </summary>
        /// <param name="uncPath">Specified path</param>
        /// <param name="win32FindData">FindData to fill</param>
        /// <param name="win32Error">Win32Error Code. 0 on success</param>
        /// <returns><see cref="Win32FileHandle" /> of specified path</returns>
        static Win32FileHandle FindFirstFileManaged(String uncPath, Win32FindData win32FindData, out Int32 win32Error)
        {
            var handle = Win32SafeNativeMethods.FindFirstFile(uncPath, win32FindData);
            win32Error = Marshal.GetLastWin32Error();
            return handle;
        }

        /// <summary>
        ///     Determined all files of a directory
        /// </summary>
        /// <param name="uncDirectoryPath">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns>Collection of files</returns>
        internal static IEnumerable<FileDetail> EnumerateFiles(String uncDirectoryPath, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None)
        {
            // Match for start of search
            string currentPath = PathTools.Combine(uncDirectoryPath, pattern);

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstFileManaged(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid && EnumerationHandleInvalidFileHandle(uncDirectoryPath, enumerateOptions, win32Error))
                {
                    yield return null;
                }

                // Treffer auswerten
                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    string resultPath = PathTools.Combine(uncDirectoryPath, win32FindData.cFileName);

                    // Check for Directory
                    if (!ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        yield return new FileDetail(resultPath, win32FindData);
                    }
                    else
                    {
                        // SubFolders?!
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            foreach (var match in EnumerateFiles(resultPath, pattern, searchOption, enumerateOptions))
                            {
                                yield return match;
                            }
                        }
                    }
                    // Create new FindData object for next result
                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }
        }

        /// <summary>
        ///     Loads a file from specified path
        /// </summary>
        /// <param name="pathInfo">Full path</param>
        /// <returns>
        ///     <see cref="FileDetail" />
        /// </returns>
        public static FileDetail ReadFileDetails(PathInfo pathInfo)
        {
            Win32FindData findData;
            if (!TryGetFindDataFromPath(pathInfo, out findData))
            {
                throw new Exception("PathNotFound " + pathInfo.FullName);
            }
            if (DetermineFileSystemEntry(findData) != FileOrDirectory.File)
            {
                throw new Exception("UnmatchedFileSystemEntryType " + FileOrDirectory.File + ", " + FileOrDirectory.Directory + ", " + pathInfo.FullName);
            }
            return new FileDetail(pathInfo, findData);
        }

        internal static FileOrDirectory DetermineFileSystemEntry(Win32FindData findData)
        {
            return !ContainsFileAttribute(findData.dwFileAttributes, FileAttributes.Directory) ? FileOrDirectory.File : FileOrDirectory.Directory;
        }

        /// <summary>
        ///     Loads a directory from specified path
        /// </summary>
        /// <param name="pathInfo">Full path</param>
        /// <returns>
        ///     <see cref="DirectoryDetail" />
        /// </returns>
        public static DirectoryDetail ReadDirectoryDetails(PathInfo pathInfo)
        {
            Win32FindData findData;
            if (!TryGetFindDataFromPath(pathInfo, out findData))
            {
                throw new Exception("PathNotFound " + pathInfo.FullName);
            }
            if (DetermineFileSystemEntry(findData) != FileOrDirectory.Directory)
            {
                throw new Exception("UnmatchedFileSystemEntryType " + FileOrDirectory.File + ", " + FileOrDirectory.Directory + ", " + pathInfo.FullName);
            }
            return new DirectoryDetail(pathInfo, findData);
        }

        /// <summary>
        ///     Search Exection
        /// </summary>
        /// <param name="uncDirectoryPath">Start directory path</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <param name="pathFormatReturn">Specifies the type of path to return.</param>
        /// <param name="filterType">
        ///     <see cref="FileOrDirectory" />
        /// </param>
        /// <returns>Collection of path</returns>
        static IEnumerable<String> FindPaths(String uncDirectoryPath, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, FileOrDirectory? filterType = null, SuppressExceptions enumerateOptions = SuppressExceptions.None, UncOrRegular pathFormatReturn = UncOrRegular.Regular)
        {
            // Result Container
            var results = new List<String>();

            // Match for start of search
            string currentPath = PathTools.Combine(uncDirectoryPath, pattern);

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid && EnumerationHandleInvalidFileHandle(uncDirectoryPath, enumerateOptions, win32Error))
                {
                    return new List<String>();
                }

                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    string resultPath = PathTools.Combine(uncDirectoryPath, win32FindData.cFileName);

                    // if it's a file, add to the collection
                    if (!ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        if (filterType == null || ((FileOrDirectory)filterType == FileOrDirectory.File))
                        {
                            // It's a file
                            results.Add(FormatPathByType(pathFormatReturn, resultPath));
                        }
                    }
                    else
                    {
                        // It's a directory
                        // Check for search searchFocus directories
                        if (filterType != null && ((FileOrDirectory)filterType == FileOrDirectory.Directory))
                        {
                            results.Add(FormatPathByType(pathFormatReturn, resultPath));
                        }

                        // SubFolders?!
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            var r = new List<String>(FindPaths(resultPath, pattern, searchOption, filterType, enumerateOptions));
                            if (r.Count > 0)
                            {
                                results.AddRange(r);
                            }
                        }
                    }

                    // Create new FindData object for next result
                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }
            // Return result;
            return results;
        }

        /// <summary>
        ///     Handles the options to the fired exception
        /// </summary>
        static bool EnumerationHandleInvalidFileHandle(string path, SuppressExceptions enumerateOptions, int win32Error)
        {
            try
            {
                NativeExceptionMapping(path, win32Error);
            }
            catch (Exception)
            {
                if (enumerateOptions == SuppressExceptions.SuppressAllExceptions)
                {
                    return true;
                }

                throw;
            }
            return false;
        }

        /// <summary>
        ///     Formats a path
        /// </summary>
        /// <param name="pathFormatReturn">Target format type</param>
        /// <param name="uncPath">Path to format</param>
        /// <returns>Formatted path</returns>
        static string FormatPathByType(UncOrRegular pathFormatReturn, string uncPath)
        {
            return pathFormatReturn == UncOrRegular.Regular ? PathTools.ToRegularPath(uncPath) : uncPath;
        }

        /// <summary>
        ///     Sets the specified <see cref="FileAttributes" /> of the entry on the specified path.
        /// </summary>
        /// <param name="pathInfo">The path to the entry.</param>
        /// <param name="attributes">A bitwise combination of the enumeration values.</param>
        /// <exception cref="Win32Exception">Unmatched Exception</exception>
        public static void SetAttributes(PathInfo pathInfo, FileAttributes attributes)
        {
            if (Win32SafeNativeMethods.SetFileAttributes(pathInfo.FullNameUnc, (uint)attributes))
            {
                return;
            }
            int win32Error = Marshal.GetLastWin32Error();
            NativeExceptionMapping(pathInfo.FullName, win32Error);
        }

        /// <summary>
        ///     Copies a file and overwrite existing files if desired.
        /// </summary>
        /// <param name="sourceFilePath">Full source path</param>
        /// <param name="targetFilePath">Full target path</param>
        /// <param name="overwrite">true to overwrite existing files</param>
        /// <returns>True if copy succeeded, false if not. Check last Win32 Error to get further information.</returns>
        public static bool CopyFile(PathInfo sourceFilePath, PathInfo targetFilePath, bool overwrite = false)
        {
            bool failOnExists = !overwrite;

            bool result = Win32SafeNativeMethods.CopyFile(sourceFilePath.FullNameUnc, targetFilePath.FullNameUnc, failOnExists);
            int win32Error = Marshal.GetLastWin32Error();
            NativeExceptionMapping(sourceFilePath.FullName, win32Error);
            return result;
        }

        /// <summary>
        /// Moves a file
        /// </summary>
        /// <param name="sourceFilePath">Full source path</param>
        /// <param name="targetFilePath">Full target path</param>
        public static void MoveFile(PathInfo sourceFilePath, PathInfo targetFilePath)
        {
            if (Win32SafeNativeMethods.MoveFile(sourceFilePath.FullNameUnc, targetFilePath.FullNameUnc))
            {
                return;
            }
            int win32Error = Marshal.GetLastWin32Error();
            NativeExceptionMapping(sourceFilePath.FullName, win32Error);
        }

        /// <summary>
        ///     Sets the dates and times of given directory or file.
        /// </summary>
        /// <param name="pathInfo">Affected file or directory</param>
        /// <param name="creationTimeUtc">The time that is to be used (UTC)</param>
        /// <param name="lastAccessTimeUtc">The time that is to be used (UTC)</param>
        /// <param name="lastWriteTimeUtc">The time that is to be used (UTC)</param>
        public static void SetAllFileTimes(PathInfo pathInfo, DateTime creationTimeUtc, DateTime lastAccessTimeUtc, DateTime lastWriteTimeUtc)
        {
            long longCreateTime = creationTimeUtc.ToFileTime();
            long longAccessTime = lastAccessTimeUtc.ToFileTime();
            long longWriteTime = lastWriteTimeUtc.ToFileTime();

            using (SafeFileHandle fileHandle = OpenReadWriteFileSystemEntryHandle(pathInfo.FullNameUnc))
            {
                if (Win32SafeNativeMethods.SetAllFileTimes(fileHandle, ref longCreateTime, ref longAccessTime, ref longWriteTime) != 0)
                {
                    return;
                }
                int win32Error = Marshal.GetLastWin32Error();
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Sets the time at which the file or directory was created (UTC)
        /// </summary>
        /// <param name="pathInfo">Affected file or directory</param>
        /// <param name="utcTime">The time that is to be used (UTC)</param>
        public static void SetCreationTimeUtc(PathInfo pathInfo, DateTime utcTime)
        {
            long longTime = utcTime.ToFileTime();
            using (SafeFileHandle fileHandle = OpenReadWriteFileSystemEntryHandle(pathInfo.FullNameUnc))
            {
                if (Win32SafeNativeMethods.SetCreationFileTime(fileHandle, ref longTime, IntPtr.Zero, IntPtr.Zero))
                {
                    return;
                }
                int win32Error = Marshal.GetLastWin32Error();
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Sets the time at which the file or directory was last written to (UTC)
        /// </summary>
        /// <param name="pathInfo">Affected file or directory</param>
        /// <param name="utcTime">The time that is to be used (UTC)</param>
        public static void SetLastWriteTimeUtc(PathInfo pathInfo, DateTime utcTime)
        {
            long longTime = utcTime.ToFileTime();
            using (SafeFileHandle fileHandle = OpenReadWriteFileSystemEntryHandle(pathInfo.FullNameUnc))
            {
                if (Win32SafeNativeMethods.SetLastWriteFileTime(fileHandle, IntPtr.Zero, IntPtr.Zero, ref longTime))
                {
                    return;
                }
                int win32Error = Marshal.GetLastWin32Error();
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Sets the time at which the file or directory was last accessed to (UTC)
        /// </summary>
        /// <param name="pathInfo">Affected file or directory</param>
        /// <param name="utcTime">The time that is to be used (UTC)</param>
        public static void SetLastAccessTimeUtc(PathInfo pathInfo, DateTime utcTime)
        {
            long longTime = utcTime.ToFileTime();
            using (SafeFileHandle fileHandle = OpenReadWriteFileSystemEntryHandle(pathInfo.FullNameUnc))
            {
                if (Win32SafeNativeMethods.SetLastAccessFileTime(fileHandle, IntPtr.Zero, ref longTime, IntPtr.Zero))
                {
                    return;
                }
                int win32Error = Marshal.GetLastWin32Error();
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Gets Share Result
        /// </summary>
        /// <param name="machineName">Machine</param>
        /// <param name="level">API level</param>
        /// <param name="buffer">Buffer</param>
        /// <param name="entriesRead">Entries total read</param>
        /// <param name="totalEntries">Entries total</param>
        /// <param name="resumeHandle">Handle</param>
        /// <returns>Error Code</returns>
        /// <remarks>http://msdn.microsoft.com/en-us/library/windows/desktop/bb525387(v=vs.85).aspx</remarks>
        public static int GetShareEnumResult(string machineName, AdminOrNormal level, out IntPtr buffer, out int entriesRead, out int totalEntries, ref int resumeHandle)
        {
            return Win32SafeNativeMethods.NetShareEnum(machineName, (int)level, out buffer, -1, out entriesRead, out totalEntries, ref resumeHandle);
        }
    }
}