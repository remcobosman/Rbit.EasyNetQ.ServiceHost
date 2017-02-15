using Ninject;
using Rbit.EasyNetQ.ServiceHost.Interfaces;

namespace Rbit.EasyNetQ.Sample
{
    public class ServiceConfiguration : IConfigureThisBusWihtNinject
    {
        public IKernel Configure(IKernel kernel)
        {
            return kernel;
        }
    }
}
