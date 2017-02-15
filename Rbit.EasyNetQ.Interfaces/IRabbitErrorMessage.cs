using System;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces
{
    public interface IRabbitErrorMessage
    {
        int Id { get; set; }
        DateTime DateTime { get; set; }
        string Server { get; set; }
        string Exchange { get; set; }
        string Properties { get; set; }
        string Queue { get; set; }
        string Topic { get; set; }
        string Payload { get; set; }
        string Error { get; set; }
        string StackTrace { get; set; }
        Guid CorrelationId { get; set; }
        string Type { get; set; }
        string ConsumerTag { get; set; }
        string RoutingKey { get; set; }
        string VirtualHost { get; set; }
        Guid RunId { get; set; }
        Guid MessageId { get; set; }
    }
}
