using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoggingResearch
{
    public class KeyGenerator : IKeyGenerator
    {
        protected long RowId;

        public KeyGenerator()
        {
            RowId = 0L;
        }

        public string GeneratePartitionKey(LogEvent logEvent)
        {
            var utcEventTime = logEvent.Timestamp.UtcDateTime;
            var utcDateString = utcEventTime.ToString("yyyyMMddHH00");
            return utcDateString;
        }

        public string GenerateRowKey(LogEvent logEvent, string suffix = null)
        {
            var utcEventTime = logEvent.Timestamp.UtcDateTime;
            var timeWithoutMilliseconds = utcEventTime.AddMilliseconds(-utcEventTime.Millisecond);
            var rowId = Interlocked.Increment(ref RowId);
            return $"{utcEventTime.ToString("yyyyMMddHHmmss")}|{rowId.ToString("000000000000")}|{logEvent.Level}|{logEvent.MessageTemplate}|{Guid.NewGuid()}";
        }
    }
}
