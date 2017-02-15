using Ninject;

namespace Rbit.EasyNetQ.ServiceHost.Interfaces
{
    /// <summary>
    /// Interface that defines how to configure a Rabbit generic handler. You should have one and only one class in the project that implements this interface. You
    /// use the implementation to build the Ninject kernel and all required modules/bindings. And you can define youy sheduled tasks required for the handler.
    /// </summary>
    public interface IConfigureThisBusWihtNinject
    {
        /// <summary>
        /// Configures the handler and returns the Ninject IOC container to the generic handler.
        /// </summary>
        /// <returns></returns>
        IKernel Configure(IKernel kernel);
    }
}
