using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace System.IO
{
    public static partial class JunctionTesting
    {
        public const string baseDirectory = "testing";
        public const string actualTarget1 = baseDirectory + "\\target1";
        public const string actualTarget2 = baseDirectory + "\\target2";

        public const string junctionDirectory = baseDirectory + "\\junc";
        public const string junctionDirectory2 = baseDirectory + "\\junc2";

        public const string junc1FilePath = "file1";
        public const string junc2FilePath = "file2";

#if false
        public static void Main()
        {
            //var path = "C:\\";
            //var listing = Junction.GetDirectoryListing(path);
            //var item = Path.Combine(path, listing[0].cFileName);

            Junction.CreateSymlink(@"C:\ProgramData\Oracle\Java\javapath\java.exe", @"C:\Program Files\Java\jre1.8.0_144\bin\java.exe", true); // C:\Windows.old\Program Files\Java\jre1.8.0_144\bin\java.exe

            Junction.CreateSymlink(@"C:\ProgramData\Oracle\Java\javapath\java.exe", @"C:\Program Files\Java\jre1.8.0_144\bin\java.exe", true); // C:\Windows.old\Program Files\Java\jre1.8.0_144\bin\java.exe

            string file, test;
            file = @"C:\Users\Matthew Vukomanovic\Documents";

            file = @"C:\Documents and Settings";
            test = Junction.GetTarget(file);
            file = @"C:\Users\Matthew Vukomanovic\.gitconfig";
            test = Junction.GetTarget(file);

            Junction.CreateSymlink2(file + "3", test, true, true);
            Junction.CreateSymlink(file + "2", test, true, true);
        }

        public static void Main2()
#else
        public static void Main()
#endif
        {
            ClearBaseDirectory();

            Directory.CreateDirectory(actualTarget1);

            try
            {
                RunTests();
                System.Console.WriteLine("Tests Finished");
            }
            catch(Exception exc)
            {
                System.Console.Error.WriteLine(exc.Message);
                System.Console.Error.WriteLine(exc.StackTrace);
                Environment.Exit(-2);
            }
            finally
            {
                ClearBaseDirectory();
            }
        }

        public static void ClearBaseDirectory()
        {
            try
            {
                System.Console.WriteLine("Clearing directrory");
                if( Directory.Exists(baseDirectory))
                {
                    System.Console.WriteLine($"Directory {baseDirectory} exists so delete");
                    Directory.Delete(baseDirectory, true);
                }
            }
            catch (Exception exc)
            {
                System.Console.Error.WriteLine($"Could not delete {baseDirectory}, failed with error {exc.Message}");
            }
        }

        public static void RunTests()
        {
            RunTestGetRelative();

            // Make sure that the directories dont already exist
            Assert(!Directory.Exists(junctionDirectory));
            Assert(!Directory.Exists(junctionDirectory2));

            // Make sure that the junctions don't report existing
            Assert(!Junction.Exists(junctionDirectory));
            Assert(!Junction.Exists(junctionDirectory2));

            // Create a junction to a directory which does exist
            Junction.Create(junctionDirectory, actualTarget1);
            Assert(Directory.Exists(junctionDirectory));
            Assert(Junction.Exists(junctionDirectory));

            //  Make sure that the create junction throws an exception when trying to recreate it
            AssertThrows(() => Junction.Create(junctionDirectory, actualTarget1));
            Junction.Create(junctionDirectory, actualTarget1, true);
            Assert(Junction.Exists(junctionDirectory));

            // Make sure that the create junction with allow override but without target not exist will throw against no existant directory
            AssertThrows(() => Junction.Create(junctionDirectory, actualTarget2, true, false));
            Junction.Create(junctionDirectory, actualTarget2, true, true);
            Assert(Junction.Exists(junctionDirectory));

            // Make sure that the junctionDirectory2 doesn't exist yet
            Assert(!Junction.Exists(junctionDirectory2));

            // Make sure that the create junction with allow override but without target and existing junction not exist will throw against no existant directory
            AssertThrows(() => Junction.Create(junctionDirectory2, actualTarget2, true));
            Assert(!Directory.Exists(junctionDirectory2));
            Assert(!Junction.Exists(junctionDirectory2));

            // Make sure that the create junction with allow override and not exist allowed where the target doesn't exist will NOT throw an exception
            Junction.Create(junctionDirectory2, actualTarget2, true, true);
            Assert(Directory.Exists(junctionDirectory2));
            Assert(Junction.Exists(junctionDirectory2));

            // Make sure that the junction will also be allowed to be cleared
            Junction.ClearJunction(junctionDirectory2);
            Assert(Directory.Exists(junctionDirectory2));
            Assert(!Junction.Exists(junctionDirectory2));
            //System.Console.WriteLine("Exists: " + junctionDirectory2);

            // Clear Junction should fail if the directory is not a junction
            AssertThrows(() => Junction.ClearJunction(junctionDirectory2));
            Assert(Directory.Exists(junctionDirectory2));
            //  //System.Console.WriteLine("Exists: " + junctionDirectory2);
            //  Assert(!Junction.Exists(junctionDirectory2));

            // System.Console.WriteLine(junctionDirectory);
            // System.Console.WriteLine($"GetJunctionData for {junctionDirectory} = {Junction.GetJunctionData(junctionDirectory)}");
            // System.Console.WriteLine($"GetTarget for {junctionDirectory} = {Junction.GetTarget(junctionDirectory)}");
            // var fullPath = Path.GetFullPath(junctionDirectory);
            // System.Console.WriteLine($"Full Path {fullPath}");

            // TODO: This one shouldn't throw an exception if it points to an incorrect folder
            //System.Console.WriteLine($"GetSymbolicLinkTarget for {fullPath}= {Junction.GetSymbolicLinkTarget(fullPath)}");

            // void Create(string Junction, string targetDir, bool overwrite = false, bool allowTargetNotExist = false)
            // string GetTarget(string Junction)
            // string GetJunctionData(string Junction)
            // string GetSymbolicLinkTarget(string fullPath)
            // string GetSymbolicLinkTarget(System.IO.DirectoryInfo symlink)

            // Make sure that the GetTarget is the same as the full path for the actualTarget2 (even when not exists)
            Assert(Junction.GetTarget(junctionDirectory) == Path.GetFullPath(actualTarget2));

            // Create a junction2 to the existant directory
            Junction.Create(junctionDirectory2, actualTarget1, true, true);
            // Override the junction to the other junction
            Junction.Create(junctionDirectory, junctionDirectory2, true, true);

            // System.Console.WriteLine($"GetJunctionData for {junctionDirectory} = {Junction.GetJunctionData(junctionDirectory)}");
            // System.Console.WriteLine($"GetTarget for {junctionDirectory} = {Junction.GetTarget(junctionDirectory)}");

            //  System.Console.WriteLine($"GetSymbolicLinkTarget for {Path.GetFullPath(junctionDirectory)} = {Junction.GetSymbolicLinkTarget(Path.GetFullPath(junctionDirectory))}");
            //  System.Console.WriteLine($"{Path.GetFullPath(actualTarget1)}");

            // Make sure that the GetJunctionData, GetTarget, and GetSymbolicLinkTarget return the correct data
            Assert(Junction.GetJunctionData(junctionDirectory) == Path.GetFullPath(junctionDirectory2));
            Assert(Junction.GetTarget(junctionDirectory) == Path.GetFullPath(junctionDirectory2));
            Assert(Junction.GetSymbolicLinkTarget(Path.GetFullPath(junctionDirectory)).Equals(Path.GetFullPath(actualTarget1), StringComparison.InvariantCultureIgnoreCase));

            // void ClearJunction(string Junction)
            // void Delete(string Junction)
            // bool Exists(string path)

            // Make sure that the junction will delete correctly when it's pointing to another junction
            Junction.Delete(junctionDirectory);
            Assert(!Junction.Exists(junctionDirectory));
            Assert(!Directory.Exists(junctionDirectory));

            // make sure that the clear junction works when it's pointing to a real directory
            Junction.ClearJunction(junctionDirectory2);
            Assert(!Junction.Exists(junctionDirectory2));
            Assert(Directory.Exists(junctionDirectory2));

            const string volumeTestLocation = @"\??\Volume{00000035-3fe0-82c9-571e-d4016a000000}\";
            const string volumeTestLocationDrive = @"E:\";
            // Test creating a junction to a volume
            Junction.Create(junctionDirectory2, volumeTestLocation, true, true);

            // Test that the junction will still say it exists
            Assert(Junction.Exists(junctionDirectory2));
            Assert(Directory.Exists(junctionDirectory2));

            // Make sure that the get symbolic links functions return the correct values
            Assert(Junction.GetJunctionData(junctionDirectory2) == volumeTestLocation);
            Assert(Junction.GetTarget(junctionDirectory2) == volumeTestLocation);
            Assert(Junction.GetSymbolicLinkTarget(Path.GetFullPath(junctionDirectory2)).Equals(volumeTestLocationDrive, StringComparison.InvariantCultureIgnoreCase));
            // Test clearing that same junction
            Junction.ClearJunction(junctionDirectory2);

            Directory.Delete(junctionDirectory2, true);

            // Test creating hard link files
            var combined1FilePath = Path.Combine(actualTarget1, junc1FilePath);
            var full1FilePath = Path.Combine(Path.GetFullPath(actualTarget1), junc1FilePath);

            var combined2FilePath = Path.Combine(actualTarget1, junc2FilePath);
            var full2FilePath = Path.Combine(Path.GetFullPath(actualTarget1), junc2FilePath);

            var combined3FilePath = Path.Combine(baseDirectory, junc1FilePath);
            var full3FilePath = Path.Combine(Path.GetFullPath(baseDirectory), junc1FilePath);

            var contents = "This is the content of the file\r\n";
            var contents2 = "This is the extra contents of the file\r\n";
            var contents2Complete = contents + contents2;

            SetContentOfFile(combined1FilePath, contents);

            // Ensure that the file contents is the same as expected
            Assert(File.ReadAllText(full1FilePath) == contents);

            // Test getting the target locations for the file
            var targetList = Junction.GetFileLocations(combined1FilePath);
            Assert(targetList.Count == 1);
            Assert(targetList[0] == full1FilePath);

            // Create a hard link to the file we just created
            Junction.CreateHardLink(combined2FilePath, combined1FilePath);
            // Ensure that the hard link was created
            Assert(File.Exists(combined2FilePath));
            // Ensure that the hard link has the correct contents
            Assert(File.ReadAllText(full2FilePath) == contents);

            int testingIndex;

            // Ensure that the hard link get file locations function works for the location
            targetList = Junction.GetFileLocations(combined1FilePath);
            targetList.Sort();
            Assert(targetList.Count == 2);
            testingIndex = 0;
            Assert(targetList[testingIndex++] == full1FilePath);
            Assert(targetList[testingIndex++] == full2FilePath);

            // Ensure that the hard link from the second file function works for the location
            targetList = Junction.GetFileLocations(combined2FilePath);
            targetList.Sort();
            Assert(targetList.Count == 2);
            testingIndex = 0;
            Assert(targetList[testingIndex++] == full1FilePath);
            Assert(targetList[testingIndex++] == full2FilePath);

            AppendContentToFile(combined2FilePath, contents2);
            Assert(File.ReadAllText(full1FilePath) == contents2Complete);

            // Create a hard link to the file we just created
            Junction.CreateHardLink(combined3FilePath, combined2FilePath);

            // Ensure that the function works from the second for all 3
            targetList = Junction.GetFileLocations(combined2FilePath);
            targetList.Sort();
            Assert(targetList.Count == 3);

            testingIndex = 0;
            Assert(targetList[testingIndex++] == full3FilePath);
            Assert(targetList[testingIndex++] == full1FilePath);
            Assert(targetList[testingIndex++] == full2FilePath);

            targetList = Junction.GetFileLocations(combined3FilePath);
            targetList.Sort();
            Assert(targetList.Count == 3);

            testingIndex = 0;
            Assert(targetList[testingIndex++] == full3FilePath);
            Assert(targetList[testingIndex++] == full1FilePath);
            Assert(targetList[testingIndex++] == full2FilePath);

            Directory.Delete(actualTarget1, true);
        }

        private static void RunTestGetRelative()
        {
            var failing = new[] {
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"c:\1\2\path\we\follow", targetPath = @"g:\1\2\onthedrive", expected=@"g:\1\2\onthedrive", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"..\..\onthedrive", expected=@"..\..\..\..\onthedrive", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\we\nowtarget", expected=@"nowtarget", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\we\differentiate\nowtarget", expected=@"differentiate\nowtarget", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\that\we\differentiate\nowtarget", expected=@"..\that\we\differentiate\nowtarget", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"..\path\we\nowtarget", expected=@"..\..\..\path\we\nowtarget", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"..\path\we\..\..\path\we\nowtarget", expected=@"..\..\..\path\we\nowtarget", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"..\something\else\..\..\path\we\nowtarget", expected=@"..\..\..\path\we\nowtarget", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\we\follow", expected=@"follow", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\we", expected=@".", },
            new { ignoreCase=true, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path", expected=@"..", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"c:\1\2\path\we\follow", targetPath = @"g:\1\2\onthedrive", expected=@"g:\1\2\onthedrive", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"..\..\onthedrive", expected=@"..\..\..\..\..\onthedrive", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we\nowtarget", expected=@"..\nowtarget", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we\differentiate\nowtarget", expected=@"..\differentiate\nowtarget", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\that\we\differentiate\nowtarget", expected=@"..\..\that\we\differentiate\nowtarget", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"..\path\we\nowtarget", expected=@"..\..\..\..\path\we\nowtarget", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"..\path\we\..\..\path\we\nowtarget", expected=@"..\..\..\..\path\we\nowtarget", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"..\something\else\..\..\path\we\nowtarget", expected=@"..\..\..\..\path\we\nowtarget", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we\follow", expected=@".", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we", expected=@"..", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path", expected=@"..\..", },
            new { ignoreCase=true, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we\follow\extra", expected=@"extra", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we\FOLLOW\extra", expected=@"..\FOLLOW\extra", },

            new { ignoreCase=false, relativeToParent=true, junctionPath = @"c:\1\2\path\we\follow", targetPath = @"g:\1\2\onthedrive", expected=@"g:\1\2\onthedrive", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"..\..\onthedrive", expected=@"..\..\..\..\onthedrive", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\we\nowtarget", expected=@"nowtarget", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\WE\nowtarget", expected=@"..\WE\nowtarget", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"patH\we\differentiate\nowtarget", expected=@"..\..\patH\we\differentiate\nowtarget", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\that\we\differentiate\nowtarget", expected=@"..\that\we\differentiate\nowtarget", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"..\path\we\nowtarget", expected=@"..\..\..\path\we\nowtarget", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"..\path\we\..\..\path\we\nowtarget", expected=@"..\..\..\path\we\nowtarget", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"..\something\else\..\..\path\we\nowtarget", expected=@"..\..\..\path\we\nowtarget", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\WE\follow", expected=@"..\WE\follow", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\we", expected=@".", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path\WE", expected=@"..\WE", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"path", expected=@"..", },
            new { ignoreCase=false, relativeToParent=true, junctionPath = @"path\we\follow", targetPath = @"PATH", expected=@"..\..\PATH", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"c:\1\2\path\we\follow", targetPath = @"g:\1\2\onthedrive", expected=@"g:\1\2\onthedrive", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"..\..\onthedrive", expected=@"..\..\..\..\..\onthedrive", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we\nowtarget", expected=@"..\nowtarget", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we\differentiate\nowtarget", expected=@"..\differentiate\nowtarget", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\that\we\differentiate\nowtarget", expected=@"..\..\that\we\differentiate\nowtarget", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"..\path\we\nowtarget", expected=@"..\..\..\..\path\we\nowtarget", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"..\path\we\..\..\path\we\nowtarget", expected=@"..\..\..\..\path\we\nowtarget", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"..\something\else\..\..\path\we\nowtarget", expected=@"..\..\..\..\path\we\nowtarget", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we\follow", expected=@".", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we\FOLLOW", expected=@"..\FOLLOW", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path\we", expected=@"..", },
            new { ignoreCase=false, relativeToParent=false, junctionPath = @"path\we\follow", targetPath = @"path", expected=@"..\..", },
        }
            .Select(s => new { junctionPath = s.junctionPath, targetPath = s.targetPath, relativeToParent = s.relativeToParent, ignoreCase = s.ignoreCase, expected = s.expected, result = Junction.GetRelativePath(s.junctionPath, s.targetPath, s.relativeToParent, s.ignoreCase) })
            .Select(s => new { junctionPath = s.junctionPath, targetPath = s.targetPath, relativeToParent = s.relativeToParent, ignoreCase = s.ignoreCase, expected = s.expected, result = s.result, same = s.expected == s.result })
            .Where(s=>
                    !s.same
            ).ToList()
            ;

            Assert(failing.Count == 0, "There are relative tests which are failing");

        }

        private static void SetContentOfFile(string file, string contents)
        {
            using (var writer = File.CreateText(file))
            {
                writer.Write(contents);
            }
        }

        private static void AppendContentToFile(string file, string contents)
        {
            using (var writer = File.AppendText(file))
            {
                writer.Write(contents);
            }
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

        public static void AssertThrows(Action action, string message = null)
        {
            try
            {
                action();

                if( string.IsNullOrWhiteSpace(message))
                {
                    message = "Assert Throw Exception Failed";
                }
                System.Console.Error.WriteLine("Failed: " + message);
                System.Console.Error.WriteLine(Environment.StackTrace);

                Environment.Exit(-1);
            }
            catch //( Exception exc)
            {
            }
        }
    }
}

