using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Diagnostics;

namespace BinaryAPIScanner
{
    /**
     * UAPApiParser class is used to collect the api lists and group them in a meaningful way into a sqlite database
     */
    public class UAPApiParser
    {

        public enum DllType
        {
            OS = 0,
            UAP = 1,
            UD = 2
        }

        private const string functionSelect = "SELECT * FROM FUNCTION WHERE F_NAME = \'{0}\'";
        private const string functionSelectWithDll = functionSelect  + " AND F_DLL_NAME = '{1}';";
        private const string selectDll = "SELECT * FROM DLL WHERE D_NAME = '{0}'";
        private const string insertDll = "INSERT INTO DLL (D_NAME, D_{0}) VALUES";
        private const string retrieveDll = "SELECT * FROM DLL;";
        private const string insertFunction = "INSERT INTO FUNCTION (F_NAME, F_DLL_NAME, F_ORDINAL) VALUES('{0}', '{1}', 0)";
        private const string insertFunctionWithOrdinal = "INSERT INTO FUNCTION (F_NAME, F_DLL_NAME, F_ORDINAL) VALUES";

        private const string updateDll = "UPDATE DLL SET D_{0}=1 WHERE D_NAME='{1}';";

        private static List<KeyValuePair<string, string>> fnList = new List<KeyValuePair<string, string>>();
        private static List<string> dllList = new List<string>();
        private static List<string> storedDlls;
        private const string dbPath = @"apis.db";
        private static bool firstPush = true;

        /* Database management */
        #region DB Management
         
