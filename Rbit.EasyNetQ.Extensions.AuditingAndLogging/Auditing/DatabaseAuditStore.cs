using System.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;
using Rbit.EasyNetQ.Interfaces;
using Rbit.EasyNetQ.Interfaces.Enums;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Auditing
{
    public class DatabaseAuditStore : ILogStore<RabbitAuditMessage>
    {
        private readonly string _connection;
        private readonly string _handlerName;
        private readonly ILogger _logger;

        public DatabaseAuditStore(string connection, string handlerName, ILogger logger)
        {
            _connection = connection;
            _handlerName = handlerName;
            _logger = logger;
        }

        public string Save(RabbitAuditMessage message)
        {
            using (var db = new SqlConnection(_connection))
            {

                db.Open();

                return db.ExecuteScalar(@"INSERT INTO [dbo].[AuditLog]
                               ([ProducerName]
                               ,[HandlerName]
                               ,[CorrelationId]
                               ,[Type]
                               ,[TypeAssembly]
                               ,[Direction]
                               ,[Properties]
                               ,[Payload]
                               ,[DateTime]
                               ,[MessageId]
                               ,[RunId]
                                ,[ReferenceCode])
                         VALUES
                               (@producername
                               ,@handlername
                               ,@correlationid
                               ,@type
                               ,@assembly
                               ,@direction
                               ,@properties
                               ,@payload
                               ,@datetime
                               ,@messageid
                               ,@runid
                               ,@referencecode);SELECT CAST(scope_identity() AS int)",
                    new
                    {
                        @producername = message.ProducerName.Replace("_", " "),
                        @handlername = _handlerName,
                        @direction = message.Direction == Direction.In ? "Incoming" : "Outgoing",
                        @properties = JsonConvert.SerializeObject(message.Properties),
                        @payload = message.Message,
                        @datetime = message.DateTime,
                        @correlationid = message.CorrelationId,
                        @messageid = message.Properties.MessageId,
                        @type = message.Properties.Type,
                        @assembly = message.Properties.Type.Split(':')[1],
                        @runid = message.RunId,
                        @referencecode = message.ReferenceCode,
                    }).ToString();
            }
        }
    }
}