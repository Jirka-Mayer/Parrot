using System;
using System.Runtime.Serialization;

namespace Parrot
{
    [Serializable]
    public class NetworkingException : Exception
    {
        public NetworkingException() { }
        
        public NetworkingException(string message) : base(message) { }
        
        public NetworkingException(
            string message, Exception inner
        ) : base(message, inner) { }
        
        protected NetworkingException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context) { }
    }
}