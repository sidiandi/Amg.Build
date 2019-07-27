using System;
using System.Threading.Tasks;

namespace Amg.Build
{
    internal class TargetDefinitionBase
    {

    }

    internal class TargetDefinition<Input, Output> : TargetDefinitionBase
    {
        private string name;
        private Target<Input, Output> once;

        public TargetDefinition(string name, Func<Input, Task<Output>> work, TargetProgress progress)
        {
            this.name = name;
            this.once = new Target<Input, Output>(FunctionUtils.Once(async (Input input) =>
            {
                try
                {
                    progress.Begin(name, input);
                    try
                    {
                        var output = await work(input);
                        progress.End(name, input, output);
                        return output;
                    }
                    catch (Exception ex)
                    {
                        throw new TargetFailed(name, input, ex);
                    }
                }
                catch (Exception exception)
                {
                    progress.Fail(name, input, exception);
                    throw;
                }
                finally
                {
                }
            }));
        }

        public Target<Input, Output> Run => once;
    }
}