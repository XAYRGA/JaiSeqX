using System;
using System.Collections.Generic;
using System.Text;

namespace xayrga.console
{
    public static class ConsoleAppHelper
    {
        public static string[] ArgumentList;
        public static string assertArg(int argn, string assert)
        {
            if (ArgumentList.Length <= argn)
            {
                Console.WriteLine("Missing required argument #{0} for '{1}'", argn, assert);
                Environment.Exit(0);
            }
            return ArgumentList[argn];
        }

        public static string findDynamicStringArgument(string name, string def)
        {
            for (int i = 0; i < ArgumentList.Length; i++)
            {
                if (ArgumentList[i] == name || ArgumentList[i] == "-" + name)
                {
                    if (ArgumentList.Length >= i + 1)
                        return ArgumentList[i + 1];
                    break;
                }
            }
            return def;
        }


        public static int findDynamicNumberArgument(string name, int def)
        {
            for (int i = 0; i < ArgumentList.Length; i++)
            {
                if (ArgumentList[i] == name || ArgumentList[i] == "-" + name)
                {
                    if (ArgumentList.Length < i + 1)
                    {
                        int v = 0;
                        var ok = int.TryParse(ArgumentList[i + 1], out v);
                        if (!ok)
                        {
                            Console.WriteLine($"Invalid parameter for '{ArgumentList[i]}' (Number expected, couldn't parse '{ArgumentList[i + 1]}' as a number.)");
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Number argument for '{ArgumentList[i]}' expected.");
                    }
                    break;
                }
            }
            return def;
        }

        public static bool findDynamicFlagArgument(string name)
        {
            for (int i = 0; i < ArgumentList.Length; i++)
            {
                if (ArgumentList[i] == name || ArgumentList[i] == "-" + name)
                {
                    return true;
                }
            }
            return false;
        }

        public static int assertArgNum(int argn, string assert)
        {
            if (ArgumentList.Length <= argn)
            {
                Console.WriteLine("Missing required argument #{0} for '{1}'", argn, assert);
                Environment.Exit(0);
            }
            int b = 1;
            var w = int.TryParse(ArgumentList[argn], out b);
            if (w == false)
            {
                Console.WriteLine("Cannot parse argument #{0} for '{1}' (expected number, got {2}) ", argn, assert, ArgumentList[argn]);
                Environment.Exit(0);
            }
            return b;
        }

        public static string tryArg(int argn, string assert)
        {
            if (ArgumentList.Length <= argn)
            {
                if (assert != null)
                {
                    Console.WriteLine("No argument #{0} specified {1}.", argn, assert);
                }
                return null;
            }
            return ArgumentList[argn];
        }
        public static void assert(string text, params object[] wtf)
        {
            Console.WriteLine(text, wtf);
            Environment.Exit(0);
        }
        public static void assert(bool cond, string text)
        {
            if (cond == true)
                return;
            Console.WriteLine(text);
            Environment.Exit(0);
        }
    }
}
