
namespace Rbit.EasyNetQ.AutoReceiver.Interfaces
{
    public interface IAutoReceiverMessageDispatcher
    {
        void Dispatch<TMessage, TReceiver>(TMessage message)
            where TMessage : class
            where TReceiver : IReceive<TMessage>;
    }
}
