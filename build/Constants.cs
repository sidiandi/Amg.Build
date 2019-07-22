partial class BuildTargets : Amg.Build.Targets
{
    string name => "Amg.Build";
    string company => "Amg";
    // string nugetPushSource => @"C:\src\local-nuget-repository";
	string nugetPushSource => @"https://captain.rtf.siemens.net/artifactory/api/nuget/chp-release-nuget"
    string nugetPushSymbolSource => nugetPushSource;
}
