using System;
using System.Configuration;
using EasyNetQ;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Conventions
{
    /// <summary>
    /// Convention custumizations for EasynetQ. It changes the naming convention for error queues and coinsumer tags as used by EasyNetQ.
    /// </summary>
     public class CustomConventions : global::EasyNetQ.Conventions
    {
        public CustomConventions(ITypeNameSerializer typeNameSerializer)
            : base(typeNameSerializer)
        {
            // The naming conventions are based of the setting in the EasyNetQ.GenericHost.config file.
            var service = ConfigurationManager.AppSettings["Rabbit.ServiceName"] ?? string.Empty;

            if (string.IsNullOrEmpty(service))
            {
                base.ErrorExchangeNamingConvention = info => string.Format("ErrorExchange_{0}", info.ConsumerTag);
            }
            else
            {
                base.ErrorExchangeNamingConvention = info => string.Format("ErrorExchange_{0}", service.Replace(" ", "_"));
            }
            
            if (string.IsNullOrEmpty(service))
            {
                base.ConsumerTagConvention = () => Guid.NewGuid().ToString();
            }
            else
            {
                base.ConsumerTagConvention = () => service.Replace(" ", "_");
            }
        }
    }
}
