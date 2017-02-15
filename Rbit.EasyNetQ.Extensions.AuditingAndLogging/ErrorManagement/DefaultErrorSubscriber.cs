using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;
using Newtonsoft.Json;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;
using Rbit.EasyNetQ.Interfaces;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.ErrorManagement
{
    public class DefaultErrorSubscriber
    {
        private readonly ILogStore<RabbitErrorMessage> _store;
        private readonly ILogStore<RabbitErrorMessage> _fallbackStore;
        private readonly ILogger _logger;
        private readonly IBus _bus;

        public DefaultErrorSubscriber(ILogStore<RabbitErrorMessage> store, ILogStore<RabbitErrorMessage> fallbackStore, ILogger logger, IBus bus)
        {
            _store = store;
            _fallbackStore = fallbackStore;
            _logger = logger;
            _bus = bus;
        }

        /// <summary>
        /// Handles an error message by saving it in the erro rdatabase/table.
        /// </summary>
        /// <param name="message">The Rabbit error message.</param>
        /// <returns>The identoifier of the saved error. In this case the record number in the table.</returns>
        public Task HandleDefaultErrorMessage(IMessage<RabbitErrorMessage> message)
        {
            var task = Task.Run(
                () =>
                {
                    try
                    {
                        var errorId = _store.Save(message.Body);
                        _logger.Debug($"Saved error for correlation id {message.Body.CorrelationId} with error id {errorId}." );
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error trying to save exception in store: [{0}], trying to use the fallback store: [{1}]", _store.GetType().Name, _fallbackStore.GetType().Name);

                        try
                        {
                            var errorId = _fallbackStore.Save(message.Body);
                            _logger.Debug($"Saved in the fallback error store error for correlation id {message.Body.CorrelationId} with error id {errorId}.");
                        }
                        catch (Exception ex2)
                        {
                            _logger.Error(ex2, "Error trying to save exception in the fallbackstore of type: [{0}], will silently continue without saving. The original exception message was: [{1}]", _fallbackStore.GetType().Name, JsonConvert.SerializeObject(message.Body));
                        }
                    }
                });

            return task;
        }
    }
}
