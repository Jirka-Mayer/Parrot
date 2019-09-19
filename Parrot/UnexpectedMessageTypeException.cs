using System;
using System.Runtime.Serialization;

namespace Parrot
{
    [Serializable]
    public class UnexpectedMessageTypeException : NetworkingException
    {
        public UnexpectedMessageTypeException() { }

        public UnexpectedMessageTypeException(
            string message
        ) : base(message) { }

        public UnexpectedMessageTypeException(
            string message, Exception inner
        ) : base(message, inner) { }

        protected UnexpectedMessageTypeException(
            SerializationInfo info,
            StreamingContext context
        ) : base(info, context) { }
    }
}