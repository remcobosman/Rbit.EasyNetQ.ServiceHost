using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyNetQ;
using EasyNetQ.AutoSubscribe;
using Ninject;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.AutoReceiver;
using Rbit.EasyNetQ.AutoResponder;
using Rbit.EasyNetQ.ServiceHost.Interfaces;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public class EasyNetQScannerManager : IEasyNetQScannerManager
    {
        private readonly ILogger _logger;

        public EasyNetQScannerManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Performs the scanning of subscribers and receivers.
        /// </summary>
        public void ScanReceiversAndSubscribers(IKernel kernel, string queue)
        {
            // Read configuration for queue name
            var assemblies = GetSubscriberAndReceiverAssemblies();

            if (!string.IsNullOrEmpty(queue))
            {
                // Load all send/receive receivers
                ScanForReceivers(
                    kernel.Get<IBus>(), 
                    assemblies, 
                    ConfigurationExtensions.ApplicationSetting("Rabbit.Queue", string.Empty), 
                    kernel);

                _logger.Info("Receivers added, if any where defined.");
            }
            else
            {
                _logger.Info("No Rabbit.Queue configuration found, skipping loading of Receivers.");
            }

            ScanForSubscribers(
                kernel.Get<IBus>(), 
                assemblies, 
                kernel);

            _logger.Info("Subscribers added, if any where defined.");

            ScanForResponders(
                kernel.Get<IBus>(), 
                assemblies, 
                kernel);

            _logger.Info("Responders added, if any where defined.");
        }

        /// <summary>
        /// Scans the current directory for defined easynetq receivers.
        /// </summary>
        /// <param name="bus">The easynetq rabbitmq bus instance.</param>
        /// <param name="assemblies">The list of assemblies containeg subscriber classes.</param>
        /// <param name="queueName">The name of the queue to listen to for commands for the receivers.</param>
        /// <param name="container">The injection container for adding the subscribers.</param>
        private static void ScanForReceivers(IBus bus, IEnumerable<Assembly> assemblies, string queueName, IKernel container)
        {
            // Load all send/receive receivers
            var receiverScanner = new AutoReceiver.AutoReceiver(bus, container.Get<ILoggerFactory>().GetCurrentClassLogger(),
                queueName)
            {
                AutoSubscriberMessageDispatcher = new NinjectAutoReceiverMessageDispatcher(container)
            };

            receiverScanner.Subscribe(assemblies.ToArray());
        }

        private static void ScanForResponders(IBus bus, IEnumerable<Assembly> assemblies, IKernel container)
        {
            // Load all send/receive receivers
            var responderScanner = new AutoResponder.AutoResponder(bus, container.Get<ILoggerFactory>().GetCurrentClassLogger())
            {
                AutoResponderMessageDispatcher = new NinjectAutoResponderMessageDispatcher(container)
            };

            responderScanner.Subscribe(assemblies.ToArray());

        }

        /// <summary>
        /// Scans the current directory for defined easynetq subscribers.
        /// </summary>
        /// <param name="bus">The easynetq rabbitmq bus instance.</param>
        /// <param name="assemblies">The list of assemblies containeg subscriber classes.</param>
        /// <param name="container">The injection container for adding the subscribers.</param>
        private static void ScanForSubscribers(IBus bus, IEnumerable<Assembly> assemblies, IKernel container)
        {
            // Load all publish/subscribe subscribers
            var subscriberScanner = new AutoSubscriber(bus, ConfigurationExtensions.ApplicationSetting("Rabbit.ServiceName").Replace(" ", "_"))
            {
                AutoSubscriberMessageDispatcher = new NinjectAutoSubscriberMessageDispatcher(container)
            };

            subscriberScanner.Subscribe(assemblies.ToArray());
        }

        /// <summary>
        /// Retrieves a list of assemblies from the current directory that contain subscribers and receivers.
        /// </summary>
        /// <returns>A list of assemblies that contain subscribers and receiver classes.</returns>
        private static List<Assembly> GetSubscriberAndReceiverAssemblies()
        {
            var assemblyScannerPrefix = ConfigurationExtensions.ApplicationSetting("Rabbit.AssemblyScannerPrefix", string.Empty);
            var assemblies = !string.IsNullOrEmpty(assemblyScannerPrefix) ?
                AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.StartsWith(assemblyScannerPrefix)).ToList() :
                AppDomain.CurrentDomain.GetAssemblies().ToList();

            // Check if its actually worthwhile to proceed
            if (assemblies == null || !assemblies.Any())
            {
                throw new Exception("No assemblies found that implement either receivers or subscribers for RabbitMQ, service has nothing to do.");
            }
            return assemblies;
        }
    }
}
