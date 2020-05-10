using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    /// <summary>
    /// Provides access to NTFS junction points in .Net.
    /// </summary>
    public static partial class Junction
    {
        public const int MAX_PATH = 260;
        public const int HIGH_MAX_PATH = 0x7FFF; // (32767)
        public const uint INVALID_HANDLE_VALUE = 0xffffffff;
        public const ulong INVALID_HANDLE_VALUE64 = 0xffffffffffffffff;

        [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetFinalPathNameByHandle(IntPtr handle, [In, Out] StringBuilder path, int bufLen, int flags);

        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, int dwShareMode, IntPtr SecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(string lpFileName, Junction.EFileAccess dwDesiredAccess, Junction.EFileShare dwShareMode
                , IntPtr SecurityAttributes
                , Junction.ECreationDisposition dwCreationDisposition, Junction.EFileAttributes dwFlagsAndAttributes
                , IntPtr hTemplateFile);

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLinkEnum dwFlags);

        enum SymbolicLinkEnum
        {
            File = 0,
            Directory = 1
        }

        public static string GetSymbolicLinkTarget(System.IO.DirectoryInfo symlink)
        {
            return GetSymbolicLinkTarget(symlink.FullName);
        }

        public static string GetSymbolicLinkTarget(string fullPath)
        {
            fullPath = Path.GetFullPath(fullPath);
            SafeFileHandle directoryHandle = CreateFile(fullPath, (int)Junction.EFileAccess.GenericNone, (int)Junction.EFileShare.Write, System.IntPtr.Zero, (int)Junction.ECreationDisposition.OpenExisting, (int)Junction.EFileAttributes.BackupSemantics, System.IntPtr.Zero);
            if (directoryHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            StringBuilder path = new StringBuilder(HIGH_MAX_PATH);
            int size = GetFinalPathNameByHandle(directoryHandle.DangerousGetHandle(), path, path.Capacity, 0);
            if (size < 0) throw new Win32Exception(Marshal.GetLastWin32Error()); // The remarks section of GetFinalPathNameByHandle mentions the return being prefixed with "\\?\" // More information about "\\?\" here -> http://msdn.microsoft.com/en-us/library/aa365247(v=VS.85).aspx

            var targetDir = path.ToString();
            if (targetDir.StartsWith(NonInterpretedPathPrefix))
            {
                if (!targetDir.StartsWith(VolumePrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    targetDir = targetDir.Substring(NonInterpretedPathPrefix.Length);
                }
            }
            else if (targetDir.StartsWith(NonInterpretedPathPrefix2))
            {
                targetDir = targetDir.Substring(NonInterpretedPathPrefix2.Length);
            }

            return targetDir;
        }

        /// <summary>
        /// The file or directory is not a reparse point.
        /// </summary>
        private const int ERROR_NOT_A_REPARSE_POINT = 4390;

        /// <summary>
        /// The reparse point attribute cannot be set because it conflicts with an existing attribute.
        /// </summary>
        private const int ERROR_REPARSE_ATTRIBUTE_CONFLICT = 4391;

        /// <summary>
        /// The data present in the reparse point buffer is invalid.
        /// </summary>
        private const int ERROR_INVALID_REPARSE_DATA = 4392;

        /// <summary>
        /// The tag present in the reparse point buffer is invalid.
        /// </summary>
        private const int ERROR_REPARSE_TAG_INVALID = 4393;

        /// <summary>
        /// There is a mismatch between the tag specified in the request and the tag present in the reparse point.
        /// </summary>
        private const int ERROR_REPARSE_TAG_MISMATCH = 4394;

        /// <summary>
        /// Command to set the reparse point data block.
        /// </summary>
        private const int FSCTL_SET_REPARSE_POINT = 0x000900A4;

        /// <summary>
        /// Command to get the reparse point data block.
        /// </summary>
        private const int FSCTL_GET_REPARSE_POINT = 0x000900A8;

        /// <summary>
        /// Command to delete the reparse point data base.
        /// </summary>
        private const int FSCTL_DELETE_REPARSE_POINT = 0x000900AC;

        /// <summary>
        /// Reparse point tag used to identify mount points and junction points.
        /// </summary>
        private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;

        /// <summary>
        /// This prefix indicates to NTFS that the path is to be treated as a non-interpreted
        /// path in the virtual file system.
        /// </summary>
        private const string NonInterpretedPathPrefix = @"\??\";
        private const string VolumePrefix = NonInterpretedPathPrefix + "Volume";

        private const string NonInterpretedPathPrefix2 = @"\\?\";

        [Flags]
        public enum EFileAccess : uint
        {
            GenericNone        = 0x00000000,
            GenericRead        = 0x80000000,
            GenericWrite    = 0x40000000,
            GenericExecute    = 0x20000000,
            GenericAll        = 0x10000000,
        }

        [Flags]
        public enum EFileShare : uint
        {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004,
        }

        public enum ECreationDisposition : uint
        {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5,
        }

        [Flags]
        public enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        //https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/ntifs/ns-ntifs-_reparse_data_buffer
        [StructLayout(LayoutKind.Sequential)]
        private struct REPARSE_DATA_BUFFER
        {
            /// <summary>
            /// Reparse point tag. Must be a Microsoft reparse point tag.
            /// </summary>
            public uint ReparseTag;

            /// <summary>
            /// Size, in bytes, of the data after the Reserved member. This can be calculated by:
            /// (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength +
            /// (namesAreNullTerminated ? 2 * sizeof(char) : 0);
            /// </summary>
            public ushort ReparseDataLength;

            /// <summary>
            /// Reserved; do not use.
            /// </summary>
            public ushort Reserved;

            /// <summary>
            /// Offset, in bytes, of the substitute name string in the PathBuffer array.
            /// </summary>
            public ushort SubstituteNameOffset;

            /// <summary>
            /// Length, in bytes, of the substitute name string. If this string is null-terminated,
            /// SubstituteNameLength does not include space for the null character.
            /// </summary>
            public ushort SubstituteNameLength;

            /// <summary>
            /// Offset, in bytes, of the print name string in the PathBuffer array.
            /// </summary>
            public ushort PrintNameOffset;

            /// <summary>
            /// Length, in bytes, of the print name string. If this string is null-terminated,
            /// PrintNameLength does not include space for the null character.
            /// </summary>
            public ushort PrintNameLength;

            /// <summary>
            /// A buffer containing the unicode-encoded path string. The path string contains
            /// the substitute name string and print name string.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
            public byte[] PathBuffer;
        }

        //https://docs.microsoft.com/en-us/windows/desktop/api/winnt/ns-winnt-_reparse_guid_data_buffer
        [StructLayout(LayoutKind.Sequential)]
        private struct REPARSE_GUID_DATA_BUFFER
        {
            /// <summary>
            /// Reparse point tag. Must be a Microsoft reparse point tag.
            /// </summary>
            public uint ReparseTag;

            /// <summary>
            /// Size, in bytes, of the data after the Reserved member. This can be calculated by:
            /// (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength +
            /// (namesAreNullTerminated ? 2 * sizeof(char) : 0);
            /// </summary>
            public ushort ReparseDataLength;

            /// <summary>
            /// Reserved; do not use.
            /// </summary>
            public ushort Reserved;

            /// <summary>
            ///
            /// </summary>
            public Guid ReparseGuid;

            /// <summary>
            /// A buffer containing the unicode-encoded Data Buffer.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
            public byte[] DataBuffer;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
                IntPtr InBuffer, int nInBufferSize,
                IntPtr OutBuffer, int nOutBufferSize,
                out int pBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
                string lpFileName,
                EFileAccess dwDesiredAccess,
                EFileShare dwShareMode,
                IntPtr lpSecurityAttributes,
                ECreationDisposition dwCreationDisposition,
                EFileAttributes dwFlagsAndAttributes,
                IntPtr hTemplateFile);

        /// <summary>
        /// Creates a junction point from the specified directory to the specified target directory.
        /// </summary>
        /// <remarks>
        /// Only works on NTFS.
        /// </remarks>
        /// <param name="Junction">The junction point path</param>
        /// <param name="targetDir">The target directory</param>
        /// <param name="overwrite">If true overwrites an existing reparse point or empty directory</param>
        /// <param name="allowTargetNotExist">If true then allows the target directory to not exist, therefore creating a junction pointing to a location which doesn't exist</param>
        /// <exception cref="IOException">Thrown when the junction point could not be created or when
        /// an existing directory was found and <paramref name="overwrite" /> if false</exception>
        public static void Create(string Junction, string targetDir, bool overwrite = false, bool allowTargetNotExist = false)
        {
            var isVolume = false;
            if( string.IsNullOrWhiteSpace(targetDir))
            {
                if( !allowTargetNotExist)
                {
                    throw new IOException("Target path not specified.");
                }
            }
            else
            {
                if (targetDir.StartsWith(VolumePrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    isVolume = true;
                }
                else
                {
                    targetDir = Path.GetFullPath(targetDir);
                }
            }

            if (!isVolume && !allowTargetNotExist && !Directory.Exists(targetDir))
            {
                throw new IOException("Target path does not exist or is not a directory.");
            }

            if (Directory.Exists(Junction))
            {
                if (!overwrite)
                    throw new IOException("Directory already exists and overwrite parameter is false.");
            }
            else
            {
                Directory.CreateDirectory(Junction);
            }

            using (SafeFileHandle handle = OpenReparsePoint(Junction, EFileAccess.GenericWrite))
            {
                byte[] targetDirBytes;
                if( string.IsNullOrEmpty(targetDir))
                {
                    targetDirBytes = new byte[] {};
                }
                else
                {
                    if( isVolume)
                    {
                        targetDirBytes = Encoding.Unicode.GetBytes(targetDir);
                    }
                    else
                    {
                        targetDirBytes = Encoding.Unicode.GetBytes(NonInterpretedPathPrefix + Path.GetFullPath(targetDir));
                    }
                }

                REPARSE_DATA_BUFFER reparseDataBuffer = new REPARSE_DATA_BUFFER();

                reparseDataBuffer.ReparseTag = IO_REPARSE_TAG_MOUNT_POINT;
                reparseDataBuffer.ReparseDataLength = (ushort)(targetDirBytes.Length + 12);
                reparseDataBuffer.SubstituteNameOffset = 0;
                reparseDataBuffer.SubstituteNameLength = (ushort)targetDirBytes.Length;
                reparseDataBuffer.PrintNameOffset = (ushort)(targetDirBytes.Length + 2);
                reparseDataBuffer.PrintNameLength = 0;
                reparseDataBuffer.PathBuffer = new byte[0x3ff0];
                Array.Copy(targetDirBytes, reparseDataBuffer.PathBuffer, targetDirBytes.Length);

                int inBufferSize = Marshal.SizeOf(reparseDataBuffer);
                IntPtr inBuffer = Marshal.AllocHGlobal(inBufferSize);

                try
                {
                    Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                    int bytesReturned;
                    bool result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_SET_REPARSE_POINT,
                            inBuffer, targetDirBytes.Length + 20, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                    if (!result)
                    {
                        ThrowLastWin32Error("Unable to create junction point.");
                    }
                }
                finally
                {Marshal.FreeHGlobal(inBuffer);
                }
            }
        }

        /// <summary>
        /// Creates a junction point from the specified directory to the specified target directory.
        /// </summary>
        /// <remarks>
        /// Only works on NTFS.
        /// </remarks>
        /// <param name="Junction">The junction point path</param>
        /// <param name="targetDir">The target directory</param>
        /// <param name="overwrite">If true overwrites an existing reparse point or empty directory</param>
        /// <param name="allowTargetNotExist">If true then allows the target directory to not exist, therefore creating a junction pointing to a location which doesn't exist</param>
        /// <exception cref="IOException">Thrown when the junction point could not be created or when
        /// an existing directory was found and <paramref name="overwrite" /> if false</exception>
        public static string GetJunctionData(string Junction)
        {
            if (!Directory.Exists(Junction))
            {
                throw new IOException("Directory does not exist so cannot get junction data");
            }

            using (SafeFileHandle handle = OpenReparsePoint(Junction, EFileAccess.GenericWrite))
            {
                int outBufferSize = Marshal.SizeOf(typeof(REPARSE_DATA_BUFFER));
                IntPtr outBuffer = Marshal.AllocHGlobal(outBufferSize);

                try
                {
                    int bytesReturned;
                    bool result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_GET_REPARSE_POINT,
                            IntPtr.Zero, 0, outBuffer, outBufferSize, out bytesReturned, IntPtr.Zero);

                    if (!result)
                    {
                        int error = Marshal.GetLastWin32Error();
                        if (error == ERROR_NOT_A_REPARSE_POINT)
                            return null;

                        ThrowLastWin32Error("Unable to get information about junction point.");
                    }

                    REPARSE_DATA_BUFFER reparseDataBuffer = (REPARSE_DATA_BUFFER)
                        Marshal.PtrToStructure(outBuffer, typeof(REPARSE_DATA_BUFFER));

                    if (reparseDataBuffer.ReparseTag != IO_REPARSE_TAG_MOUNT_POINT)
                        return null;

                    string targetDir = Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer,
                            reparseDataBuffer.SubstituteNameOffset, reparseDataBuffer.SubstituteNameLength);

                    if (targetDir.StartsWith(NonInterpretedPathPrefix))
                    {
                        if(!targetDir.StartsWith(VolumePrefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            targetDir = targetDir.Substring(NonInterpretedPathPrefix.Length);
                        }
                    }

                    return targetDir;
                }
                finally
                {
                    Marshal.FreeHGlobal(outBuffer);
                }
            }
        }

        /// <summary>
        /// Deletes a junction point at the specified source directory along with the directory itself.
        /// Does nothing if the junction point does not exist.
        /// </summary>
        /// <remarks>
        /// Only works on NTFS.
        /// </remarks>
        /// <param name="Junction">The junction point path</param>
        public static void Delete(string Junction)
        {
            if (!Directory.Exists(Junction))
            {
                if (File.Exists(Junction))
                    throw new IOException("Path is not a junction point.");

                return;
            }

            using (SafeFileHandle handle = OpenReparsePoint(Junction, EFileAccess.GenericWrite))
            {
                REPARSE_DATA_BUFFER reparseDataBuffer = new REPARSE_DATA_BUFFER();

                reparseDataBuffer.ReparseTag = IO_REPARSE_TAG_MOUNT_POINT;
                reparseDataBuffer.ReparseDataLength = 0;
                reparseDataBuffer.PathBuffer = new byte[0x3ff0];

                int inBufferSize = Marshal.SizeOf(reparseDataBuffer);
                IntPtr inBuffer = Marshal.AllocHGlobal(inBufferSize);
                try
                {
                    Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                    int bytesReturned;
                    bool result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_DELETE_REPARSE_POINT,
                            inBuffer, 8, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                    if (!result)
                        ThrowLastWin32Error("Unable to delete junction point.");
                }
                finally
                {
                    Marshal.FreeHGlobal(inBuffer);
                }

                try
                {
                    Directory.Delete(Junction);
                }
                catch (IOException ex)
                {
                    throw new IOException("Unable to delete junction point.", ex);
                }
            }
        }

        /// <summary>
        /// Deletes a junction point at the specified source directory but NOT the directory itself.
        /// Does nothing if the junction point does not exist.
        /// </summary>
        /// <remarks>
        /// Only works on NTFS.
        /// </remarks>
        /// <param name="Junction">The junction point path</param>
        public static void ClearJunction(string Junction)
        {
            if (!Directory.Exists(Junction))
            {
                if (File.Exists(Junction))
                    throw new IOException("Path is not a junction point.");

                return;
            }

            using (SafeFileHandle handle = OpenReparsePoint(Junction, EFileAccess.GenericWrite))
            {
                REPARSE_DATA_BUFFER reparseDataBuffer = new REPARSE_DATA_BUFFER();

                reparseDataBuffer.ReparseTag = IO_REPARSE_TAG_MOUNT_POINT;
                reparseDataBuffer.ReparseDataLength = 0;
                reparseDataBuffer.PathBuffer = new byte[0x3ff0];

                int inBufferSize = Marshal.SizeOf(reparseDataBuffer);
                IntPtr inBuffer = Marshal.AllocHGlobal(inBufferSize);
                try
                {
                    Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                    int bytesReturned;
                    bool result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_DELETE_REPARSE_POINT,
                            inBuffer, 8, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                    if (!result)
                    {
                        ThrowLastWin32Error("Unable to delete junction point.");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(inBuffer);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified path exists and refers to a junction point.
        /// </summary>
        /// <param name="path">The junction point path</param>
        /// <returns>True if the specified path represents a junction point</returns>
        /// <exception cref="IOException">Thrown if the specified path is invalid
        /// or some other error occurs</exception>
        public static bool Exists(string path)
        {
            if (!Directory.Exists(path))
                return false;

            using (SafeFileHandle handle = OpenReparsePoint(path, EFileAccess.GenericRead))
            {
                string target = InternalGetTarget(handle);
                return target != null;
            }
        }

        /// <summary>
        /// Gets the target of the specified junction point.
        /// </summary>
        /// <remarks>
        /// Only works on NTFS.
        /// </remarks>
        /// <param name="Junction">The junction point path</param>
        /// <returns>The target of the junction point</returns>
        /// <exception cref="IOException">Thrown when the specified path does not
        /// exist, is invalid, is not a junction point, or some other error occurs</exception>
        public static string GetTarget(string Junction)
        {
            using (SafeFileHandle handle = OpenReparsePoint(Junction, EFileAccess.GenericRead))
            {
                return InternalGetTarget(handle);
            }
        }

        private static string InternalGetTarget(SafeFileHandle handle)
        {
            int outBufferSize = Marshal.SizeOf(typeof(REPARSE_DATA_BUFFER));
            IntPtr outBuffer = Marshal.AllocHGlobal(outBufferSize);

            try
            {
                int bytesReturned;
                bool result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_GET_REPARSE_POINT,
                        IntPtr.Zero, 0, outBuffer, outBufferSize, out bytesReturned, IntPtr.Zero);

                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == ERROR_NOT_A_REPARSE_POINT)
                        return null;

                    ThrowLastWin32Error("Unable to get information about junction point.");
                }

                REPARSE_DATA_BUFFER reparseDataBuffer = (REPARSE_DATA_BUFFER)
                    Marshal.PtrToStructure(outBuffer, typeof(REPARSE_DATA_BUFFER));

                if (reparseDataBuffer.ReparseTag != IO_REPARSE_TAG_MOUNT_POINT)
                    return null;

                string targetDir = Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer,
                        reparseDataBuffer.SubstituteNameOffset, reparseDataBuffer.SubstituteNameLength);

                if (targetDir.StartsWith(NonInterpretedPathPrefix))
                {
                    if (!targetDir.StartsWith(VolumePrefix, StringComparison.InvariantCultureIgnoreCase))
                    {
                        targetDir = targetDir.Substring(NonInterpretedPathPrefix.Length);
                    }
                }

                return targetDir;
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }

        private static SafeFileHandle OpenReparsePoint(string reparsePoint, EFileAccess accessMode)
        {
            SafeFileHandle reparsePointHandle = new SafeFileHandle(CreateFile(reparsePoint, accessMode,
                        EFileShare.Read | EFileShare.Write | EFileShare.Delete,
                        IntPtr.Zero, ECreationDisposition.OpenExisting,
                        EFileAttributes.BackupSemantics | EFileAttributes.OpenReparsePoint, IntPtr.Zero), true);

            if (Marshal.GetLastWin32Error() != 0)
                ThrowLastWin32Error("Unable to open reparse point.");

            return reparsePointHandle;
        }

        private static void ThrowLastWin32Error(string message)
        {
            throw new IOException(message, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateHardLinkW(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        /// <summary>
        /// Creates a file as a hard link to an existing file
        /// </summary>
        /// <param name="newLinkName"></param>
        /// <param name="existingFileName"></param>
        /// <returns></returns>
        public static bool CreateHardLink(string newLinkName, string existingFileName)
        {
            //[DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
            //static extern bool CreateHardLinkW(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
            if( !CreateHardLinkW(newLinkName, existingFileName, IntPtr.Zero))
            {
                if (Marshal.GetLastWin32Error() != 0)
                {
                    ThrowLastWin32Error("Unable to create hard link.");
                }
            }
            return true;
        }

        /// <summary>Retrieves the volume mount point where the specified path is mounted.</summary>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// To get extended error information call Win32Exception()
        /// </returns>
        /// <remarks>Minimum supported client: Windows XP</remarks>
        /// <remarks>Minimum supported server: Windows Server 2003</remarks>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetVolumePathNameW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetVolumePathNameW([MarshalAs(UnmanagedType.LPWStr)] string lpszFileName, StringBuilder lpszVolumePathName, [MarshalAs(UnmanagedType.U4)] uint cchBufferLength);

        /// <summary>Creates an enumeration of all the hard links to the specified file.
        /// The FindFirstFileNameW function returns a handle to the enumeration that can be used on subsequent calls to the FindNextFileNameW function.
        /// </summary>
        /// <returns>
        /// If the function succeeds, the return value is a search handle that can be used with the FindNextFileNameW function or closed with the FindClose function.
        /// If the function fails, the return value is INVALID_HANDLE_VALUE (0xffffffff). To get extended error information, call the GetLastError function.
        /// </returns>
        /// <remarks>Minimum supported client: Windows Vista</remarks>
        /// <remarks>Minimum supported server: Windows Server 2008</remarks>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "FindFirstFileNameW")]
        static extern IntPtr FindFirstFileNameW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName, uint dwFlags, [In, Out] ref uint stringLength, StringBuilder linkName);

        /// <summary>
        ///   Continues enumerating the hard links to a file using the handle returned by a successful call to the FindFirstFileName function.
        /// </summary>
        /// <remarks>Minimum supported client: Windows Vista [desktop apps only].</remarks>
        /// <remarks>Minimum supported server: Windows Server 2008 [desktop apps only].</remarks>
        /// <returns>
        ///   If the function succeeds, the return value is nonzero. If the function fails, the return value is zero (0). To get extended error
        ///   information, call GetLastError. If no matching files can be found, the GetLastError function returns ERROR_HANDLE_EOF.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "FindNextFileNameW")]
        static extern bool FindNextFileNameW(IntPtr handle, [In, Out] ref uint stringLength, StringBuilder linkName);

        /// <summary>
        ///   Closes a file search handle opened by the FindFirstFile, FindFirstFileEx, FindFirstFileNameW, FindFirstFileNameTransactedW,
        ///   FindFirstFileTransacted, FindFirstStreamTransactedW, or FindFirstStreamW functions.
        /// </summary>
        /// <remarks>Minimum supported client: Windows XP [desktop apps | Windows Store apps].</remarks>
        /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps | Windows Store apps].</remarks>
        /// <returns>
        ///   If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error
        ///   information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FindClose(IntPtr hFindFile);

        /// <summary>
        /// Gets the list of file locations for <paramref name="filename"/> i.e., the list of all the hardlinked files
        /// </summary>
        /// <param name="filename">The name of the file to get the hard links for</param>
        /// <returns>The list of file locations for <paramref name="filename"/> i.e., the list of all the hardlinked files </returns>
        public static Collections.Generic.List<string> GetFileLocations(string filename)
        {
            var list = new Collections.Generic.List<string>();

            StringBuilder szVolumeRoot = new StringBuilder(HIGH_MAX_PATH);

            if (!GetVolumePathNameW(filename, szVolumeRoot, (uint)szVolumeRoot.MaxCapacity))
            {
                if (Marshal.GetLastWin32Error() != 0)
                {
                    ThrowLastWin32Error("Unable to create hard link.");
                }
            }

            StringBuilder szFileName = new StringBuilder(HIGH_MAX_PATH);
            uint length = (uint)szFileName.MaxCapacity;

            var hFind = FindFirstFileNameW(filename, 0, ref length, szFileName);
            try
            {
                if ((ulong)hFind.ToInt64() == INVALID_HANDLE_VALUE64)
                {
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        ThrowLastWin32Error("Unable to get the file locations");
                    }
                }

                list.Add(CombinePaths(szVolumeRoot.ToString(), szFileName.ToString()));

                length = (uint)szFileName.MaxCapacity;

                while (FindNextFileNameW(hFind, ref length, szFileName))
                {
                    list.Add(CombinePaths(szVolumeRoot.ToString(), szFileName.ToString()));
                    length = (uint)szFileName.MaxCapacity;
                }
            }
            finally
            {
                if ((ulong)hFind.ToInt64() != INVALID_HANDLE_VALUE64)
                {
                    if(!FindClose(hFind))
                    {
                        if (Marshal.GetLastWin32Error() != 0)
                        {
                            ThrowLastWin32Error("Unable to close the file locations file handle");
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Combines the paths where the second one might start with a '\'
        /// </summary>
        /// <param name="start"></param>
        /// <param name="finish"></param>
        /// <returns>The combined paths</returns>
        private static string CombinePaths(string start, string finish)
        {
            if( start.Length > 0 && start[start.Length-1] == '\\'
                &&
                finish.Length > 1 && finish[0] == '\\'
                )
            {
                if( finish.Length > 2)
                {
                    return Path.Combine(start, finish.Substring(1));
                }
                return start;
            }
            return Path.Combine(start, finish);
        }
    }
}

