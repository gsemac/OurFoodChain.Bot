using System;

namespace OurFoodChain.Debug {

    public enum LogSeverity {
        Info,
        Warning,
        Error
    }

    public interface ILogMessage {

        LogSeverity Severity { get; set; }
        string Message { get; set; }
        string Source { get; set; }

    }

}
