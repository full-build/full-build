// Copyright (c) 2014, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System.IO;
using System.Xml.Linq;
using FullBuildInterface.Config;
using FullBuildInterface.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace FullBuildInterface.Actions
{
    internal class AnthologyUpdateHandler : Handler<AnthologyUpdateOptions>
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        protected override void ExecuteWithOptions(AnthologyUpdateOptions initAnthologyUpdateOptions)
        {
            var workspace = WellKnownFolders.GetWorkspaceDirectory();
            var config = ConfigManager.GetConfig(workspace);

            // get all csproj in all repos only
            var projectGraph = CreateProjectGraph(config, workspace);

            // sanity check
            var anthology = projectGraph.ToAnthology();

            // first merge with existing one
            anthology = MergeNewAnthologyWithExisting(anthology);

            // Generate import files
            GenerateImports(anthology);
        }

        private void GenerateImports(Anthology anthology)
        {
            var targetDir = WellKnownFolders.GetProjectDirectory();
            targetDir.Create();

            foreach(var project in anthology.Projects)
            {
                var projectFile = Path.Combine(WellKnownFolders.MsBuildSolutionDir, project.ProjectFile);
                var binFile = Path.Combine(WellKnownFolders.MsBuildBinDir, project.AssemblyName + project.Extension);
                var srcCondition = string.Format("'$({0}_Src)' != ''", project.AssemblyName.Replace('.', '_'));
                var binCondition = string.Format("'$({0}_Src)' == ''", project.AssemblyName.Replace('.', '_'));

                var xdoc = new XElement(XmlHelpers.NsMsBuild + "Project",
                                        new XElement(XmlHelpers.NsMsBuild + "Import",
                                                     new XAttribute("Project", Path.Combine(WellKnownFolders.MsBuildViewDir, "$(SolutionName).targets")),
                                                     new XAttribute("Condition", "'$(BinSrcConfig)' == ''")),
                                        new XElement(XmlHelpers.NsMsBuild + "ItemGroup",
                                                     new XElement(XmlHelpers.NsMsBuild + "ProjectReference",
                                                                  new XAttribute("Include", projectFile),
                                                                  new XAttribute("Condition", srcCondition),
                                                                  new XElement(XmlHelpers.NsMsBuild + "Project", project.Guid.ToString("B")),
                                                                  new XElement(XmlHelpers.NsMsBuild + "Name", project.AssemblyName)),
                                                     new XElement(XmlHelpers.NsMsBuild + "Reference",
                                                                  new XAttribute("Include", project.AssemblyName),
                                                                  new XAttribute("Condition", binCondition),
                                                                  new XElement(XmlHelpers.NsMsBuild + "HintPath", binFile))));

                var targetFileName = Path.GetFileNameWithoutExtension(project.ProjectFile) + ".targets";
                var prjImport = targetDir.GetFile(targetFileName);
                xdoc.Save(prjImport.FullName);
            }
        }

        private static ProjectGraph CreateProjectGraph(FullBuildConfig config, DirectoryInfo workspace)
        {
            var projectGraph = new ProjectGraph();
            foreach(var repo in config.SourceRepos)
            {
                var repoDir = workspace.GetDirectory(repo.Name);

                // delete all solution files
                var slns = repoDir.EnumerateFiles("*.sln", SearchOption.AllDirectories);
                slns.ForEach(x => x.Delete());

                // process all projects
                var csprojs = repoDir.EnumerateFiles("*.csproj", SearchOption.AllDirectories);
                csprojs.ForEach(x => projectGraph.Parse(workspace, x));
            }

            return projectGraph;
        }

        private static Anthology MergeNewAnthologyWithExisting(Anthology anthology)
        {
            // merge anthology files
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthologyFile = admDir.GetFile(Anthology.AnthologyFileName);
            if (anthologyFile.Exists)
            {
                var oldJson = File.ReadAllText(anthologyFile.FullName);
                var prevAnthology = JsonConvert.DeserializeObject<Anthology>(oldJson);
                var newJ = JObject.FromObject(anthology);
                var oldJ = JObject.FromObject(prevAnthology);

                var mergeSettings = new JsonMergeSettings {MergeArrayHandling = MergeArrayHandling.Replace};
                oldJ.Merge(newJ, mergeSettings);
                anthology = oldJ.ToObject<Anthology>();
            }

            var json = JsonConvert.SerializeObject(anthology, Formatting.Indented);
            File.WriteAllText(anthologyFile.FullName, json);

            return anthology;
        }
    }
}