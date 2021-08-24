using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace PublishPackages
{
    public static class Program
    {
        private const string ConfigFileName = "config.ini";
        private const string LastLogFileName = "lastlog.log";

        private static Dictionary<string, string> _configParameters;

        public static string NuGetCLIPath
        {
            get
            {
                if (_configParameters == null)
                    return string.Empty;

                if (!_configParameters.TryGetValue(nameof(NuGetCLIPath), out string value))
                    return string.Empty;

                return value;
            }
        }
        public static string PackagesDirPath
        {
            get
            {
                if (_configParameters == null)
                    return string.Empty;

                if (!_configParameters.TryGetValue(nameof(PackagesDirPath), out string value))
                    return string.Empty;

                return value;
            }
        }
        public static string ApiKey
        {
            get
            {
                if (_configParameters == null)
                    return string.Empty;

                if (!_configParameters.TryGetValue(nameof(ApiKey), out string value))
                    return string.Empty;

                return value;
            }
        }
        public static string Source
        {
            get
            {
                if (_configParameters == null)
                    return string.Empty;

                if (!_configParameters.TryGetValue(nameof(Source), out string value))
                    return string.Empty;

                return value;
            }
        }
        public static bool SkipDuplicate
        {
            get
            {
                if (_configParameters == null)
                    return true;

                if (!_configParameters.TryGetValue(nameof(SkipDuplicate), out string value))
                    return true;

                if (!bool.TryParse(value, out bool valueBool))
                    return true;

                return valueBool;
            }
        }
        public static bool NonInteractive
        {
            get
            {
                if (_configParameters == null)
                    return true;

                if (!_configParameters.TryGetValue(nameof(NonInteractive), out string value))
                    return true;

                if (!bool.TryParse(value, out bool valueBool))
                    return true;

                return valueBool;
            }
        }
        public static VerbosityType Verbosity
        {
            get
            {
                if (_configParameters == null)
                    return VerbosityType.Normal;

                if (!_configParameters.TryGetValue(nameof(Verbosity), out string value))
                    return VerbosityType.Normal;

                if (!Enum.TryParse<VerbosityType>(value, out VerbosityType valueVerbosityType))
                    return VerbosityType.Normal;

                return valueVerbosityType;
            }
        }

        private static void Main(string[] args)
        {
            StreamWriter writer = new StreamWriter(LastLogFileName, false, new UTF8Encoding(false))
            {
                AutoFlush = true,
            };

            Console.SetOut(writer);
            Console.SetError(writer);

            if (args.Length == 0)
            {
                LoadConfig();
                PublishPackages();
            }
            else if (args.Length == 1)
            {
                LoadConfig();
                PublishPackages(args[0]);
            }
            else if (args.Length >= 2)
            {
                LoadConfig();
                PublishPackages(args[0], args[1]);
            }

            writer.Close();
        }

        public static void LoadConfig()
        {
            if (!File.Exists(ConfigFileName))
            {
                CreateConfig();

                return;
            }

            _configParameters = new Dictionary<string, string>();

            using (StreamReader reader = new StreamReader(ConfigFileName, new UTF8Encoding(false)))
            {
                while (!reader.EndOfStream)
                {
                    string configParameter = reader.ReadLine();

                    if (configParameter == null)
                        continue;

                    configParameter = configParameter.Trim();

                    if (configParameter.Length == 0)
                        continue;

                    if (configParameter.StartsWith(';'))
                        continue;

                    if (configParameter.StartsWith('['))
                        continue;

                    string[] packageReferenceComponents = configParameter.Split('=');

                    if (packageReferenceComponents.Length < 2)
                        continue;

                    string parameterName = packageReferenceComponents[0].Trim();
                    string parameterValue = packageReferenceComponents[1].Trim();

                    if (!_configParameters.ContainsKey(parameterName))
                        _configParameters.Add(parameterName, parameterValue);
                }
            }
        }

        public static void CreateConfig()
        {
            Assembly assembly = typeof(Program).Assembly;
            Stream resourceStream = assembly.GetManifestResourceStream($"PublishPackages.Resources.{ConfigFileName}");

            if (resourceStream == null)
                return;

            using (StreamReader reader = new StreamReader(resourceStream, new UTF8Encoding(false)))
            {
                using (StreamWriter writer = new StreamWriter(ConfigFileName, false, new UTF8Encoding(false)))
                {
                    while (!reader.EndOfStream)
                    {
                        writer.WriteLine(reader.ReadLine());
                    }
                }
            }
        }

        public static void PublishPackages()
        {
            PublishPackages(PackagesDirPath);
        }
        public static void PublishPackages(string packagesPath)
        {
            PublishPackages(packagesPath, NuGetCLIPath);
        }
        public static void PublishPackages(string packagesPath, string nugetPath)
        {
            if (_configParameters == null)
                return;

            if (packagesPath == null)
                return;

            packagesPath = packagesPath.Trim();

            if (packagesPath.Length == 0)
                return;

            if (ApiKey.Length == 0 || Source.Length == 0)
                return;

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = nugetPath,
                WorkingDirectory = $"{Path.GetDirectoryName(nugetPath)}{Path.DirectorySeparatorChar}",
                Arguments = $@"push ""{Path.Combine(packagesPath, "*.nupkg")}"" -ApiKey ""{ApiKey}"" -Source ""{Source}"" {(SkipDuplicate ? "-SkipDuplicate" : string.Empty)} {(NonInteractive ? "-NonInteractive" : string.Empty)} -Verbosity {Verbosity.ToString().ToLowerInvariant()}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process process = new Process
            {
                StartInfo = startInfo
            };

            process.OutputDataReceived += OutputDataReceived;
            process.ErrorDataReceived += ErrorDataReceived;

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
        }

        public static void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        }

        public static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine("Error : " + e.Data);
            }
        }
    }
}
