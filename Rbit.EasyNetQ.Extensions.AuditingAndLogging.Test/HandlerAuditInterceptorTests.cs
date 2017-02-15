using System;
using System.Collections.Generic;
using System.Text;
using EasyNetQ;
using EasyNetQ.Interception;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ninject.Extensions.Logging;
using RabbitMQ.Client;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Auditing;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interceptors;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;
using Rbit.EasyNetQ.Interfaces.Enums;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;
using IConnectionFactory = EasyNetQ.IConnectionFactory;

namespace Brunel.Messaging.EasyNetQ.Extensions.Test
{
    [TestClass]
    public class HandlerAuditInterceptorTests
    {
        [TestMethod]
        public void When_A_New_Message_Without_CorrelationId_Is_Produced_We_Need_To_Set_A_New_CorrelationId()
        {
            // OnProduce should fire and set the correct correlation id
            var intercepter = Mock.Create<HandlerAuditIntercepter>(
                Mock.Create<ILogger>(),
                Mock.Create<IConnectionFactory>(),
                Mock.Create<ISerializer>(),
                Mock.Create<ITypeNameSerializer>(),
                Mock.Create<IPersistCorrelation>());

            Mock.Arrange(() => intercepter.OnProduce(Arg.IsAny<RawMessage>())).CallOriginal();
            Mock.Arrange(() => intercepter.ShouldAudit()).Returns(true);
            Mock.Arrange(() => intercepter.GenerateNewCorrelationId()).CallOriginal().OccursOnce("No new correlation id was requested");

            var rawMessage = new RawMessage(
                new MessageProperties()
                {
                    TypePresent = true,
                    Type = "MyAssembly"
                }, Encoding.UTF8.GetBytes("my message"));

            var t = intercepter.OnProduce(rawMessage);

            Assert.IsTrue(t.Properties.CorrelationIdPresent, "Missing correlation id present property.");
            Assert.AreNotEqual(Guid.Empty.ToString(), t.Properties.CorrelationId, "No new correlation id was found.");
            Mock.Assert(intercepter);
        }

        [TestMethod]
        public void When_A_New_Message_After_Consuming_An_Incoming_Message_Is_Produced_We_Need_To_Reuse_That_CorrelationId()
        {
            var correlationId = Guid.NewGuid();

            var correlationPersister = Mock.Create<IPersistCorrelation>();

            // OnProduce should fire and set the correct correlation id
            var intercepter = Mock.Create<HandlerAuditIntercepter>(
                Mock.Create<ILogger>(),
                Mock.Create<IConnectionFactory>(),
                Mock.Create<ISerializer>(),
                Mock.Create<ITypeNameSerializer>(),
                correlationPersister);

            Mock.Arrange(() => correlationPersister.CorrelationId).Returns(correlationId);

            Mock.Arrange(() => intercepter.OnProduce(Arg.IsAny<RawMessage>())).CallOriginal();
            Mock.Arrange(() => intercepter.ShouldAudit()).Returns(true);
            Mock.Arrange(() => intercepter.GenerateNewCorrelationId()).CallOriginal().OccursNever("There should not be a request for a new correlation id if one is present.");

            var rawMessage = new RawMessage(
                new MessageProperties()
                {
                    TypePresent = true,
                    Type = "MyAssembly",
                }, Encoding.UTF8.GetBytes("my message"));

            var t = intercepter.OnProduce(rawMessage);

            Assert.IsTrue(t.Properties.CorrelationIdPresent, "Missing correlation id present property.");
            Assert.AreEqual(correlationId.ToString(), t.Properties.CorrelationId, "An incorrect correlation id was found.");
            Mock.Assert(intercepter);
        }

