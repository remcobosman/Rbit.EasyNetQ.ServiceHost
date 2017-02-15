using System;
using EasyNetQ.Interception;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interceptors
{
    /// <summary>
    /// Use this intercepter from code that sends messages. When adding auditing to consumer(/producers) i.e. handlers use the HandlerAuditIntercepter.
    /// </summary>
    public class ProducerAuditIntercepter : IProduceConsumeInterceptor
    {
        /// <summary>
        /// The name of the producer, set in the appid of the message when sending messages to the queue.
        /// </summary>
        private readonly string _producerName;

        /// <summary>
        /// Initializes an instance of the ProducerAudiIntercepter accepting the name of the producer, used in the AppId of a message when sending messages.
        /// </summary>
        /// <param name="producerName">The name of the producer used in the AppId of the message.</param>
        public ProducerAuditIntercepter(string producerName)
        {
            _producerName = producerName;
        }

        /// <summary>
        /// Addes a messageid and producer name (AppIs) to the message when sending.
        /// </summary>
        /// <param name="rawMessage">The raw message for RabbitMQ.</param>
        /// <returns>The message with extra the properties AppId and MessageId.</returns>
        public RawMessage OnProduce(RawMessage rawMessage)
        {
            // Set a new message id so we can recreate the order of send and received messages (we can trace an outgoing message corresponding to the incoming message by id)
            rawMessage.Properties.MessageId = Guid.NewGuid().ToString();

            // Set the appid to the application sending the mnessage
            rawMessage.Properties.AppId = _producerName;

            return rawMessage;
        }

        /// <summary>
        /// Returns the messages untainted.
        /// </summary>
        /// <param name="rawMessage">The RabbitMQ message.</param>
        /// <returns>The untainted RabbitMQ message.</returns>
        public RawMessage OnConsume(RawMessage rawMessage)
        {
            return rawMessage;
        }
    }
}