using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
// ReSharper disable AssignNullToNotNullAttribute

namespace GrafGenerator.ResxLocalize.ResxLocalizeCommand
{
    [Cmdlet(VerbsCommon.New, "LocalizedResx", DefaultParameterSetName = "DefaultParameterSet")]
    public class ResxLocalizeCommand : PSCmdlet
    {
        #region Parameters

        [Parameter(ParameterSetName = "DefaultParameterSet")]
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string ProjectFilePath { get; set; }


        [Parameter(ParameterSetName = "DefaultParameterSet")]
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string TargetConfigPath { get; set; }

        [Parameter(ParameterSetName = "DefaultParameterSet")]
        [Parameter(Position = 2, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string TransformBaseFileName { get; set; }

        [Parameter(ParameterSetName = "DefaultParameterSet")]
        [Parameter(Mandatory = false)]
        public string[] Configurations { get; set; }

        [Parameter(ParameterSetName = "DefaultParameterSet")]
        [Parameter(Mandatory = false)]
        public bool? GenerateConfigFiles { get; set; }

        [Parameter(ParameterSetName = "DefaultParameterSet")]
        [Parameter(Mandatory = false)]
        public bool? GenerateProjectEntries { get; set; }

        [Parameter(ParameterSetName = "DefaultParameterSet")]
        [Parameter(Mandatory = false)]
        public bool? PreserveExistingFiles { get; set; }

        [Parameter(ParameterSetName = "DefaultParameterSet")]
        [Parameter(Mandatory = false)]
        public bool? PreserveExistingEntries { get; set; }

        [Parameter(ParameterSetName = "DefaultParameterSet")]
        [Parameter(Mandatory = false)]
        public string VsVersion { get; set; }

        #endregion

        #region Defaults

        private const string DefaultVsVersion = "11.0";
        private static readonly string[] DefaultConfigurations = {"Debug", "Release"};

        #endregion

        #region Presets

        private const string TransformFileNameTemplate = "{0}.{1}.{2}";

        private const string ConfigTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<section>
  
</section>";

        private const string TransformTemplate = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<section xmlns:xdt=""http://schemas.microsoft.com/XML-Document-Transform"">
  
</section>";

        #endregion

        #region Overrides

        protected override void BeginProcessing()
        {
            if (!File.Exists(ProjectFilePath))
            {
                ThrowTerminatingError(new ErrorRecord(new FileNotFoundException("Input project file not found.", ProjectFilePath),
                    "FileNotFound", ErrorCategory.InvalidArgument, ProjectFilePath));
            }

            if (Configurations == null || Configurations.Length == 0)
            {
                Configurations = DefaultConfigurations;
            }

            Configurations = Configurations.Distinct().ToArray();

            if (!GenerateConfigFiles.HasValue)
            {
                GenerateConfigFiles = false;
            }

            if (!GenerateProjectEntries.HasValue)
            {
                GenerateProjectEntries = false;
            }
            if (!PreserveExistingFiles.HasValue)
            {
                PreserveExistingFiles = true;
            }

            if (!PreserveExistingEntries.HasValue)
            {
                PreserveExistingEntries = true;
            }

            if (string.IsNullOrWhiteSpace(VsVersion))
            {
                VsVersion = DefaultVsVersion;
            }
        }

        protected override void ProcessRecord()
        {
            var projectFileInfo = new FileInfo(ProjectFilePath);
            var targetConfigInfo = new FileInfo(Path.Combine(projectFileInfo.DirectoryName, TargetConfigPath));

            var configsFolder = targetConfigInfo.DirectoryName;
            var configsBaseInfo = new FileInfo(Path.Combine(configsFolder, TransformBaseFileName));

            var configFileName = configsBaseInfo.Name.Replace(configsBaseInfo.Extension, string.Empty);
            var configFileExtension = configsBaseInfo.Extension;

            var targetFileInfos = Configurations.Select(c => new FileInfo(Path.Combine(configsFolder,
                string.Format(TransformFileNameTemplate, configFileName, c, configFileExtension))));

            if (GenerateConfigFiles != null && GenerateConfigFiles.Value)
            {
                WriteConfigFiles(targetConfigInfo, configsBaseInfo, targetFileInfos);
            }
        }

        private void WriteConfigFiles(FileInfo targetConfigInfo, FileInfo configsBaseInfo, IEnumerable<FileInfo> targetFileInfosMap)
        {
            Directory.CreateDirectory(targetConfigInfo.DirectoryName);

            var files = new[]
                {
                    new {IsTransform = false, Info = targetConfigInfo},
                    new {IsTransform = false, Info = configsBaseInfo},
                }
                .Concat(targetFileInfosMap.Select(x => new {IsTransform = true, Info = x}));

            foreach (var file in files)
            {
                if (PreserveExistingFiles != null && PreserveExistingFiles.Value && file.Info.Exists)
                    continue;

                var content = file.IsTransform ? TransformTemplate : ConfigTemplate;

                File.WriteAllText(file.Info.FullName, content);
            }
        }

        #endregion
    }
}