using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APIScannerDatabaseUpdater;

namespace BinaryAPIScanner
{
    class HtmlFormatter
    {
        private readonly string _binaryName;
        private readonly string _outputFilename;
        public HtmlFormatter(string filepath, UapApiParser.DllType type)
        {
            _binaryName = filepath.Split('\\').Last();
            _outputFilename = _binaryName + "_analysis" + UapApiParser.DllTypeToString(type);
        }

        public string GenerateOutput(List<KeyValuePair<string, string>> apisFailed, List<string> apisUsed, List<Attribute> dotNetAttrFailed, List<Type> dotNetTypesFailed, List<UapApiParser.Resolution> resolutions)
        {
            string sOutFile = "output\\" + _outputFilename  + ".html";
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }

            if (apisFailed.Any())
            {
                Console.Out.WriteLine("Failure: " + apisFailed.Count + " of the " + (apisUsed.Count + apisFailed.Count) + " APIs used are unsupported");
            }
            else
            {
                Console.Out.WriteLine("Congrats! All APIS are supported!\n");
            }

            StreamWriter sw = new StreamWriter(sOutFile);
            sw.Write("<!DOCTYPE html><html><body><style>th{background-color: #00FFFF;}</style>");
            sw.Write("<font color=\"blue\"><h1>Windows 10 IoT Core API Scanner</h1></font>");
            sw.Write("<font color=\"blue\"><h2>Scanning: {0}</h2></font>", _binaryName);
            sw.Write("<h3>The following issues have been found:</h3>");

            int unsupportedApiCount = apisFailed.Count();
            int totalApIsScanned = unsupportedApiCount + apisUsed.Count();
            int unsupportedDllCount = 0;
            // tablecontents go here.
            if (apisFailed.Any() || dotNetTypesFailed.Any() || dotNetAttrFailed.Any())
            {
                List<string> unsupportedDlls = new List<string>();
                sw.Write("<table border=\"1\"><tr><th>API</th><th>Import DLL</th><th>API Resolution</th><th>Resolution Notes</th></tr>");
                while (apisFailed.Any())
                {
                    sw.Write("<tr>");
                    string altApi = "<font color=\"red\">No Resolution Found</font>";
                    string resNotes = "";
                    if (apisFailed.Count > 0)
                    {
                        var resolution = from res in resolutions
                                         where res.ApiName.Equals(apisFailed.ElementAt(0).Key) && res.Dll.Equals(apisFailed.ElementAt(0).Value.ToLower())
                                         select res;
                        var selectedResolution = resolution as UapApiParser.Resolution[] ?? resolution.ToArray();
                        if (selectedResolution.Count() == 1)
                        {
                            var res = selectedResolution.ElementAt(0);
                            altApi = res.AlternateApi;
                            resNotes = "<font color=\"green\">" + res.Notes + " </font>";
                        }
                        sw.Write("<td><font color=\"red\">{0}</font></td><td><font color=\"red\">{1}</font></td><td>{2}</td><td>{3}</td>", apisFailed.ElementAt(0).Key, apisFailed.ElementAt(0).Value, altApi, resNotes);
                        if (!unsupportedDlls.Contains(apisFailed.ElementAt(0).Value))
                        {
                            unsupportedDlls.Add(apisFailed.ElementAt(0).Value);
                            unsupportedDllCount++;
                        }
                        apisFailed.RemoveAt(0);
                    }
                    sw.Write("</tr>");  // end of table row
                }
                while (dotNetTypesFailed.Any())
                {
                    sw.Write("<tr>");
                    sw.Write("<td><font color=\"red\">{0}</font></td><td><font color=\"red\">{1}</font></td><td>{2}</td><td>{3}</td>", dotNetTypesFailed.ElementAt(0).GetType().ToString(), dotNetTypesFailed.ElementAt(0).Assembly.ToString(), "", ".NET Type Unsupported");
                    dotNetTypesFailed.RemoveAt(0);
                    sw.Write("</tr>");
                }
                while (dotNetAttrFailed.Any())
                {
                    sw.Write("<tr>");
                    sw.Write("<td><font color=\"red\">{0}</font></td><td><font color=\"red\">{1}</font></td><td>{2}</td><td>{3}</td>", dotNetAttrFailed.ElementAt(0).TypeId.ToString(), dotNetAttrFailed.ElementAt(0).GetType().ToString(), "", ".NET Attribute Unsupported");
                    dotNetAttrFailed.RemoveAt(0);
                    sw.Write("</tr>");
                }
            }
            else
            {
                sw.Write("<h3><font color=\"green\">All apis used are supported!<font><h3>");
            }
            sw.WriteLine("</table><br>Summary:");
            sw.WriteLine("<br><br>Scanned {0} Import Libraries", totalApIsScanned);
            sw.WriteLine("<br>Found {0} unsupported APIS across {1} import DLLs", unsupportedApiCount, unsupportedDllCount);
            sw.Write("</body></html> ");

            sw.Flush();
            sw.Close();
            return Environment.CurrentDirectory + "\\" +sOutFile;
        }
    }
}
