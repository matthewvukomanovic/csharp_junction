using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CreateCheatEngineJunction
{
    public static class CreateCheatEngineJunctionClass
    {
        public static bool ToBool(string arg)
        {
            arg = arg.ToLower();
            return arg == "true" || arg == "t" || arg == "1";
        }

        public static bool RunHard(string[] args)
        {
                string location = null;
                string target = null;
                for(int i=1;i<args.Length;i++)
                {
                    switch(i)
                    {
                        case 1:
                            location = args[i];
                            break;
                        case 2:
                            target = args[i];
                            continue;
                    }
                }
                if(string.IsNullOrEmpty(location) || string.IsNullOrEmpty(target))
                {
                    return true;
                }
                try
                {
                    Junction.CreateHardLink(location, target);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                return false;
        }

        public static int Main(string[] args)
        {
            bool showHelp = false;
            if(args.Length > 0)
            {
                bool isHard = false;
                string location = null;
                string target = null;
                bool overrideLocation = false;
                bool allowTargetNotExist = false;
                for(int i=0;i<args.Length && !isHard;i++)
                {
                    switch(i)
                    {
                        case 0:
                            if(args[i] == "/h") { isHard = true; showHelp = RunHard(args);}
                            else
                            {
                                location = args[i];
                            }
                            break;
                        case 1:
                            target = args[i];
                            break;
                        case 2:
                            overrideLocation = ToBool(args[i]);
                            break;
                        case 3:
                            allowTargetNotExist = ToBool(args[i]);
                            break;

                    }
                }
                if(isHard)
                {
                }
                else if(string.IsNullOrEmpty(target))
                {
                    showHelp = true;
                }
                else
                {
                    try
                    {
                        Junction.Create(location, target, overrideLocation, allowTargetNotExist);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                
            }
            else
            {
                showHelp = true;
            }
            if(showHelp)
            {
                var app = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
                app = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                Console.WriteLine($@"Creates a junction while optionally allowing overriding an existing directory and allowing a target which doesnt exist.
Second option allows createing a hard link.

{app} location targetDir [overwriteLocation] [allowTargetNotExist]
{app} /h location targetDir");
            }
            return 0;
        }
    }
}
