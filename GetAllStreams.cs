using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Junction
{

    public static class GetAllStreams
    {
        public static void Main(string[] args)
        {
            // Get all the stream names from the first argument down

            if( args.Length == 0)
            {
                return;
            }

            List<string> filenamesWithStreams = ProcessFilename(args[0]);

          //  PrintList(filenamesWithStreams);
        }

        private static void PrintList(List<string> filenamesWithStreams)
        {
            foreach (var item in filenamesWithStreams)
            {
                Console.WriteLine(item);
            }
        }

        private static List<string> ProcessFilename(string filename)
        {
            string filename2 = "\\\\?\\" + filename;
            int error;
            List<string> streamnames = new List<string>();
            // Process a single file

            WIN32_FIND_STREAM_DATA data;
            IntPtr h = IntPtr.Zero;

            try
            {
                var attributes = System.IO.File.GetAttributes(filename);
                //var attributes = Win32Functions.GetFileAttributes(filename2);
                //if( attributes == Constants.INVALID_FILE_ATTRIBUTES)
                //{
                //    error = Marshal.GetLastWin32Error();
                //    Win32Functions.PrintError((uint)error);
                //    throw new Exception("Exception happened get the actual one");
                //}

                h = Win32Functions.FindFirstStream(filename, STREAM_INFO_LEVELS.FindStreamInfoStandard, out data);

                if( h.IsInvalidPointer())
                {
                    if( attributes.HasFlag(FileAttributes.Directory))
                    {
                        // This is a directory with no streams so just need to process the directory
                        streamnames = ProcessDirectory(filename);
                        return streamnames;
                    }
                    error = Marshal.GetLastWin32Error();
                    if( error == Win32Error.ERROR_FILE_NOT_FOUND)
                    {
                        return streamnames;
                    }
                    if (error == Win32Error.ERROR_ACCESS_DENIED)
                    {
                        return streamnames;
                    }
                    Win32Functions.PrintError((uint)error);
                    throw new Exception("Exception happened get the actual one");
                }

                do
                {
                    var foundIndex = data.cStreamName.IndexOf(":$DATA");
                    var actualStreamName = data.cStreamName.Substring(1, foundIndex - 1);
                    if( actualStreamName.Length > 0)
                    {

                        if( actualStreamName != "$CMDTCID"
                            && actualStreamName != "$CmdTcID"
                            && actualStreamName != "$CmdZnID"
                            && actualStreamName != "Zone.Identifier"
                             && actualStreamName != "encryptable"
                            )
                        {

                        }

                        string combinedName = filename + ":" + actualStreamName;
                        streamnames.Add(combinedName);

                        Console.WriteLine(combinedName + "\t" + data.StreamSize);

                    }
                } while (Win32Functions.FindNextStream(h, out data));
                //    yield return data;
                //var err2 = Win32Error.GetLastError();
                //if (err2 != Win32Error.ERROR_HANDLE_EOF)
                //    err2.ThrowIfFailed();

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    // This is a directory with no streams so just need to process the directory
                    streamnames.AddRange(ProcessDirectory(filename));
                    return streamnames;
                }
            }
            finally
            {
                if (!h.IsInvalidPointer())
                {
                    Win32Functions.FindClose(h);
                }
            }

            return streamnames;
        }

        private static List<string> ProcessDirectory(string filename)
        {
            List<string> streamnames = new List<string>();
            IntPtr handle = IntPtr.Zero;
            try
            {
                WIN32_FIND_DATA findData;

                if( !filename.EndsWith("\\"))
                {
                    filename += "\\";
                }

                string searchPath = filename + "*";

                handle = Win32Functions.FindFirstFile(searchPath, out findData);

                if (handle.IsInvalidPointer())
                {
                    return streamnames;
                }

                
                do
                {
                    if(findData.cFileName != "." && findData.cFileName != "..")
                    {
                        var newPath = Path.Combine(filename, findData.cFileName);
                        streamnames.AddRange(ProcessFilename(newPath));
                    }
                } while (Win32Functions.FindNextFile(handle, out findData));
            }
            finally
            {
                if (!handle.IsInvalidPointer())
                {
                    Win32Functions.FindClose(handle);
                }
            }

            // FindFirstFile
            //Then While (FindNextFile)

            return streamnames;
        }
    }

    public static class Constants
    {
        public const int MAX_PATH = 260;

        public const uint FORMAT_MESSAGE_IGNORE_INSERTS  = 0x00000200;
        public const uint FORMAT_MESSAGE_FROM_STRING     = 0x00000400;
        public const uint FORMAT_MESSAGE_FROM_HMODULE    = 0x00000800;
        public const uint FORMAT_MESSAGE_FROM_SYSTEM     = 0x00001000;
        public const uint FORMAT_MESSAGE_ARGUMENT_ARRAY  = 0x00002000;
        public const uint FORMAT_MESSAGE_MAX_WIDTH_MASK  = 0x000000FF;

        public const uint FILE_ATTRIBUTE_READONLY            = 0x00000001; 
        public const uint FILE_ATTRIBUTE_HIDDEN              = 0x00000002; 
        public const uint FILE_ATTRIBUTE_SYSTEM              = 0x00000004; 
        public const uint FILE_ATTRIBUTE_DIRECTORY           = 0x00000010; 
        public const uint FILE_ATTRIBUTE_ARCHIVE             = 0x00000020; 
        public const uint FILE_ATTRIBUTE_DEVICE              = 0x00000040; 
        public const uint FILE_ATTRIBUTE_NORMAL              = 0x00000080; 
        public const uint FILE_ATTRIBUTE_TEMPORARY           = 0x00000100; 
        public const uint FILE_ATTRIBUTE_SPARSE_FILE         = 0x00000200; 
        public const uint FILE_ATTRIBUTE_REPARSE_POINT       = 0x00000400; 
        public const uint FILE_ATTRIBUTE_COMPRESSED          = 0x00000800; 
        public const uint FILE_ATTRIBUTE_OFFLINE             = 0x00001000; 
        public const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000; 
        public const uint FILE_ATTRIBUTE_ENCRYPTED           = 0x00004000; 
        public const uint FILE_ATTRIBUTE_INTEGRITY_STREAM    = 0x00008000; 
        public const uint FILE_ATTRIBUTE_VIRTUAL             = 0x00010000;
        public const uint FILE_ATTRIBUTE_NO_SCRUB_DATA       = 0x00020000;
        public const uint INVALID_FILE_ATTRIBUTES = uint.MaxValue;
    }


    /// <summary>
    /// <para>Contains information about the stream found by the <c>FindFirstStreamW</c> or <c>FindNextStreamW</c> function.</para>
    /// </summary>
    // typedef struct _WIN32_FIND_STREAM_DATA { LARGE_INTEGER StreamSize; WCHAR cStreamName[MAX_PATH + 36];} WIN32_FIND_STREAM_DATA, *PWIN32_FIND_STREAM_DATA;
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WIN32_FIND_STREAM_DATA
    {
        /// <summary>
        /// <para>A <c>LARGE_INTEGER</c> value that specifies the size of the stream, in bytes.</para>
        /// </summary>
        public long StreamSize;

        /// <summary>
        /// <para>The name of the stream. The string name format is ":streamname:$streamtype".</para>
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.MAX_PATH + 36)]
        public readonly string cStreamName;
    }

    public enum STREAM_INFO_LEVELS
    {

        FindStreamInfoStandard,
        FindStreamInfoMaxInfoLevel
    }

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
        public ulong FileSize => Macros.MAKELONG64(nFileSizeLow, nFileSizeHigh);

    }


    public class Win32Functions
    {
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "FindFirstStreamW")]
        public static extern IntPtr FindFirstStream2(string lpFileName, STREAM_INFO_LEVELS InfoLevel, out WIN32_FIND_STREAM_DATA lpFindStreamData, [Optional] uint dwFlags);

        /// <summary>
        /// <para>Enumerates the first stream with a ::$DATA stream type in the specified file or directory.</para>
        /// <para>To perform this operation as a transacted operation, use the <c>FindFirstStreamTransactedW</c> function.</para>
        /// </summary>
        /// <param name="lpFileName">The fully qualified file name.</param>
        /// <param name="InfoLevel">
        /// <para>
        /// The information level of the returned data. This parameter is one of the values in the <c>STREAM_INFO_LEVELS</c> enumeration type.
        /// </para>
        /// <para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>FindStreamInfoStandard = 0</term>
        /// <term>The data is returned in a WIN32_FIND_STREAM_DATA structure.</term>
        /// </item>
        /// </list>
        /// </para>
        /// </param>
        /// <param name="lpFindStreamData">
        /// A pointer to a buffer that receives the file stream data. The format of this data depends on the value of the InfoLevel parameter.
        /// </param>
        /// <param name="dwFlags">Reserved for future use. This parameter must be zero.</param>
        /// <returns>
        /// <para>
        /// If the function succeeds, the return value is a search handle that can be used in subsequent calls to the <c>FindNextStreamW</c> function.
        /// </para>
        /// <para>If the function fails, the return value is <c>INVALID_HANDLE_VALUE</c>. To get extended error information, call <c>GetLastError</c>.</para>
        /// <para>If no streams can be found, the function fails and <c>GetLastError</c> returns <c>ERROR_HANDLE_EOF</c> (38).</para>
        /// </returns>
        // HANDLE WINAPI FindFirstStreamW( _In_ LPCWSTR lpFileName, _In_ STREAM_INFO_LEVELS InfoLevel, _Out_ LPVOID lpFindStreamData,
        // _Reserved_ DWORD dwFlags); https://msdn.microsoft.com/en-us/library/windows/desktop/aa364424(v=vs.85).aspx
        [DllImport(Lib.Kernel32, SetLastError = true, EntryPoint = "FindFirstStreamW", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstStream(string lpFileName, STREAM_INFO_LEVELS InfoLevel, out WIN32_FIND_STREAM_DATA lpFindStreamData, [Optional] uint dwFlags);


        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "FindNextStreamW")]
        public static extern bool FindNextStream(IntPtr hFindStream, out WIN32_FIND_STREAM_DATA lpFindStreamData);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "FindClose")]
        public static extern bool FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "FormatMessage")]
        public static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, [Out] StringBuilder lpBuffer, uint nSize, IntPtr Arguments);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetFileAttributesW")]
        public static extern uint GetFileAttributes(string lpFileName);

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



        public static void PrintError(uint dwErr)
        {
            StringBuilder msgBuilder = new StringBuilder(256);

            string formatExpression = "%1,%2%!";
            string[] formatArgs = new string[] { "Hello", "world" };

            IntPtr formatPtr = Marshal.StringToHGlobalAnsi(formatExpression);

            uint dwFlags = Constants.FORMAT_MESSAGE_IGNORE_INSERTS |
                Constants.FORMAT_MESSAGE_MAX_WIDTH_MASK |
                Constants.FORMAT_MESSAGE_FROM_SYSTEM;

            //must specify the FORMAT_MESSAGE_ARGUMENT_ARRAY flag when pass an array
            uint length = FormatMessage(dwFlags, IntPtr.Zero, dwErr, 0, msgBuilder, (uint)msgBuilder.MaxCapacity, IntPtr.Zero);

            Console.WriteLine(msgBuilder);
        }

        
    }

    public static partial class Macros
    {
        /// <summary>Retrieves the signed x-coordinate from the specified <c>LPARAM</c> value.</summary>
        /// <param name="lp">The value to be converted.</param>
        /// <returns>The signed x-coordinate.</returns>
        // https://docs.microsoft.com/en-us/windows/win32/api/windowsx/nf-windowsx-get_x_lparam
        // void GET_X_LPARAM( lp );
        public static int GET_X_LPARAM(IntPtr lp) => unchecked((short)(long)lp);

        /// <summary>Retrieves the signed y-coordinate from the given <c>LPARAM</c> value.</summary>
        /// <param name="lp">The value to be converted.</param>
        /// <returns>The signed y-coordinate.</returns>
        // https://docs.microsoft.com/en-us/windows/win32/api/windowsx/nf-windowsx-get_y_lparam
        // void GET_Y_LPARAM( lp );
        public static int GET_Y_LPARAM(IntPtr lp) => unchecked((short)((long)lp >> 16));

        /// <summary>Retrieves the high-order byte from the given 16-bit value.</summary>
        /// <param name="wValue">The value to be converted.</param>
        /// <returns>The return value is the high-order byte of the specified value.</returns>
        public static byte HIBYTE(ushort wValue) => (byte)((wValue >> 8) & 0xff);

        /// <summary>Gets the high 8-bytes from a <see cref="long"/> value.</summary>
        /// <param name="lValue">The <see cref="long"/> value.</param>
        /// <returns>The high 8-bytes as a <see cref="int"/>.</returns>
        public static int HighPart(this long lValue) => unchecked((int)(lValue >> 32));

        /// <summary>Retrieves the high-order word from the specified 32-bit value.</summary>
        /// <param name="dwValue">The value to be converted.</param>
        /// <returns>The return value is the high-order word of the specified value.</returns>
        public static ushort HIWORD(uint dwValue) => (ushort)((dwValue >> 16) & 0xffff);

        /// <summary>Retrieves the high-order word from the specified 32-bit value.</summary>
        /// <param name="dwValue">The value to be converted.</param>
        /// <returns>The return value is the high-order word of the specified value.</returns>
        public static ushort HIWORD(IntPtr dwValue) => unchecked((ushort)((long)dwValue >> 16));

        /// <summary>Retrieves the high-order word from the specified 32-bit value.</summary>
        /// <param name="dwValue">The value to be converted.</param>
        /// <returns>The return value is the high-order word of the specified value.</returns>
        public static ushort HIWORD(UIntPtr dwValue) => unchecked((ushort)((ulong)dwValue >> 16));

        /// <summary>Determines whether a value is an integer identifier for a resource.</summary>
        /// <param name="ptr">The pointer to be tested whether it contains an integer resource identifier.</param>
        /// <returns>If the value is a resource identifier, the return value is TRUE. Otherwise, the return value is FALSE.</returns>
          public static bool IS_INTRESOURCE(IntPtr ptr) => unchecked((ulong)ptr.ToInt64()) >> 16 == 0;

        /// <summary>Retrieves the low-order byte from the given 16-bit value.</summary>
        /// <param name="wValue">The value to be converted.</param>
        /// <returns>The return value is the low-order byte of the specified value.</returns>
        public static byte LOBYTE(ushort wValue) => (byte)(wValue & 0xff);

        /// <summary>Gets the lower 8-bytes from a <see cref="long"/> value.</summary>
        /// <param name="lValue">The <see cref="long"/> value.</param>
        /// <returns>The lower 8-bytes as a <see cref="uint"/>.</returns>
        public static uint LowPart(this long lValue) => (uint)(lValue & 0xffffffff);

        /// <summary>Retrieves the low-order word from the specified 32-bit value.</summary>
        /// <param name="dwValue">The value to be converted.</param>
        /// <returns>The return value is the low-order word of the specified value.</returns>
        public static ushort LOWORD(uint dwValue) => (ushort)(dwValue & 0xffff);

        /// <summary>Retrieves the low-order word from the specified 32-bit value.</summary>
        /// <param name="dwValue">The value to be converted.</param>
        /// <returns>The return value is the low-order word of the specified value.</returns>
        public static ushort LOWORD(IntPtr dwValue) => unchecked((ushort)(long)dwValue);

        /// <summary>Retrieves the low-order word from the specified 32-bit value.</summary>
        /// <param name="dwValue">The value to be converted.</param>
        /// <returns>The return value is the low-order word of the specified value.</returns>
        public static ushort LOWORD(UIntPtr dwValue) => unchecked((ushort)(ulong)dwValue);

        ///// <summary>
        ///// Converts an integer value to a resource type compatible with the resource-management functions. This macro is used in place of a
        ///// string containing the name of the resource.
        ///// </summary>
        ///// <param name="id">The integer value to be converted.</param>
        ///// <returns>The return value is string representation of the integer value.</returns>
        //public static ResourceId MAKEINTRESOURCE(int id) => id;

        /// <summary>Creates a LONG value by concatenating the specified values.</summary>
        /// <param name="wLow">The low-order word of the new value.</param>
        /// <param name="wHigh">The high-order word of the new value.</param>
        /// <returns>The return value is a LONG value.</returns>
        public static uint MAKELONG(ushort wLow, ushort wHigh) => ((uint)wHigh << 16) | ((uint)wLow & 0xffff);

        /// <summary>Creates a LONG64 value by concatenating the specified values.</summary>
        /// <param name="dwLow">The low-order double word of the new value.</param>
        /// <param name="dwHigh">The high-order double word of the new value.</param>
        /// <returns>The return value is a LONG64 value.</returns>
        public static ulong MAKELONG64(uint dwLow, uint dwHigh) => ((ulong)dwHigh << 32) | ((ulong)dwLow & 0xffffffff);

        /// <summary>Creates a LONG64 value by concatenating the specified values.</summary>
        /// <param name="dwLow">The low-order double word of the new value.</param>
        /// <param name="dwHigh">The high-order double word of the new value.</param>
        /// <returns>The return value is a LONG64 value.</returns>
        public static long MAKELONG64(uint dwLow, int dwHigh) => ((long)dwHigh << 32) | ((long)dwLow & 0xffffffff);

        /// <summary>Creates a value for use as an lParam parameter in a message. The macro concatenates the specified values.</summary>
        /// <param name="wLow">The low-order word of the new value.</param>
        /// <param name="wHigh">The high-order word of the new value.</param>
        /// <returns>The return value is an LPARAM value.</returns>
        public static IntPtr MAKELPARAM(ushort wLow, ushort wHigh) => new IntPtr(MAKELONG(wLow, wHigh));

        /// <summary>Creates a WORD value by concatenating the specified values.</summary>
        /// <param name="bLow">The low-order byte of the new value.</param>
        /// <param name="bHigh">The high-order byte of the new value.</param>
        /// <returns>The return value is a WORD value.</returns>
        public static ushort MAKEWORD(byte bLow, byte bHigh) => (ushort)(bHigh << 8 | bLow & 0xff);

        /// <summary>Retrieves the high-order 16-bit value from the specified 32-bit value.</summary>
        /// <param name="iValue">The value to be converted.</param>
        /// <returns>The return value is the high-order 16-bit value of the specified value.</returns>
        public static short SignedHIWORD(int iValue) => (short)((iValue >> 16) & 0xffff);

        /// <summary>Retrieves the high-order 16-bit value from the specified 32-bit value.</summary>
        /// <param name="iValue">The value to be converted.</param>
        /// <returns>The return value is the high-order 16-bit value of the specified value.</returns>
        public static short SignedHIWORD(IntPtr iValue) => SignedHIWORD(unchecked((int)iValue.ToInt64()));

        /// <summary>Retrieves the low-order 16-bit value from the specified 32-bit value.</summary>
        /// <param name="iValue">The value to be converted.</param>
        /// <returns>The return value is the low-order 16-bit value of the specified value.</returns>
        public static short SignedLOWORD(int iValue) => (short)(iValue & 0xffff);

        /// <summary>Retrieves the low-order 16-bit value from the specified 32-bit value.</summary>
        /// <param name="iValue">The value to be converted.</param>
        /// <returns>The return value is the low-order 16-bit value of the specified value.</returns>
        public static short SignedLOWORD(IntPtr iValue) => SignedLOWORD(unchecked((int)iValue.ToInt64()));

        public static bool IsInvalidPointer(this IntPtr pointer) => IntPtr.Equals(pointer, new IntPtr(-1));
    }



}
