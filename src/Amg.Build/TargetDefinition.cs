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
                var jobId = new JobId(name, input);
                try
                {
                    progress.Begin(jobId);
                    try
                    {
                        var output = await work(input);
                        progress.End(jobId, output);
                        return output;
                    }
                    catch (Exception ex)
                    {
                        throw new TargetFailed(jobId, ex);
                    }
                }
                catch (Exception exception)
                {
                    progress.Fail(jobId, exception);
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