        /**
         * Creates necessary Tables in the DB if needed. Also clears the database to start from scratch
         */
        public static void Init()
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
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

                }
                connection.Close();
            }
        }

        /**
         * Pushes all functions and Dlls collected in fnList and dllList respectively to the
         * database
         */
        private static void updateTables(SQLiteCommand command, DllType type,bool deleteDlls = true)
        {
            string additionalElementWithOrdinal = "('{0}', '{1}', {2}),";
            string additionalDllElement = "('{0}', 1),";
            const int maxInsert = 200;
            storedDlls = PullDllsFromDB(command);
            string finalCommand = null;

            string typeString = DllTypeToString(type);

            int iterations = 0;
            while (dllList.Count > iterations*maxInsert)
            {
                finalCommand = string.Format(insertDll,typeString);
                bool addingAtLeastOneDll = false;
                for (int i = 0; i < maxInsert && (i + iterations * maxInsert) < dllList.Count; i++)
                {
                    string curr = dllList.ElementAt(i + iterations*maxInsert);
                    if (!storedDlls.Contains(curr))
                    {
                        addingAtLeastOneDll = true;
                        finalCommand += string.Format(additionalDllElement, curr);
                        storedDlls.Add(curr);
                    }
                    else
                    {
                        command.CommandText = string.Format(updateDll, typeString, curr);
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
            if(deleteDlls == true)
            {
                dllList.Clear();
            }


            while (fnList.Count > 0)
            {
                command.CommandText = insertFunctionWithOrdinal;
                for (int i = 0; i < maxInsert && fnList.Count > 0; i++)
                {
                    KeyValuePair<string, string> curr = fnList.ElementAt(0);

                    string func = curr.Key;
                    string dll = curr.Value;
                    string ordinal = "0";
                    if (func.Contains('@'))//meaning there IS an ordinal
                    {
                        string[] parts = func.Split('@');

                        if(parts.Length == 2)
                        {
                            func = parts[0];
                            ordinal = parts[1];
                        }
                        else
                        {
                            func = parts[1];
                            ordinal = parts[2];
                        }
                    }
                    func = func.Replace("'", "''");
                    command.CommandText += string.Format(additionalElementWithOrdinal, func, dll, ordinal);
                    fnList.Remove(curr);
                }
                command.CommandText = command.CommandText.Remove(command.CommandText.Length - 1) + ';';
                command.ExecuteNonQuery();
            }
        }

        /**
         * Pulls all stored Dlls from the database
         */
        private static List<string> PullDllsFromDB(SQLiteCommand command)
        {
            List <string>  dlls = new List<string>();
            if (!firstPush)
            {
                command.CommandText = retrieveDll;
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dlls.Add(reader.GetString(0));
                }
                reader.Close();
            }
            else
            {
                firstPush = false;
            }
            return dlls;
        }


        #endregion
        /* end DB Management*/


        /* Generate the Universal Driver API Database */
        #region Universal Driver
        /**
         * Collects and stores all APIs that are permitted within the Universal Driver specification
         * in the database
         */
        public static void GenerateUDDatabase(string apiPath)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {

                    try
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        long totalTimeElapsed = 0;
                        watch.Start();
                        //first, retrieve the whitelist, shared by all cores; x86 was chosen arbitrarily
                        parseXML(apiPath + "\\x86\\ModuleWhitelist.xml",command,false);

                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for whiteList:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                        watch.Start();
                        //Next, go through the arm non-whitelist apis
                        parseXML(apiPath + "\\arm\\UniversalDDIs.xml", command,false);

                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for arm:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                        watch.Start();
                        //Next, go through the arm64 non-whitelist apis
                        parseXML(apiPath + "\\arm64\\UniversalDDIs.xml", command);
                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for arm64:: " + watch.ElapsedMilliseconds);
                        watch.Reset();


                        watch.Start();
                        //Next, go through the x86 non-whitelist apis
                        parseXML(apiPath + "\\x86\\UniversalDDIs.xml", command);


                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for x86:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                        watch.Start();
                        //Next, go through the x64 non-whitelist apis
                        parseXML(apiPath + "\\x64\\UniversalDDIs.xml", command);

                        watch.Stop();
                        totalTimeElapsed += watch.ElapsedMilliseconds;
                        Console.WriteLine("Time for x64:: " + watch.ElapsedMilliseconds);
                        watch.Reset();

                        

                        //Then, make be sure to add all the functions to the database
                        watch.Start();
                        updateTables(command, DllType.UD);
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
        private static void parseXML(string pathToUDXml, SQLiteCommand command, bool checkForDuplicates = true)
        {
            using (XmlReader reader = XmlReader.Create(new StreamReader(pathToUDXml)))
            {
                var watch = new System.Diagnostics.Stopwatch();

                for (int i = 0; i < 6 && !reader.EOF; i++)
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
                    KeyValuePair<string, string> pair = new KeyValuePair<string, string>(function, dll);
                    if (checkForDuplicates == true)
                    {
                        if (fnList.Contains(pair))
                        {
                            continue;
                        }
                    }
                    watch.Stop();

                    attemptToAddDll(dll);
                    fnList.Add(pair);
                }
                reader.Close();
            }
        }

        private static void attemptToAddDll(string dll)
        {
            if (dll == null)
            {
                dll = "0";
            }
            var exists = dllList.Contains(dll);

            if (!exists)
            {
                dllList.Add(dll);
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
        public static void GenerateUAPDatabase(string pathToAPIFile)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {

                    try
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        //first, retrieve the whitelist, shared by all cores; x86 was chosen arbitrarily
                        parseUAPApis(pathToAPIFile);

                        watch.Stop();
                        updateTables(command,DllType.OS);
                        command.CommandText = "UPDATE DLL SET D_OS=1 WHERE D_NAME='msvcrt.dll';";
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
        private static void parseUAPApis(string pathToAPIFile)
        {
            using (var reader = new StreamReader(pathToAPIFile))
            {
                string currentLine = null;
                if (!reader.EndOfStream)
                {
                    reader.ReadLine();
                }
                while (!reader.EndOfStream)
                {
                    currentLine = reader.ReadLine();

                    //break the string into seperate parts::
                    string[] parts = currentLine.Split(',');
                    var function = parts[1];
                    var dll = parts[2];
                    if (function.StartsWith("_"))
                    {
                        function = function.Substring(1);
                    }
                    KeyValuePair<string, string> pair = new KeyValuePair<string, string>(function, dll);

                    attemptToAddDll(dll);

                    fnList.Add(pair);
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
        public static void GenerateWin32Database(string pathToXML)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {

                    try
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        //first, retrieve the whitelist, shared by all cores; x86 was chosen arbitrarily
                        parseWin32Apis(pathToXML);

                        updateTables(command, DllType.UAP,false);
                        updateTables(command, DllType.OS);
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
        private static void parseWin32Apis(string pathToXML, bool checkForDuplicates = true)
        {
            using (XmlReader reader = XmlReader.Create(new StreamReader(pathToXML)))
            {
                var watch = new System.Diagnostics.Stopwatch();

                for (int i = 0; i < 6 && !reader.EOF; i++)
                {
                    reader.Read();
                }

                while (reader.Read())
                {
                    string function = null;
                    string header = null;
                    string dll = null;
                    string lib = null;
                    string idl = null;
                    string guid = null;
                    if (!((reader.NodeType == XmlNodeType.Element) && reader.HasAttributes))
                    {
                        continue;
                    }
                    else
                    {
                        XmlReader tree = reader.ReadSubtree();
                        tree.Read();
                        function = tree.GetAttribute("Name");
                        if (reader.Name == "Export")
                        {
                            if (tree.ReadToDescendant("Header"))
                                header = tree.GetAttribute("Name");
                            if (tree.ReadToNextSibling("Lib"))
                                lib = tree.GetAttribute("Name");
                            if (tree.ReadToNextSibling("Module"))
                                dll = tree.GetAttribute("Name");
                        }
                        else if (reader.Name == "ComInterface")
                        {
                            if(tree.ReadToDescendant("Header"))
                                header = tree.GetAttribute("Name");
                            if(tree.ReadToNextSibling("Idl"))
                                idl = tree.GetAttribute("Name");
                        }
                        else if (reader.Name == "ComClass")
                        {
                            if (tree.ReadToDescendant("Header"))
                                header = tree.GetAttribute("Name");
                            if (tree.ReadToNextSibling("Idl"))
                                idl = tree.GetAttribute("Name");
                            if (tree.ReadToNextSibling("GUID"))
                                guid = tree.GetAttribute("Name");
                        }
                        else if(reader.Name == "Inline")
                        {
                            if (tree.ReadToDescendant("Header"))
                                header = tree.GetAttribute("Name");
                        }
                    }
                    

                    watch.Start();
                    KeyValuePair<string, string> pair = new KeyValuePair<string, string>(function, dll);
                    if (checkForDuplicates == true)
                    {
                        if (fnList.Contains(pair))
                        {
                            continue;
                        }
                    }
                    watch.Stop();

                    attemptToAddDll(dll);

                    fnList.Add(pair);
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
        private static void CheckDeveloperPrompt()
        {
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
            var dumpbin = new Process();
            dumpbin.StartInfo.FileName = "dumpbin.exe";
            dumpbin.StartInfo.Arguments = "/EXPORTS \"" + target + '"';
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
        public static void GenerateCRTDatabase(string pathToDLLDirectory)
        {
            CheckDeveloperPrompt();
            string[] crtDlls = { "vcruntime140.dll", "ucrtbase.dll"};
            foreach(string dll in crtDlls)
            {
                string[] linesFromDump = GetDumpbinOutput(pathToDLLDirectory + "\\" + dll);
                parseCRTDump(linesFromDump, dll);
            }

            using (var connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                //WhiteList.xml first, this is shared by all, so we'll just use x86 for now
                using (var command = new SQLiteCommand(connection))
                {

                    try
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        updateTables(command, DllType.OS);
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
        private static void parseCRTDump(string[] lines,string dll)
        {
            dllList.Add(dll);
            const int lineOffset = 17 + 2;//17 is where the ordinal "table" begins, 19 is where the first line STARTS...
            const int ordinalOffset = 11;
            const int funcOffset = 26;
            for (int i = lineOffset; i < lines.Length && lines[i].Length > 1; i++)
            {
                string line = lines[i];
                var ordinal = line.Substring(0, ordinalOffset);
                var func = line.Substring(funcOffset);
                fnList.Add(new KeyValuePair<string, string>(func + '@' + ordinal, dll));
            }
        }
        #endregion
        /* End  CRT Api Database */

        /* Generate Substituation Database */
        #region Substitutions

        #endregion
        /* End Sub database */

        /* Helper functions */
        #region Helpers
        public static string DllTypeToString(DllType type)
        {
            string typeString = null;
            switch (type)
            {
                case DllType.OS:
                    typeString = "OS";
                    break;
                case DllType.UAP:
                    typeString = "UAP";
                    break;
                case DllType.UD:
                    typeString = "UD";
                    break;

            }
            return typeString;
        }
        #endregion
        /* End Helpers */
    }
}