using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace APIScannerDatabaseUpdater
{
    /**
     * UAPApiParser class is used to collect the api lists and group them in a meaningful way into a sqlite database
     */
    public class UapApiParser
    {

        public enum DllType
        {
            Os = 0,
            Uap = 1,
            Ud = 2
        }
        private const int MaxInsert = 200;
        private const string InsertDll = "INSERT INTO DLL (D_NAME, D_{0}) VALUES";
        private const string RetrieveDll = "SELECT * FROM DLL;";
        private const string InsertFunctionWithOrdinal = "INSERT INTO FUNCTION (F_NAME, F_DLL_NAME, F_ORDINAL) VALUES";

        private const string UpdateDll = "UPDATE DLL SET D_{0}=1 WHERE D_NAME='{1}';";

        private const string InsertResolutions = "INSERT INTO RESOLUTION (A_API,A_DLL,A_ALTERNATEAPI,A_NOTES) VALUES ";

        //JAMESEDIT MAKE CLEANER::
        private class ApiEntry
        {
            public readonly string Apiname = null;
            public readonly string Dll = null;
            public readonly int Ordinal = 0;

            public ApiEntry(string iApiname, string iDll, int iOrdinal = 0)
            {
                Apiname = iApiname;
                Dll = iDll;
                Ordinal = iOrdinal;
            }
        }

        private static List<ApiEntry> _fnList = new List<ApiEntry>();//api,
        private static List<string> _dllList = new List<string>();
        private static List<string> _storedDlls;
        private static List<Resolution> _resolutionList = new List<Resolution>();
        private const string DbPath = @"apis.db";
        private static bool _firstPush = true;

        /* Database management */
        #region DB Management
         
        /**
         * Creates necessary Tables in the DB if needed. Also clears the database to start from scratch
         */
        public static void Init()
        {
            using (var connection = new SQLiteConnection("Data Source=" + DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {
                    // Create the DLL Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS DLL 
                                            (
                                                D_NAME VARCHAR(255) NOT NULL PRIMARY KEY,
                                                D_OS BOOLEAN NOT NULL DEFAULT 0,
                                                D_UAP BOOLEAN NOT NULL DEFAULT 0,
                                                D_UD BOOLEAN NOT NULL DEFAULT 0
                                            );";
                    command.ExecuteNonQuery();

                    // Create the Function Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS FUNCTION 
                                            (
                                                F_ID INTEGER PRIMARY KEY,
                                                F_NAME INTEGER NOT NULL,
                                                F_DLL_NAME INTEGER NOT NULL,
                                                F_ORDINAL INTEGER NOT NULL,
                                                FOREIGN KEY(F_DLL_NAME) REFERENCES DLL(D_NAME)
                                            );";
                    command.ExecuteNonQuery();

                    //JAMES EDIT MISMATCH ERROR!
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS RESOLUTION 
                                            (
                                                A_ID INTEGER PRIMARY KEY,
                                                A_API INTEGER NOT NULL,
                                                A_DLL INTEGER NOT NULL,
                                                A_ALTERNATEAPI INTEGER,
                                                A_NOTES INTEGER
                                            );";
                    command.ExecuteNonQuery();

                    command.CommandText = @"CREATE TABLE IF NOT EXISTS ORDINALMAP
                                            (
                                                O_ID INTEGER PRIMARY KEY,
                                                O_ORDINAL INTEGER NOT NULL,
                                                O_API INTEGER NOT NULL,
                                                O_DLL INTEGER NOT NULL
                                            );";
                    command.ExecuteNonQuery();

                }
                connection.Close();
            }
        }

        /**
         * Pushes all functions and Dlls collected in fnList and dllList respectively to the
         * database
         */
        private static void UpdateTables(SQLiteCommand command, DllType type,bool deleteDlls = true)
        {
            string additionalElementWithOrdinal = "('{0}', '{1}', {2}),";
            string additionalDllElement = "('{0}', 1),";
            _storedDlls = PullDllsFromDb(command);

            string typeString = DllTypeToString(type);

            int iterations = 0;
            while (_dllList.Count > iterations*MaxInsert)
            {
                var finalCommand = string.Format(InsertDll,typeString);
                bool addingAtLeastOneDll = false;
                for (int i = 0; i < MaxInsert && (i + iterations * MaxInsert) < _dllList.Count; i++)
                {
                    string curr = _dllList.ElementAt(i + iterations*MaxInsert);
                    if (!_storedDlls.Contains(curr))
                    {
                        addingAtLeastOneDll = true;
                        finalCommand += string.Format(additionalDllElement, curr);
                        _storedDlls.Add(curr);
                    }
                    else
                    {
                        command.CommandText = string.Format(UpdateDll, typeString, curr);
                        command.ExecuteNonQuery();
                    }
                }
                if (addingAtLeastOneDll)
                {
                    command.CommandText = finalCommand.Remove(finalCommand.Length - 1) + ';';
                    command.ExecuteNonQuery();
                }
                iterations++;
            }
            if(deleteDlls)
            {
                _dllList.Clear();
            }


            while (_fnList.Count > 0)
            {
                command.CommandText = InsertFunctionWithOrdinal;
                for (int i = 0; i < MaxInsert && _fnList.Count > 0; i++)
                {
                    ApiEntry curr = _fnList.ElementAt(0);

                    string func = curr.Apiname;
                    string dll = curr.Dll;
                    int ordinal = curr.Ordinal;

                    func = func.Replace("'", "''");
                    command.CommandText += string.Format(additionalElementWithOrdinal, func, dll, ordinal);
                    _fnList.Remove(curr);
                }
                command.CommandText = command.CommandText.Remove(command.CommandText.Length - 1) + ';';
                command.ExecuteNonQuery();
            }
        }

        /**
         * Pulls all stored Dlls from the database
         */
        private static List<string> PullDllsFromDb(SQLiteCommand command)
        {
            List <string>  dlls = new List<string>();
            if (!_firstPush)
            {
                command.CommandText = RetrieveDll;
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dlls.Add(reader.GetString(0));
                }
                reader.Close();
            }
            else
            {
                _firstPush = false;
            }
            return dlls;
        }

        private static void UpdateResolutionTable(SQLiteCommand command)
        {
            string additionalResolutionElement = "('{0}', '{1}','{2}','{3}'),";
            while (_resolutionList.Count > 0)
            {
                command.CommandText = InsertResolutions;
                for (int i = 0; i < MaxInsert && _resolutionList.Count > 0; i++)
                {
                    Resolution curr = _resolutionList.ElementAt(0);
                    command.CommandText += string.Format(additionalResolutionElement, curr.ApiName,curr.Dll,curr.AlternateApi,curr.Notes);
                    _resolutionList.RemoveAt(0);
                }
                command.CommandText = command.CommandText.Remove(command.CommandText.Length - 1) + ';';
                command.ExecuteNonQuery();
            }
        }

        #endregion
        /* end DB Management*/


        /* Generate the Universal Driver API Database */
        #region Universal Driver
        /**
         * Collects and stores all APIs that are permitted within the Universal Driver specification
         * in the database
         */
        public static void GenerateUdDatabase(string apiPath)
        {
            using (var connection = new SQLiteConnection("Data Source=" + DbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {

                    try
                    {
                        var watch = new Stopwatch();
                        long totalTimeElapsed = 0;
                        watch.Start();
                        //first, retrieve the whitelist, shared by all cores; x86 was chosen arbitrarily
                        ParseXml(apiPath + "\\x86\\ModuleWhitelist.xml",false);

                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for whiteList:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                        watch.Start();
                        //Next, go through the arm non-whitelist apis
                        ParseXml(apiPath + "\\arm\\UniversalDDIs.xml",false);

                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for arm:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                        watch.Start();
                        //Next, go through the arm64 non-whitelist apis
                        ParseXml(apiPath + "\\arm64\\UniversalDDIs.xml");
                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for arm64:: " + watch.ElapsedMilliseconds);
                        watch.Reset();


                        watch.Start();
                        //Next, go through the x86 non-whitelist apis
                        ParseXml(apiPath + "\\x86\\UniversalDDIs.xml");


                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for x86:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                        watch.Start();
                        //Next, go through the x64 non-whitelist apis
                        ParseXml(apiPath + "\\x64\\UniversalDDIs.xml");

                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for x64:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                        

                        //Then, make be sure to add all the functions to the database
                        watch.Start();
                        UpdateTables(command, DllType.Ud);
                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for Update Table:: " + watch.ElapsedMilliseconds);
                        Console.WriteLine("Total Time Elapsed:: " + totalTimeElapsed);
                    }
                    catch (IOException)
                    {
                        Console.Error.WriteLine("Error Reading Input File");
                        Environment.Exit(1);
                    }
                    
                }
                connection.Close();
            }
        }

        /**
         * Stores all APIs that are permitted within the Universal Driver specification
         * in memory
         */
        private static void ParseXml(string pathToUdXml, bool checkForDuplicates = true)
        {
            const int xmlNodesToSkip = 6;
            using (XmlReader reader = XmlReader.Create(new StreamReader(pathToUdXml)))
            {
                var watch = new Stopwatch();

                //Skips the first 6 nodes because they do not give us any meaningful data
                for (int i = 0; i < xmlNodesToSkip && !reader.EOF; i++)
                {
                    reader.Read();
                }

                while (reader.Read())
                {
                    if (!reader.HasAttributes)
                    {
                        continue;
                    }
                    //break the string into seperate parts::
                    var function = reader.GetAttribute("Name");
                    var dll = reader.GetAttribute("ModuleName");

                    watch.Start();
                    if(dll == null)
                    {
                        dll = "UNKNOWN";
                    }
                    //KeyValuePair<string, string> pair = new KeyValuePair<string, string>(function, dll);
                    if (checkForDuplicates)
                    {
                        var apiSearch = from sPair in _fnList
                            where sPair.Dll.Equals(dll) 
                            select sPair.Apiname;

                        var enumerable = apiSearch as string[] ?? apiSearch.ToArray();
                        if (!enumerable.Any())
                        {
                            _dllList.Add(dll);
                            _fnList.Add(new ApiEntry(function, dll));
                        }

                        else if (!enumerable.Contains(function))
                            {
                            _fnList.Add(new ApiEntry(function, dll));
                        }
                        

                    }
                    else
                    {
                        AttemptToAddDll(dll);
                        _fnList.Add(new ApiEntry(function, dll));
                    }
                    watch.Stop();
                }
                reader.Close();
            }
        }

        private static void AttemptToAddDll(string dll)
        {
            var exists = _dllList.Contains(dll);

            if (!exists)
            {
                _dllList.Add(dll);
            }
        }
        #endregion
        /* end Universal Driver  */

        /* generate UAP API Database */
        #region One Core UAP
        /**
         * Collects and stores all APIs that are permitted within the OS level specification
         * in the database
         */
        public static void GenerateUapDatabase(string pathToApiFile)
        {
            using (var connection = new SQLiteConnection("Data Source=" + DbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {

                    try
                    {
                        var watch = new Stopwatch();
                        watch.Start();
                        //first, retrieve the whitelist, shared by all cores; x86 was chosen arbitrarily
                        ParseUapApis(pathToApiFile);

                        watch.Stop();
                        UpdateTables(command,DllType.Os);
                        Console.WriteLine("Time for UAP Api List:: " + watch.ElapsedMilliseconds);
                        watch.Reset();
                        
                    }
                    catch (IOException)
                    {
                        Console.Error.WriteLine("Error Reading Input File");
                        Environment.Exit(1);
                    }

                }
            }
        }

        /**
         * Stores all APIs that are permitted within the OS level specification
         * in memory
         */
        private static void ParseUapApis(string pathToApiFile)
        {
            using (var reader = new StreamReader(pathToApiFile))
            {
                if (!reader.EndOfStream)
                {
                    reader.ReadLine();
                }
                while (!reader.EndOfStream)
                {
                    var currentLine = reader.ReadLine();

                    //break the string into seperate parts::
                    if (currentLine != null)
                    {
                        string[] parts = currentLine.Split(',');
                        var function = parts[1];
                        var dll = parts[2];
                        if (function.StartsWith("_"))
                        {
                            function = function.Substring(1);
                        }
                        if(dll == null)
                        {
                            dll = "UNKNOWN";
                        }
                        ApiEntry apiEntry = new ApiEntry(function, dll);
                        bool alreadyExists = false;
                        var dllSearch = from sPair in _fnList
                                        where sPair.Dll.Equals(dll)
                                        select sPair.Apiname;
                        var enumerable = dllSearch as string[] ?? dllSearch.ToArray();
                        if (enumerable.Any() == false)
                        {
                            _dllList.Add(dll);
                        }
                        else
                        {
                            foreach (string api in enumerable)
                            {
                                if (api.Equals(function))
                                {
                                    alreadyExists = true;
                                }
                            }
                        }
                        if(!alreadyExists)
                            _fnList.Add(apiEntry);
                    }
                }
                reader.Close();
            }
        }
        #endregion
        /* end UAP API Database */


        /* Generate Win32 API Dependency database */
        #region Win32
        /**
         * Collects and stores all win32 APIs that are permitted within the UAP specification
         * in the database
         */
        public static void GenerateWin32Database(string pathToXml)
        {
            using (var connection = new SQLiteConnection("Data Source=" + DbPath))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {

                    try
                    {
                        var watch = new Stopwatch();
                        watch.Start();
                        //first, retrieve the whitelist, shared by all cores; x86 was chosen arbitrarily
                        ParseWin32Apis(pathToXml);

                        UpdateTables(command, DllType.Uap,false);
                        UpdateTables(command, DllType.Os);
                        watch.Stop();
                        Console.WriteLine("Time for Win32 UAP Api List:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                    }
                    catch (IOException e)
                    {
                        Console.Error.WriteLine("Error Reading Input File: " + e.Message);
                        Environment.Exit(1);
                    }

                }
                connection.Close();
            }
        }

        /**
         * Stores all win32 APIs that are permitted within the UAP specification
         * in memory
         */
        private static void ParseWin32Apis(string pathToXml, bool checkForDuplicates = true)
        {
            using (XmlReader reader = XmlReader.Create(new StreamReader(pathToXml)))
            {
                var watch = new Stopwatch();

                for (int i = 0; i < 6 && !reader.EOF; i++)
                {
                    reader.Read();
                }

                while (reader.Read())
                {
                    string dll = null;
                    if (!((reader.NodeType == XmlNodeType.Element) && reader.HasAttributes))
                    {
                        continue;
                    }
                    XmlReader tree = reader.ReadSubtree();
                    tree.Read();
                    var function = tree.GetAttribute("Name");
                    if (reader.Name == "Export")
                    {
                        if (tree.ReadToDescendant("Header"))
                            tree.GetAttribute("Name");
                        if (tree.ReadToNextSibling("Lib"))
                            tree.GetAttribute("Name");
                        if (tree.ReadToNextSibling("Module"))
                            dll = tree.GetAttribute("Name");
                    }
                    else if (reader.Name == "ComInterface")
                    {
                        if(tree.ReadToDescendant("Header"))
                            tree.GetAttribute("Name");
                        if(tree.ReadToNextSibling("Idl"))
                            tree.GetAttribute("Name");
                    }
                    else if (reader.Name == "ComClass")
                    {
                        if (tree.ReadToDescendant("Header"))
                            tree.GetAttribute("Name");
                        if (tree.ReadToNextSibling("Idl"))
                            tree.GetAttribute("Name");
                        if (tree.ReadToNextSibling("GUID"))
                            tree.GetAttribute("Name");
                    }
                    else if(reader.Name == "Inline")
                    {
                        if (tree.ReadToDescendant("Header"))
                            tree.GetAttribute("Name");
                    }


                    watch.Start();
                    if(dll == null)
                    {
                        dll = "UNKNOWN";
                    }
                    ApiEntry pair = new ApiEntry(function, dll);
                    if (checkForDuplicates)
                    {
                        if (_fnList.Contains(pair))
                        {
                            continue;
                        }
                    }
                    watch.Stop();

                    AttemptToAddDll(dll);

                    _fnList.Add(pair);
                }
                reader.Close();
            }
        }

        #endregion
        /* End Win32 API database */

        /* Generate CRT API Dependency Database */
        #region CRT
        /*
         * Verifies that the current command prompt contains DUMPBIN.EXE, which
         * means we are executing within the Visual Studio Developer console
         */
        public static void CheckDeveloperPrompt()
        {
            var haveDeveloperPrompt = false;
            var path = Environment.GetEnvironmentVariable("PATH");
            if (path != null)
            {
                var paths = path.Split(';');

                // Parse the PATH for dumpbin
                if (paths.Any() && paths.Select(s => Path.Combine(s, "dumpbin.exe")).Any(File.Exists))
                {
                    haveDeveloperPrompt = true;
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
        public static string[] GetDumpbinOutput(string target,bool exports = true)
        {
            var dumpbin = new Process();
            dumpbin.StartInfo.FileName = "dumpbin.exe";
            if (exports)
                dumpbin.StartInfo.Arguments = "/EXPORTS \"" + target + '"';
            else
            {
                dumpbin.StartInfo.Arguments = "/IMPORTS \"" + target + '"';
            }
            dumpbin.StartInfo.UseShellExecute = false;
            dumpbin.StartInfo.RedirectStandardOutput = true;
            dumpbin.Start();

            var console = dumpbin.StandardOutput.ReadToEnd();
            var lines = console.Split('\n');

            dumpbin.WaitForExit();
            return lines;
        }

        /**
         * Collects and stores all APIs that are permitted within the Universal Driver specification
         * in the database
         */
        public static void GenerateCrtDatabase(string pathToDllDirectory)
        {
            CheckDeveloperPrompt();
            string[] crtDlls = { "vcruntime140.dll", "ucrtbase.dll","winusb.dll","devobj.dll", "msvcrt.dll", "cfgmgr32.dll" };
            foreach(string dll in crtDlls)
            {
                string[] linesFromDump = GetDumpbinOutput(pathToDllDirectory + "\\" + dll);
                ParseCrtDump(linesFromDump, dll);
            }

            using (var connection = new SQLiteConnection("Data Source=" + DbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {

                    try
                    {
                        var watch = new Stopwatch();
                        watch.Start();
                        UpdateTables(command, DllType.Os,false);
                        UpdateTables(command, DllType.Uap);
                        command.CommandText = string.Format(UpdateDll, DllTypeToString(DllType.Os), "msvcrt.dll");
                        command.ExecuteNonQuery();
                        watch.Stop();
                        Console.WriteLine("Time for CRT Native Api List:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                    }
                    catch (IOException)
                    {
                        Console.Error.WriteLine("Error Reading Input File");
                        Environment.Exit(1);
                    }

                }
                connection.Close();
            }
        }
        /**
         * Collects all Exported CRT Apis available from the os level
         */
        private static void ParseCrtDump(IReadOnlyList<string> lines, string dll)
        {
            _dllList.Add(dll);
            const int lineOffset = 19;//17 is where the ordinal "table" begins, 19 is where the first line STARTS...
            for (var i = lineOffset; i < lines.Count && lines[i].Trim().Length > 0; i++)
            {
                var line = lines[i].Trim();
                line = line.Replace("   ", " ");
                line = line.Replace("  ", " ");
                var parts = line.Split(' ');
                var ordinal = Convert.ToInt32(parts[0]);
                var func = parts[3];
                _fnList.Add(new ApiEntry(func,dll,ordinal));
            }
        }
        #endregion
        /* End  CRT Api Database */

        /* Generate Substituation Database */
        #region Resolutions
        public static void GenerateApiResolutionsDatabase()
        {
            var req = (HttpWebRequest)WebRequest.Create("http://onecoresdk/Substitutions/default.htm");
            var res = (HttpWebResponse)req.GetResponse();
            if (res.StatusCode == HttpStatusCode.OK)
            {
                var responseStream = res.GetResponseStream();
                StreamReader sw = null;

                if(res.CharacterSet == null)
                {
                    if (responseStream != null) sw = new StreamReader(responseStream);
                }
                else
                {
                    if (responseStream != null)
                        sw = new StreamReader(responseStream, Encoding.GetEncoding(res.CharacterSet));
                }
                string htmlOut = "";
                while (sw != null && !sw.EndOfStream)
                {
                    string currentLine = sw.ReadLine();
                    if (currentLine != null && (!currentLine.Contains("<!") && currentLine.Contains('!')))
                    {
                        htmlOut += currentLine;
                    }
                }
                htmlOut = htmlOut.Replace("<tr>", "");
                htmlOut = htmlOut.Replace("<td>", "");
                htmlOut = htmlOut.Replace("</tr>","~");
                string[] lines = htmlOut.Split('~');
                foreach(string line in lines)
                {

                    if (line.Contains("<p>"))
                    {
                        continue;
                    }
                    if (line.Contains("</table>"))
                    {
                        break;
                    }
                    var newLine = line.Replace("<td />", "~");
                    newLine = newLine.Replace("</td>", "~");
                    var lineSplit = newLine.Split('~');
                    var apiSplit = lineSplit[0].Split('!');
                    var sub = new Resolution();
                    sub.Dll = apiSplit[0];
                    sub.ApiName = apiSplit[1];
                    sub.AlternateApi = lineSplit[1];
                    sub.Notes = lineSplit[2];
                    _resolutionList.Add(sub);
                }
                using (var connection = new SQLiteConnection("Data Source=" + DbPath))
                {
                    connection.Open();
                    //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                    using (var command = new SQLiteCommand(connection))
                    {
                        UpdateResolutionTable(command);
                    }
                }
            }
            else
            {
                Console.Out.WriteLine("ERROR: Unable to read API Alternate API list");
            }
        }

        public class Resolution
        {
            public string Dll;
            public string ApiName;
            public string AlternateApi;
            public string Notes;
        }
        #endregion
        /* End Sub database */

        /* Ordinal Map */

        public static void GenerateOrdinalMap()
        {
            string insertOrdinalDllMap = "INSERT INTO ORDINALMAP (O_ORDINAL, O_API, O_DLL) VALUES ";
            string additionalOrdinalDllMap = "({0},'{1}','{2}'),";
            string[] dllList = {"oleaut32.dll", "Ws2_32.dll" };
            string folder = @"C:\Windows\System32";
            using (var connection = new SQLiteConnection("Data Source=" + DbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {
                    foreach (var dll in dllList)
                    {
                        var outputLines = GetDumpbinOutput(folder + "\\" + dll);
                        string commandStr = insertOrdinalDllMap;
                        for (int i = 20; i < outputLines.Length && outputLines[i].Trim().Length > 0; i++)
                        {
                            var line = outputLines[i].Trim();
                            line = line.Replace("   ", " ");
                            line = line.Replace("  ", " ");
                            var parts = line.Split(' ');
                            var ordinal = parts[0];
                            var api = parts[3];
                            commandStr += string.Format(additionalOrdinalDllMap, ordinal, api, dll);
                        }
                        command.CommandText = commandStr.Remove(commandStr.Length - 1) + ';';
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
        }

        /* End Ordinal Map */

                /* Helper functions */
                #region Helpers
            public static
            string DllTypeToString(DllType type)
        {
            string typeString = null;
            switch (type)
            {
                case DllType.Os:
                    typeString = "OS";
                    break;
                case DllType.Uap:
                    typeString = "UAP";
                    break;
                case DllType.Ud:
                    typeString = "UD";
                    break;

            }
            return typeString;
        }
        #endregion
        /* End Helpers */
    }
}