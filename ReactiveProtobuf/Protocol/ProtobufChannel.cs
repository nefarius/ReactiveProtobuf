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
    /// <summary>
    ///     Implements the <see cref="IChannel{T}" /> over ReactiveSockets.
    /// </summary>
    /// <typeparam name="T">The type to get (de-)serialized.</typeparam>
    public class ProtobufChannel<T> : IChannel<T>
    {
        private readonly string _encKey;
        private readonly bool _isCompressed;
        private readonly bool _isEncrypted;
        private readonly IReactiveSocket _socket;

        /// <summary>
        ///     Initializes a new protocol channel.
        /// </summary>
        /// <param name="socket">The <see cref="IReactiveSocket" /> to subscribe to.</param>
        /// <param name="isCompressed">
        ///     True to compress the serialized data, false otherwise.
        ///     The default value is false.
        /// </param>
        public ProtobufChannel(IReactiveSocket socket, bool isCompressed)
            : this(socket, isCompressed, false, null)
        {
        }

        /// <summary>
        ///     Initializes a new protocol channel.
        /// </summary>
        /// <param name="socket">The <see cref="IReactiveSocket" /> to subscribe to.</param>
        /// <param name="isCompressed">
        ///     True to compress the serialized data, false otherwise.
        ///     The default value is false.
        /// </param>
        /// <param name="isEncrypted">
        ///     True to encrypt the serialized data with a static key.
        ///     The default value is false.
        /// </param>
        /// <param name="encKey">The static key being used to encrypt/decrypt.</param>
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
                select Deserialize(body.ToEnumerable().ToArray());
        }

        /// <summary>
        ///     The receiving channel to subscribe to.
        /// </summary>
        /// <example>
        ///     protocol.Receiver.Subscribe(person =>
        ///     {
        ///     if (person != null)
        ///     {
        ///     Console.WriteLine("Person {0} {1} connected", person.FirstName, person.LastName);
        ///     }
        ///     });
        /// </example>
        public IObservable<T> Receiver { get; private set; }

        /// <summary>
        ///     Sends the provided message to all subscribed channels.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The async task.</returns>
        public Task SendAsync(T message)
        {
            return _socket.SendAsync(Serialize(message));
        }

        private static byte[] GetBytes(string str)
        {
            var bytes = new byte[str.Length*sizeof (char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private T Deserialize(byte[] buffer)
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

        private byte[] Serialize(T obj)
        {
            byte[] data;

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                data = ms.ToArray();
            }

            if (_isCompressed)
            {
                data = QuickLZ.compress(data, 3);
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