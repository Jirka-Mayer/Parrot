using System;
using System.Runtime.Serialization;

namespace Parrot
{
    [Serializable]
    public class ConnectionEndedException : NetworkingException
    {
        public ConnectionEndedException() { }
        
        public ConnectionEndedException(string message) : base(message) { }
        
        public ConnectionEndedException(
            string message, Exception inner
        ) : base(message, inner) { }
        
        protected ConnectionEndedException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context) { }
    }
}