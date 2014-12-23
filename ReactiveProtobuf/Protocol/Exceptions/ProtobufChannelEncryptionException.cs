using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveProtobuf.Protocol.Exceptions
{
    class ProtobufChannelEncryptionException : Exception
    {
        public ProtobufChannelEncryptionException(string message)
            : base(message)
        { }
    }
}
