using Ninject;
using Rbit.EasyNetQ.ServiceHost.Interfaces;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public class HandlerConfigurationDefault : IConfigureThisBusWihtNinject
    {
        /// <summary>
        /// Configures the handler and returns the Ninject IOC container to the generic handler.
        /// </summary>
        /// <returns></returns>
        public virtual IKernel Configure(IKernel kernel)
        {
            return kernel;
        }
    }
}
