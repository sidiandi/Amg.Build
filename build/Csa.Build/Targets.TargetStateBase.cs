using System;

namespace Csa.Build
{
    partial class Targets
    {
        class TargetStateBase
        {
            public DateTime? begin;
            public DateTime? end;
            public string id;
            public TimeSpan Duration
            {
                get
                {
                    return begin.HasValue && end.HasValue
                        ? (end.Value - begin.Value)
                        : TimeSpan.Zero;
                }
            }

            public Exception exception;

            public enum States
            {
                Pending,
                InProgress,
                Done,
                Failed
            }

            public States State
            {
                get
                {
                    if (begin.HasValue)
                    {
                        if (end.HasValue)
                        {
                            if (exception == null)
                            {
                                return States.Done;
                            }
                            else
                            {
                                return States.Failed;
                            }
                        }
                        else
                        {
                            return States.InProgress;
                        }
                    }
                    else
                    {
                        return States.Pending;
                    }
                }
            }
        }
    }
}
