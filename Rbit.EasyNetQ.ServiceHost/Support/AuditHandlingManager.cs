using System;
using EasyNetQ;
using Ninject;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Auditing;
using Rbit.EasyNetQ.Interfaces;
using Rbit.EasyNetQ.ServiceHost.Interfaces;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public class AuditHandlingManager : LoggingManagerBase, IAuditHandlingManager
    {
        private readonly ILogger _logger;

        public AuditHandlingManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Sets up audit management for the generic easynetq host.
        /// </summary>
        /// <param name="container">The injection container for adding audit management classes.</param>
        /// <param name="serviceName">The name of the consumer (easynetq handler name).</param>
        /// <param name="connectionString">The config name of the database connection for database auditing.</param>
        public void InitializeAuditing(IKernel container, string serviceName, string connectionString)
        {
            // Default to a file store
            ILogStore<RabbitAuditMessage> store = new FileAuditStore("audit");

            // If we have a database connection setup the db errorstore
            if (!string.IsNullOrEmpty(connectionString))
            {
                _logger.Info("Using a database for auditing, connection: [{0}]", connectionString);
                store = new DatabaseAuditStore(connectionString, serviceName.Replace("_", " "), _logger);

                var folder = $@"{Environment.CurrentDirectory}\{"audit"}";
                ProcessSavedFiles(_logger, store, folder);
            }
            else
            {
                _logger.Info("Falling back to using the file system for writing audit records.");
            }

            var auditSubscriber = new DefaultAuditSubscriber(store, new FileAuditStore("audit"), _logger);

            // Setup the default error reader
            container.Get<IBus>().Advanced.Consume(
                container.Get<IBus>().Advanced.QueueDeclare(string.Format("{0}_audit",
                        serviceName)), h => h.Add<RabbitAuditMessage>((message, info) => auditSubscriber.HandleAuditMessage(message)));

            _logger.Info("Audit Management configured.");
        }
    }
}
