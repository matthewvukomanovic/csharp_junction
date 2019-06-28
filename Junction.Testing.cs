using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public static partial class Junction
    {
        public static class Testing
        {
            public static void Main()
            {

                GenericReparseGeneralBuffer buff = new GenericReparseGeneralBuffer();
                buff.MountPointReparseBuffer.PrintNameOffset = 5;
                buff.MountPointReparseBuffer.PrintNameLength = 50;
                buff.MountPointReparseBuffer.Flags = 79;

                Assert(buff.SymbolicLinkReparseBuffer.PrintNameOffset==5, "buff.SymbolicLinkReparseBuffer.PrintNameOffset==5");
                Assert(buff.SymbolicLinkReparseBuffer.PrintNameLength==50, "buff.SymbolicLinkReparseBuffer.PrintNameLength==50");

                buff.SymbolicLinkReparseBuffer.PrintNameOffset = 45;
                buff.SymbolicLinkReparseBuffer.PrintNameLength = 6;

                Assert(buff.MountPointReparseBuffer.PrintNameOffset==45, "buff.MountPointReparseBuffer.PrintNameOffset==45");
                Assert(buff.MountPointReparseBuffer.PrintNameLength==6, "buff.MountPointReparseBuffer.PrintNameLength==6");


                //JunctionPoint.Create(@"s:\test\junc", @"b:\", true, true);
                //
                //JunctionPoint.Create(@"B:\J\Temp", @"K:\", true, true);
                //
                //JunctionPoint.GetJunctionPointData(@"B:\J\Video\Movies");
                //JunctionPoint.GetJunctionPointData(@"s:\test\junc");
                //
                //JunctionPoint.Create(@"s:\test\junc", @"s:\test\junc2", true, true);
                ////JunctionPoint.Create(@"s:\test\junc", @"\\?\UNC\10.1.1.2\b\", true, true);
                //
                //GetSymbolicLinkTarget(@"b:\").Dump();
                //GetSymbolicLinkTarget(@"s:\test\target").Dump();
                //GetSymbolicLinkTarget(@"s:\test\").Dump();
                //GetSymbolicLinkTarget(new DirectoryInfo(@"s:\test\target")).Dump();
                ////Directory.CreateDirectory( @"s:\test\target");
                ////JunctionPoint.Create(@"s:\test\junc", @"s:\test\target", true, true);
                //JunctionPoint.Create(@"s:\test\junc", @"s:\test\junc2", true, true);
                //JunctionPoint.Create(@"s:\test\junc2", @"s:\test\target", true, true);
                ////JunctionPoint.Create(@"s:\test\junc", @"s:\test\t2", true, true);
                ////JunctionPoint.Create(@"s:\test\junc", @"s:\test\", true, true);
                //////JunctionPoint.Create(@"s:\test\junc", null, true, true);
                ////JunctionPoint.ClearJunctionPoint(@"s:\test\junc");
                ////uint i32 = (uint)Marshal.GetHRForLastWin32Error();
                //JunctionPoint.GetTarget(@"s:\test\junc2").Dump();
                //JunctionPoint.GetTarget(@"s:\test\junc").Dump();
                //GetSymbolicLinkTarget(new DirectoryInfo(@"s:\test\junc2")).Dump();
                //GetSymbolicLinkTarget(new DirectoryInfo(@"s:\test\junc")).Dump();
                System.Console.WriteLine(buff.MountPointReparseBuffer.PrintNameOffset);
            }

            public static void Assert(bool condition, string message = null)
            {
                if( !condition)
                {
                    if( string.IsNullOrWhiteSpace(message))
                    {
                        message = "Assert Failed";
                    }
                    System.Console.Error.WriteLine("Failed: " + message);
                    System.Console.Error.WriteLine(Environment.StackTrace);
                    
                    Environment.Exit(-1);
                }
            }

            // public static unsafe void Main()
            // {
            //     REPARSE_DATA_BUFFER_2 ex = new REPARSE_DATA_BUFFER_2();
            //     byte* addr = (byte*)&ex;
            //     Console.WriteLine("Size:      													{0}", sizeof(GenericReparseGeneralBuffer));
            //     Console.WriteLine("ex.GenericReparseBuffer.PathBuffer Offset:					{0}", (byte*)ex.Buffer.GenericReparseBuffer.PathBuffer - addr);
            //     Console.WriteLine("ex.SymbolicLinkReparseBuffer.SubstituteNameOffset Offset:	{0}", (byte*)&ex.Buffer.SymbolicLinkReparseBuffer.SubstituteNameOffset - addr);
            //     Console.WriteLine("ex.SymbolicLinkReparseBuffer.SubstituteNameLength Offset:	{0}", (byte*)&ex.Buffer.SymbolicLinkReparseBuffer.SubstituteNameLength - addr);
            //     Console.WriteLine("ex.SymbolicLinkReparseBuffer.PrintNameOffset Offset:			{0}", (byte*)&ex.Buffer.SymbolicLinkReparseBuffer.PrintNameOffset - addr);
            //     Console.WriteLine("ex.SymbolicLinkReparseBuffer.PrintNameLength Offset:			{0}", (byte*)&ex.Buffer.SymbolicLinkReparseBuffer.PrintNameLength - addr);
            //     Console.WriteLine("ex.SymbolicLinkReparseBuffer.PathBuffer Offset:				{0}", (byte*)ex.Buffer.SymbolicLinkReparseBuffer.PathBuffer - addr);

            //     Console.WriteLine("ex.MountPointReparseBuffer.SubstituteNameOffset Offset:		{0}", (byte*)&ex.Buffer.MountPointReparseBuffer.SubstituteNameOffset - addr);
            //     Console.WriteLine("ex.MountPointReparseBuffer.SubstituteNameLength Offset:		{0}", (byte*)&ex.Buffer.MountPointReparseBuffer.SubstituteNameLength - addr);
            //     Console.WriteLine("ex.MountPointReparseBuffer.PrintNameOffset Offset:			{0}", (byte*)&ex.Buffer.MountPointReparseBuffer.PrintNameOffset - addr);
            //     Console.WriteLine("ex.MountPointReparseBuffer.PrintNameLength Offset:			{0}", (byte*)&ex.Buffer.MountPointReparseBuffer.PrintNameLength - addr);
            //     Console.WriteLine("ex.MountPointReparseBuffer.Flags Offset:						{0}", (byte*)&ex.Buffer.MountPointReparseBuffer.Flags - addr);
            //     Console.WriteLine("ex.MountPointReparseBuffer.PathBuffer Offset:				{0}", (byte*)ex.Buffer.MountPointReparseBuffer.PathBuffer - addr);
            //     ex.Reserved = 7;
            //     ex.Buffer.MountPointReparseBuffer.Flags = 7881;

            //     ex.Buffer.SymbolicLinkReparseBuffer.PathBuffer[0] = 'h';
            //     ex.Buffer.SymbolicLinkReparseBuffer.PathBuffer[1] = 'e';
            //     ex.Buffer.SymbolicLinkReparseBuffer.PathBuffer[2] = 'l';
            //     ex.Buffer.SymbolicLinkReparseBuffer.PathBuffer[3] = 'l';
            //     ex.Buffer.SymbolicLinkReparseBuffer.PathBuffer[4] = '0';
            //     ex.Buffer.SymbolicLinkReparseBuffer.PathBuffer[5] = '\0';
            // }

        }
    }
}

