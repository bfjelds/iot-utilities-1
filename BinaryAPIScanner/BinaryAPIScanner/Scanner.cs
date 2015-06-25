using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using APIScannerDatabaseUpdater;

namespace BinaryAPIScanner
{

    class Scanner
    {
        private static string _winCEdbPath = @"WinCE.db";
        private const string DbPath = @"apis.db";


        private static List<KeyValuePair<string, int>> _winCeOrdinalMap;
        //holds the mapping of APIs to DLLs AND ordinals WITHIN those dlls
        private static List<ApiEntry> _availableFunctionMap;
        private static List<string> _apisUsed;//holds a list of all APIS from the Available function mappings that were used
        private static List<KeyValuePair<string,string>> _apIsFailed;//function to dll mapping for all apis that are not permitted but utilized
        private static List<string> _allowedDlls;//a list of all dlls that are permitted within this specification
        private static List<UapApiParser.Resolution> _resolutionList;
        private static List<ApiEntry> _ordinalMap;//dll -> (apiname -> ordinal)


        private class ApiEntry
        {
            public readonly string apiname = null;
            public readonly string dll = null;
            public readonly int ordinal = 0;

            public ApiEntry(string i_apiname, string i_dll, int i_ordinal = 0)
            {
                apiname = i_apiname;
                dll = i_dll;
                ordinal = i_ordinal;
            }
        }

        public static void Scan(string filePath, UapApiParser.DllType type)
        {
            UapApiParser.CheckDeveloperPrompt();
            PullFromDb(type);
            GetBinaryDependencies(filePath);
            Console.Out.WriteLine("Number of Failed APIS: "+_apIsFailed.Count());
            HtmlFormatter outputHtml = new HtmlFormatter(filePath,type);
            string htmlOutputPath = outputHtml.GenerateOutput(_apIsFailed, _apisUsed, _resolutionList);
            Console.Out.WriteLine("Html Output Generated at filepath:\n" + htmlOutputPath);
        }

        /* Output Files (CSV and html) Generation */
        #region Output Generation


        #endregion End Output Gen
        /* End Output File Gen */

        /**
         * Retrieves Import list from a call to DUMPBIN.exe and then compares those
         * with the list of APIS that are permitted (depending on the specified paramater
         * -os, -uap, or -ud)
         */
        private static void GetBinaryDependencies(string filePath)
        {
            string[] lines = UapApiParser.GetDumpbinOutput(filePath,false);
            _apisUsed = new List<string>();
            _apIsFailed = new List<KeyValuePair<string,string>>();
            string currentLib = "";
            int lineNumber = 0;
            for (; lineNumber<lines.Length; lineNumber++)
            {
                string line = lines[lineNumber];
                if (!line.Equals("") && line.ElementAt(0) != ' ')//if this line DOESN'T start with a space, then it is a useless line.
                {
                    continue;
                }
                else
                {
                    line = line.Trim();
                }

                if (line.Equals(""))
                {
                    currentLib = "";
                    continue;
                }

                if (line.Contains('.'))
                {
                    lineNumber += 5;
                    currentLib = line;
                    if(currentLib.ToLower().Equals("ucrtbased.dll") || (currentLib.ToLower().StartsWith("vcruntime") && currentLib.ToLower().EndsWith("d.dll")))
                    {
                        Console.Out.Write("WARNING:: Some of the following APIS may be included for Debug purposes only; Please verify the binary is a release client");
                    }
                }
                else if(line.StartsWith("Ordinal"))//If the line begins with ordinal, then a remapping is required.
                {
                   
                    int ordinal = Convert.ToInt32(line.Substring(7));
                    if (currentLib.ToLower().Equals("coredll.dll"))//if coredll.dll is the currentLib, then we need to remap with the WinCEOrdinalMap
                    {
                        var selectedApi = from api in _winCeOrdinalMap
                            where api.Value == ordinal
                            select api.Key;
                        foreach (var apiCheck in selectedApi)
                        {
                            string apiFound = null;
                            if (_availableFunctionMap.Any(a => a.apiname.Equals(apiCheck)))
                            {
                                apiFound = (apiCheck);
                            }
                            if (apiFound != null)
                            {
                                _apisUsed.Add(apiFound);
                            }
                            else
                            {
                                _apIsFailed.Add(new KeyValuePair<string, string>(apiCheck, currentLib));
                            }
                        }
                    }
                    else//for dlls that are NOT WinCE's CoreDLL we just need to map with the ordinals stored in the "allowedDlls" list
                    {
                        var lib = currentLib;
                        var selectedApis = from api in _availableFunctionMap
                            where api.dll.ToLower().Equals(lib) && ordinal == api.ordinal
                            select api.apiname;

                        var apis = selectedApis as string[] ?? selectedApis.ToArray();
                        if (apis.Count() > 0)
                        {
                            _apisUsed.Add(apis.First());
                        }
                        else
                        {
                            var possiblyAvailable = from api in _ordinalMap
                                                    where api.dll.ToLower().Equals(lib.ToLower()) && ordinal == api.ordinal
                                                    select api.apiname;
                            var enumerable = possiblyAvailable as string[] ?? possiblyAvailable.ToArray();
                            if (enumerable.Count() > 0)
                            {
                                string toMatch = enumerable.First();
                                var match = from apientry in _availableFunctionMap
                                    where apientry.apiname.ToLower().Equals(toMatch)
                                    select apientry.apiname;
                                if (match.Count() > 0)
                                    _apisUsed.Add(toMatch);
                                else
                                {
                                    _apIsFailed.Add(new KeyValuePair<string, string>(toMatch,
                                            currentLib));
                                }
                            }
                            else
                            {
                                _apIsFailed.Add(new KeyValuePair<string, string>("Ordinal#: " + Convert.ToString(ordinal), currentLib));
                            }
                        }
                    }
                }
                else if(!currentLib.Equals("") && !line.Equals("Summary"))//meaning we have a direct api name mapping..
                {
                    var apiName = line.Split(' ')[1];
                    apiName = apiName.Replace("\r","");
                    var selectedApis = from api in _availableFunctionMap
                        where api.apiname.Equals(apiName)
                        select api.apiname;

                    if (selectedApis.Any())
                    {
                        _apisUsed.Add(apiName);
                    }
                    else
                    {
                        _apIsFailed.Add(new KeyValuePair<string, string>(apiName, currentLib));
                    }
                }
            }
        }

