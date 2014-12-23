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
        private readonly bool _isCompressed;
        private readonly bool _isEncrypted;
        private readonly SecureString _encKey;

        public ProtobufChannel(IReactiveSocket socket, bool isCompressed)
            : this(socket, isCompressed, false, null)
        { }

        public ProtobufChannel(IReactiveSocket socket, bool isCompressed = false, bool isEncrypted = false, SecureString encKey = null)
        {
            _socket = socket;
            _isCompressed = isCompressed;
            _isEncrypted = isEncrypted;
            _encKey = encKey;

            Receiver = from header in socket.Receiver.Buffer(sizeof(int))
                       let retval = BitConverter.ToInt32(header.ToArray(), 0)
                       let length = (retval >= 0) ? retval : 0
                       let body = socket.Receiver.Take(length)
                       select CreateObject(body.ToEnumerable().ToArray());
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private T CreateObject(byte[] buffer)
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
                return Serializer.Deserialize<T>(ms);
            }
        }

        public IObservable<T> Receiver { get; private set; }

        public Task SendAsync(T message)
        {
            return _socket.SendAsync(Convert(message));
        }

        private byte[] Convert(T obj)
        {
            byte[] data;

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                data = ms.ToArray();
            }

            if (_isCompressed)
            {
                data = QuickLZ.compress(data, 1);
            }

            if (_isEncrypted)
            {
                data = AesHelper.AesEncrypt(data, GetBytes(_encKey.ToString()));
            }

            var body = data;
            var header = BitConverter.GetBytes(body.Length);
            var payload = header.Concat(body).ToArray();

            return payload;
        }
    }
}
