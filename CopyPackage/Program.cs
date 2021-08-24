using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CopyPackage
{
    public static class Program
    {
        private static readonly XNamespace MsBuildNamespace = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

        private static XDocument _document;

        private static void Main(string[] args)
        {
            if (args.Length == 3)
            {
                CopyPackage(args[0], args[1], args[2]);
            }
            else if (args.Length >= 4)
            {
                CopyPackage(args[0], args[1], args[2], args[3]);
            }
        }

        public static void DeletePackage(string packagesPath, string projectPackageName)
        {
            Regex packageFileNamePatternRegex = new Regex($@"^{projectPackageName}\.[0-9].*\.nupkg$", RegexOptions.Multiline, TimeSpan.FromSeconds(5));

            string[] packages = Directory.GetFiles(packagesPath, $"{projectPackageName}.*.nupkg", SearchOption.TopDirectoryOnly);

            for (int i = 0; i < packages.Length; ++i)
            {
                ref string package = ref packages[i];

                string packageFileName = Path.GetFileName(package);

                if (packageFileName == null)
                    continue;

                if (!packageFileNamePatternRegex.Match(packageFileName).Success)
                    continue;

                File.Delete(package);
            }
        }

        public static void CopyPackage(string packagesPath, string projectFilePath, string projectConfigurationName)
        {
            _document = XDocument.Load(projectFilePath, LoadOptions.PreserveWhitespace);

            if (_document == null)
                return;

            XElement element = _document?.XPathSelectElement("//Project/PropertyGroup/PackageId");

            if (element == null)
                return;

            string projectPackageName = element.Value;

            if (string.IsNullOrEmpty(projectPackageName))
                return;

            CopyPackageInternal(packagesPath, projectFilePath, projectConfigurationName, projectPackageName, true);
        }
        public static void CopyPackage(string packagesPath, string projectFilePath, string projectConfigurationName,
            string projectPackageName)
        {
            CopyPackageInternal(packagesPath, projectFilePath, projectConfigurationName, projectPackageName);
        }
        private static void CopyPackageInternal(string packagesPath, string projectFilePath, string projectConfigurationName,
            string projectPackageName, bool fileAlreadyLoaded = false)
        {
            if (!fileAlreadyLoaded)
                _document = XDocument.Load(projectFilePath, LoadOptions.PreserveWhitespace);

            if (_document == null)
                return;

            XElement element = _document?.XPathSelectElement("//Project/PropertyGroup/Version");

            if (element == null)
                return;

            string projectPackageVersion = element.Value;

            if (string.IsNullOrEmpty(projectPackageVersion))
                return;

            string projectDirPath = Path.GetDirectoryName(projectFilePath);

            if (projectDirPath == null)
                return;

            string projectPackageDirPath = Path.Combine(projectDirPath, "bin", projectConfigurationName);
            string projectPackageFileName = $"{projectPackageName}.{projectPackageVersion}.nupkg";
            string projectPackagePath = Path.Combine(projectPackageDirPath, projectPackageFileName);

            DeletePackage(packagesPath, projectPackageName);

            File.Copy(projectPackagePath, Path.Combine(packagesPath, projectPackageFileName), true);
        }
    }
}
