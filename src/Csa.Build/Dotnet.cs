namespace Csa.Build
{
    public class Dotnet : Targets
    {
        public Target<Tool> Tool => DefineTarget(() =>
        {
            return new Tool("dotnet");
        });
    }
}
