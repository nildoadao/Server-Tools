using System;
using System.Runtime.Serialization;


namespace Server_Tools
{
    
    /// <summary>
    /// Exeção gerada quando um comando racadm retorna != 0 ou ERROR
    /// </summary>
    class RacadmException : Exception
    {
        public RacadmException(){}

        public RacadmException(string message) : base(message){}

        public RacadmException(string message, Exception innerException) : base(message, innerException){}

        protected RacadmException(SerializationInfo info, StreamingContext context) : base(info, context){}
    }
}
