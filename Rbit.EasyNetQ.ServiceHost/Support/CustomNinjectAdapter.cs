using System;
using EasyNetQ;
using Ninject;
using IServiceProvider = EasyNetQ.IServiceProvider;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public class CustomNinjectAdapter : IContainer, IDisposable
    {
        private readonly IKernel _ninjectContainer;

        public CustomNinjectAdapter(IKernel ninjectContainer)
        {
            _ninjectContainer = ninjectContainer;
        }

        public TService Resolve<TService>() where TService : class
        {
            return _ninjectContainer.Get<TService>();
        }

        public IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            // EasyNetQ first adds the overrides, then the defaults, which would then set the default again.
            if (!_ninjectContainer.CanResolve<TService>())
            {
                _ninjectContainer.Bind<TService>().ToMethod(ctx => serviceCreator(this)).InSingletonScope();
            }
          
            return this;
        }

        public IServiceRegister Register<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            // EasyNetQ first adds the overrides, then the defaults, which would then set the default again.
            if (!_ninjectContainer.CanResolve<TService>())
            {
                _ninjectContainer.Bind<TService>().ToMethod(ctx => ctx.Kernel.Get<TImplementation>()).InSingletonScope();
            }
           
            return this;
        }

        public void Dispose()
        {
            // Somehow when including interception the containers seem to go wrong,so i disable disposing here, should be disposing at topshelf in progrm.cs
            //_ninjectContainer.Dispose();
        }
    }
}