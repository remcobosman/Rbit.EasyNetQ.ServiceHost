using EasyNetQ;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces
{
    /// <summary>
    /// A factory class that allows us to change the connection on each instantiation of the bus. Used in the xrm rabbit scheduled task base, that will 
    /// need to set different connections based on either the configuration in xrm or in the object itself.
    /// </summary>
    public interface IMessagBusFactory
    {
        IBus GetBus(string connection);
        IBus GetBus(string connection, string producerName);
    }
}