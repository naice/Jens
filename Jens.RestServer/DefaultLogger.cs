using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Jens.RestServer
{
    public class DefaultLogger : ILogger
    {
        public void Error(string message)
        {
            Trace.TraceError(message);
        }

        public void Info(string message)
        {
            Trace.TraceInformation(message);
        }

        public void Warn(string message)
        {
            Trace.TraceWarning(message);
        }
    }
}
