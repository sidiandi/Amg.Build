partial class BuildTargets : Csa.Build.Targets
{
    string name => "Amg.Build";
    string company => "Amg";
    string nugetPushSource => @"C:\src\local-nuget-repository";
    string nugetPushSymbolSource => nugetPushSource;
}
