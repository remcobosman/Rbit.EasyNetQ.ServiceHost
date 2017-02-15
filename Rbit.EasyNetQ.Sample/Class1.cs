using System;
using Rbit.EasyNetQ.AutoReceiver.Interfaces;

namespace Rbit.EasyNetQ.Sample
{
    public class SampleReceiver : IReceive<string>
    {
        public void Receive(string message)
        {
            throw new NotImplementedException();
        }
    }
}
