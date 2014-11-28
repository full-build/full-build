using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FullBuild.Helpers;

namespace FullBuild.Commands
{
    class NuGetResult
    {
        public NuGetResult(XContainer entry)
        {
            Content = GetValue(entry, XmlHelpers.Atom + "content").Single().Attribute("src").Value;
            PackageSize = Int64.Parse(GetValue(entry, XmlHelpers.DataServices + "PackageSize").Single().Value);
            PackageHash = GetValue(entry, XmlHelpers.DataServices + "PackageHash").Single().Value;
            IsLatestVersion = Boolean.Parse(GetValue(entry, XmlHelpers.DataServices + "IsAbsoluteLatestVersion").Single().Value);
            Published = DateTime.Parse(GetValue(entry, XmlHelpers.DataServices + "Published").Single().Value);
            Version = GetValue(entry, XmlHelpers.DataServices + "Version").Single().Value;
        }

        public string Content { get; private set; }

        public Int64 PackageSize { get; private set; }

        public string PackageHash { get; private set; }

        public bool IsLatestVersion { get; private set; }

        public DateTime Published { get; private set; }

        public string Version { get; private set; }

        private static IEnumerable<XElement> GetValue(XContainer entry, XName xName)
        {
            return entry.Descendants(xName);
        }
    }
}