using System;

namespace Amg.Build
{
    public partial class Targets
    {
        internal class TargetStateBase
        {
            public virtual DateTime? Begin { get; set; }
            public virtual DateTime? End { get; set; }
            public string Id { get; set; }
            public virtual TimeSpan Duration
            {
                get
                {
                    return Begin.HasValue && End.HasValue
                        ? (End.Value - Begin.Value)
                        : TimeSpan.Zero;
                }
            }

            public override string ToString() => $"Target {Id}";

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
                    if (Begin.HasValue)
                    {
                        if (End.HasValue)
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

            public bool Failed => exception != null;
        }
    }
}
