using System;
using System.Configuration;
using EasyNetQ;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    /// <summary>
    /// Helper class that builds the rabbitMQ bus with the configured connection string
    /// </summary>
    public class BusBuilder
    {
        public IBus CreateMessageBus(Action<IServiceRegister> services)
        {
            if (string.IsNullOrEmpty(this.RabbitConnection))
            {
                throw new ConfigurationErrorsException("Missing required connection configuration for RabbitMQ. Please specify the connection in 'Rabbit.Connection' in the form 'host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=0'.");
            }
            
            return RabbitHutch.CreateBus(this.RabbitConnection, services);
        }

        public virtual string RabbitConnection => ConfigurationManager.AppSettings["Rabbit.Connection"];
    }
}