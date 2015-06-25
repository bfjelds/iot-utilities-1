using System;
using System.Configuration;

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
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            if (appSettings.Count == 0)
            {
                Console.Out.WriteLine("ERROR: App.config not found or improperly configured!");
                return;
            }

            string udXmlFolder = @appSettings["udXmlFolder"];  //"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\WdkBuildUniversalDDIsRoot";
            string uapApiListFilepath = @appSettings["uapApiList"]; //"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\onecoreuap.lib.txt";
            string win32ApiListFilepath = @appSettings["win32ApiList"];//"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\ModernApis.xml";
            string system32Folder = @appSettings["system32Folder"];//"C:\Windows\System32";
            Console.Out.WriteLine("You are about to update and overwrite the previously created database, are you sure you want to do this?\n" +
                                  "Press Any key to continue..");
            Console.ReadLine();
            UapApiParser.Init();
            UapApiParser.GenerateOrdinalMap();
            UapApiParser.GenerateUdDatabase(udXmlFolder);
            UapApiParser.GenerateApiResolutionsDatabase();
            UapApiParser.GenerateUapDatabase(uapApiListFilepath);
            UapApiParser.GenerateCrtDatabase(system32Folder);
            UapApiParser.GenerateWin32Database(win32ApiListFilepath);
        }
    }
}
