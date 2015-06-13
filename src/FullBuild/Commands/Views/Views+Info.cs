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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FullBuild.Helpers;
using FullBuild.Model;

namespace FullBuild.Commands.Views
{
    public partial class Views
    {
        public static void Describe(string viewName)
        {
            var viewDir = WellKnownFolders.GetViewDirectory();
            var viewFile = viewDir.GetFile(viewName + ".view");
            if (! viewFile.Exists)
            {
                throw new ArgumentException("Invalid view name");
            }

            var repos = File.ReadAllLines(viewFile.FullName)
                            .Distinct(StringComparer.InvariantCultureIgnoreCase)
                            .Where(x => !string.IsNullOrEmpty(x)).ToList();

            foreach (var repo in repos)
            {
                if (! string.IsNullOrEmpty(repo))
                {
                    Console.WriteLine(repo);
                }
            }
        }

        public static void List()
        {
            var viewDir = WellKnownFolders.GetViewDirectory();
            var views = viewDir.EnumerateFiles("*.view");
            foreach (var view in views)
            {
                var viewName = Path.GetFileNameWithoutExtension(view.Name);
                if (! string.IsNullOrEmpty(viewName))
                {
                    Console.WriteLine(viewName);
                }
            }
        }

        public static void Graph(string viewName)
        {
            var wsdir = WellKnownFolders.GetWorkspaceDirectory();

            // read anthology.json
            var admDir = WellKnownFolders.GetAdminDirectory();
            var anthology = Anthology.Load(admDir);

            // get current view
            var viewDir = WellKnownFolders.GetViewDirectory();
            viewDir.Create();

            var viewFileName = viewDir.GetFile(viewName + ".view");
            if (!viewFileName.Exists)
            {
                throw new ArgumentException("Initialize first solution with a list of repositories to include.");
            }

            var view = File.ReadAllLines(viewFileName.FullName);

            var projectInView = from repo in view
                                from prj in anthology.Projects
                                where prj.ProjectFile.InvariantStartsWith(repo + "/")
                                select prj;

            var dgmlFileName = viewName + ".dgml";
            var dgmlFile = wsdir.GetFile(dgmlFileName);

            var xNodes = new XElement(XmlHelpers.Dgml + "Nodes");
            var xLinks = new XElement(XmlHelpers.Dgml + "Links");

            var binaries = new List<string>();
            var packages = new List<string>();
            var projects = new List<Project>();
            foreach (var prj in projectInView)
            {
                var xnode = new XElement(XmlHelpers.Dgml + "Node",
                                         new XAttribute("Id", prj.Guid),
                                         new XAttribute("Label", prj.AssemblyName),
                                         new XAttribute("Category", "Project"));
                xNodes.Add(xnode);

                foreach (var prjRef in prj.ProjectReferences)
                {
                    var target = anthology.Projects.Single(x => x.Guid == prjRef);
                    if (!projects.Contains(target))
                    {
                        projects.Add(target);

                        var xnodetarget = new XElement(XmlHelpers.Dgml + "Node",
                                                       new XAttribute("Id", target.Guid),
                                                       new XAttribute("Label", target.AssemblyName),
                                                       new XAttribute("Category", "Project"));
                        xNodes.Add(xnodetarget);
                    }

                    var xlink = new XElement(XmlHelpers.Dgml + "Link",
                                             new XAttribute("Source", prj.Guid),
                                             new XAttribute("Target", target.Guid),
                                             new XAttribute("Category", "ProjectReference"));
                    xLinks.Add(xlink);
                }

                foreach (var binRef in prj.BinaryReferences)
                {
                    var target = anthology.Binaries.Single(x => x.AssemblyName.InvariantEquals(binRef));
                    if (null == target.HintPath)
                    {
                        continue;
                    }

                    binaries.Add(binRef);

                    var xlink = new XElement(XmlHelpers.Dgml + "Link",
                                             new XAttribute("Source", prj.Guid),
                                             new XAttribute("Target", target.AssemblyName),
                                             new XAttribute("Category", "BinaryReference"));
                    xLinks.Add(xlink);
                }

                foreach (var pkgRef in prj.PackageReferences)
                {
                    packages.Add(pkgRef);

                    var xlink = new XElement(XmlHelpers.Dgml + "Link",
                                             new XAttribute("Source", prj.Guid),
                                             new XAttribute("Target", pkgRef),
                                             new XAttribute("Category", "PackageReference"));
                    xLinks.Add(xlink);
                }
            }

            foreach (var bin in binaries)
            {
                var xnode = new XElement(XmlHelpers.Dgml + "Node",
                                         new XAttribute("Id", bin),
                                         new XAttribute("Label", bin),
                                         new XAttribute("Category", "Binary"));
                xNodes.Add(xnode);
            }

            foreach (var package in packages)
            {
                var target = anthology.Packages.Single(x => x.Name.InvariantEquals(package));

                var xnode = new XElement(XmlHelpers.Dgml + "Node",
                                         new XAttribute("Id", package),
                                         new XAttribute("Label", target.Name),
                                         new XAttribute("Category", "Package"),
                                         new XAttribute("PackageVersion", target.Version));
                xNodes.Add(xnode);
            }

            var xCategories = new XElement(XmlHelpers.Dgml + "Categories");

            var allCategories = new Dictionary<string, string>
                                {
                                    {"Project", "Green"},
                                    {"Binary", "Red"},
                                    {"Package", "Orange"},
                                    {"ProjectReference", "Green"},
                                    {"BinaryReference", "Red"},
                                    {"PackageReference", "Red"},
                                };
            foreach (var cat in allCategories)
            {
                xCategories.Add(new XElement(XmlHelpers.Dgml + "Category",
                                             new XAttribute("Id", cat.Key),
                                             new XAttribute("Background", cat.Value)));
            }
            xCategories.Add(new XElement(XmlHelpers.Dgml + "Category",
                                         new XAttribute("Id", "PackageVersion"),
                                         new XAttribute("Label", "Version"),
                                         new XAttribute("DataType", "System.String")));

            var xLayout = new XAttribute("Layout", "ForceDirected");

            var xdoc = new XElement(XmlHelpers.Dgml + "DirectedGraph", xLayout, xNodes, xLinks, xCategories);

            xdoc.Save(dgmlFile.FullName);
        }
    }
}
