using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace BinaryAPIScanner
{

    class Scanner
    {
        public static bool test = false;
        private static string WinCEdbPath = @"WinCE.db";
        private const string dbPath = @"apis.db";


        private static List<KeyValuePair<string, int>> WinCEOrdinalMap = null;
        //holds the mapping of APIs to DLLs AND ordinals WITHIN those dlls
        private static List<KeyValuePair<string, KeyValuePair<string, int>>> AvailableFunctionMap = null;
        private static List<string> APIsUsed = null;//holds a list of all APIS from the Available function mappings that were used
        private static List<KeyValuePair<string,string>> APIsFailed = null;//function to dll mapping for all apis that are not permitted but utilized
        private static List<string> allowedDlls = null;//a list of all dlls that are permitted within this specification

        public static void Scan(string filePath, UAPApiParser.DllType type)
        {
            CheckDeveloperPrompt();
            PullFromDB(type);
            GetBinaryDependencies(filePath);
            foreach(var api in APIsFailed)
            {
                //TODO:: Look for Potential solutions to missing APIs
            }

            if (APIsFailed.Count > 0) {
               Console.Out.WriteLine("EPIC FAILURE, You sad should be :( :: " + APIsFailed.Count + " of the " + (APIsUsed.Count + APIsFailed.Count) + " APIs used are unsupported");
            }
            else
            {
                Console.Out.WriteLine("Congrats! All your APIS are belong to us!");
            }
<<<<<<< HEAD
            string csvFile = "output\\" + filePath.Split('\\').Last() + "_analysis";
            GenerateHtml(csvFile, type);
=======
            string binaryName = filePath.Split('\\').Last();
            string outFile = binaryName + "_analysis";
            GenerateHtml(outFile, type, binaryName);
>>>>>>> - Altered output file generated to be ONLY an html with a better name
        }

        /* Output Files (CSV and html) Generation */
        #region Output Generation

<<<<<<< HEAD
        private static void GenerateHtml(string outputFileName, UAPApiParser.DllType type)
=======
        private static void GenerateHtml(string outputFileName, UAPApiParser.DllType type,string binaryName)
>>>>>>> - Altered output file generated to be ONLY an html with a better name
        {

            int uiRowCount = (APIsUsed.Count > APIsFailed.Count) ? APIsUsed.Count : APIsFailed.Count;
            int uiColCount = 3;

            // go through the file again and build the HTML table.
            string sOutFile = "output\\" + outputFileName + UAPApiParser.DllTypeToString(type) + ".html";
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }
<<<<<<< HEAD
            if (File.Exists(sOutFile))
                File.Delete(sOutFile);
=======
>>>>>>> - Altered output file generated to be ONLY an html with a better name

            StreamWriter sw = new StreamWriter(sOutFile);
            sw.Write("<!DOCTYPE html><html><body><table border=\"1\">");
            sw.Write("<h1>Microsoft IoT Binary API Scanner for IoT Compatability</h1>");
            sw.Write(string.Format("<h2>Scanning: {0}</h2>",binaryName));
            // tablecontents go here.
            if (APIsFailed.Count > 0)
            {
                sw.Write("Failure! {0} of the {1} total APIS are not supported",APIsFailed.Count,APIsUsed.Count + APIsFailed.Count);
                sw.Write("<tr><td>Supported Apis</td><td>Unsupported Apis</td><td>DLL referenced</td></tr>");
                while (APIsFailed.Count > 0 || APIsUsed.Count > 0)
                {
                    sw.Write("<tr>");
                    if (APIsUsed.Count > 0)
                    {
<<<<<<< HEAD
                        sw.Write(string.Format("<td>{0}</td>", APIsUsed.ElementAt(0)));
=======
                        sw.Write(string.Format("<td><font color=\"green\">{0}</font></td>", APIsUsed.ElementAt(0)));
>>>>>>> - Altered output file generated to be ONLY an html with a better name
                        APIsUsed.RemoveAt(0);
                    }
                    else
                    {
                        sw.Write("<td></td>");
                    }
                    if (APIsFailed.Count > 0)
                    {
<<<<<<< HEAD
                        sw.Write(string.Format("<td>{0}</td><td>{1}</td>", APIsFailed.ElementAt(0).Key, APIsFailed.ElementAt(0).Value));
=======
                        sw.Write(string.Format("<td><font color=\"red\">{0}</font></td><td><font color=\"red\">{1}</font></td>", APIsFailed.ElementAt(0).Key, APIsFailed.ElementAt(0).Value));
>>>>>>> - Altered output file generated to be ONLY an html with a better name
                        APIsFailed.RemoveAt(0);
                    }
                    else
                    {
                        sw.Write("<td></td><td></td>");
                    }

                    sw.Write("</tr>");  // end of table row
                }
            }
            else
            {
                sw.Write("All apis used are supported!");
                sw.Write("<tr><td>Supported Apis</td></tr>");
                while(APIsUsed.Count > 0)
                {
                    sw.Write("<tr><td>{0}</td></tr>",APIsUsed.ElementAt(0));
                    APIsUsed.RemoveAt(0);
                }
            }
            

            sw.Write("</table></body></html> ");

            sw.Flush();
            sw.Close();
        }
        #endregion End Output Gen
        /* End Output File Gen */

        /**
         * Retrieves Import list from a call to DUMPBIN.exe and then compares those
         * with the list of APIS that are permitted (depending on the specified paramater
         * -os, -uap, or -ud)
         */
        private static void GetBinaryDependencies(string filePath)
        {
            string[] lines = GetDumpbinOutput(filePath);
            APIsUsed = new List<string>();
            APIsFailed = new List<KeyValuePair<string,string>>();
            string currentLib = "";
            for (int i=0; i<lines.Length; i++)
            {
                string line = lines[i].Trim();
                if(line.Length < 2)
                {
                    currentLib = "";
                    continue;
                }
                else if (line.Length > 20)
                {
                    continue;
                }
                else if (line.Contains('.'))//TODO:: find a better solution of determining whether or not its a dll
                {
                    i += 5;
                    currentLib = line;
                    if(currentLib.ToLower().Equals("ucrtbased.dll") || (currentLib.ToLower().StartsWith("vcruntime") && currentLib.ToLower().EndsWith("d.dll")))
                    {
                        Console.Out.Write("WARNING:: Some of the following APIS may be included for Debug purposes only; Please verify the binary is a release client");
                    }
 /*                 else if (currentLib.ToLower().StartsWith("msv"))
                    {
                        currentLib = "msvcrt.dll";
                    }
                    */
                }
                else if(line.StartsWith("Ordinal"))//If the line begins with ordinal, then a remapping is required.
                {
                   
                    int ordinal = Convert.ToInt32(line.Substring(7));
                    if(ordinal == 1044)
                    {
                        Console.Out.WriteLine();
                    }
                    if (currentLib.ToLower().Equals("coredll.dll"))//if coredll.dll is the currentLib, then we need to remap with the WinCEOrdinalMap
                    {
                        var selectedApi = from api in WinCEOrdinalMap
                                          where api.Value == ordinal
                                          select api.Key;
                        foreach (var apiCheck in selectedApi)
                        {
                            string apiFound = null;
                            foreach(var a in AvailableFunctionMap)
                            {
                                if (a.Key.Equals(apiCheck))
                                {
                                    apiFound = (apiCheck);
                                    break;
                                }
                            }
                            if (apiFound != null)
                            {
                                APIsUsed.Add(apiFound);
                            }
                            else
                            {
                                Console.Out.WriteLine("API in DLL:: '" + currentLib + "' NOT FOUND:: " + apiCheck);
                                APIsFailed.Add(new KeyValuePair<string, string>(apiCheck, currentLib));
                            }
                        }
                    }
                    else//for dlls that are NOT WinCE's CoreDLL we just need to map with the ordinals stored in the "allowedDlls" list
                    {
                        var selectedApis = from api in AvailableFunctionMap
                                           where api.Value.Key.ToLower().Equals(currentLib) && ordinal == api.Value.Value
                                           select api.Key;
                        if (selectedApis.Count() == 1)
                        {
                            foreach (var api in selectedApis)
                            {
                                APIsUsed.Add(api);
                            }
                        }
                        else
                        {
                            Console.Out.WriteLine("API in DLL:: '" + currentLib + "' NOT FOUND; Ordinal::" + ordinal);
                            APIsFailed.Add(new KeyValuePair<string, string>(Convert.ToString(ordinal), currentLib));
                        }
                    }
                }
                else if(!currentLib.Equals(""))//meaning we have a direct api name mapping..
                {
                    string apiName = line.Split(' ')[1];
                    var selectedApis = from api in AvailableFunctionMap
                                       where api.Key.Equals(apiName)
                                       select api.Key;

                    if (selectedApis.Count() > 0)
                    {
                        APIsUsed.Add(apiName);
                    }
                    else
                    {
                        Console.Out.WriteLine("API in DLL:: '" + currentLib + "' NOT FOUND:: " + apiName);
                        APIsFailed.Add(new KeyValuePair<string, string>(apiName, currentLib));
                    }
                }
            }
        }

        /**
         * Pulls all permitted apis from the database based on the specified type and pulls ALL of the WinCE dependencies
         * to remap from COREDLL.DLL if the binary was created in WinCE
         */
        private static void PullFromDB(UAPApiParser.DllType type)
        {
            const string retrieveDll = "SELECT * FROM DLL WHERE D_{0}=1;";
            const string retrieveWinCEMap = "SELECT * FROM COREDLL;";
            const string retrieveFunc = "SELECT F_NAME,F_DLL_NAME,F_ORDINAL FROM FUNCTION WHERE F_DLL_NAME='{0}'";
            const string additionalDllForRetrieveFunc = " OR F_DLL_NAME='{0}'";


            /* Pull Everything from COREDLL table in WinCE.db */
            WinCEOrdinalMap = new List<KeyValuePair<string, int>>();
            using (var connection = new SQLiteConnection("Data Source=" + WinCEdbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = retrieveWinCEMap;
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        WinCEOrdinalMap.Add(new KeyValuePair<string, int>(reader.GetString(0) , reader.GetInt32(1)));
                    }
                    reader.Close();
                }
                connection.Close();
            }
            

            /* Pull ONLY the necessary entries from DLL/FUNCTION tables */
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {
                    allowedDlls = new List<string>();
                    command.CommandText = string.Format(retrieveDll,UAPApiParser.DllTypeToString(type));
                    SQLiteDataReader reader = command.ExecuteReader();
                    
                    while (reader.Read())
                    {
                        allowedDlls.Add(reader.GetString(0));
                    }
                    reader.Close();

                    string getFuncCommand = string.Format(retrieveFunc, allowedDlls.ElementAt(0));
                    for(int i=0; i < allowedDlls.Count; i++)
                    {
                        string dll = allowedDlls.ElementAt(i);
                        getFuncCommand += string.Format(additionalDllForRetrieveFunc, dll);
                    }
                    command.CommandText = getFuncCommand + ';';
                    reader = command.ExecuteReader();
                    AvailableFunctionMap = new List<KeyValuePair<string, KeyValuePair<string,int>>>();
                    while (reader.Read())
                    {
                        string apiName = reader.GetString(0).Replace("\r", "");
                        //Console.Out.WriteLine("apiName: " + apiName);
                        string dll = reader.GetString(1);
                        //Console.Out.WriteLine("dll: " + dll);
                        int ordinal = reader.GetInt32(2);
                        //Console.Out.WriteLine("ordinal: " + ordinal);
                        AvailableFunctionMap.Add(new KeyValuePair<string, KeyValuePair<string, int>>(apiName,new KeyValuePair<string, int>(dll,ordinal)));
                    }
                    reader.Close();
                }
                connection.Close();
            }
        }
        /*
         * Verifies that the current command prompt PATH contains DUMPBIN.EXE, which
         * means we are executing within the Visual Studio Developer console
         */
        private static void CheckDeveloperPrompt()
        {
            if (test == true)
            {
                return;
            }
            var haveDeveloperPrompt = false;
            var path = Environment.GetEnvironmentVariable("PATH");
            var paths = path.Split(';');

            // Parse the PATH for dumpbin
            if (paths.Count() > 0)
            {
                foreach (var s in paths)
                {
                    var pth = Path.Combine(s, "dumpbin.exe");
                    if (File.Exists(pth))
                    {
                        haveDeveloperPrompt = true;
                        break;
                    }
                }
            }

            if (!haveDeveloperPrompt)
            {
                Console.WriteLine("\nPlease launch from a developer command prompt\n");
                Console.WriteLine("I can't find Dumpbin.exe on the current path\n");
                Environment.Exit(1);
            }
        }

        /*
         * Executes a dumpbin /exports on specified target binary
         */
        private static string[] GetDumpbinOutput(string target)
        {
            if (!test)
            {
                var dumpbin = new Process();
                dumpbin.StartInfo.FileName = "dumpbin.exe";
                dumpbin.StartInfo.Arguments = "/IMPORTS \"" + target + '"';
                dumpbin.StartInfo.UseShellExecute = false;
                dumpbin.StartInfo.RedirectStandardOutput = true;
                dumpbin.Start();

                var console = dumpbin.StandardOutput.ReadToEnd();
                var lines = console.Split('\n');

                dumpbin.WaitForExit();
                return lines;
            }
            else
            {
                return File.ReadAllLines(@"C:\Users\t-jdeck\Development\MS-IOT\athens-utilities\BinaryAPIScanner\BinaryAPIScanner\APIs\dump.test");
            }
        }
    }
}
