using System;
using System.Threading.Tasks;
using EasyNetQ;
using Newtonsoft.Json;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;
using Rbit.EasyNetQ.Interfaces;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Auditing
{
    /// <summary>
    /// This is the code for the audit subscriber, i.e. the handler processing audit messages for this handler executable from rabbit.
    /// </summary>
    public class DefaultAuditSubscriber
    {
        /// <summary>
        /// The primary store to write audit records to (usually the database).
        /// </summary>
        private readonly ILogStore<RabbitAuditMessage> _store;

        /// <summary>
        /// A fallback store to write to when the primary fails (usually a file store).
        /// </summary>
        private readonly ILogStore<RabbitAuditMessage> _fallbackStore;

        /// <summary>
        /// The usual logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiate a new instance of the DefaultAuditSubscriber class. This constructor takes the primary store, fallback store and a loger.
        /// </summary>
        /// <param name="store">The primary store to write to.</param>
        /// <param name="fallbackStore">The fallback store to write to.</param>
        /// <param name="logger">The logger.</param>
        public DefaultAuditSubscriber(ILogStore<RabbitAuditMessage> store, ILogStore<RabbitAuditMessage> fallbackStore, ILogger logger)
        {
            _store = store;
            _fallbackStore = fallbackStore;
            _logger = logger;
        }

        /// <summary>
        /// Handles autit messages from for the current handler.
        /// </summary>
        /// <param name="message">The audit message.</param>
        /// <returns>An asyncrounous task.</returns>
        public Task HandleAuditMessage(IMessage<RabbitAuditMessage> message)
        {
            var task = Task.Run(
                () =>
                {
                    try
                    {
                        _store.Save(message.Body);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error trying to save audit record in store: [{0}], trying to use the fallback store: [{1}]", _store.GetType().Name, _fallbackStore.GetType().Name);

                        try
                        {
                            _fallbackStore.Save(message.Body);
                        }
                        catch (Exception ex2)
                        {
                            _logger.Error(ex2, "Error trying to save audit record in the fallbackstore of type: [{0}], will silently continue without saving. The original exception message was: [{1}]", _fallbackStore.GetType().Name, JsonConvert.SerializeObject(message.Body));
                        }
                    }
                });

            return task;
        }
    }
}