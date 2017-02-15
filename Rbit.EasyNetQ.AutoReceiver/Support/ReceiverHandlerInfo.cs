using System;
using System.Reflection;

namespace Rbit.EasyNetQ.AutoReceiver.Support
{
    public class ReceiverHandlerInfo
    {
        public MethodInfo AddHandler { get; set; }
        public Delegate Handler { get; set; }

    }
}