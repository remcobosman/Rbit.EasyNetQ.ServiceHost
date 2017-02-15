using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Text;
using System.Threading;
using EasyNetQ;
using EasyNetQ.Consumer;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using IConnectionFactory = EasyNetQ.IConnectionFactory;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.ErrorManagement
{
    public class RetryErrorStrategy : IConsumerErrorStrategy
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private readonly IEasyNetQLogger _logger;

        private readonly ISerializer _serializer;
        private readonly IConventions _conventions;
        private readonly ITypeNameSerializer _typeNameSerializer;

        private readonly int _flrMaxRetries;

        private readonly ConcurrentDictionary<string, string> _errorExchanges = new ConcurrentDictionary<string, string>();
        private bool _errorQueueDeclared;

        public RetryErrorStrategy(IConnectionFactory connectionFactory,
                                            ISerializer serializer,
                                            IEasyNetQLogger logger,
                                            IConventions conventions,
                                            ITypeNameSerializer typeNameSerializer)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _serializer = serializer;
            _conventions = conventions;
            _typeNameSerializer = typeNameSerializer;

            // Read the retries from the configuration, default to 0
            _flrMaxRetries = 0;
            int.TryParse(ConfigurationManager.AppSettings["Rabbit.FLR.MaxRetries"], out _flrMaxRetries);
        }

        /// <summary>
        /// This method is fired when an exception is thrown. Implement a strategy for
        /// handling the exception here.
        /// </summary>
        /// <param name="context">The consumer execution context.</param>
        /// <param name="exception">The exception</param>
        /// <returns><see cref="AckStrategy"/> for processing the original failed message</returns>
        public AckStrategy HandleConsumerError(ConsumerExecutionContext context, Exception exception)
        {
            try
            {
                // First write the exception to the logger
                _logger.ErrorWrite(exception);

                Connect();

                using (var model = _connection.CreateModel())
                {
                    var properties = context.Properties;

                    var flrRetry = 1;
                    if (properties.HeadersPresent && properties.Headers.ContainsKey("easynetq.retry.count"))
                    {
                        flrRetry = Convert.ToInt16(properties.Headers["easynetq.retry.count"]) + 1;
                    }

                    // Max retries set to 2 means we will run it once more, 3 means run it twice more, 3 etc etc
                    if (flrRetry < _flrMaxRetries)
                    {
                        _logger.InfoWrite(
                            $"(FLR) First Level Retry [{flrRetry}] for message of type [{properties.Type}].");

                        RetryMessage(context, model, properties, flrRetry);

                        _logger.DebugWrite("Message resent.");
                    }
                    else
                    {
                        _logger.DebugWrite(
                            $"Reached maximum First Level Retry, handing over to dead letter queue, message of type [{properties.Type}].");

                        // Send message to the deadletter queue
                        SendMessageToErrorQueue(context, exception, model, properties);
                    }
                }
            }
            catch (BrokerUnreachableException)
            {
                // thrown if the broker is unreachable during initial creation.
                _logger.ErrorWrite("EasyNetQ Consumer Error Handler cannot connect to Broker\n" +
                    CreateConnectionCheckMessage());
            }
            catch (OperationInterruptedException interruptedException)
            {
                // thrown if the broker connection is broken during declare or publish.
                _logger.ErrorWrite("EasyNetQ Consumer Error Handler: Broker connection was closed while attempting to publish Error message.\n" +
                                   $"Message was: '{interruptedException.Message}'\n" +
                    CreateConnectionCheckMessage());
            }
            catch (Exception unexpectedException)
            {
                // Something else unexpected has gone wrong :(
                _logger.ErrorWrite("EasyNetQ Consumer Error Handler: Failed to publish error message\nException is:\n"
                    + unexpectedException);
            }

            return AckStrategies.Ack;
        }

        private void SendMessageToErrorQueue(ConsumerExecutionContext context, Exception exception, IModel model, MessageProperties properties)
        {
            var errorExchange = DeclareErrorExchangeQueueStructure(model, context);

            _logger.InfoWrite(
                $"(DEAD LETTERED) Second Level Retry max reached for message of type [{properties.Type}], message is sent to dead letter queue: [{errorExchange}].");

            var messageBody = CreateDefaultErrorMessage(context, exception.InnerException);

            var errorProperties = model.CreateBasicProperties();
            properties.CopyTo(errorProperties);
            errorProperties.Persistent =true;
            errorProperties.Type = _typeNameSerializer.Serialize(typeof(RabbitErrorMessage));

            model.BasicPublish(errorExchange, context.Info.RoutingKey, errorProperties, messageBody);
        }

        private static void RetryMessage(ConsumerExecutionContext context, IModel model, MessageProperties properties, int flrRetry)
        {
            // Ensure and update the retrycount
            properties.Headers.Remove("easynetq.retry.count");
            properties.Headers.Add("easynetq.retry.count", flrRetry);

            // Copy all properties to BasicProperties for the Rabbit
            var basicProperties = model.CreateBasicProperties();
            properties.CopyTo(basicProperties);

            // Resend the message
            model.BasicPublish(context.Info.Exchange, context.Info.RoutingKey, basicProperties, context.Body);
        }

        /// <summary>
        /// This method is fired when the task returned from the UserHandler is cancelled. 
        /// Implement a strategy for handling the cancellation here.
        /// </summary>
        /// <param name="context">The consumer execution context.</param>
        /// <returns><see cref="AckStrategy"/> for processing the original cancelled message</returns>
        public AckStrategy HandleConsumerCancelled(ConsumerExecutionContext context)
        {
            return AckStrategies.Ack;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }
        }

        private void Connect()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = _connectionFactory.CreateConnection();
            }
        }

        private string CreateConnectionCheckMessage()
        {
            return
                "Please check EasyNetQ connection information and that the RabbitMQ Service is running at the specified endpoint.\n" +
                $"\tHostname: '{_connectionFactory.CurrentHost.Host}'\n" +
                $"\tVirtualHost: '{_connectionFactory.Configuration.VirtualHost}'\n" +
                $"\tUserName: '{_connectionFactory.Configuration.UserName}'\n" +
                "Failed to write error message to error queue";
        }

        private byte[] CreateDefaultErrorMessage(ConsumerExecutionContext context, Exception exception)
        {
            var error = new RabbitErrorMessage
            {
                DateTime = DateTime.UtcNow,
                Error = SerializeException(exception),
                Exchange = context.Info.Exchange,
                Properties = (context.Properties != null) ? JsonConvert.SerializeObject(context.Properties) : string.Empty,
                Payload = Encoding.UTF8.GetString(context.Body),
                Queue = context.Info.Queue,
                StackTrace = exception.StackTrace,
                Topic = context.Info.RoutingKey,
                Type = (context.Properties != null) ? context.Properties.Type : string.Empty,
                CorrelationId = (context.Properties != null && context.Properties.CorrelationIdPresent) ? new Guid(context.Properties.CorrelationId) : Guid.Empty,
                ConsumerTag = context.Info.ConsumerTag,
                RoutingKey = context.Info.RoutingKey,
                Server = _connectionFactory.CurrentHost.Host,
                VirtualHost = _connectionFactory.Configuration.VirtualHost,
                MessageId = context.Properties!= null ? context.Properties.MessageIdPresent ? new Guid(context.Properties.MessageId) : Guid.Empty : Guid.Empty,
                RunId = Thread.GetData(Thread.GetNamedDataSlot("___runid")) != null ? (Guid)Thread.GetData(Thread.GetNamedDataSlot("___runid")) : Guid.Empty,
            };

            return _serializer.MessageToBytes(error);
        }

        private string SerializeException(Exception exception)
        {
            _logger.DebugWrite("Processing exception [{0}].", exception.Message);
            var result = exception.Message;

            while (exception.InnerException != null)
            {
                _logger.DebugWrite("Exception seems to have an inner exception.");
                exception = exception.InnerException;
                result = $"\r\n{exception.Message}";
            }

            return result;
        }

        private string DeclareErrorExchangeQueueStructure(IModel model, ConsumerExecutionContext context)
        {
            DeclareDefaultErrorQueue(model, string.IsNullOrEmpty(context.Info.Queue) ? context.Info.RoutingKey : context.Info.ConsumerTag);
            return DeclareErrorExchangeAndBindToDefaultErrorQueue(model, context);
        }

        private void DeclareDefaultErrorQueue(IModel model, string name)
        {
            if (!_errorQueueDeclared)
            {
                model.QueueDeclare(
                    queue: $"{name}_deadletter",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                _errorQueueDeclared = true;
            }
        }

        private string DeclareErrorExchangeAndBindToDefaultErrorQueue(IModel model, ConsumerExecutionContext context)
        {
            var originalRoutingKey = context.Info.RoutingKey;

            return _errorExchanges.GetOrAdd(originalRoutingKey, _ =>
            {
                var exchangeName = _conventions.ErrorExchangeNamingConvention(context.Info);
                model.ExchangeDeclare(exchangeName, ExchangeType.Direct, durable: true);
                model.QueueBind(
                    $"{(string.IsNullOrEmpty(context.Info.Queue) ? context.Info.RoutingKey : context.Info.ConsumerTag)}_deadletter", exchangeName, originalRoutingKey);
                return exchangeName;
            });
        }
    }
}
