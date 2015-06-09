using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryAPIScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            //Be sure to update hardcoded File locations (or just use the db paird with the project)
            string UD_XMLFolder = @"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\WdkBuildUniversalDDIsRoot";
            string UAP_ApiList_filepath = @"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\onecoreuap.lib.txt";
            string WIN32_ApiList_filepath = @"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\ModernApis.xml";
            string system32_folder = @"C:\Windows\System32";
            if (args.Length < 1 || args.Length > 2)
            {
                InvalidUsage();
            }
            else if(args.Length == 1)
            {
                //Updates the API lookup database
                if (args[0].Equals("-update"))
                {
                    Console.Out.WriteLine("Are you sure you want to overwrite the database???!?!?!??");
                    Console.Read();
                    UAPApiParser.Init();
                    UAPApiParser.GenerateAPIResolutionsDatabase();
                    UAPApiParser.GenerateCRTDatabase(system32_folder);
                    UAPApiParser.GenerateUAPDatabase(UAP_ApiList_filepath);
                    UAPApiParser.GenerateUDDatabase(UD_XMLFolder);
                    UAPApiParser.GenerateWin32Database(WIN32_ApiList_filepath);
                }
                else
                {
                    InvalidUsage();
                }
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
