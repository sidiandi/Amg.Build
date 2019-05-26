partial class BuildTargets : Csa.Build.Targets
{
    string name => "Csa.Build";
    string company => "Acme";
    string nugetPushSource => @"C:\src\local-nuget-repository";
    string nugetPushSymbolSource => nugetPushSource;
}
