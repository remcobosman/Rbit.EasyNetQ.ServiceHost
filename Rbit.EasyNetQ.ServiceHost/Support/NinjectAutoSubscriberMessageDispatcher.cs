using System;
using EasyNetQ.AutoSubscribe;
using Ninject;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public class NinjectAutoSubscriberMessageDispatcher : IAutoSubscriberMessageDispatcher
    {
        private readonly IKernel _container;

        public NinjectAutoSubscriberMessageDispatcher(IKernel container)
        {
            _container = container;
        }
        public void Dispatch<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsume<TMessage>
        {
            // Call the handler
            var receiver = _container.Get<TConsumer>();
            if (receiver == null)
            {
                throw new Exception(string.Format("Unable to instantiate subscriber of type [{0}].", typeof(TConsumer)));
            }

            receiver.Consume(message);
        }

        public System.Threading.Tasks.Task DispatchAsync<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsumeAsync<TMessage>
        {
            var consumer = _container.Get<TConsumer>();
            if (consumer == null)
            {
                throw new Exception(string.Format("Unable to instantiate subscriber of type [{0}].", typeof(TConsumer)));
            }

            return consumer.Consume(message).ContinueWith(t=> _container.Release(consumer));
        }
    }
}