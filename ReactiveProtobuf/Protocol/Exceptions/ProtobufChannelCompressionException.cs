using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveProtobuf.Protocol.Exceptions
{
    class ProtobufChannelCompressionException : Exception
    {
        public ProtobufChannelCompressionException(string message)
            : base(message)
        { }
    }
}
