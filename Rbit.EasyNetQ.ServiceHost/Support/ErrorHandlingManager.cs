using System;
using System.IO;
using EasyNetQ;
using Ninject;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.ErrorManagement;
using Rbit.EasyNetQ.Interfaces;
using Rbit.EasyNetQ.ServiceHost.Interfaces;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public class ErrorHandlingManager : LoggingManagerBase, IErrorHandlingManager
    {
        private readonly ILogger _logger;

        public ErrorHandlingManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initailizes and setup of error logging by the handler.
        /// </summary>
        /// <param name="container">The injection container for adding error handlers and classes.</param>
        /// <param name="serviceName">The name of the specific handler.</param>
        /// <param name="connectionString">The conection config key to the error database.</param>
        public void InitializeErrorHandling(IKernel container, string serviceName, string connectionString)
        {
            // Default to a file store
            ILogStore<RabbitErrorMessage> store = new FileErrorStore("errors");

            // If we have a database connection setup the db errorstore
            if (!string.IsNullOrEmpty(connectionString))
            {
                _logger.Info("Using a database for errors, connection: [{0}]", connectionString);
                store = new DatabaseErrorStore(connectionString);

                var folder = $@"{Environment.CurrentDirectory}\{"errors"}";
                if (Directory.Exists(folder))
                {
                    _logger.Info("Looking for previously saved files to store in the management database.");

                    // See if we previously saved fles to disk, so we can now load them up into the database
                    ProcessSavedFiles(_logger, store, folder);
                }
            }
            else
            {
                _logger.Info("Falling back to using the file system for writing errors.");
            }

            var errorSubscriber = new DefaultErrorSubscriber(store, new FileErrorStore("errors"), _logger, container.Get<IBus>());

            // Setup the default error reader
            container.Get<IBus>().Advanced.Consume(
                container.Get<IBus>().Advanced.QueueDeclare(string.Format("{0}_deadletter",
                        serviceName)), h => h.Add<RabbitErrorMessage>((message, info) => errorSubscriber.HandleDefaultErrorMessage(message)));

            _logger.Info("Error Management configured.");
        }
    }
}
