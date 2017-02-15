using System;

namespace Rbit.EasyNetQ.AutoResponder.Support
{
    public class ResponderHandlerInfo
    {
        public readonly Type ConcreteType;
        public readonly Type RequestType;
        public readonly Type RespondType;

        public ResponderHandlerInfo(Type concreteType, Type requestType, Type respondType)
        {
            ConcreteType = concreteType;
            RequestType = requestType;
            RespondType = respondType;
        }
    }
}