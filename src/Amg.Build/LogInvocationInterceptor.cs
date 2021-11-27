using Castle.DynamicProxy;
using System.Collections;
using System.Reflection;
using System.Text;

namespace Amg.Build
{
    internal class LogInvocationInterceptor : StandardInterceptor
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly StringBuilder sb = new StringBuilder();
        private readonly List<string> invocations = new List<string>();

        public bool Proceed = true;

        protected override void PreProceed(Castle.DynamicProxy.IInvocation invocation)
        {
            invocations.Add(invocation.Method.Name);
            sb.Append(String.Format("{0} ", invocation.Method.Name));
            Logger.Information("{name}", invocation.Method.Name);
        }

        protected override void PerformProceed(Castle.DynamicProxy.IInvocation invocation)
        {
            if (Proceed)
            {
                base.PerformProceed(invocation);
            }
            else if (invocation.Method.ReturnType.GetTypeInfo().IsValueType && invocation.Method.ReturnType != typeof(void))
            {
                invocation.ReturnValue = Activator.CreateInstance(invocation.Method.ReturnType); // set default return value
            }
        }

        public String LogContents
        {
            get { return sb.ToString(); }
        }

        public IList Invocations
        {
            get { return invocations; }
        }
    }
}