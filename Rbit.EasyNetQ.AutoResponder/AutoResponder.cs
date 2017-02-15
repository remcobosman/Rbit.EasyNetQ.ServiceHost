using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyNetQ;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.AutoResponder.Interfaces;
using Rbit.EasyNetQ.AutoResponder.Support;

namespace Rbit.EasyNetQ.AutoResponder
{
    /// <summary>
    /// Lets you scan assemblies for implementations of <see cref="IRespond{TRequest,TResponse}"/> so that
    /// these will get registrered as subscribers in the bus.
    /// </summary>
    public sealed class AutoResponder
    {
        private const string DispatchMethodName = "Dispatch";

        private readonly IBus _bus;
        private readonly ILogger _logger;

        public IAutoResponderMessageDispatcher AutoResponderMessageDispatcher { get; set; }

        public AutoResponder(IBus bus, ILogger logger)
        {
            _bus = bus;
            _logger = logger;
            AutoResponderMessageDispatcher = null;
        }

        public void Subscribe(params Assembly[] assemblies)
        {
            var subscriptionInfos = GetSubscriptionInfos(assemblies.SelectMany(a => a.GetTypes()), typeof(IRespond<,>));

            InvokeMethods(
                subscriptionInfos,
                DispatchMethodName,
                (requestType, respondType) => typeof(Func<,>).MakeGenericType(requestType, respondType));
        }

        private void InvokeMethods(IEnumerable<KeyValuePair<Type, ResponderHandlerInfo[]>> subscriptionInfos,
            string dispatchName,
            Func<Type, Type, Type> subscriberTypeFromMessageTypeDelegate)
        {

            foreach (var kv in subscriptionInfos)
            {
                foreach (var subscriptionInfo in kv.Value)
                {
                    // Gets a reference to the ninject dispatch method, the one that is really called when a request is made.
                    var dispatchMethod =
                            AutoResponderMessageDispatcher.GetType()
                                                           .GetMethod(dispatchName, BindingFlags.Instance | BindingFlags.Public)
                                                           .MakeGenericMethod(subscriptionInfo.RequestType, subscriptionInfo.RespondType, subscriptionInfo.ConcreteType);

                    // This is a delegate to the dispatch function, which is used to add as the actual handler to Respond.
                    var dispatchDelegate =
                        Delegate.CreateDelegate(
                            subscriberTypeFromMessageTypeDelegate(subscriptionInfo.RequestType, subscriptionInfo.RespondType),
                            AutoResponderMessageDispatcher,
                            dispatchMethod);

                    // Get a handle to the Respond function of the IBus that has one parameter (the callback function)
                    var f = typeof(RabbitBus)
                        .GetMethods()
                        .Where(x => x.Name == "Respond")
                        .Select(m => new { Method = m, Params = m.GetParameters() })
                        .Single(m => m.Params[0].Name == "responder" && m.Params.Length == 1)
                        .Method;

                    // Now execute the Respond function via reflection, bypassing any type checks
                    f.MakeGenericMethod(subscriptionInfo.RequestType, subscriptionInfo.RespondType).Invoke(_bus, new object[] { dispatchDelegate });

                    _logger.Info("Added Responder: {0} for message: {1} from assembly: {2}", subscriptionInfo.ConcreteType.Name, subscriptionInfo.RequestType.Name, subscriptionInfo.ConcreteType.Assembly.FullName);
                }
            }
        }

        private IEnumerable<KeyValuePair<Type, ResponderHandlerInfo[]>> GetSubscriptionInfos(IEnumerable<Type> types, Type interfaceType)
        {
            // Get all the classes that implement IRespond in some way (whic is set in the type parameter)
            foreach (var concreteType in types.Where(t => t.IsClass && !t.IsAbstract))
            {
                var subscriptionInfos = concreteType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType && !i.GetGenericArguments()[0].IsGenericParameter)
                    .Select(i => new ResponderHandlerInfo(concreteType, i.GetGenericArguments()[0], i.GetGenericArguments()[1]))
                    .ToArray();

                if (subscriptionInfos.Any())
                    yield return new KeyValuePair<Type, ResponderHandlerInfo[]>(concreteType, subscriptionInfos);
            }
        }

    }
}