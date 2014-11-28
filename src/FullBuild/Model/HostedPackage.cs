using System;
using System.Linq;
using System.Xml.Linq;
using FullBuild.Helpers;

namespace FullBuild.Model
{
    internal class HostedPackage
    {
        public static HostedPackage CreateFrom(Package package, XContainer entry)
        {
            var content = entry.Descendants(XmlHelpers.Atom + "content").Single().Attribute("src").Value;
            var packageSize = Int64.Parse(entry.Descendants(XmlHelpers.DataServices + "PackageSize").Single().Value);
            var packageHash = entry.Descendants(XmlHelpers.DataServices + "PackageHash").Single().Value;
            var isLatestVersion = Boolean.Parse(entry.Descendants(XmlHelpers.DataServices + "IsAbsoluteLatestVersion").Single().Value);
            var published = DateTime.Parse(entry.Descendants(XmlHelpers.DataServices + "Published").Single().Value);
            var version = entry.Descendants(XmlHelpers.DataServices + "Version").Single().Value;

            return new HostedPackage(package.Name, version, new Uri(content), packageSize, packageHash, isLatestVersion, published);
        }

        private HostedPackage(string name, string version, Uri content, long packageSize, string packageHash, bool isLatestVersion, DateTime published)
        {
            Name = name;
            Version = version;
            Published = published;
            IsLatestVersion = isLatestVersion;
            Content = content;
            PackageSize = packageSize;
            PackageHash = packageHash;
        }
        public string Name { get; private set; }
        public string Version { get; private set; }
        public Uri Content { get; private set; }
        public Int64 PackageSize { get; private set; }
        public string PackageHash { get; private set; }
        public bool IsLatestVersion { get; private set; }
        public DateTime Published { get; private set; }
    }
}