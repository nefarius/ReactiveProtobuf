using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Libarius.Compression.QuickLZ;
using Libarius.Cryptography;
using ProtoBuf;
using ReactiveProtobuf.Protocol.Exceptions;
using ReactiveSockets;

namespace ReactiveProtobuf.Protocol
{
    public class ProtobufChannel<T> : IChannel<T>
    {
        private readonly string _encKey;
        private readonly bool _isCompressed;
        private readonly bool _isEncrypted;
        private readonly IReactiveSocket _socket;

        public ProtobufChannel(IReactiveSocket socket, bool isCompressed)
            : this(socket, isCompressed, false, null)
        {
        }

        public ProtobufChannel(IReactiveSocket socket, bool isCompressed = false, bool isEncrypted = false,
            string encKey = "")
        {
            _socket = socket;
            _isCompressed = isCompressed;
            _isEncrypted = isEncrypted;
            _encKey = encKey;

            Receiver = from header in socket.Receiver.Buffer(sizeof (int))
                let retval = BitConverter.ToInt32(header.ToArray(), 0)
                let length = (retval >= 0) ? retval : 0
                let body = socket.Receiver.Take(length)
                select CreateObject(body.ToEnumerable().ToArray());
        }

        public IObservable<T> Receiver { get; private set; }

        public Task SendAsync(T message)
        {
            return _socket.SendAsync(Convert(message));
        }

        private static byte[] GetBytes(string str)
        {
            var bytes = new byte[str.Length*sizeof (char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private T CreateObject(byte[] buffer)
        {
            var data = buffer;

            if (_isEncrypted)
            {
                try
                {
                    data = AesHelper.AesDecrypt(data, GetBytes(_encKey));
                }
                catch (CryptographicException)
                {
                    throw new ProtobufChannelEncryptionException(
                        "Object integrity invalid, maybe supplied wrong encryption key?");
                }
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
                data = AesHelper.AesEncrypt(data, GetBytes(_encKey));
            }

            var body = data;
            var header = BitConverter.GetBytes(body.Length);
            var payload = header.Concat(body).ToArray();

            return payload;
        }
    }
}