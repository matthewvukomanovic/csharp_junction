using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public static partial class JunctionTesting
    {
        public const string baseDirectory = "testing";
        public const string actualTarget1 = baseDirectory + "\\target1";
        public const string actualTarget2 = baseDirectory + "\\target2";

        public const string junctionDirectory = baseDirectory + "\\junc";
        public const string junctionDirectory2 = baseDirectory + "\\junc2";

        public const string junc1FileContent = "file1";
        public const string junc2FileContent = "file2";

        public static void Main()
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
            Assert(!Directory.Exists(junctionDirectory));
            Assert(!Directory.Exists(junctionDirectory2));
            Assert(!Junction.Exists(junctionDirectory));
            Assert(!Junction.Exists(junctionDirectory2));

            Junction.Create(junctionDirectory, actualTarget1);
            Assert(Directory.Exists(junctionDirectory));
            Assert(Junction.Exists(junctionDirectory));

            AssertThrows(()=>Junction.Create(junctionDirectory, actualTarget1));
            Junction.Create(junctionDirectory, actualTarget1, true);
            Assert(Junction.Exists(junctionDirectory));

            AssertThrows(()=>Junction.Create(junctionDirectory, actualTarget2, true, false));
            Junction.Create(junctionDirectory, actualTarget2, true, true);
            Assert(Junction.Exists(junctionDirectory));

            Assert(!Junction.Exists(junctionDirectory2));

            AssertThrows(()=>Junction.Create(junctionDirectory2, actualTarget2, true));
            Assert(!Directory.Exists(junctionDirectory2));
            Assert(!Junction.Exists(junctionDirectory2));

            Junction.Create(junctionDirectory2, actualTarget2, true, true);
            Assert(Directory.Exists(junctionDirectory2));
            Assert(Junction.Exists(junctionDirectory2));

            Junction.ClearJunction(junctionDirectory2);
            Assert(Directory.Exists(junctionDirectory2));
            Assert(!Junction.Exists(junctionDirectory2));
            //System.Console.WriteLine("Exists: " + junctionDirectory2);

             AssertThrows(()=>Junction.ClearJunction(junctionDirectory2));
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

            Assert(Junction.GetTarget(junctionDirectory) == Path.GetFullPath(actualTarget2));

            Junction.Create(junctionDirectory2, actualTarget1, true, true);
            Junction.Create(junctionDirectory, junctionDirectory2, true, true);

            // System.Console.WriteLine($"GetJunctionData for {junctionDirectory} = {Junction.GetJunctionData(junctionDirectory)}");
            // System.Console.WriteLine($"GetTarget for {junctionDirectory} = {Junction.GetTarget(junctionDirectory)}");

            //  System.Console.WriteLine($"GetSymbolicLinkTarget for {Path.GetFullPath(junctionDirectory)} = {Junction.GetSymbolicLinkTarget(Path.GetFullPath(junctionDirectory))}");
            //  System.Console.WriteLine($"{Path.GetFullPath(actualTarget1)}");

            Assert(Junction.GetJunctionData(junctionDirectory) == Path.GetFullPath(junctionDirectory2));
            Assert(Junction.GetTarget(junctionDirectory) == Path.GetFullPath(junctionDirectory2));
            Assert(Junction.GetSymbolicLinkTarget(Path.GetFullPath(junctionDirectory)).Equals(Path.GetFullPath(actualTarget1), StringComparison.InvariantCultureIgnoreCase));

// void ClearJunction(string Junction)
// void Delete(string Junction)
// bool Exists(string path)

            Junction.Delete(junctionDirectory);
            Assert(!Junction.Exists(junctionDirectory));
            Assert(!Directory.Exists(junctionDirectory));
            Junction.ClearJunction(junctionDirectory2);
            Assert(!Junction.Exists(junctionDirectory2));
            Assert(Directory.Exists(junctionDirectory2));


            Junction.Create(junctionDirectory2, @"\??\Volume{00000035-3fe0-82c9-571e-d4016a000000}\", true, true);
            Junction.ClearJunction(junctionDirectory2);
            Directory.Delete(junctionDirectory2, true);
            Directory.Delete(actualTarget1, true);
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

