using System;
using System.Collections.Generic;
using System.Text;

namespace Amg.Build
{

    /// <summary>
    /// Receives information about the progress of single target invocations (jobs)
    /// </summary>
    public interface TargetProgress
    {
        /// <summary>
        /// When the job begins
        /// </summary>
        /// <param name="id"></param>
        void Begin(JobId id);
        /// <summary>
        /// When the job ends successfully
        /// </summary>
        /// <param name="id"></param>
        /// <param name="output"></param>
        void End(JobId id, object output);
        /// <summary>
        /// When the job fails.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="exception"></param>
        void Fail(JobId id, Exception exception);
    }
}
