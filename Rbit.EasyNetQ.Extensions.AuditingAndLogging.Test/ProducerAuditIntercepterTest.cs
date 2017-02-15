using System;
using System.Text;
using EasyNetQ;
using EasyNetQ.Interception;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interceptors;
using Telerik.JustMock;

namespace Brunel.Messaging.EasyNetQ.Extensions.Test
{
    [TestClass]
    public class ProducerAuditIntercepterTest
    {
        [TestMethod]
        public void When_A_Producer_Is_Set_It_Should_Be_Stored_In_The_Message_Property()
        {
            // OnProduce should fire and set the correct correlation id
            var intercepter = Mock.Create<ProducerAuditIntercepter>("TestProducer");

            Mock.Arrange(() => intercepter.OnProduce(Arg.IsAny<RawMessage>())).CallOriginal();

            var rawMessage = new RawMessage(
                new MessageProperties()
                {
                    TypePresent = true,
                    Type = "MyAssembly"
                }, Encoding.UTF8.GetBytes("my message"));

            var t = intercepter.OnProduce(rawMessage);

            Assert.IsTrue(t.Properties.MessageIdPresent, "Missing message id present property.");
            Assert.AreNotEqual(Guid.Empty.ToString(), t.Properties.MessageId, "Incorrect message id set.");

            Assert.IsTrue(t.Properties.AppIdPresent, "Missing app id present property.");
            Assert.AreEqual("TestProducer", t.Properties.AppId, "Incorrect app id set.");
        }
    }
}
