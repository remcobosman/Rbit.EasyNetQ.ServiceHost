
namespace Rbit.EasyNetQ.AutoReceiver.Interfaces
{
    public interface IReceive<in T> where T : class
    {
        void Receive(T message);
    }
}
