namespace Rbit.EasyNetQ.AutoResponder.Interfaces
{
    public interface IRespond<TRequest, TResponse> where TResponse : class
    {
        TResponse Respond(TRequest request);
    }
}
