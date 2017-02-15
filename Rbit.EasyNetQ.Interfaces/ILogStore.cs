namespace Rbit.EasyNetQ.Interfaces
{
    public interface ILogStore<T>
    {
        string Save(T message);
    }
}
