using System.Data.SqlClient;
using Dapper;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;
using Rbit.EasyNetQ.Interfaces;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.ErrorManagement
{
    /// <summary>
    /// This class acts as the error database store for the easynetq service.
    /// </summary>
    public class DatabaseErrorStore : ILogStore<RabbitErrorMessage>
    {
        /// <summary>
        /// The internal connection to the database.
        /// </summary>
        private readonly string _connection;

        /// <summary>
        /// Creates a new instance of the DatabaseErrorStore for persisting RabbitMQ exceptions. This overload takes the connection 
        /// string to the database as a parameter.
        /// </summary>
        /// <param name="connection"></param>
        public DatabaseErrorStore(string connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Save the RabbitMQ exception to the database.
        /// </summary>
        /// <param name="message">The exception message that is stored in the database.</param>
        /// <returns>The id of the record crreated for the error in the databse.</returns>
        public string Save(RabbitErrorMessage message)
        {
            using (var db = new SqlConnection(_connection))
            {

                db.Open();

                return db.ExecuteScalar(@"INSERT INTO [dbo].[DeadLetter]
                                ([DateTime]
                                ,[Server]
                                ,[VirtualHost]
                                ,[Exchange]
                                ,[Properties]
                                ,[Queue]
                                ,[Topic]
                                ,[Payload]
                                ,[Error]
                                ,[StackTrace]
                                ,[CorrelationId]
                                ,[Type]
                                ,[ConsumerTag]
                                ,[RoutingKey]
                                ,[RunId]
                                ,[MessageId])
                            VALUES
                                (@datetime
                                ,@server
                                ,@virtualhost
                                ,@exchange
                                ,@basicproperties
                                ,@queue
                                ,@topic
                                ,@payload
                                ,@error
                                ,@stacktrace
                                ,@correlationid
                                ,@type
                                ,@consumertag
                                ,@routingkey
                                ,@runid
                                ,@messageid);SELECT CAST(scope_identity() AS int)",
                    new
                    {
                        @datetime = message.DateTime,
                        @server = message.Server,
                        @virtualhost = message.VirtualHost,
                        @exchange = message.Exchange,
                        @basicproperties = message.Properties,
                        @queue = message.Queue,
                        @topic = message.Topic,
                        @payload = message.Payload,
                        @error = message.Error,
                        @stacktrace = message.StackTrace,
                        @correlationid = message.CorrelationId,
                        @type = message.Type,
                        @consumertag = message.ConsumerTag,
                        @routingkey = message.RoutingKey,
                        @runid = message.RunId,
                        @messageid = message.MessageId
                    }).ToString();
            }
        }
    }
}