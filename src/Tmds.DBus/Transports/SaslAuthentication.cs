// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Transports
{
    internal class SaslAuthentication
    {
        class AuthCommand
        {
            public readonly string Value;
            private readonly List<string> _args = new List<string>();

            public AuthCommand(string value)
            {
                this.Value = value.Trim();
                _args.AddRange(Value.Split(' '));
            }

            public string this[int index]
            {
                get
                {
                    if (index >= _args.Count)
                        return String.Empty;
                    return _args[index];
                }
            }
        }

        private readonly Stream _stream;
        private Guid _guid;

        public SaslAuthentication(Stream stream)
        {
            _stream = stream;
        }

        public async Task<Guid> AuthenticateAsync(CancellationToken cancellationToken)
        {
            byte[] bs = Encoding.ASCII.GetBytes(Environment.UserId);
            string initialData = ToHex(bs);
            var commands = new[]
            {
                "AUTH EXTERNAL " + initialData,
                "AUTH ANONYMOUS"
            };

            foreach (var command in commands)
            {
                if (await AuthenticateAsync(command, cancellationToken))
                {
                    return _guid;
                }
            }

            throw new ConnectionException("Authentication failure");
        }

        private async Task<bool> AuthenticateAsync(string command, CancellationToken cancellationToken)
        {
            await WriteLineAsync(command, cancellationToken);
            AuthCommand reply = await ReadReplyAsync(cancellationToken);

            if (reply[0] == "OK")
            {
                _guid = reply[1] != string.Empty ? Guid.ParseExact(reply[1], "N") : Guid.Empty;
                await WriteLineAsync("BEGIN", cancellationToken);
                return true;
            }
            else if (reply[0] == "REJECTED")
            {
                return false;
            }
            else
            {
                await WriteLineAsync("ERROR", cancellationToken);
                return false;
            }
        }

        private async Task WriteLineAsync(string message, CancellationToken cancellationToken)
        {
            message += "\r\n";
            var bytes = Encoding.ASCII.GetBytes(message);
            await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }

        private async Task<AuthCommand> ReadReplyAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1];
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                int length = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                byte b = buffer[0];
                if (length == 0)
                {
                    throw new DisconnectedException();
                }
                else if (b == '\r')
                {
                    length = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    b = buffer[0];
                    if (b == '\n')
                    {
                        string ln = sb.ToString();
                        if (ln != string.Empty)
                        {
                            return new AuthCommand(ln);
                        }
                        else
                        {
                            throw new ProtocolException("Received empty authentication message from server");
                        }
                    }
                    throw new ProtocolException("Authentication messages from server must end with '\\r\\n'");
                }
                else
                {
                    sb.Append((char) b);
                }
            }
        }

        static private string ToHex(byte[] input)
        {
            StringBuilder result = new StringBuilder(input.Length * 2);
            string alfabeth = "0123456789abcdef";

            foreach (byte b in input)
            {
                result.Append(alfabeth[(int)(b >> 4)]);
                result.Append(alfabeth[(int)(b & 0xF)]);
            }

            return result.ToString();
        }
    }
}
