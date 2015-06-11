using System;
using APIScannerDatabaseUpdater;

namespace BinaryAPIScanner
{
    /**
     * Scans the API Database Created by the APIScannerDatabaseUpdater to determine which apis from the api lists
     * are supported by the specified binary at each scope; UAP/UWP, OS level, and Universal Drivers
     */
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Be sure to update hardcoded File locations (or just use the db paird with the project)
            if (args.Length < 1 || args.Length > 2)
            {
                InvalidUsage();
            }
            else switch (args.Length)
            {
                case 1:
                    InvalidUsage();
                    break;
                case 2:
                    switch (args[1].ToLower())
                    {
                        case "-os": //scans for a missing binaries in a potentially OS level application
                            Scanner.Scan(args[0], UapApiParser.DllType.Os);
                            break;
                        case "-ud": //scans for a missing binaries in a potentially Universal Driver application
                            Scanner.Scan(args[0], UapApiParser.DllType.Ud);
                            break;
                        case "-uap": //scans for a missing binaries in a potentially Universal Windows application
                            Scanner.Scan(args[0], UapApiParser.DllType.Uap);
                            break;
                            default:
                                InvalidUsage();
                            break;
                    }
                    break;
                    default:
                        InvalidUsage();
                    break;
                }
        }

        /* 
         * notifies the user that they have incorrectly used the application
         */

        private static void InvalidUsage()
        {
            Console.Out.WriteLine("Usage: BinaryAPIScanner.exe <WinCE Binary> [-ud | -os | -uap]");
            Console.Out.WriteLine("To Update the database:");
            Console.Out.WriteLine("Usage: BinaryAPIScanner.exe -update");
            Environment.Exit(1);
        }
    }
}
