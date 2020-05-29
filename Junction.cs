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
        public const uint SYMLINK_FLAG_RELATIVE = 0x00000001;

        public const uint ERROR_ALREADY_EXISTS = 0x000000B7;

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
        public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLinkEnum dwFlags);

        [Flags]
        public enum SymbolicLinkEnum
        {
            File = 0,
            Directory = 1,
            UnprivilegedCreate = 2,
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
        private const uint IO_REPARSE_TAG_HSM                      = 0xC0000004;
        private const uint IO_REPARSE_TAG_HSM2                     = 0x80000006;
        private const uint IO_REPARSE_TAG_SIS                      = 0x80000007;
        private const uint IO_REPARSE_TAG_WIM                      = 0x80000008;
        private const uint IO_REPARSE_TAG_CSV                      = 0x80000009;
        private const uint IO_REPARSE_TAG_DFS                      = 0x8000000A;
        private const uint IO_REPARSE_TAG_SYMLINK                  = 0xA000000C;
        private const uint IO_REPARSE_TAG_DFSR                     = 0x80000012;

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
        [StructLayout(LayoutKind.Explicit)]
        private struct OnlyType_REPARSE_DATA_BUFFER
        {
            /// <summary>
            /// Reparse point tag. Must be a Microsoft reparse point tag.
            /// </summary>
            [FieldOffset(0)]
            public uint ReparseTag;

            /// <summary>
            /// Size, in bytes, of the data after the Reserved member. This can be calculated by:
            /// (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength +
            /// (namesAreNullTerminated ? 2 * sizeof(char) : 0);
            /// </summary>
            [FieldOffset(4)]
            public ushort ReparseDataLength;

            /// <summary>
            /// Reserved; do not use.
            /// </summary>
            [FieldOffset(6)]
            public ushort Reserved;
        }

        //https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/ntifs/ns-ntifs-_reparse_data_buffer
        [StructLayout(LayoutKind.Sequential)]
        private struct MountPoint_REPARSE_DATA_BUFFER
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

        //https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/ntifs/ns-ntifs-_reparse_data_buffer
        [StructLayout(LayoutKind.Sequential)]
        private struct SymbolicLink_REPARSE_DATA_BUFFER
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
            /// Flags
            /// </summary>
            public uint Flags;

            /// <summary>
            /// A buffer containing the unicode-encoded path string. The path string contains
            /// the substitute name string and print name string.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FEC)]
            public byte[] PathBuffer;
        }

        //https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/ntifs/ns-ntifs-_reparse_data_buffer
        [StructLayout(LayoutKind.Sequential)]
        private struct Generic_REPARSE_DATA_BUFFER
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
            /// A buffer containing the unicode-encoded path string. The path string contains
            /// the substitute name string and print name string.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF8)]
            public byte[] DataBuffer;
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


        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetFileAttributesW")]
        public static extern uint GetFileAttributes(string lpFileName);

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

                MountPoint_REPARSE_DATA_BUFFER reparseDataBuffer = new MountPoint_REPARSE_DATA_BUFFER();

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
        public static void CreateSymlink2(string file, string targetFile, bool overwrite = false, bool allowTargetNotExist = false, bool asRelative = false)
        {
            //https://stackoverflow.com/questions/5188527/how-to-deal-with-files-with-a-name-longer-than-259-characters
            //https://docs.microsoft.com/en-us/archive/blogs/jeremykuhne/more-on-new-net-path-handling
            //https://nixhacker.com/understanding-and-exploiting-symbolic-link-in-windows/
            //http://www.flexhex.com/docs/articles/hard-links.phtml
            //https://docs.microsoft.com/en-us/windows/win32/fileio/reparse-points
            //https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/e57148b1-300b-4d1e-8f67-091de2de815e
            var isVolume = false;

            var fullTargetFilePath = targetFile;

            if (string.IsNullOrWhiteSpace(targetFile))
            {
                if (!allowTargetNotExist)
                {
                    throw new IOException("Target path not specified.");
                }
            }
            else
            {
                if (targetFile.StartsWith(VolumePrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    isVolume = true;
                }
                else
                {
                    if(asRelative)
                    {
                        var directory = Path.GetDirectoryName(file);

                        var actualTarget = Path.Combine(directory, targetFile);

                        fullTargetFilePath = Path.GetFullPath(actualTarget);
                    }
                    else
                    {
                        targetFile = Path.GetFullPath(targetFile);
                        fullTargetFilePath = targetFile;
                    }
                }
            }

            if (!isVolume && !allowTargetNotExist && !Directory.Exists(fullTargetFilePath) && !File.Exists(fullTargetFilePath))
            {
                throw new IOException("Target path does not exist, and have not allowed the target to not exist");
            }

            if (Directory.Exists(file))
            {
                if (!overwrite)
                    throw new IOException("Directory already exists and overwrite parameter is false.");
            }
            else if (File.Exists(file))
            {
                if (!overwrite)
                    throw new IOException("File already exists and overwrite parameter is false.");
            }
            else
            {
                //Directory.CreateDirectory(Junction);
            }

            using (SafeFileHandle handle = OpenReparsePoint(file, EFileAccess.GenericWrite, ECreationDisposition.OpenAlways))
            {
                byte[] targetFileBytes;
                if (string.IsNullOrEmpty(targetFile))
                {
                    targetFileBytes = new byte[] { };
                }
                else
                {
                    if (isVolume)
                    {
                        targetFileBytes = Encoding.Unicode.GetBytes(targetFile);
                    }
                    else
                    {
                        targetFileBytes = Encoding.Unicode.GetBytes(NonInterpretedPathPrefix + targetFile);
                    }
                }

                SymbolicLink_REPARSE_DATA_BUFFER reparseDataBuffer = new SymbolicLink_REPARSE_DATA_BUFFER();

                reparseDataBuffer.ReparseTag = IO_REPARSE_TAG_SYMLINK;
                reparseDataBuffer.ReparseDataLength = (ushort)(targetFileBytes.Length + 16);
                reparseDataBuffer.SubstituteNameOffset = 0;
                reparseDataBuffer.SubstituteNameLength = (ushort)targetFileBytes.Length;
                reparseDataBuffer.PrintNameOffset = (ushort)(targetFileBytes.Length + 2);
                reparseDataBuffer.PrintNameLength = 0;
                reparseDataBuffer.Flags = asRelative ? SYMLINK_FLAG_RELATIVE : 0x00;
                reparseDataBuffer.PathBuffer = new byte[0x3fec];
                Array.Copy(targetFileBytes, reparseDataBuffer.PathBuffer, targetFileBytes.Length);

                int inBufferSize = Marshal.SizeOf(reparseDataBuffer);
                IntPtr inBuffer = Marshal.AllocHGlobal(inBufferSize);

                try
                {
                    Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                    int bytesReturned;
                    bool result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_SET_REPARSE_POINT,
                            inBuffer, targetFileBytes.Length + 24, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                    if (!result)
                    {
                        ThrowLastWin32Error();
                        ThrowLastWin32Error("Unable to create junction point.");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(inBuffer);
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
        public static void CreateSymlink(string file, string targetFile, bool overwrite = false, bool allowTargetNotExist = false)
        {
            var isVolume = false;
            string fullPath = string.Empty;
            if (string.IsNullOrWhiteSpace(targetFile))
            {
                if (!allowTargetNotExist)
                {
                    throw new IOException("Target path not specified.");
                }
            }
            else
            {
                if (targetFile.StartsWith(VolumePrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    isVolume = true;
                }
                else
                {
                    fullPath = Path.GetFullPath(targetFile);
                }
            }

            if (!isVolume && !allowTargetNotExist && !Directory.Exists(targetFile) && !File.Exists(targetFile))
            {
                throw new IOException("Target path does not exist, and have not allowed the target to not exist");
            }

            if (Directory.Exists(file))
            {
                if (!overwrite)
                    throw new IOException("Directory already exists and overwrite parameter is false.");
            }
            else if (File.Exists(file))
            {
                if (!overwrite)
                    throw new IOException("File already exists and overwrite parameter is false.");

                File.Delete(file);
            }
            else
            {
                //Directory.CreateDirectory(Junction);
            }

            if( string.Equals(fullPath, targetFile))
            {
                targetFile = NonInterpretedPathPrefix2 + targetFile;
            }

            var success = CreateSymbolicLink(file, targetFile, SymbolicLinkEnum.File);
            if( !success)
            {
                ThrowLastWin32Error("Unable to create junction point.");
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
                int outBufferSize = Marshal.SizeOf(typeof(MountPoint_REPARSE_DATA_BUFFER));
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

                    OnlyType_REPARSE_DATA_BUFFER reparseDataBufferType = (OnlyType_REPARSE_DATA_BUFFER)
                        Marshal.PtrToStructure(outBuffer, typeof(OnlyType_REPARSE_DATA_BUFFER));

                    if (reparseDataBufferType.ReparseTag == IO_REPARSE_TAG_MOUNT_POINT)
                    {
                        MountPoint_REPARSE_DATA_BUFFER reparseDataBuffer = (MountPoint_REPARSE_DATA_BUFFER)
                            Marshal.PtrToStructure(outBuffer, typeof(MountPoint_REPARSE_DATA_BUFFER));

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
                    else if (reparseDataBufferType.ReparseTag == IO_REPARSE_TAG_SYMLINK)
                    {
                        SymbolicLink_REPARSE_DATA_BUFFER reparseDataBuffer = (SymbolicLink_REPARSE_DATA_BUFFER)
                            Marshal.PtrToStructure(outBuffer, typeof(SymbolicLink_REPARSE_DATA_BUFFER));

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
                    return null;
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
                MountPoint_REPARSE_DATA_BUFFER reparseDataBuffer = new MountPoint_REPARSE_DATA_BUFFER();

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
                MountPoint_REPARSE_DATA_BUFFER reparseDataBuffer = new MountPoint_REPARSE_DATA_BUFFER();

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

        //private static string InternalGetTargetOriginal(SafeFileHandle handle)
        //{
        //    int outBufferSize = Marshal.SizeOf(typeof(REPARSE_DATA_BUFFER));
        //    IntPtr outBuffer = Marshal.AllocHGlobal(outBufferSize);

        //    try
        //    {
        //        int bytesReturned;
        //        bool result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_GET_REPARSE_POINT,
        //                IntPtr.Zero, 0, outBuffer, outBufferSize, out bytesReturned, IntPtr.Zero);

        //        if (!result)
        //        {
        //            int error = Marshal.GetLastWin32Error();
        //            if (error == ERROR_NOT_A_REPARSE_POINT)
        //                return null;

        //            ThrowLastWin32Error("Unable to get information about junction point.");
        //        }

        //        REPARSE_DATA_BUFFER2 reparseDataBuffer = (REPARSE_DATA_BUFFER2)
        //            Marshal.PtrToStructure(outBuffer, typeof(REPARSE_DATA_BUFFER2));

        //        if (reparseDataBuffer.ReparseTag == IO_REPARSE_TAG_MOUNT_POINT)
        //        {
        //            string targetDir = Encoding.Unicode.GetString(reparseDataBuffer.MountPointReparseBuffer.PathBuffer,
        //   reparseDataBuffer.MountPointReparseBuffer.SubstituteNameOffset, reparseDataBuffer.MountPointReparseBuffer.SubstituteNameLength);

        //            if (targetDir.StartsWith(NonInterpretedPathPrefix))
        //            {
        //                if (!targetDir.StartsWith(VolumePrefix, StringComparison.InvariantCultureIgnoreCase))
        //                {
        //                    targetDir = targetDir.Substring(NonInterpretedPathPrefix.Length);
        //                }
        //            }

        //            return targetDir;

        //        }
        //   //     else if (reparseDataBuffer.ReparseTag == IO_REPARSE_TAG_SYMLINK)
        //   //     {
        //   //         string targetDir = Encoding.Unicode.GetString(reparseDataBuffer.SymbolicLinkReparseBuffer.PathBuffer,
        //   //reparseDataBuffer.SymbolicLinkReparseBuffer.SubstituteNameOffset, reparseDataBuffer.SymbolicLinkReparseBuffer.SubstituteNameLength);

        //   //         if (targetDir.StartsWith(NonInterpretedPathPrefix))
        //   //         {
        //   //             if (!targetDir.StartsWith(VolumePrefix, StringComparison.InvariantCultureIgnoreCase))
        //   //             {
        //   //                 targetDir = targetDir.Substring(NonInterpretedPathPrefix.Length);
        //   //             }
        //   //         }

        //   //         return targetDir;

        //   //     }
        //        return null;

        //    }
        //    finally
        //    {
        //        Marshal.FreeHGlobal(outBuffer);
        //    }
        //}

        private static string InternalGetTarget(SafeFileHandle handle)
        {
            int outBufferSize = Marshal.SizeOf(typeof(MountPoint_REPARSE_DATA_BUFFER));
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

                OnlyType_REPARSE_DATA_BUFFER onlyTypeBuffer = (OnlyType_REPARSE_DATA_BUFFER)
                    Marshal.PtrToStructure(outBuffer, typeof(OnlyType_REPARSE_DATA_BUFFER));

                if (onlyTypeBuffer.ReparseTag == IO_REPARSE_TAG_MOUNT_POINT)
                {
                    MountPoint_REPARSE_DATA_BUFFER reparseDataBuffer = (MountPoint_REPARSE_DATA_BUFFER)
                        Marshal.PtrToStructure(outBuffer, typeof(MountPoint_REPARSE_DATA_BUFFER));

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
                else if (onlyTypeBuffer.ReparseTag == IO_REPARSE_TAG_SYMLINK)
                {
                    SymbolicLink_REPARSE_DATA_BUFFER reparseDataBuffer = (SymbolicLink_REPARSE_DATA_BUFFER)
                        Marshal.PtrToStructure(outBuffer, typeof(SymbolicLink_REPARSE_DATA_BUFFER));

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
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }

        private static SafeFileHandle OpenReparsePoint(string reparsePoint, EFileAccess accessMode
            , ECreationDisposition openDisposition = ECreationDisposition.OpenExisting
            , EFileShare share = EFileShare.Read | EFileShare.Write | EFileShare.Delete
            , EFileAttributes flags = EFileAttributes.BackupSemantics | EFileAttributes.OpenReparsePoint
            //, EFileAttributes flags = EFileAttributes.OpenReparsePoint
            )
        {
            SafeFileHandle reparsePointHandle = new SafeFileHandle(CreateFile(reparsePoint, accessMode,
                        share,
                        IntPtr.Zero, openDisposition,
                        flags, IntPtr.Zero), true);

            var lastError = Marshal.GetLastWin32Error();

            if( reparsePointHandle.IsInvalid)
            {
                ThrowLastWin32Error();
            }

            //if ((openDisposition == ECreationDisposition.OpenAlways && lastError != 0 && lastError != ERROR_ALREADY_EXISTS)
            //    || (openDisposition != ECreationDisposition.OpenAlways && lastError != 0)
            //    )
            //    {
            //    ThrowLastWin32Error();
            //}

            return reparsePointHandle;
        }
        public static void ThrowLastWin32Error(string message)
        {
            throw new IOException(message, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
        }

        public static void ThrowLastWin32Error()
        {
            var errpr = Marshal.GetLastWin32Error();
            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
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
        /// Searches a directory for a file or subdirectory with a name that matches a specific name (or partial name if wildcards are used).
        /// </summary>
        /// <param name="lpFileName">
        /// The directory or path, and the file name. The file name can include wildcard characters, for example, an asterisk (*) or a
        /// question mark (?).
        /// <para>
        /// This parameter should not be NULL, an invalid string (for example, an empty string or a string that is missing the terminating
        /// null character), or end in a trailing backslash (\).
        /// </para>
        /// <para>
        /// If the string ends with a wildcard, period (.), or directory name, the user must have access permissions to the root and all
        /// subdirectories on the path.
        /// </para>
        /// </param>
        /// <param name="lpFindFileData">A pointer to the WIN32_FIND_DATA structure that receives information about a found file or directory.</param>
        /// <returns>
        /// If the function succeeds, the return value is a search handle used in a subsequent call to FindNextFile or FindClose, and the
        /// lpFindFileData parameter contains information about the first file or directory found.
        /// <para>
        /// If the function fails or fails to locate files from the search string in the lpFileName parameter, the return value is
        /// INVALID_HANDLE_VALUE and the contents of lpFindFileData are indeterminate. To get extended error information, call the
        /// GetLastError function.
        /// </para>
        /// <para>If the function fails because no matching files can be found, the GetLastError function returns ERROR_FILE_NOT_FOUND.</para>
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "FindFirstFileW")]
        public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        /// <summary>Continues a file search from a previous call to the FindFirstFile, FindFirstFileEx, or FindFirstFileTransacted functions.</summary>
        /// <param name="hFindFile">The search handle returned by a previous call to the FindFirstFile or FindFirstFileEx function.</param>
        /// <param name="lpFindFileData">A pointer to the WIN32_FIND_DATA structure that receives information about the found file or subdirectory.</param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero and the lpFindFileData parameter contains information about the next file
        /// or directory found.
        /// <para>
        /// If the function fails, the return value is zero and the contents of lpFindFileData are indeterminate. To get extended error
        /// information, call the GetLastError function.
        /// </para>
        /// <para>If the function fails because no more matching files can be found, the GetLastError function returns ERROR_NO_MORE_FILES.</para>
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindNextFile([In] IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            /// <summary>
            /// The file attributes of a file.
            /// <para>
            /// For possible values and their descriptions, see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/gg258117(v=vs.85).aspx">File Attribute Constants</a>.
            /// </para>
            /// <para>The FILE_ATTRIBUTE_SPARSE_FILE attribute on the file is set if any of the streams of the file have ever been sparse.</para>
            /// </summary>
            public FileAttributes dwFileAttributes;

            /// <summary>
            /// A FILETIME structure that specifies when a file or directory was created.
            /// <para>If the underlying file system does not support creation time, this member is zero.</para>
            /// </summary>
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;

            /// <summary>
            /// A FILETIME structure.
            /// <para>For a file, the structure specifies when the file was last read from, written to, or for executable files, run.</para>
            /// <para>
            /// For a directory, the structure specifies when the directory is created. If the underlying file system does not support last
            /// access time, this member is zero.
            /// </para>
            /// <para>
            /// On the FAT file system, the specified date for both files and directories is correct, but the time of day is always set to midnight.
            /// </para>
            /// </summary>
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;

            /// <summary>
            /// A FILETIME structure.
            /// <para>
            /// For a file, the structure specifies when the file was last written to, truncated, or overwritten, for example, when WriteFile or
            /// SetEndOfFile are used. The date and time are not updated when file attributes or security descriptors are changed.
            /// </para>
            /// <para>
            /// For a directory, the structure specifies when the directory is created. If the underlying file system does not support last write
            /// time, this member is zero.
            /// </para>
            /// </summary>
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;

            /// <summary>
            /// The high-order DWORD value of the file size, in bytes.
            /// <para>This value is zero unless the file size is greater than MAXDWORD.</para>
            /// <para>The size of the file is equal to (nFileSizeHigh * (MAXDWORD+1)) + nFileSizeLow.</para>
            /// </summary>
            public uint nFileSizeHigh;

            /// <summary>The low-order DWORD value of the file size, in bytes.</summary>
            public uint nFileSizeLow;

            /// <summary>
            /// If the dwFileAttributes member includes the FILE_ATTRIBUTE_REPARSE_POINT attribute, this member specifies the reparse point tag.
            /// <para>Otherwise, this value is undefined and should not be used.</para>
            /// </summary>
            public int dwReserved0;

            /// <summary>Reserved for future use.</summary>
            public int dwReserved1;

            /// <summary>The name of the file.</summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;

            /// <summary>
            /// An alternative name for the file.
            /// <para>This name is in the classic 8.3 file name format.</para>
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;

            /// <summary>Gets the size of the file, combining <see cref="nFileSizeLow"/> and <see cref="nFileSizeHigh"/>.</summary>
            /// <value>The size of the file.</value>
            public ulong FileSize => MAKELONG64(nFileSizeLow, nFileSizeHigh);

        }

        private static ulong MAKELONG64(uint dwLow, uint dwHigh) => ((ulong)dwHigh << 32) | ((ulong)dwLow & 0xffffffff);

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
                    ThrowLastWin32Error("Unable to get the volume information for " + filename);
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
                        ThrowLastWin32Error("Unable to get the file locations for " + filename);
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

        public static Collections.Generic.List<WIN32_FIND_DATA> GetDirectoryListing(string filename)
        {
            Collections.Generic.List<WIN32_FIND_DATA> streamnames = new Collections.Generic.List<WIN32_FIND_DATA>();
            IntPtr handle = IntPtr.Zero;
            try
            {
                WIN32_FIND_DATA findData;

                if (!filename.EndsWith("\\"))
                {
                    filename += "\\";
                }

                string searchPath = filename + "*";

                handle = FindFirstFile(searchPath, out findData);

                if (handle.IsInvalidPointer())
                {
                    return streamnames;
                }


                do
                {
                    if (findData.cFileName != "." && findData.cFileName != "..")
                    {
                        streamnames.Add(findData);
                    }
                } while (FindNextFile(handle, out findData));
            }
            finally
            {
                if (!handle.IsInvalidPointer())
                {
                    FindClose(handle);
                }
            }

            // FindFirstFile
            //Then While (FindNextFile)

            return streamnames;
        }

        public static bool IsInvalidPointer(this IntPtr pointer) => IntPtr.Equals(pointer, new IntPtr(-1));
    }

    public static partial class Junction
    {
        public static class Constants
        {
            public const int MAX_PATH = 260;

            public const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
            public const uint FORMAT_MESSAGE_FROM_STRING = 0x00000400;
            public const uint FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
            public const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            public const uint FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
            public const uint FORMAT_MESSAGE_MAX_WIDTH_MASK = 0x000000FF;

            public const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
            public const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
            public const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
            public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
            public const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
            public const uint FILE_ATTRIBUTE_DEVICE = 0x00000040;
            public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
            public const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;
            public const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200;
            public const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
            public const uint FILE_ATTRIBUTE_COMPRESSED = 0x00000800;
            public const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
            public const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
            public const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
            public const uint FILE_ATTRIBUTE_INTEGRITY_STREAM = 0x00008000;
            public const uint FILE_ATTRIBUTE_VIRTUAL = 0x00010000;
            public const uint FILE_ATTRIBUTE_NO_SCRUB_DATA = 0x00020000;
            public const uint INVALID_FILE_ATTRIBUTES = uint.MaxValue;
        }
    }
}

