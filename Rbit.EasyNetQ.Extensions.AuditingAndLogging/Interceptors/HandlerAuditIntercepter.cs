using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using EasyNetQ;
using EasyNetQ.Interception;
using Newtonsoft.Json;
using Ninject.Extensions.Logging;
using RabbitMQ.Client;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Auditing;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;
using Rbit.EasyNetQ.Interfaces.Enums;
using IConnectionFactory = EasyNetQ.IConnectionFactory;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interceptors
{
    /// <summary>
    /// Use this class to add auditing to the RabbitMQ, EasynetQ handler.
    /// </summary>
    public class HandlerAuditIntercepter : IProduceConsumeInterceptor, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private readonly ISerializer _serializer;
        private readonly ITypeNameSerializer _typeNameSerializer;
        private readonly IPersistCorrelation _correlationPersister;
        private bool _auditqueueDeclared;

        public HandlerAuditIntercepter(
            ILogger logger,
            IConnectionFactory connectionFactory,
            ISerializer serializer,
            ITypeNameSerializer typeNameSerializer,
            IPersistCorrelation correlationPersister)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _serializer = serializer;
            _typeNameSerializer = typeNameSerializer;
            _correlationPersister = correlationPersister;
        }

        /// <summary>
        /// Fires when the audit interceptor is configured in EasyNetQ. It will add extra data to the outgoing message for auditing purposes. It 
        /// sets the correlationid and the messageid.
        /// </summary>
        /// <param name="rawMessage">The untainted message from RabbitMQ.</param>
        /// <returns>The modified version of the message, containing the correlationid and messageid.</returns>
        public RawMessage OnProduce(RawMessage rawMessage)
        {
            // Get an existing run id and put it on the thread.
            Thread.SetData(Thread.GetNamedDataSlot(_correlationPersister.RunIdKeyName), _correlationPersister.RunId);

            // Skip the internal error and audit messages, specially for audit messages, they will cause an endless loop!
            if (rawMessage.Properties.Type.StartsWith("Brunel.Messaging.EasyNetQ.")) { return rawMessage; }

            // If we are not setup for auditing in the handler, get outa here!
            if (!ShouldAudit()) { return rawMessage; }

            // We are sending or publishing a new message, get the correlation id we saved earlier (in onconsume) and set it in the 
            // message properties. When there is none on the thread we use the correlation id in the message itself
            // First try the message itself
            var correlationId = GetCorrelationFromRawMessage(rawMessage);

            if (correlationId != Guid.Empty) { _logger.Debug("Found correlation [{0}] set in outgoing message", correlationId); }

            // If this is empty, try to get one from the persister
            if (correlationId == Guid.Empty)
            {
                correlationId = _correlationPersister.CorrelationId;

                if (correlationId != Guid.Empty) { _logger.Debug("Found correlation [{0}] for outgoing message set in correlation persistor", correlationId); }
            }

            // See if we had one, when we originate from a consume
            if (correlationId == Guid.Empty)
            {
                correlationId = this.GenerateNewCorrelationId();
                _logger.Debug("Created new correlation id [{0}] for outgoing message", correlationId);
            }

            // Set the correlationid in the message so it canbe picked up when consumed
            rawMessage.Properties.CorrelationId = correlationId.ToString();

            // Set a new message id so we can recreate the order of send and received messages (we can trace an outgoing message corresponding to the incoming message by id)
            rawMessage.Properties.MessageId = Guid.NewGuid().ToString();

            // Set the appid to the application sending the mnessage
            rawMessage.Properties.AppId = GetConsumerProducerName();

            // Send out an audit message to Rabbit
            SendAuditMessage(rawMessage, Direction.Out);

            return rawMessage;
        }

        /// <summary>
        /// Fires when we receive a message. It sets the correlation id on the thread so we can resuse thsi id for audit tracing. It will also set a new runid on the thread so we can keep track 
        /// of a session (i.e. a complete run through a consumer or receiver).
        /// </summary>
        /// <param name="rawMessage">The untainted message from RabbitMQ.</param>
        /// <returns>An unmodified version of the message. We set the correlation id found in the incomng message on a thread variable.</returns>
        public RawMessage OnConsume(RawMessage rawMessage)
        {
            // Always set a new run id when we start consuming
            _correlationPersister.RunId = Guid.NewGuid();

            // Skip the internal error and audit messages, specially for audit messages, they will cause an endless loop!
            if (rawMessage.Properties.Type.StartsWith("Brunel.Messaging.EasyNetQ.")) { return rawMessage; }

            // Not auditing, get out now!
            if (!ShouldAudit()) { return rawMessage; }

            // Set the current correlationid on the current thread
            if (!rawMessage.Properties.CorrelationIdPresent)
            {
                _correlationPersister.CorrelationId = Guid.Empty;
                return rawMessage;
            }

            // Set the correlation id we got from the incoming message.
            _correlationPersister.CorrelationId = new Guid(rawMessage.Properties.CorrelationId);

            _logger.Debug("Found correlation id [{0}] in incoming message", _correlationPersister.CorrelationId);

            // Send an audit message to Rabbit.
            SendAuditMessage(rawMessage, Direction.In);

            return rawMessage;
        }

        public virtual string ConsumerProducerName => ConfigurationManager.AppSettings["Rabbit.ServiceName"];

        /// <summary>
        /// Checks if auditing is enabled in the config.
        /// </summary>
        /// <returns>True if the auditing key is set and hasd "true" in it, else false is returned.</returns>
        public virtual bool ShouldAudit() => ConfigurationManager.AppSettings["Rabbit.Management.EnableAuditing"] == "true";

        public virtual Guid GenerateNewCorrelationId()
        {
            return Guid.NewGuid();
        }

        private static Guid GetCorrelationFromRawMessage(RawMessage rawMessage)
        {
            // Because we send automatic interface message replies when things go wrong which is done on another thread we loose the correlation 
            // id (or might try to use an incorrect one). That is why we set an extra property when sending interface message replies
            // so we can detect we are allowed to re-use them. This functions tests that and returns a guid when it a correct one is there.
            if (!rawMessage.Properties.HeadersPresent
                || !rawMessage.Properties.Headers.ContainsKey("re-use-correlationid")
                || !(bool)rawMessage.Properties.Headers["re-use-correlationid"])
            {
                return Guid.Empty;
            }

            var id = Guid.Empty;
            if (rawMessage.Properties.CorrelationIdPresent) { return Guid.TryParse(rawMessage.Properties.CorrelationId, out id) ? id : Guid.Empty; }

            return Guid.Empty;
        }

        /// <summary>
        /// Composes the outgoing audit message to RabbitMQ. The handler has its own internal audit log receiver, see the setyup function in the service host.
        /// </summary>
        /// <param name="rawMessage">The raw message from RabbitMQ.</param>
        /// <param name="direction">The direction, either incoming or outgoing.</param>
        private void SendAuditMessage(RawMessage rawMessage, Direction direction)
        {
            // Connect to Rabbit
            Connect();

            using (var model = _connection.CreateModel())
            {
                // Set the queue name, [handler]_audit and exchange.
                var consumer = GetConsumerProducerName().Replace(" ", "_");
                var queue = $"{consumer}_audit";
                var exchange = $"AuditExchange_{consumer}";

                // Make sure we have an audit queue.
                CreatAuditQueue(model, queue, exchange);

                // Create the message and its properties. (this is the place to add stuff for auditing)
                var properties = model.CreateBasicProperties();
                properties.Persistent = true;
                properties.Type = _typeNameSerializer.Serialize(typeof(RabbitAuditMessage));

                var message = Encoding.UTF8.GetString(rawMessage.Body);
                var reference = GetReferenceCode(rawMessage.Properties.Type, message);

                var msg = _serializer.MessageToBytes(new RabbitAuditMessage
                {
                    ProducerName = rawMessage.Properties.AppIdPresent ? rawMessage.Properties.AppId : "Unknown",
                    Direction = direction,
                    CorrelationId = new Guid(rawMessage.Properties.CorrelationId),
                    Properties = rawMessage.Properties,
                    Message = message,
                    DateTime = DateTime.UtcNow,
                    RunId = _correlationPersister.RunId,
                    ReferenceCode = reference,
                });

                // Send the message
                model.BasicPublish(exchange, queue, properties, msg);
            }
        }

        private string GetReferenceCode(string messageType, string message)
        {
            // Try to get the reference code for auditing, but wrapped in a try catch
            var reference = string.Empty;

            try
            {
                // Split up the type and assembly
                var typespec = messageType.Split(':');

                // Only if we got both continue
                if (typespec.Length == 2)
                {
                    // Get the type
                    var ass = Assembly.Load(typespec[1]);

                    // Check if it implements IContainUniqueReference
                    var type = ass.GetTypes().FirstOrDefault(x => x.FullName == typespec[0]);

                    // If we found the type
                    if (type != null)
                    {
                        // Deserialize the message
                        var obj = JsonConvert.DeserializeObject(message, type);

                        // Try casting it
                        var casted = obj as IContainUniqueReference;

                        // and if its not null, get the code
                        if (casted != null)
                        {
                            // Get the unique reference code
                            reference = casted.UniqueReferenceCode;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error trying to get the IContainUniqueReference.");
            }

            return reference;
        }

        /// <summary>
        /// Gets the friendly name of the consumer/producer, either a website, app or handler.
        /// </summary>
        /// <returns>The friendly name of the consumer/producer. If not found we default to the app name.</returns>
        private string GetConsumerProducerName()
        {
            var handlerName = this.ConsumerProducerName;
            if (string.IsNullOrEmpty(handlerName))
            {
                _logger.Warn("No service name configured in 'Rabbit.ServiceName', please specify a service name for auditing purposes.");
                handlerName = Assembly.GetExecutingAssembly().GetName().Name;
            }
            return handlerName;
        }

        /// <summary>
        /// Connects to RabbitMQ.
        /// </summary>
        private void Connect()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = _connectionFactory.CreateConnection();
            }
        }

        /// <summary>
        /// Ensures we have an exchange and queue.
        /// </summary>
        /// <param name="model">The Rabbit Model to connect and create stuff.</param>
        /// <param name="queue">The name of the queue.</param>
        /// <param name="exchange">The name of the exchange.</param>
        private void CreatAuditQueue(IModel model, string queue, string exchange)
        {
            if (_auditqueueDeclared) return;

            model.QueueDeclare(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            model.ExchangeDeclare(exchange, ExchangeType.Direct, durable: true);
            model.QueueBind(queue, exchange, queue);

            _auditqueueDeclared = true;
        }

        /// <summary>
        /// Close the rabbit connection otherwise we leave it hanging (and the service/app will not nicely stop)
        /// </summary>
        public void Dispose()
        {
            if (_connection != null && _connection.IsOpen)
            {
                _connection.Close();
            }
        }
    }
}