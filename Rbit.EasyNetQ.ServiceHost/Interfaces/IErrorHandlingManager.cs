using Ninject;

namespace Rbit.EasyNetQ.ServiceHost.Interfaces
{
    public interface IErrorHandlingManager
    {
        void InitializeErrorHandling(IKernel kernel, string serviceName, string connectionString);
    }
}