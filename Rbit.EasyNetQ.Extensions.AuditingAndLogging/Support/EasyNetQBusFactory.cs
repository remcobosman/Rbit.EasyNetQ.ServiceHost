using EasyNetQ;
using EasyNetQ.Interception;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interceptors;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Support
{
    public class EasyNetQBusFactory : IMessagBusFactory
    {
        public IBus GetBus(string connection)
        {
            return GetBus(connection, string.Empty);
        }

        public IBus GetBus(string connection, string producerName)
        {
            if (!string.IsNullOrEmpty(producerName))
            {
                return RabbitHutch.CreateBus(connection, r => r.Register<IProduceConsumeInterceptor>((e => new ProducerAuditIntercepter(producerName))));
            }
            return RabbitHutch.CreateBus(connection);
        }
    }
}