        /**
         * Pulls all permitted apis from the database based on the specified type and pulls ALL of the WinCE dependencies
         * to remap from COREDLL.DLL if the binary was created in WinCE
         */
        private static void PullFromDb(UapApiParser.DllType type)
        {
            const string retrieveDll = "SELECT * FROM DLL WHERE D_{0}=1;";
            const string retrieveWinCeMap = "SELECT * FROM COREDLL;";
            const string retrieveFunc = "SELECT F_NAME,F_DLL_NAME,F_ORDINAL FROM FUNCTION WHERE F_DLL_NAME='{0}'";
            const string additionalDllForRetrieveFunc = " OR F_DLL_NAME='{0}'";
            const string retrieveResolutions = "SELECT A_API,A_DLL,A_ALTERNATEAPI,A_NOTES FROM RESOLUTION;";
            const string retrieveOrdinalMap = "SELECT O_API,O_DLL,O_ORDINAL FROM ORDINALMAP;";


            /* Pull Everything from COREDLL table in WinCE.db */
            _winCeOrdinalMap = new List<KeyValuePair<string, int>>();
            using (var connection = new SQLiteConnection("Data Source=" + _winCEdbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = retrieveWinCeMap;
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        _winCeOrdinalMap.Add(new KeyValuePair<string, int>(reader.GetString(0) , reader.GetInt32(1)));
                    }
                    reader.Close();
                }
                connection.Close();
            }

            

            /* Pull ONLY the necessary entries from DLL/FUNCTION tables */
            using (var connection = new SQLiteConnection("Data Source=" + DbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {
                    _allowedDlls = new List<string>();
                    command.CommandText = string.Format(retrieveDll, UapApiParser.DllTypeToString(type));
                    SQLiteDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        _allowedDlls.Add(reader.GetString(0));
                    }
                    reader.Close();

                    string getFuncCommand = string.Format(retrieveFunc, _allowedDlls.ElementAt(0));
                    for (int i = 0; i < _allowedDlls.Count; i++)
                    {
                        string dll = _allowedDlls.ElementAt(i);
                        getFuncCommand += string.Format(additionalDllForRetrieveFunc, dll);
                    }
                    command.CommandText = getFuncCommand + ';';
                    reader = command.ExecuteReader();
                    _availableFunctionMap = new List<ApiEntry>();
                    while (reader.Read())
                    {
                        string apiName = reader.GetString(0).Replace("\r", "");
                        //Console.Out.WriteLine("apiName: " + apiName);
                        string dll = reader.GetString(1);
                        //Console.Out.WriteLine("dll: " + dll);
                        int ordinal = reader.GetInt32(2);
                        //Console.Out.WriteLine("ordinal: " + ordinal);
                        _availableFunctionMap.Add(new ApiEntry(apiName,dll, ordinal));
                    }
                    reader.Close();

                    /* Pull Everything from the Resolution Database */
                    _resolutionList = new List<UapApiParser.Resolution>();
                    command.CommandText = retrieveResolutions;
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        UapApiParser.Resolution resolution = new UapApiParser.Resolution();
                        resolution.ApiName = reader.GetString(0);
                        resolution.Dll = reader.GetString(1) + ".dll";
                        resolution.AlternateApi = reader.GetString(2);
                        resolution.Notes = reader.GetString(3);
                        _resolutionList.Add(resolution);
                    }
                    reader.Close();

                    /* Pull Everything from the OrdinalMap Database */
                    _ordinalMap = new List<ApiEntry>();
                    command.CommandText = retrieveOrdinalMap;
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        ApiEntry entry =
                            new ApiEntry(reader.GetString(0), reader.GetString(1), reader.GetInt32(2));
                        _ordinalMap.Add(entry);

                    }
                    reader.Close();
                }
                connection.Close();
            }
        }
    }
}
