using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIScannerDatabaseUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            string UD_XMLFolder = @"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\WdkBuildUniversalDDIsRoot";
            string UAP_ApiList_filepath = @"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\onecoreuap.lib.txt";
            string WIN32_ApiList_filepath = @"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\ModernApis.xml";
            string system32_folder = @"C:\Windows\System32";
            Console.Out.WriteLine("Are you sure you want to overwrite the database???!?!?!??");
            Console.Read();
            UAPApiParser.Init();
            UAPApiParser.GenerateAPIResolutionsDatabase();
            UAPApiParser.GenerateCRTDatabase(system32_folder);
            UAPApiParser.GenerateUAPDatabase(UAP_ApiList_filepath);
            UAPApiParser.GenerateUDDatabase(UD_XMLFolder);
            UAPApiParser.GenerateWin32Database(WIN32_ApiList_filepath);
        }
    }
}
