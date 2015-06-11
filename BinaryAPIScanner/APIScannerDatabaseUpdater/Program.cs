using System;

namespace APIScannerDatabaseUpdater
{
    /**
     * Updates the API Database used by the BinaryAPIScanner tool to include the most up-to-date api lists
     * for each type of scope; UAP/UWP, OS level, and Universal Drivers
     */
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            string udXmlFolder = @"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\WdkBuildUniversalDDIsRoot";
            string uapApiListFilepath = @"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\onecoreuap.lib.txt";
            string win32ApiListFilepath = @"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\ModernApis.xml";
            string system32Folder = @"C:\Windows\System32";
            Console.Out.WriteLine("Are you sure you want to overwrite the database???!?!?!??");
            Console.Read();
            UapApiParser.Init();
            UapApiParser.GenerateApiResolutionsDatabase();
            UapApiParser.GenerateCrtDatabase(system32Folder);
            UapApiParser.GenerateUapDatabase(uapApiListFilepath);
            UapApiParser.GenerateUdDatabase(udXmlFolder);
            UapApiParser.GenerateWin32Database(win32ApiListFilepath);
        }
    }
}
