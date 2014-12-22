using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using ReactiveSockets;

namespace ReactiveProtobuf.Protocol
{
    public class ProtobufChannel : IChannel<object>
    {
        private IReactiveSocket _socket;
        private bool _isCompressed;
        private bool _isEncrypted;
        private SecureString _encKey;

        public ProtobufChannel(IReactiveSocket socket, bool isCompressed) : this(socket, isCompressed, false, null)
        { }

        public ProtobufChannel(IReactiveSocket socket, bool isCompressed = false, bool isEncrypted = false, SecureString encKey = null)
        {
            _socket = socket;
            _isCompressed = isCompressed;
            _isEncrypted = isEncrypted;
            _encKey = encKey;
        }

        public IObservable<object> Receiver
        {
            get { throw new NotImplementedException(); }
        }

        public System.Threading.Tasks.Task SendAsync(object message)
        {
            throw new NotImplementedException();
        }
    }
}
