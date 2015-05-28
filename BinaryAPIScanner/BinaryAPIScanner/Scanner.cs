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
            GenerateCSV();
            GenerateHtml();
        }

        /* Output Files (CSV and html) Generation */
        #region Output Generation
        private static void GenerateCSV()
        {
            string current = null;
            string output = "ok,failed";
            while (APIsFailed.Count > 0)
            {
                current = APIsFailed.ElementAt(0).Key;
                output += "\n," + current;
                APIsFailed.Remove(APIsFailed.ElementAt(0));
            }
            while(APIsUsed.Count > 0)
            {
                current = APIsUsed.ElementAt(0);
                output += "\n"+ current + "," ;
                APIsUsed.Remove(current);
            }
            File.WriteAllText("analysis.csv", output);
        }

        private static void GenerateHtml()
        {
            // need to do two pass
            // first pass - count number of columns based on commas
            // Note: need to look for "Escaped Strings"
            // can also count number of rows (lines) on first pass
            // second pass - build HTML table and populate.

            int uiRowCount = 0;
            int uiColCount = 0;
            int iTotalEscapedStrings = 0;
            string sLongestLine = string.Empty;

            StreamReader sr = new StreamReader("analysis.csv");
            string sLine = string.Empty;
            while (!sr.EndOfStream)
            {
                sLine = sr.ReadLine();
                uiRowCount++;   // add a new line.
                string[] strArray = sLine.Split(',');
                int iArrayCount = strArray.Count();
                int iEscapedCount = CheckForEscapedStrings(strArray);
                iArrayCount -= iEscapedCount;
                iTotalEscapedStrings += iEscapedCount;

                if (iArrayCount > uiColCount)
                {
                    uiColCount = iArrayCount;
                    sLongestLine = sLine;
                }
            }
            sr.Close();
            //Console.WriteLine(string.Format("Rows: {0}\nCols: {1}", uiRowCount, uiColCount));
            //Console.WriteLine(string.Format("Total Escapes: {0}", iTotalEscapedStrings));
            //Console.WriteLine(string.Format("Most Elements:\n{0}", sLongestLine));

            // go through the file again and build the HTML table.
            sr = new StreamReader("analysis.csv");
            string sOutFile = "analysis.csv" + ".html";
            if (File.Exists(sOutFile))
                File.Delete(sOutFile);

            StreamWriter sw = new StreamWriter(sOutFile);
            sw.Write("<!DOCTYPE html><html><body><table border=\"1\">");
            // tablecontents go here.
            while (!sr.EndOfStream)
            {
                sLine = sr.ReadLine();
                List<string> LineElements = CreateElementList(sLine);
                sw.Write("<tr>");   // table row.

                foreach (string s in LineElements)
                {
                    sw.Write(string.Format("<td>{0}</td>", s));
                }

                sw.Write("</tr>");  // end of table row
            }

            sw.Write("</table></body></html> ");

            sw.Flush();
            sw.Close();
            sr.Close();
        }

        private static int CheckForEscapedStrings(string[] strArray)
        {
            bool bInEscape = false;
            int iEscapeCount = 0;
            int iEscapeElementCount = 0;

            for (int x = 0; x < strArray.Count(); x++)
            {
                string s = strArray[x];

                if (bInEscape)
                    iEscapeElementCount++;


                if (s.EndsWith("\""))
                {
                    iEscapeCount += iEscapeElementCount;
                    bInEscape = false;
                }

                if (s.StartsWith("\""))
                {
                    bInEscape = true;
                    iEscapeElementCount = 0;
                }
            }
            return iEscapeCount;
        }

        private static List<string> CreateElementList(string sLine)
        {
            string[] sArray = sLine.Split(',');
            List<string> LineElements = new List<string>();
            string sItem = string.Empty;

            bool bInEscape = false;

            for (int x = 0; x < sArray.Count(); x++)
            {
                string s = sArray[x];

                if (!bInEscape && !s.StartsWith("\""))
                {
                    LineElements.Add(s);
                }

                if (bInEscape)
                    sItem = sItem + "," + s;

                if (s.EndsWith("\""))
                {
                    bInEscape = false;
                    // assume string starts and ends with \"
                    // remove quotes
                    sItem = sItem.Substring(1, sItem.Length - 2);
                    LineElements.Add(sItem);
                }

                if (s.StartsWith("\""))
                {
                    bInEscape = true;
                    sItem = s;
                }

            }
            return LineElements;
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
                else if (line.ToLower().EndsWith(".dll") || line.ToLower().EndsWith(".exe") || line.ToLower().EndsWith(".dll") || line.ToLower().EndsWith(".sys"))
                {
                    i += 5;
                    currentLib = line;
                }
                else if(line.StartsWith("Ordinal"))//If the line begins with ordinal, then a remapping is required.
                {
                    int ordinal = Convert.ToInt32(line.Substring(7));
                    if (currentLib.ToLower().Equals("coredll.dll"))//if coredll.dll is the currentLib, then we need to remap with the WinCEOrdinalMap
                    {
                        var selectedApi = from api in WinCEOrdinalMap
                                          where api.Value == ordinal
                                          select api.Key;
                        foreach (var api in selectedApi)
                        {
                            var apiFound = from a in AvailableFunctionMap
                                           where a.Key.Equals(api)
                                           select a;
                            if (apiFound.Count() > 0)
                            {
                                APIsUsed.Add(api);
                            }
                            else
                            {
                                Console.Out.WriteLine("API in DLL:: '" + currentLib + "' NOT FOUND:: " + api);
                                APIsFailed.Add(new KeyValuePair<string, string>(api,currentLib));
                            }
                        }
                    }
                    else//for dlls that are NOT WinCE's CoreDLL we just need to map with the ordinals stored in the "allowedDlls" list
                    {
                        var selectedApis = from api in AvailableFunctionMap
                                           where api.Value.Key.ToLower().Equals(currentLib) && ordinal == api.Value.Value
                                           select api.Key;
                        foreach (var api in selectedApis)
                        {
                            if (selectedApis.Count() > 0)
                            {
                                APIsUsed.Add(api);
                            }
                            else
                            {
                                Console.Out.WriteLine("API in DLL:: '" + currentLib + "' NOT FOUND:: " + api);
                                APIsFailed.Add(new KeyValuePair<string, string>(api, currentLib));
                            }
                        }
                    }
                }
                else if(!currentLib.Equals(""))//meaning we have a direct api name mapping..
                {
                    string apiName = line.Split(' ')[1];
                    var selectedApis = from api in AvailableFunctionMap
                                       where api.Value.Key.ToLower().Equals(currentLib.ToLower()) && api.Key.Equals(apiName)
                                       select api.Key;
                    foreach(var api in selectedApis)
                    {
                        if (selectedApis.Count() > 0)
                        {
                            APIsUsed.Add(api);
                        }
                        else
                        {
                            Console.Out.WriteLine("API in DLL:: '" + currentLib + "' NOT FOUND:: " + api);
                            APIsFailed.Add(new KeyValuePair<string, string>(api, currentLib));
                        }
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
                        string apiName = reader.GetString(0);
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
                return File.ReadAllLines("C:\\Users\\t-jdeck\\Development\\MS-IOT\\IoT_Utilities\\BinaryAPIScanner\\BinaryAPIScanner\\APIs\\dump.test");
            }
        }
    }
}
