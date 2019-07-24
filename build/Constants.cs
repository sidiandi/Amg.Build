partial class BuildTargets : Amg.Build.Targets
{
    string name => "Amg.Build";
    string company => "Amg";
    // string nugetPushSource => @"C:\src\local-nuget-repository";
	string nugetPushSource => @"default";
    string nugetPushSymbolSource => nugetPushSource;
}
