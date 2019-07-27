using System;
using System.Collections.Generic;
using System.Text;

namespace Amg.Build
{
    public interface TargetProgress
    {
        void Begin(string id, object input);
        void End(string id, object input, object output);
        void Fail(string id, object input, Exception exception);

    }
}
