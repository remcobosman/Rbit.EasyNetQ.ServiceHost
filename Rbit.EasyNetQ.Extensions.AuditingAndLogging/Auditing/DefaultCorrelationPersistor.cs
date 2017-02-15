using System;
using System.Threading;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Auditing
{
    public class DefaultCorrelationPersistor : IPersistCorrelation
    {
        /// <summary>
        /// Key string for keeping the correlation on the thread so we have access to it when logging.
        /// </summary>
        private const string CorrelationIdThreadKey = "___correlationid";

        /// <summary>
        /// Key string for keeping the current run through the handler consumer/receiver so we can log a specific session.
        /// </summary>
        private const string RunIdThreadKey = "___runid";

        private Guid _correlationId;
        private Guid _runId;

        public Guid CorrelationId
        {
            get
            {
                return _correlationId;
            }
            set
            {
                _correlationId = value;
                // We are setting this on the current thread so log4net can read it
                Thread.SetData(Thread.GetNamedDataSlot(CorrelationIdThreadKey), value);
            }
        }

        public Guid RunId
        {
            get
            {
                return _runId;
            }
            set
            {
                _runId = value;
                // We are setting this on the current thread so log4net can read it
                Thread.SetData(Thread.GetNamedDataSlot(RunIdThreadKey), value);
            }
        }

        public string CorrelationIdKeyName => CorrelationIdThreadKey;
        public string RunIdKeyName => RunIdThreadKey;
    }
}
