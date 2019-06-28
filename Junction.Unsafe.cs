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
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct SymbolicLinkReparseBuffer
        {
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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x3FF0)]
                public fixed char PathBuffer[0x3FF0];
            //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x3FF0)]
            //    public string PathBuffer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public unsafe struct MountPointReparseBuffer
        {
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
            /// Indicates whether the symbolic link is absolute or relative.
            /// If Flags contains SYMLINK_FLAG_RELATIVE, the symbolic link contained in the PathBuffer array (at offset SubstituteNameOffset) is processed as a relative symbolic link; otherwise, it is processed as an absolute symbolic link.
            /// </summary>
            public ulong Flags;

            /// <summary>
            /// A buffer containing the unicode-encoded path string. The path string contains
            /// the substitute name string and print name string.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x3FF0)]
                public fixed char PathBuffer[0x3FF0];
            //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x3FF0)]
            //    public string PathBuffer;

        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
        public unsafe struct GenericReparseBuffer
        {
            [FieldOffset(0x0)]
            public fixed byte PathBuffer[0x7FE0];
            //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x3FF0)]
            //    public string PathBuffer;

        }

        [StructLayout(LayoutKind.Explicit)]
        public struct GenericReparseGeneralBuffer
        {
            [FieldOffset(0)]
            public SymbolicLinkReparseBuffer SymbolicLinkReparseBuffer;

            [FieldOffset(0)]
            public MountPointReparseBuffer MountPointReparseBuffer;

            [FieldOffset(0x0)]
            public GenericReparseBuffer GenericReparseBuffer;
        }

        //https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/ntifs/ns-ntifs-_reparse_data_buffer
        [StructLayout(LayoutKind.Sequential)]
        public struct REPARSE_DATA_BUFFER_2
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

            public GenericReparseGeneralBuffer Buffer;
        }       
    }
}

