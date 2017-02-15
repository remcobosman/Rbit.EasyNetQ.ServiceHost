using System;
using Ninject;
using Rbit.EasyNetQ.AutoResponder.Interfaces;

namespace Rbit.EasyNetQ.AutoResponder
{
    public class NinjectAutoResponderMessageDispatcher : IAutoResponderMessageDispatcher
    {
        private readonly IKernel _container;

        public NinjectAutoResponderMessageDispatcher(IKernel container)
        {
            _container = container;
        }
        public TResponse Dispatch<TRequest, TResponse, TResponder>(TRequest request) 
            where TRequest : class 
            where TResponse : class 
            where TResponder : IRespond<TRequest, TResponse>
        {
            var Responder = _container.Get<TResponder>();
            if (Responder == null)
            {
                throw new Exception(string.Format("Unable to instantiate receiver of type [{0}].", typeof(TResponder)));
            }

            return Responder.Respond(request);
        }
    }
}