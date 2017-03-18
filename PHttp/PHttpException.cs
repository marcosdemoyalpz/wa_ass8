using System;
using System.Runtime.Serialization;

namespace PHttp
{
    [Serializable]
    public class PHttpException : Exception
    {
        public PHttpException()
        {
        }

        public PHttpException(string message)
            : base(message)
        {
        }

        public PHttpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected PHttpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
