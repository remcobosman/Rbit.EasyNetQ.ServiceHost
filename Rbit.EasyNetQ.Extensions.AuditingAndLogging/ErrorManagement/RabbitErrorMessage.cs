using System;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.ErrorManagement
{
    public class RabbitErrorMessage : IRabbitErrorMessage
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Server { get; set; }
        public string Exchange { get; set; }
        public string Properties { get; set; }
        public string Queue { get; set; }
        public string Topic { get; set; }
        public string Payload { get; set; }
        public string Error { get; set; }
        public string StackTrace { get; set; }
        public Guid CorrelationId { get; set; }
        public string Type { get; set; }
        public string ConsumerTag { get; set; }
        public string RoutingKey { get; set; }
        public string VirtualHost { get; set; }
        public Guid RunId { get; set; }
        public Guid MessageId { get; set; }
    }
}