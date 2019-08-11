using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Amg.Build
{
    internal static class TargetEnricherExtension
    {
        public static LoggerConfiguration WithTarget(
               this LoggerEnrichmentConfiguration enrich)
        {
            return enrich.With<TargetEnricher>();
        }
    }

    class TargetEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (InvocationInfo.Current != null)
            { 
                logEvent.AddOrUpdateProperty(GetProperty(propertyFactory));
            }
        }

        private LogEventProperty GetProperty(ILogEventPropertyFactory propertyFactory)
        {
            return propertyFactory.CreateProperty("Target", InvocationInfo.Current.ToString());
        }
    }
}
