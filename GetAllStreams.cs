using System;
using System.Collections.Generic;
using System.Linq;
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

            PrintList(filenamesWithStreams);
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
            List<string> streamnames = new List<string>();
            // Process a single file


            // Test to see if this is a directory
            // If it is then 
            //    Get a directory listing
            //    ProcessAllItems under this directory

            return streamnames;
        }
    }

    public static class Constants
    {
        public const int MAX_PATH = 260;
    }

    public struct DUMMYSTRUCTNAME
    {
        uint LowPart;
        int HighPart;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct LARGE_INTEGER
    {
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        uint LowPart;
        [System.Runtime.InteropServices.FieldOffsetAttribute(4)]
        int HighPart;
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        DUMMYSTRUCTNAME u;
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        long QuadPart;
    }


    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct WIN32_FIND_STREAM_DATA
    {

        LARGE_INTEGER StreamSize;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = Constants.MAX_PATH + 36)]
        public char[] cStreamName;
    }

}
