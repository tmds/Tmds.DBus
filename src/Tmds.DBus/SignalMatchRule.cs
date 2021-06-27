// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System.Text;

namespace Tmds.DBus
{
    /// <summary>
    /// Match rules describe the messages that should be sent to a client,
    /// based on the contents of the message.
    /// </summary>
    public class SignalMatchRule
    {
        /// <summary>
        /// Match messages sent over or to a particular interface.
        /// </summary>
        public string Interface { get; set; }

        /// <summary>
        /// Match messages which have the give method or signal name.
        /// </summary>
        public string Member { get; set; }

        /// <summary>
        /// Match messages which are sent from or to the given object.
        /// </summary>
        public ObjectPath? Path { get; set; }

        /// <summary>
        /// Returns the hash code for this match rule.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (Interface == null ? 0 : Interface.GetHashCode());
            hash = hash * 23 + (Member == null ? 0 : Member.GetHashCode());
            hash = hash * 23 + Path.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object o)
        {
            SignalMatchRule r = o as SignalMatchRule;
            if (o == null)
                return false;

            return Interface == r.Interface &&
                Member == r.Member &&
                Path == r.Path;
        }

        /// <summary>
        /// Returns a string that represents the match rule.
        /// </summary>
        public override string ToString()
        {
            return ToStringWithSender(null);
        }

        private string ToStringWithSender(string sender)
        {
            StringBuilder sb = new StringBuilder();

            Append(sb, "type", "signal");

            if (Interface != null)
            {
                Append(sb, "interface", Interface);
            }
            if (Member != null)
            {
                Append(sb, "member", Member);
            }
            if (Path != null)
            {
                Append(sb, "path", Path.Value);
            }
            if (sender != null)
            {
                Append(sb, "sender", sender);
            }

            return sb.ToString();
        }

        internal static void Append(StringBuilder sb, string key, object value)
        {
            Append(sb, key, value.ToString());
        }

        static void Append(StringBuilder sb, string key, string value)
        {
            if (sb.Length != 0)
                sb.Append(',');

            sb.Append(key);
            sb.Append("='");
            sb.Append(value.Replace(@"\", @"\\").Replace(@"'", @"\'"));
            sb.Append('\'');
        }
    }
}
