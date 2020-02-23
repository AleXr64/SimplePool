using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build: NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;
    [Parameter("NuGet Api Key")] readonly string NugetAPIKey;

    [Solution] readonly Solution Solution;
    [Parameter("NuGet Source for Packages")] readonly string Source = "https://api.nuget.org/v3/index.json";

    [Parameter] string NugetApiKey;

    AbsolutePath OutputDirectory => RootDirectory / "output";

    Target Clean =>
        _ => _
            .Before(Restore)
            .Executes(() =>
             {
                 EnsureCleanDirectory(OutputDirectory);
             });

    Target Restore =>
        _ => _
           .Executes(() =>
            {
                DotNetRestore(_ => _
                                 .SetProjectFile(Solution));
            });

    Target Compile =>
        _ => _
            .DependsOn(Restore)
            .Executes(() =>
             {
                 DotNetBuild(_ => _
                                 .SetProjectFile(Solution)
                                 .SetConfiguration(Configuration)
                                 .SetAssemblyVersion(GitVersion.AssemblySemVer)
                                 .SetFileVersion(GitVersion.AssemblySemFileVer)
                                 .SetInformationalVersion(GitVersion.InformationalVersion)
                                 .EnableNoRestore());
             });

    AbsolutePath PackageDirectory => OutputDirectory / "packages";

    Target Test =>
        _ => _
            .DependsOn(Compile)
            .Executes(() =>
             {
                 DotNetTest(_ => _
                                .SetConfiguration(Configuration)
                                .SetNoBuild(InvokedTargets.Contains(Compile))
                                .ResetVerbosity());
             });

    Target Pack =>
       _ => _
           .DependsOn(Test)
           .Produces(PackageDirectory / "*.nupkg")
           .Executes(() =>
           {
               DotNetPack(s => s
                               .SetProject(Solution)
                               .SetOutputDirectory(PackageDirectory));
           });


    Target Push =>
        _ => _
            .DependsOn(Pack)
            .Executes(() =>
             {
                 var packages = PackageDirectory.GlobFiles("*.nupkg");
                 DotNetNuGetPush(_ => _
                                     .SetSource(Source)
                                     .SetApiKey(NugetAPIKey)
                                     .CombineWith(packages, (_, v) => _
                                                     .SetTargetPath(v)),
                                 5,
                                 true);
             });

    public static int Main() => Execute<Build>(x => x.Push);
}
