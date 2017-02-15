using EasyNetQ.AutoSubscribe;

namespace Rbit.EasyNetQ.ServiceHost.Interfaces
{
    public interface IAutoSubscriberMessageDispatcher
    {
        void Dispatch<TMessage, TReceiver>(TMessage message)
            where TMessage : class
            where TReceiver : IConsume<TMessage>;
    }
}