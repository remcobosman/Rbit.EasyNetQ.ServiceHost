using Ninject;

namespace Rbit.EasyNetQ.ServiceHost.Interfaces
{
    public interface IAuditHandlingManager
    {
        void InitializeAuditing(IKernel kernel, string serviceName, string connectionString);
    }
}