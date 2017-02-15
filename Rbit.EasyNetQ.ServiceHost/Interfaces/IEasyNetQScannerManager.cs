using Ninject;

namespace Rbit.EasyNetQ.ServiceHost.Interfaces
{
    public interface IEasyNetQScannerManager
    {
        void ScanReceiversAndSubscribers(IKernel kernel, string queue);
    }
}