        [TestMethod]
        public void When_A_New_Message_After_Consuming_An_Incoming_Message_Is_Produced_Which_Has_A_CorrelationId_Set_We_Should_Not_Reuse_That_CorrelationId()
        {
            var correlationId = Guid.NewGuid();
            var inMessageCorrelationId = Guid.NewGuid();

            var correlationPersister = Mock.Create<IPersistCorrelation>();

            // OnProduce should fire and set the correct correlation id
            var intercepter = Mock.Create<HandlerAuditIntercepter>(
                Mock.Create<ILogger>(),
                Mock.Create<IConnectionFactory>(),
                Mock.Create<ISerializer>(),
                Mock.Create<ITypeNameSerializer>(),
                correlationPersister);

            Mock.Arrange(() => correlationPersister.CorrelationId).Returns(correlationId);

            Mock.Arrange(() => intercepter.OnProduce(Arg.IsAny<RawMessage>())).CallOriginal();
            Mock.Arrange(() => intercepter.ShouldAudit()).Returns(true);
            Mock.Arrange(() => intercepter.GenerateNewCorrelationId()).CallOriginal().OccursNever("There should not be a request for a new correlation id if one is present.");

            var rawMessage = new RawMessage(
                new MessageProperties()
                {
                    TypePresent = true,
                    Type = "MyAssembly",
                    CorrelationIdPresent = true,
                    CorrelationId = inMessageCorrelationId.ToString(),
                }, Encoding.UTF8.GetBytes("my message"));

            var t = intercepter.OnProduce(rawMessage);

            Assert.IsTrue(t.Properties.CorrelationIdPresent, "Missing correlation id present property.");
            Assert.AreNotEqual(inMessageCorrelationId.ToString(), t.Properties.CorrelationId, "The correlationid should not be reused.");
            Mock.Assert(intercepter);
        }

        [TestMethod]
        public void When_A_New_Message_After_Consuming_An_Incoming_Message_Is_Produced_Which_Has_A_CorrelationId_Set_And_Marked_For_Reuse_We_Should_Reuse_That_CorrelationId()
        {
            var correlationId = Guid.NewGuid();
            var inMessageCorrelationId = Guid.NewGuid();

            var correlationPersister = Mock.Create<IPersistCorrelation>();

            // OnProduce should fire and set the correct correlation id
            var intercepter = Mock.Create<HandlerAuditIntercepter>(
                Mock.Create<ILogger>(),
                Mock.Create<IConnectionFactory>(),
                Mock.Create<ISerializer>(),
                Mock.Create<ITypeNameSerializer>(),
                correlationPersister);

            Mock.Arrange(() => correlationPersister.CorrelationId).Returns(correlationId);

            Mock.Arrange(() => intercepter.OnProduce(Arg.IsAny<RawMessage>())).CallOriginal();
            Mock.Arrange(() => intercepter.ShouldAudit()).Returns(true);
            Mock.Arrange(() => intercepter.GenerateNewCorrelationId()).CallOriginal().OccursNever("There should not be a request for a new correlation id if one is present.");

            var rawMessage = new RawMessage(
                new MessageProperties()
                {
                    TypePresent = true,
                    Type = "MyAssembly",
                    CorrelationIdPresent = true,
                    CorrelationId = inMessageCorrelationId.ToString(),
                    HeadersPresent = true,
                    Headers = new Dictionary<string, object>() { { "re-use-correlationid", true } },
                }, Encoding.UTF8.GetBytes("my message"));

            var t = intercepter.OnProduce(rawMessage);

            Assert.IsTrue(t.Properties.CorrelationIdPresent, "Missing correlation id present property.");
            Assert.AreEqual(inMessageCorrelationId.ToString(), t.Properties.CorrelationId, "The correlationid should be reused.");
            Mock.Assert(intercepter);
        }

        [TestMethod]
        public void When_Producing_A_Message_The_Producer_Name_Should_Be_Set()
        {
            // OnProduce should fire and set the correct correlation id
            var intercepter = Mock.Create<HandlerAuditIntercepter>(
                Mock.Create<ILogger>(),
                Mock.Create<IConnectionFactory>(),
                Mock.Create<ISerializer>(),
                Mock.Create<ITypeNameSerializer>(),
                Mock.Create<IPersistCorrelation>());

            Mock.Arrange(() => intercepter.ConsumerProducerName).Returns("TestProducer");
            Mock.Arrange(() => intercepter.OnProduce(Arg.IsAny<RawMessage>())).CallOriginal();
            Mock.Arrange(() => intercepter.ShouldAudit()).Returns(true);

            var rawMessage = new RawMessage(
                new MessageProperties()
                {
                    TypePresent = true,
                    Type = "MyAssembly"
                }, Encoding.UTF8.GetBytes("my message"));

            var t = intercepter.OnProduce(rawMessage);

            Assert.IsTrue(t.Properties.AppIdPresent, "Missing app id present property.");
            Assert.AreEqual("TestProducer", t.Properties.AppId, "No new correlation id was found.");
            Mock.Assert(intercepter);
        }

