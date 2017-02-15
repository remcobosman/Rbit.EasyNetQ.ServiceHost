using System;
using EasyNetQ;
using Rbit.EasyNetQ.Interfaces;
using Rbit.EasyNetQ.Interfaces.Enums;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Auditing
{
    /// <summary>
    /// The audit message written to a queue in RabbitMQ.
    /// </summary>
    public class RabbitAuditMessage : IRabbitAuditMessage
    {
        public string ProducerName { get; set; }
        public Direction Direction { get; set; }
        public Guid CorrelationId { get; set; }
        public MessageProperties Properties { get; set; }
        public string Message { get; set; }
        public DateTime DateTime { get; set; }
        public Guid RunId { get; set; }
        public string ReferenceCode { get; set; }
    }
}