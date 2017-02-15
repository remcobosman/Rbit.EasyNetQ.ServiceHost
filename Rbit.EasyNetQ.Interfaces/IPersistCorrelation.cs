using System;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces
{
    public interface IPersistCorrelation
    {
        Guid CorrelationId { get; set; }
        Guid RunId { get; set; }

        string CorrelationIdKeyName { get; }
        string RunIdKeyName { get; }
    }
}