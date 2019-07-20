namespace Csa.CommandLine
{
    internal class GetOptContext
    {
        public object Options;
        public bool OptionsStop = false;

        public GetOptContext(object options)
        {
            Options = options;
        }
    }
}
