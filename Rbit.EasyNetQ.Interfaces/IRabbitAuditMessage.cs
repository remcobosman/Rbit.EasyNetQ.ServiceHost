using System;
using EasyNetQ;
using Rbit.EasyNetQ.Interfaces.Enums;

namespace Rbit.EasyNetQ.Interfaces
{

    public interface IRabbitAuditMessage
    {
        string ProducerName { get; set; }
        Direction Direction { get; set; }
        Guid CorrelationId { get; set; }
        MessageProperties Properties { get; set; }
        string Message { get; set; }
        DateTime DateTime { get; set; }
        Guid RunId { get; set; }
        string ReferenceCode { get; set; }
    }
}