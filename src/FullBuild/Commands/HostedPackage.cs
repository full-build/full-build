using System;
using FullBuild.Model;

namespace FullBuild.Commands
{
    internal class HostedPackage : Package
    {
        public static HostedPackage CreateFrom(Package package, NuGetResult nuGetResult)
        {
            return new HostedPackage(package.Name, nuGetResult.Version, new Uri(nuGetResult.Content), nuGetResult.PackageSize, nuGetResult.PackageHash, nuGetResult.IsLatestVersion, nuGetResult.Published);
        }
        
        private HostedPackage(string name, string version, Uri content, long packageSize, string packageHash, bool isLatestVersion, DateTime published)
            : base(name, version)
        {
            Published = published;
            IsLatestVersion = isLatestVersion;
            Content = content;
            PackageSize = packageSize;
            PackageHash = packageHash;
        }

        public Uri Content { get; private set; }

        public Int64 PackageSize { get; private set; }
        public string PackageHash { get; private set; }
        public bool IsLatestVersion { get; private set; }
        public DateTime Published { get; private set; }
    }
}