// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus.Protocol
{
    internal class Message
    {
        private Header _header;
        private byte[] _body;

        public Message ()
        {}

        public byte[] Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                if (_header != null)
                {
                    _header.Length = _body != null ? (uint)_body.Length : 0;
                }
            }
        }

        public Header Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
                if ((_body != null) && (_header != null))
                {
                    _header.Length = (uint)_body.Length;
                }
            }
        }
    }
}
