using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Interfaces
{
    public enum LogLevels
    {
        Trace = 2,
        Debug = 1,
        Critical = 0
    }

    public interface ILogWriter
    {
        void WriteLogMessage(DateTime timestamp, LogLevels level, string message);
        LogLevels LogLevel { get; }
    }
}
