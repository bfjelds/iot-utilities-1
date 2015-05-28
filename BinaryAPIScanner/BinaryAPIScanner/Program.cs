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
            //TODO: update to the ACTUAL file locations::
            string UD_XMLFolder = @"C:\Users\t-jdeck\Development\Temp_Kit_content\WdkBuildUniversalDDIsRoot";
            string UAP_ApiList_filepath = @"C:\\Users\\t-jdeck\\Desktop\\Obi-Wan-Lib-Walker\\Debug\\onecoreuap.lib.txt";
            string WIN32_ApiList_filepath = @"C:\\Users\\t-jdeck\\Development\\MS-IOT\\IoT_Utilities\\BinaryAPIScanner\\BinaryAPIScanner\\APIs\\ModernApis.xml";
            string CRT_ApitList_folder = @"C:\\Users\\t-jdeck\\Development\\MS-IOT\\IoT_Utilities\\BinaryAPIScanner\\BinaryAPIScanner\\APIs";
            if (args.Length < 1 || args.Length > 2)
            {
                InvalidUsage();
            }
            else if(args.Length == 1)
            {
                //Updates the API lookup database
                if (args[0].Equals("-update"))
                {
                    UAPApiParser.Init();
                    UAPApiParser.GenerateCRTDatabase(CRT_ApitList_folder);
                    UAPApiParser.GenerateUAPDatabase(UAP_ApiList_filepath);
                    UAPApiParser.GenerateUDDatabase(UD_XMLFolder);
                    UAPApiParser.GenerateWin32Database(WIN32_ApiList_filepath);
                    //UAPApiParser.GenerateCoreDllDatabase() TODO:: 
                    //UAPApiParser.GenerateCoreDllDatabase() TODO:: Add in WinRT Apis to finish up the -UAP functionality
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
