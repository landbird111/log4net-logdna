using System;
using System.Diagnostics;

namespace log4net.logdna
{
    internal static class ErrorReporter
    {
        public static void ReportError(string error)
        {
            Trace.WriteLine(error);
            Console.WriteLine(error);
        }
    }
}
