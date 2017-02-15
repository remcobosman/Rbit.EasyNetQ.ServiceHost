using System;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;
using Rbit.EasyNetQ.Interfaces;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.ErrorManagement
{
    /// <summary>
    /// Writes errors to the console (used for testing).
    /// </summary>
    public class ConsoleStore : ILogStore<IRabbitErrorMessage>
    {
        public string Save(IRabbitErrorMessage message)
        {
            Console.WriteLine("error: {0} received at: {1} with correlation id: {2}", message.Error, message.ConsumerTag,message.CorrelationId );

            return "console written line";
        }
    }
}