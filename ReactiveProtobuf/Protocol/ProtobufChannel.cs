using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using ReactiveSockets;
using ProtoBuf;
using Libarius.Compression.QuickLZ;
using Libarius.Cryptography;
using System.Threading.Tasks;

namespace ReactiveProtobuf.Protocol
{
    public class ProtobufChannel<T> : IChannel<T>
    {
        private IReactiveSocket _socket;
        private readonly Type _targetType;
        private readonly bool _isCompressed;
        private readonly bool _isEncrypted;
        private readonly SecureString _encKey;

        public ProtobufChannel(IReactiveSocket socket, Type targetType, bool isCompressed)
            : this(socket, targetType, isCompressed, false, null)
        { }

        public ProtobufChannel(IReactiveSocket socket, Type targetType, bool isCompressed = false, bool isEncrypted = false, SecureString encKey = null)
        {
            _socket = socket;
            _targetType = targetType;
            _isCompressed = isCompressed;
            _isEncrypted = isEncrypted;
            _encKey = encKey;

            Receiver = from header in socket.Receiver.Buffer(sizeof(int))
                       let retval = BitConverter.ToInt32(header.ToArray(), 0)
                       let length = (retval >= 0) ? retval : 0
                       let body = socket.Receiver.Take(length)
                       select CreateObject(body.ToEnumerable().ToArray());
        }

        public IObservable<object> Receiver { get; private set; }

        public Task SendAsync(object message)
        {
            throw new NotImplementedException();
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private object CreateObject(byte[] buffer)
        {
            byte[] data = buffer;

            if (_isEncrypted)
            {
                data = AesHelper.AesDecrypt(data, GetBytes(_encKey.ToString()));
            }

            if (_isCompressed)
            {
                data = QuickLZ.decompress(data);
            }

            using (var ms = new MemoryStream(data))
            {
                MethodInfo method = typeof(Serializer).GetMethod("Deserialize");
                MethodInfo generic = method.MakeGenericMethod(_targetType);
                generic.Invoke(null, new object[] { ms });
                Serializer.Deserialize<T>(ms);
            }

            return null;
        }

        IObservable<T> IChannel<T>.Receiver
        {
            get { throw new NotImplementedException(); }
        }

        public Task SendAsync(T message)
        {
            throw new NotImplementedException();
        }
    }
}
