using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyNetQ;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.Consumer;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.AutoReceiver.Interfaces;
using Rbit.EasyNetQ.AutoReceiver.Support;

namespace Rbit.EasyNetQ.AutoReceiver
{
    /// <summary>
    /// Lets you scan assemblies for implementations of <see cref="IConsume{T}"/> so that
    /// these will get registrered as subscribers in the bus.
    /// </summary>
    public sealed class AutoReceiver
    {
        private const string DispatchMethodName = "Dispatch";

        private readonly IBus _bus;
        private readonly string _queue;
        private readonly ILogger _logger;

        /// <summary>
        /// Responsible for consuming a message with the relevant message consumer.
        /// </summary>
        public IAutoReceiverMessageDispatcher AutoSubscriberMessageDispatcher { get; set; }

        public AutoReceiver(IBus bus, ILogger logger, string queue)
        {
            _queue = queue;
            _bus = bus;
            _logger = logger;
            AutoSubscriberMessageDispatcher = null;
        }

        /// <summary>
        /// Registers all consumers in passed assembly. The actual Subscriber instances is
        /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
        /// method is determined by or if the method
        /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.
        /// </summary>
        /// <param name="assemblies">The assembleis to scan for consumers.</param>
        public void Subscribe(params Assembly[] assemblies)
        {
            var subscriptionInfos = GetSubscriptionInfos(assemblies.SelectMany(a => a.GetTypes()), typeof(IReceive<>));

            InvokeMethods(subscriptionInfos, DispatchMethodName, messageType => typeof(Action<>).MakeGenericType(messageType));
        }

        private void InvokeMethods(IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> subscriptionInfos, string dispatchName, Func<Type, Type> subscriberTypeFromMessageTypeDelegate)
        {
            // List of handlers we need to add
            var receivers = new List<ReceiverHandlerInfo>();

            foreach (var kv in subscriptionInfos)
            {
                foreach (var subscriptionInfo in kv.Value)
                {
                    var dispatchMethod =
                            AutoSubscriberMessageDispatcher.GetType()
                                                           .GetMethod(dispatchName, BindingFlags.Instance | BindingFlags.Public)
                                                           .MakeGenericMethod(subscriptionInfo.MessageType, subscriptionInfo.ConcreteType);

                    var dispatchDelegate = Delegate.CreateDelegate(subscriberTypeFromMessageTypeDelegate(subscriptionInfo.MessageType), AutoSubscriberMessageDispatcher, dispatchMethod);

                    // Create the add method in the bud
                    var addMethod = GetAddMethod(typeof(IReceiveRegistration));
                    var addReceiverMethod = addMethod.MakeGenericMethod(subscriptionInfo.MessageType);

                    // Setup the info for registering
                    receivers.Add(new ReceiverHandlerInfo
                    {
                        AddHandler = addReceiverMethod,
                        Handler = dispatchDelegate
                    });

                    _logger.Info("Adding Receiver: {0} for message: {1} from assembly: {2}", subscriptionInfo.ConcreteType.Name, subscriptionInfo.MessageType.Name, subscriptionInfo.ConcreteType.Assembly.FullName);
                }
            }

            // Now register all the handlers we found
            _bus.Receive(_queue, delegate (IReceiveRegistration registration)
            {
                foreach (var m in receivers)
                {
                    m.AddHandler.Invoke(registration, new object[] { m.Handler });
                }
            });
        }
        
        private MethodInfo GetAddMethod(Type type)
        {
            return type.GetMethods()
            .Where(m => m.Name == "Add")
            .Select(m => new { Method = m, Params = m.GetParameters() })
            .Single(m => m.Params[0].Name == "onMessage" && m.Params[0].ParameterType.UnderlyingSystemType.Name == "Action`1"
               ).Method;
        }

        private IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> GetSubscriptionInfos(IEnumerable<Type> types, Type interfaceType)
        {
            foreach (var concreteType in types.Where(t => t.IsClass && !t.IsAbstract))
            {
                var subscriptionInfos = concreteType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType && !i.GetGenericArguments()[0].IsGenericParameter)
                    .Select(i => new AutoSubscriberConsumerInfo(concreteType, i, i.GetGenericArguments()[0]))
                    .ToArray();

                if (subscriptionInfos.Any())
                    yield return new KeyValuePair<Type, AutoSubscriberConsumerInfo[]>(concreteType, subscriptionInfos);
            }
        }
    }
}