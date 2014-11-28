using System;
using FullBuild.Model;

namespace FullBuild.Commands
{
    internal class HostedPackage : Package
    {
        internal HostedPackage(HostedPackage hostedPackage)
            : this(hostedPackage.Name, hostedPackage.Version, hostedPackage.Content, hostedPackage.PackageSize, hostedPackage.PackageHash, hostedPackage.IsLatestVersion, hostedPackage.Published)
        {

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

        internal HostedPackage(Package package, NuGetResult nuGetResult)
            : this(package.Name, nuGetResult.Version, new Uri(nuGetResult.Content), nuGetResult.PackageSize, nuGetResult.PackageHash, nuGetResult.IsLatestVersion, nuGetResult.Published)
        {

        }

        public Uri Content { get; private set; }

        public Int64 PackageSize { get; private set; }
        public string PackageHash { get; private set; }
        public bool IsLatestVersion { get; private set; }
        public DateTime Published { get; private set; }
    }
}