﻿using IotCoreAppProjectExtensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IotCoreAppDeployment
{
    public class DeploymentWorker
    {
        #region Define arguments
        private class ArgumentHelper
        {
            public Regex ArgumentMatcher { get; set; }
            public Func<DeploymentWorker, String[], int, int> Handler { get; set; }
            public String HelpString { get; set; }

            public ArgumentHelper(String argName, String helpMsg)
            {
                ArgumentMatcher = new Regex(@"(^-" + argName + ")", RegexOptions.IgnoreCase);
                HelpString = "-" + argName + " " + helpMsg;
            }
            public ArgumentHelper(String[] argNames, String helpMsg)
            {
                StringBuilder args = new StringBuilder();
                for (int i = 0; i<argNames.Length; i++)
                {
                    if (i != 0) args.Append("|");
                    args.Append(argNames[i]);
                }
                ArgumentMatcher = new Regex(@"(^" + args.ToString() + ")", RegexOptions.IgnoreCase);
                HelpString = args.ToString() + " " + helpMsg;
            }
        }

        private List<ArgumentHelper> argumentHelper = new List<ArgumentHelper>() {
            new ArgumentHelper("source", "(source input)") {
                Handler = (worker, args, index) => { worker.source = args[index + 1]; return index+2; },
            },
            new ArgumentHelper("sdk", "(SDK version) {if nothing is provided, 10.0.10586.0 is the default}") {
                Handler = (worker, args, index) => {
                    switch (args[index+1])
                    {
                        case "10.0.10586.0": worker.sdk = SdkVersion.SDK_10_10586_0; break;
                        default:
                            System.Console.WriteLine("Error: suported dependency sdks are: 10.0.586.0");
                            return -1;
                    }
                    return index+2;
                },
            },
            new ArgumentHelper("config", "[Debug|Release] {if nothing is provided, Debug is the default}") {
                Handler = (worker, args, index) => {
                    switch (args[index+1])
                    {
                        case "DEBUG": worker.configuration = DependencyConfiguration.Debug; break;
                        case "RELEASE": worker.configuration = DependencyConfiguration.Release; break;
                        default:
                            System.Console.WriteLine("Error: suported dependency configurations are: Debug | Release");
                            return -1;
                    }
                    return index+2;
                },
            },
            new ArgumentHelper("targetname", "(IoT Core device name or IP address)") {
                Handler = (worker, args, index) => { worker.targetName = args[index+1]; return index+2; },
            },
            new ArgumentHelper("targettype", "[ARM|X86] {if nothing is provided, ARM is the default}") {
                Handler = (worker, args, index) => {
                    switch (args[index+1])
                    {
                        case "arm": worker.targetType = TargetPlatform.ARM; break;
                        case "x86": worker.targetType = TargetPlatform.X86; break;
                        default:
                            System.Console.WriteLine("Error: suported target types are ARM or X86.");
                            return -1;
                    }
                    return index+2;
                },
            },
            new ArgumentHelper("targetuser", "(IoT Core device username) {if nothing is provided, DefaultAccount is the default}") {
                Handler = (worker, args, index) => { worker.credentials.UserName = args[index+1]; return index+2; },
            },
            new ArgumentHelper("targetpassword", "(IoT Core device user password) {if nothing is provided, p@ssw0rd is the default}") {
                Handler = (worker, args, index) => { worker.credentials.Password = args[index+1]; return index+2; },
            },
            new ArgumentHelper("makeappx", "(MakeAppx.exe full path) {if nothing is provided, default Windows SDK installation assumed}") {
                Handler = (worker, args, index) => { worker.makeAppxPath = args[index+1]; return index+2; },
            },
            new ArgumentHelper("signtool", "(SignTool.exe full path) {if nothing is provided, default Windows SDK installation assumed}") {
                Handler = (worker, args, index) => { worker.signToolPath = args[index+1]; return index+2; },
            },
            new ArgumentHelper("output", "(full path to output APPX to) {if nothing is provided, files will not be saved}") {
                Handler = (worker, args, index) => { worker.copyOutputToFolder = args[index+1]; return index+2; },
            },
            new ArgumentHelper(new string[] { "-help", "-?", "-h" }, "Display usage") {
                Handler = (worker, args, index) => { worker.doUsage = true; return -1; },
            },
        };
        #endregion

        private String source = "";
        private String targetName = "";
        private String makeAppxPath = null;
        private String signToolPath = null;
        private String copyOutputToFolder = null;
        private TargetPlatform targetType = TargetPlatform.ARM;
        private SdkVersion sdk = SdkVersion.SDK_10_10586_0;
        private DependencyConfiguration configuration = DependencyConfiguration.Debug;

        private UserInfo credentials = new UserInfo() { UserName = "Administrator", Password = "p@ssw0rd" };
        private const int QueryInterval = 3000;

        bool doUsage = false;

        private enum RETURN_CODE
        {
            SUCCESS = 0,
            ERROR_UNKNOWN,
            ERROR_BAD_ARGUMENT,
        };

        RETURN_CODE ParseCommandLine(String[] argv)
        {

	        if (argv.Length <= 1)
	        {
		        doUsage = true;
		        return RETURN_CODE.ERROR_BAD_ARGUMENT;
	        }

            doUsage = false;
            int current = 0;
	        while (current < argv.Length)
	        {
                foreach (var helper in argumentHelper)
                {
                    if (helper.ArgumentMatcher.IsMatch(argv[current]))
                    {
                        current = helper.Handler(this, argv, current);
                        if (current == -1)
                        {
                            return RETURN_CODE.ERROR_BAD_ARGUMENT;
                        }
                        break;
                    }
                }
	        }
	        return RETURN_CODE.SUCCESS;
        }

        void DoUsage()
        {
            String appName = "IotCoreAppDeployment.exe";
            System.Console.WriteLine("Usage:", appName);
            System.Console.WriteLine("  {0} arguments:", appName);
            foreach (var helper in argumentHelper)
            {
                System.Console.WriteLine("    " + helper.HelpString);
            }
            System.Console.WriteLine("");
            System.Console.WriteLine("Example:");
            System.Console.WriteLine("  {0} s -source app.py -targetname 1.2.3.4 -targettype ARM -sdk 10.0.10586.0", appName);
            System.Console.WriteLine("");
        }

        async Task<bool> CreateAndDeployApp()
        {
            #region Find Template and Project from available providers

            // Ensure that the required Tools (MakeAppx and SignTool) can be found
            String sdkToolCmdFormat = "C:\\Program Files{0}\\Windows Kits\\10\\bin\\{1}\\{2}";
            bool is64 = Environment.Is64BitOperatingSystem;
            String makeAppxCmd = (makeAppxPath == null) ?
                String.Format(sdkToolCmdFormat, is64 ? " (x86)" : "", is64 ? "x64" : "x86", "MakeAppx.exe") :
                makeAppxPath;
            String signToolCmd = (signToolPath == null) ?
                String.Format(sdkToolCmdFormat, is64 ? " (x86)" : "", is64 ? "x64" : "x86", "SignTool.exe") :
                signToolPath;
            if (!File.Exists(makeAppxCmd) || !File.Exists(signToolCmd))
            {
                System.Console.WriteLine("Error: MakeAppx.exe and SignTool.exe must be installed.  These tools are installed as part of the Windows Standalone SDK for Windows 10 (https://go.microsoft.com/fwlink/?LinkID=698771).");
                return false;
            }
            
            // Surround tool cmd paths with quotes in case there are spaces in the paths
            makeAppxCmd = "\"" + makeAppxCmd + "\"";
            signToolCmd = "\"" + signToolCmd + "\"";

            // Find an appropriate path for the input source
            var supportedProjects = new SupportedProjects();
            IProject project = supportedProjects.FindProject(source);
            if (null == project)
            {
                System.Console.WriteLine("Error: source is not supported. {0}", source);
                return false;
            }
            System.Console.WriteLine("... project system found: {0}", project.Name);

            // Configure IProject with user input
            project.SourceInput = source;
            project.ProcessorArchitecture = targetType;
            project.SdkVersion = sdk;
            project.DependencyConfiguration = configuration;

            // Find base project type ... typically, this is C++ for non-standard UWP
            // project types like Python and Node.js
            IBaseProjectTypes baseProjectType = project.GetBaseProjectType();
            if (IBaseProjectTypes.Other == baseProjectType)
            {
                System.Console.WriteLine("Error: base project type is not supported. {0}", baseProjectType.ToString());
                return false;
            }

            // Get ITemplate to retrieve shared APPX content
            ITemplate template = supportedProjects.FindTemplate(baseProjectType);
            if (null == template)
            {
                System.Console.WriteLine("Error: base project type is not supported. {0}", baseProjectType.ToString());
                return false;
            }
            System.Console.WriteLine("... base project system found: {0}", template.Name);

            #endregion

            #region Create APPX
            String outputFolder = Path.GetTempPath() + Path.GetRandomFileName();
            String artifactsFolder = outputFolder + @"\output";
            String appxFilename = project.IdentityName + ".appx";
            String cerFilename = project.IdentityName + ".cer";
            String outputAppx = artifactsFolder + @"\" + appxFilename;
            String outputCer = artifactsFolder + @"\" + cerFilename;

            Directory.CreateDirectory(artifactsFolder);

            // 1. Copy generic base template files
            #region Set up base template contents
            var templateContents = template.GetTemplateContents();
            foreach (var content in templateContents)
            {
                content.Apply(outputFolder);
            }
            System.Console.WriteLine("... base project files found and copied: {0}", outputFolder);
            #endregion

            // 2. Copy IProject-specific (but still generic) files
            #region Add project specific content
            var appxContents = project.GetAppxContents();
            foreach (var content in appxContents)
            {
                content.Apply(outputFolder);
            }
            System.Console.WriteLine("... project files found and copied: {0}", outputFolder);
            #endregion

            // 3. Make changes to the files to tailor them to the specific user input
            #region Make changes to generic project files
            var appxManifestChangess = project.GetAppxContentChanges();
            foreach (var change in appxManifestChangess)
            {
                change.ApplyToContent(outputFolder);
            }
            System.Console.WriteLine("... project files tailored to current deployment.");
            #endregion

            // 4. Add IProject-specific capabilities
            #region Add capabilities
            var capabilityAdditions = project.GetCapabilities();
            foreach (var capability in capabilityAdditions)
            {
                capability.ApplyToContent(outputFolder);
            }
            #endregion

            // 5. Create mapping file used to build APPX
            #region Create APPX map file
            var mapFile = outputFolder + @"\main.map.txt";
            var resourceMetadata = new List<String>();
            var appxFiles = new List<String>();
            template.GetAppxMapContents(resourceMetadata, appxFiles, outputFolder);
            project.GetAppxMapContents(resourceMetadata, appxFiles, outputFolder);
            var mapFileStream = File.Create(mapFile);
            using (var mapFileWriter = new StreamWriter(mapFileStream))
            {
                mapFileWriter.WriteLine("[ResourceMetadata]");
                foreach (var md in resourceMetadata)
                {
                    mapFileWriter.WriteLine(md);
                }
                mapFileWriter.WriteLine("");
                mapFileWriter.WriteLine("[Files]");
                foreach (var appxFile in appxFiles)
                {
                    mapFileWriter.WriteLine(appxFile);
                }
            }
            System.Console.WriteLine("... APPX map file created: {0}", mapFile);
            #endregion

            // 6. Create APPX file
            #region Call MakeAppx.exe
            String makeAppxArgsFormat = "pack /l /h sha256 /m \"{0}\" /f \"{1}\" /o /p \"{2}\"";
            String makeAppxArgs = String.Format(makeAppxArgsFormat, outputFolder + @"\AppxManifest.xml", mapFile, outputAppx);
            Process makeAppxProcess = new Process();
            makeAppxProcess.StartInfo.FileName = makeAppxCmd;
            makeAppxProcess.StartInfo.Arguments = makeAppxArgs;
            makeAppxProcess.StartInfo.RedirectStandardOutput = true;
            makeAppxProcess.StartInfo.RedirectStandardError = true;
            makeAppxProcess.StartInfo.UseShellExecute = false;
            makeAppxProcess.StartInfo.CreateNoWindow = true;
            makeAppxProcess.Start();

            var makeAppxOutput = "";
            while (!makeAppxProcess.HasExited)
            {
                // give the process a kick to make sure it doesn't get hung
                makeAppxOutput += makeAppxProcess.StandardOutput.ReadToEnd();
                Thread.Sleep(100);
            }

            String makeAppxLogfile =outputFolder + @"\makeappx.log";
            using (var makeAppxLogStream = new StreamWriter(makeAppxLogfile))
            {
                String errors = makeAppxProcess.StandardError.ReadToEnd();
                if (errors != null && errors.Length != 0)
                {
                    makeAppxLogStream.WriteLine("Errors:");
                    makeAppxLogStream.Write(errors);
                }
                makeAppxLogStream.WriteLine("\n\n\n\nFull Output:");
                makeAppxLogStream.Write(makeAppxOutput);
            }
            System.Console.WriteLine("... APPX file created");
            System.Console.WriteLine("        {0}", outputAppx);
            System.Console.WriteLine("        logfile: {0}", makeAppxLogfile);

            #endregion

            // 7. Sign APPX file using shared PFX
            #region Call SignTool.exe
            String pfxFile = outputFolder + @"\TemporaryKey.pfx";
            String signToolArgsFormat = "sign /fd sha256 /f \"{0}\" \"{1}\"";
            String signToolArgs = String.Format(signToolArgsFormat, pfxFile, outputAppx);
            Process signToolProcess = new Process();
            signToolProcess.StartInfo.FileName = signToolCmd;
            signToolProcess.StartInfo.Arguments = signToolArgs;
            signToolProcess.StartInfo.RedirectStandardOutput = true;
            signToolProcess.StartInfo.RedirectStandardError = true;
            signToolProcess.StartInfo.UseShellExecute = false;
            signToolProcess.StartInfo.CreateNoWindow = true;
            signToolProcess.Start();

            var signToolOutput = "";
            while (!signToolProcess.HasExited)
            {
                // give the process a kick to make sure it doesn't get hung
                signToolOutput += signToolProcess.StandardOutput.ReadToEnd();
                Thread.Sleep(100);
            }

            String signToolLogfile = outputFolder + @"\signtool.log";
            using (var signToolLogStream = new StreamWriter(signToolLogfile))
            {
                String errors = signToolProcess.StandardError.ReadToEnd();
                if (errors != null && errors.Length != 0)
                {
                    signToolLogStream.WriteLine("Errors:");
                    signToolLogStream.Write(errors);
                }
                signToolLogStream.WriteLine("\n\n\n\nFull Output:");
                signToolLogStream.Write(signToolOutput);
            }
            System.Console.WriteLine("... APPX file signed with PFX", signToolLogfile);
            System.Console.WriteLine("        logfile: {0}", signToolLogfile);

            #endregion

            // 8. Get CER file from shared PFX
            #region Create CER file from PFX
            String getCertArgsFormat = "\"Get-PfxCertificate -FilePath \'{0}\' | Export-Certificate -FilePath \'{1}\' -Type CERT\"";
            String getCertArgs = String.Format(getCertArgsFormat, pfxFile, outputCer);
            Process powershellProcess = new Process();
            powershellProcess.StartInfo.FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
            powershellProcess.StartInfo.Arguments = getCertArgs;
            powershellProcess.StartInfo.RedirectStandardOutput = true;
            powershellProcess.StartInfo.RedirectStandardError = true;
            powershellProcess.StartInfo.UseShellExecute = false;
            powershellProcess.StartInfo.CreateNoWindow = true;
            powershellProcess.Start();

            var powershellOutput = "";
            while (!powershellProcess.HasExited)
            {
                // give the process a kick to make sure it doesn't get hung
                powershellOutput += powershellProcess.StandardOutput.ReadToEnd();
                Thread.Sleep(100);
            }

            String powershellLogfile = outputFolder + @"\powershell.log";
            using (var powershellLogStream = new StreamWriter(powershellLogfile))
            {
                String errors = powershellProcess.StandardError.ReadToEnd();
                if (errors != null && errors.Length != 0)
                {
                    powershellLogStream.WriteLine("Errors:");
                    powershellLogStream.Write(errors);
                }
                powershellLogStream.WriteLine("\n\n\n\nFull Output:");
                powershellLogStream.Write(powershellOutput);
            }
            System.Console.WriteLine("... CER file generated from PFX");
            System.Console.WriteLine("        {0}", outputCer);
            System.Console.WriteLine("        logfile: {0}", signToolLogfile);
            #endregion

            // 9. Copy appropriate Dependencies from IProject
            #region Gather Dependencies

            var dependencies = project.GetDependencies(supportedProjects.DependencyProviders);
            foreach (var dependency in dependencies)
            {
                dependency.Apply(artifactsFolder);
            }
            System.Console.WriteLine("... dependencies copied into place");

            #endregion

            #endregion

            #region Deploy APPX

            #region Create list of all APPX and CER files for deployment
            var files = new List<FileInfo>();
            files.Add(new FileInfo(outputAppx));
            files.Add(new FileInfo(outputCer));
            foreach (var dependency in dependencies)
            {
                files.Add(new FileInfo(artifactsFolder + @"\" + dependency.AppxRelativePath));
            }
            #endregion

            #region Call WEBB Rest APIs to deploy
            var webbHelper = new WebbHelper();
            System.Console.WriteLine("... Starting to deploy certificate, APPX, and dependencies");
            var result = await webbHelper.DeployAppAsync(files, targetName, credentials);
            if (result == HttpStatusCode.Accepted)
            {
                await webbHelper.PollInstallStateAsync(targetName, credentials);
            }
            else
            {
                System.Console.WriteLine("... Deployment failed.");
                return false;
            }
            System.Console.WriteLine("... Deployment finished.");
            #endregion

            #endregion

            #region Copy artifacts if requested

            if (null != copyOutputToFolder)
            {
                if (Directory.Exists(copyOutputToFolder))
                {
                    Directory.Delete(copyOutputToFolder, true);
                }
                Directory.CreateDirectory(copyOutputToFolder);
                File.Copy(outputAppx, copyOutputToFolder + @"\" + appxFilename);
                File.Copy(outputCer, copyOutputToFolder + @"\" + cerFilename);
                foreach (var dependency in dependencies)
                {
                    dependency.Apply(copyOutputToFolder);
                }
            }

            #endregion

            #region Cleanup

            Directory.Delete(outputFolder, true);
            System.Console.WriteLine("... Temp files cleaned up");

            #endregion

            System.Console.WriteLine("\r\n\r\n***");
            System.Console.WriteLine("*** PackageFullName = {0}_1.0.0.0_{1}__1w720vyc4ccym", project.IdentityName, configuration.ToString());
            System.Console.WriteLine("***\r\n\r\n");

            return true;
        }

        public static async Task<bool> Execute(string[] args)
        {
            DeploymentWorker worker = new DeploymentWorker();
            var result = worker.ParseCommandLine(args);
            if (worker.doUsage) { worker.DoUsage(); return false; }

            System.Console.WriteLine("Starting utility to deploy an Iot Core app based on source ...");
            bool ret = await worker.CreateAndDeployApp();
            System.Console.WriteLine("... finished");

            return ret;
        }
    }
}
