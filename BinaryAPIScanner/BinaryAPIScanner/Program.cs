using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APIScannerDatabaseUpdater;

namespace BinaryAPIScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            //Be sure to update hardcoded File locations (or just use the db paird with the project)
            if (args.Length < 1 || args.Length > 2)
            {
                InvalidUsage();
            }
            else if(args.Length == 1)
            {
                    InvalidUsage();
            }
            else if(args.Length == 2)
            {
                switch (args[1].ToLower())
                {
                    case "-os"://scans for a missing binaries in a potentially OS level application
                        Scanner.Scan(args[0], UAPApiParser.DllType.OS);
                        break;
                    case "-ud"://scans for a missing binaries in a potentially Universal Driver application
                        Scanner.Scan(args[0], UAPApiParser.DllType.UD);
                        break;
                    case "-uap"://scans for a missing binaries in a potentially Universal Windows application
                        Scanner.Scan(args[0], UAPApiParser.DllType.UAP);
                        break;
                }
            }
            


            Console.Out.WriteLine("Done! Press enter to quit...");
            Console.Read();//get rid of this eventually, not necessary
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
