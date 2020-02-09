using OurFoodChain.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Debug {

    public class LogMessage :
        ILogMessage {

        public LogSeverity Severity { get; set; } = LogSeverity.Info;
        public string Message { get; set; }
        public string Source { get; set; }

        public LogMessage(string source, string message) {

            this.Source = source;
            this.Message = message;

        }
        public LogMessage(LogSeverity severity, string source, string message) {

            this.Severity = severity;
            this.Source = source;
            this.Message = message;

        }

        public override string ToString() {

            StringBuilder sb = new StringBuilder();

            sb.Append(DateTime.Now.ToString("HH:mm:ss"));
            sb.Append(' ');
            sb.Append(Source.PadRight(11).Truncate(11));
            sb.Append(' ');
            sb.Append(Message);

            return sb.ToString();

        }

    }

}