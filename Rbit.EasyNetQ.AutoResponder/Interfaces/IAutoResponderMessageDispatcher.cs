namespace Rbit.EasyNetQ.AutoResponder.Interfaces
{
    public interface IAutoResponderMessageDispatcher
    {
        TResponse Dispatch<TRequest, TResponse, TResponder>(TRequest request)
            where TRequest : class
            where TResponse : class 
            where TResponder : IRespond<TRequest, TResponse>;
    }
}