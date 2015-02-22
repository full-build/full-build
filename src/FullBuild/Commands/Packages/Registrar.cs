using System.Collections.Generic;
using FullBuild.NatLangParser;

namespace FullBuild.Commands.Packages
{
    public class Registrar
    {
        public static IEnumerable<Matcher> Commands()
        {
            var url = Parameter<string>.Create("url");
            var package = Parameter<string>.Create("package");
            var version = Parameter<string>.Create("version");

            // add nuget feed
            yield return MatcherBuilder.Describe("add nuget feed")
                                       .Command("add")
                                       .Command("nuget")
                                       .Param(url)
                                       .Do(ctx => Packages.AddNuGet(ctx.Get(url)));

            // list nuget feed
            yield return MatcherBuilder.Describe("list nuget feeds")
                                       .Command("list")
                                       .Command("nugets")
                                       .Do(ctx => Packages.ListNuGets());

            // list packages
            yield return MatcherBuilder.Describe("list packages")
                                       .Command("list")
                                       .Command("packages")
                                       .Do(ctx => Packages.ListPackages());

            // install package
            yield return MatcherBuilder.Describe("install packages")
                                       .Command("install")
                                       .Command("packages")
                                       .Do(ctx => Packages.InstallAll());

            // check packages
            yield return MatcherBuilder.Describe("check for new packages versions")
                                       .Command("check")
                                       .Command("packages")
                                       .Do(ctx => Packages.CheckPackages());

            // upgrade packages
            yield return MatcherBuilder.Describe("check for new packages versions and upgrade packages")
                                       .Command("upgrade")
                                       .Command("packages")
                                       .Do(ctx => Packages.UpgradePackages());

            // check packages
            yield return MatcherBuilder.Describe("use package with version (* for latest)")
                                       .Command("use")
                                       .Command("package")
                                       .Param(package)
                                       .Command("version")
                                       .Param(version)
                                       .Do(ctx => Packages.UsePackage(ctx.Get(package), ctx.Get(version)));
        }
    }
}