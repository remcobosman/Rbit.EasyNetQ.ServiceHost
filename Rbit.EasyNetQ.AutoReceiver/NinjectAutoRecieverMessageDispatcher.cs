using System;
using Ninject;
using Rbit.EasyNetQ.AutoReceiver.Interfaces;

namespace Rbit.EasyNetQ.AutoReceiver
{
    public class NinjectAutoReceiverMessageDispatcher : IAutoReceiverMessageDispatcher
    {
        private readonly IKernel _container;

        public NinjectAutoReceiverMessageDispatcher(IKernel container)
        {
            _container = container;
        }
        public void Dispatch<TMessage, TReceiver>(TMessage message)
            where TMessage : class
            where TReceiver : IReceive<TMessage>
        {
            // Call the handler
            var Receiver = _container.Get<TReceiver>();
            if (Receiver == null)
            {
                throw new Exception(string.Format("Unable to instantiate receiver of type [{0}].", typeof(TReceiver)));
            }

            Receiver.Receive(message);
        }
    }
}