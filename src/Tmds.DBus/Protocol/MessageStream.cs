/// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Transports;

namespace Tmds.DBus.Protocol
{
    class MessageStream : IMessageStream
    {
        private readonly Stream _stream;
        private readonly byte[] _headerReadBuffer = new byte[16];

        public static async Task<MessageStream> OpenAsync(AddressEntry entry, CancellationToken cancellationToken)
        {
            Transport transport = Transport.CreateTransport(entry);

            var stream = await transport.OpenAsync(cancellationToken);
            var messageStream = new MessageStream(stream);

            return messageStream;
        }

        internal MessageStream(Stream stream)
        {
            _stream = stream;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public async Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            int bytesRead = await ReadAsync(_headerReadBuffer, 0, 16);
            if (bytesRead == 0)
                return null;
            if (bytesRead != 16)
                throw new ProtocolException("Header read length mismatch: " + bytesRead + " of expected " + "16");

            EndianFlag endianness = (EndianFlag)_headerReadBuffer[0];
            MessageReader reader = new MessageReader(endianness, new ArraySegment<byte>(_headerReadBuffer));

            //discard endian byte, message type and flags, which we don't care about here
            reader.Seek(3);

            byte version = reader.ReadByte();
            if (version != ProtocolInformation.Version)
                throw new NotSupportedException("Protocol version '" + version.ToString() + "' is not supported");

            uint bodyLength = reader.ReadUInt32();

            //discard _methodSerial
            reader.ReadUInt32();

            uint headerLength = reader.ReadUInt32();

            int bodyLen = (int)bodyLength;
            int toRead = (int)headerLength;

            //we fixup to include the padding following the header
            toRead = ProtocolInformation.Padded(toRead, 8);

            long msgLength = toRead + bodyLen;
            if (msgLength > ProtocolInformation.MaxMessageLength)
                throw new ProtocolException("Message length " + msgLength + " exceeds maximum allowed " + ProtocolInformation.MaxMessageLength + " bytes");

            byte[] header = new byte[16 + toRead];
            Array.Copy(_headerReadBuffer, header, 16);
            bytesRead = await ReadAsync(header, 16, toRead);
            if (bytesRead != toRead)
                throw new ProtocolException("Message header length mismatch: " + bytesRead + " of expected " + toRead);

            byte[] body = null;
            //read the body
            if (bodyLen != 0)
            {
                body = new byte[bodyLen];

                bytesRead = await ReadAsync(body, 0, bodyLen);

                if (bytesRead != bodyLen)
                    throw new ProtocolException("Message body length mismatch: " + bytesRead + " of expected " + bodyLen);
            }

            Message msg = new Message()
            {
                Header = Header.FromBytes(new ArraySegment<byte>(header)),
                Body = body
            };

            return msg;
        }

        public virtual async Task SendMessageAsync(Message msg, CancellationToken cancellationToken)
        {
            var headerBytes = msg.Header.ToArray();
            await _stream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);

            if (msg.Body != null && msg.Body.Length != 0)
            {
                await _stream.WriteAsync(msg.Body, 0, msg.Body.Length, CancellationToken.None);
            }

            await _stream.FlushAsync(CancellationToken.None);
        }

        private async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int nread = await _stream.ReadAsync(buffer, offset + read, count - read);
                if (nread == 0)
                    break;
                read += nread;
            }
            return read;
        }
    }
}