        [TestMethod]
        public void When_Producing_A_Message_And_Auditing_Is_Off_No_Work_Should_Be_Done()
        {
            // OnProduce should fire and set the correct correlation id
            var intercepter = Mock.Create<HandlerAuditIntercepter>(
                Mock.Create<ILogger>(),
                Mock.Create<IConnectionFactory>(),
                Mock.Create<ISerializer>(),
                Mock.Create<ITypeNameSerializer>(),
                Mock.Create<IPersistCorrelation>());

            Mock.Arrange(() => intercepter.OnProduce(Arg.IsAny<RawMessage>())).CallOriginal();
            Mock.Arrange(() => intercepter.ShouldAudit()).Returns(false);

            var rawMessage = new RawMessage(
                new MessageProperties()
                {
                    TypePresent = true,
                    Type = "MyAssembly"
                }, Encoding.UTF8.GetBytes("my message"));

            var t = intercepter.OnProduce(rawMessage);

            Assert.IsFalse(t.Properties.CorrelationIdPresent, "The message should not have been modified.");
            Mock.Assert(intercepter);
        }

        [TestMethod]
        public void When_Producing_A_Message_And_The_Type_Is_An_Internal_Message_No_Work_Should_Be_Done()
        {
            // OnProduce should fire and set the correct correlation id
            var intercepter = Mock.Create<HandlerAuditIntercepter>(
                Mock.Create<ILogger>(),
                Mock.Create<IConnectionFactory>(),
                Mock.Create<ISerializer>(),
                Mock.Create<ITypeNameSerializer>(),
                Mock.Create<IPersistCorrelation>());

            Mock.Arrange(() => intercepter.OnProduce(Arg.IsAny<RawMessage>())).CallOriginal();
            Mock.Arrange(() => intercepter.ShouldAudit()).Returns(false);

            var rawMessage = new RawMessage(
                new MessageProperties()
                {
                    TypePresent = true,
                    Type = "Brunel.Messaging.EasyNetQ."
                }, Encoding.UTF8.GetBytes("my message"));

            var t = intercepter.OnProduce(rawMessage);

            Assert.IsFalse(t.Properties.CorrelationIdPresent, "The message should not have been modified.");
            Mock.Assert(intercepter);
        }

        [TestMethod]
        public void When_Producing_A_Message_An_Audit_Message_Should_Be_Sent()
        {
            // Setup the correlation is to test
            var correlationId = Guid.NewGuid();

            // Create a serializer used to create the message bytes and visa versa
            var serializer = new JsonSerializer(new TypeNameSerializer());

            // Create a model, actually this is the bus for easynetq
            var model = Mock.Create<IModel>();

            // Arrange the basic publish to see if we actually send out a message with the correlationid, direction and producer name
            Mock.Arrange(() => model.BasicPublish(
                "AuditExchange_TestProducer",
                "TestProducer_audit",
                Arg.IsAny<IBasicProperties>(),
                Arg.Matches<byte[]>(m => 
                    serializer.BytesToMessage<RabbitAuditMessage>(m).Direction == Direction.Out 
                    && serializer.BytesToMessage<RabbitAuditMessage>(m).CorrelationId == correlationId
                    && serializer.BytesToMessage<RabbitAuditMessage>(m).ProducerName == "TestProducer")))
            .Occurs(1, "No audit message was sent to the queue");

            // Let the connection return a model to publish to
            var connection = Mock.Create<IConnection>();
            Mock.Arrange(() => connection.CreateModel()).Returns(model);

            // Let the connection factory return a connection to rabbitmq
            var connectionFactory = Mock.Create<IConnectionFactory>();
            Mock.Arrange(() => connectionFactory.CreateConnection()).Returns(connection);

            // OnProduce should fire and set the correct correlation id
            var intercepter = Mock.Create<HandlerAuditIntercepter>(
                Mock.Create<ILogger>(),
                connectionFactory,
                serializer,
                Mock.Create<ITypeNameSerializer>(),
                Mock.Create<IPersistCorrelation>());

            // We need the correct consumer name and correlation id so we can verify
            Mock.Arrange(() => intercepter.ConsumerProducerName).Returns("TestProducer");
            Mock.Arrange(() => intercepter.GenerateNewCorrelationId()).Returns(correlationId);
            Mock.Arrange(() => intercepter.OnProduce(Arg.IsAny<RawMessage>())).CallOriginal();
            Mock.Arrange(() => intercepter.ShouldAudit()).Returns(true);

            var rawMessage = new RawMessage(
                new MessageProperties()
                {
                    TypePresent = true,
                    Type = "MyType"
                }, Encoding.UTF8.GetBytes("my message"));

            intercepter.OnProduce(rawMessage);

            Mock.Assert(model);
        }
    }
}
