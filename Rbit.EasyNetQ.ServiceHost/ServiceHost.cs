using System;
using EasyNetQ;
using EasyNetQ.Consumer;
using EasyNetQ.Interception;
using Ninject;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Auditing;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Conventions;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.ErrorManagement;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interceptors;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Support;
using Rbit.EasyNetQ.ServiceHost.Interfaces;
using Rbit.EasyNetQ.ServiceHost.Support;
using ILoggerFactory = Ninject.Extensions.Logging.ILoggerFactory;

namespace Rbit.EasyNetQ.ServiceHost
{
    public class ServiceHost : IBusServiceHost
    {
        private readonly IKernel _container;
        private readonly ILogger _logger;
        private readonly IAuditHandlingManager _auditHandlingManager;
        private readonly IErrorHandlingManager _errorHandlingManager;
        private readonly IEasyNetQScannerManager _easyNetQScannerManager;
        private readonly IQuartzTaskManager _quartzTaskManager;

        public ServiceHost(
            IKernel container,
            ILogger logger,
            IAuditHandlingManager auditHandlingManager,
            IErrorHandlingManager errorHandlingManager,
            IEasyNetQScannerManager easyNetQScannerManager,
            IQuartzTaskManager quartzTaskManager)
        {
            _container = container;
            _logger = logger;
            _auditHandlingManager = auditHandlingManager;
            _errorHandlingManager = errorHandlingManager;
            _easyNetQScannerManager = easyNetQScannerManager;
            _quartzTaskManager = quartzTaskManager;
        }

        public void Start()
        {
            try
            {
                _container.Bind<IPersistCorrelation>().To<DefaultCorrelationPersistor>().InSingletonScope();

                // Set the custom (ninject) ioc container
                RabbitHutch.SetContainerFactory(() => new CustomNinjectAdapter(_container));

                // Create the rabbit Bus
                new BusBuilder().CreateMessageBus(s => s
                    .Register<IEasyNetQLogger>(l => new Log4NetLogger(_container.Get<ILoggerFactory>().GetCurrentClassLogger()))
                    .Register<IConventions>(c => c.Resolve<CustomConventions>())
                    .Register<IConsumerErrorStrategy>(e => e.Resolve<RetryErrorStrategy>())
                    .Register(e => EnableAuditing ? e.Resolve<HandlerAuditIntercepter>() as IProduceConsumeInterceptor : e.Resolve<DefaultInterceptor>() as IProduceConsumeInterceptor)
                );

                // Scan for consumers and receivers
                _easyNetQScannerManager.ScanReceiversAndSubscribers(_container, QueueName);

                if (EnableErrorHandling)
                {
                    // Initialize erro handling to files and database
                    _errorHandlingManager.InitializeErrorHandling(_container, ServiceName, ManagementDatabaseConnection);
                }
                else
                {
                    _logger.Info("No Error Management configured.");
                }

                if (EnableAuditing)
                {
                    // Initialize auditing to files and database
                    _auditHandlingManager.InitializeAuditing(_container, ServiceName, ManagementDatabaseConnection);
                }
                else
                {
                    _logger.Info("No Audit Management configured.");
                }

                // Configure the quartz tasks
                _quartzTaskManager.InitializeScheduledTasks(_container);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error starting EasynetQ Generic Host.");
                throw;
            }
        }

        private static string QueueName => ConfigurationExtensions.ApplicationSetting("Rabbit.Queue", string.Empty);

        private static string ServiceName => ConfigurationExtensions.ApplicationSetting("Rabbit.ServiceName").Replace(" ", "_");

        private static string ManagementDatabaseConnection => ConfigurationExtensions.GetConnectionString("RabbitMQ.Management.Store", string.Empty);
        /// <summary>
        /// Gets or sets whether or not auditing is enabled in the application config.
        /// </summary>
        private static bool EnableAuditing => ConfigurationExtensions.ApplicationSetting("Rabbit.Management.EnableAuditing", "false").ToLower() == "true";

        /// <summary>
        /// Gets or sets whether or not error handling is enabled in the application config.
        /// </summary>
        private static bool EnableErrorHandling => ConfigurationExtensions.ApplicationSetting("Rabbit.Management.EnableErrors", "false").ToLower() == "true";
    }
}