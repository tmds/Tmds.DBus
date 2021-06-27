// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// Copyright 2021 David Lechner <david@lechnology.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using Tmds.DBus.Protocol;

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
        /// Match messages which are sent from or to an object for which the 
        /// object path is either the given value, or that value followed by one
        /// or more path components. 
        /// </summary>
        public ObjectPath? PathNamespace { get; set; }

        /// <summary>
        /// Arg matches are special and are used for further restricting the
        /// match based on the arguments in the body of a message.
        /// </summary>
        public  (int index, string arg)[] Args { get; set; }

        /// <summary>
        /// Argument path matches provide a specialized form of wildcard
        /// matching for path-like namespaces.
        /// </summary>
        public (int index, string argPath)[] ArgPaths { get; set; }

        /// <summary>
        /// Match messages whose first argument is of type STRING, and is a bus
        /// name or interface name within the specified namespace.
        /// </summary>
        public string Arg0Namespace { get; set; }

        /// <summary>
        /// Returns the hash code for this match rule.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (Interface == null ? 0 : Interface.GetHashCode());
            hash = hash * 23 + (Member == null ? 0 : Member.GetHashCode());
            hash = hash * 23 + Path.GetHashCode();
            hash = hash * 23 + PathNamespace.GetHashCode();
            hash = hash * 23 + (Args == null ? 0 : Args.GetHashCode());
            hash = hash * 23 + (ArgPaths == null ? 0 : ArgPaths.GetHashCode());
            hash = hash * 23 + (Arg0Namespace == null ? 0 : Arg0Namespace.GetHashCode());
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
                Path == r.Path &&
                PathNamespace == r.PathNamespace &&
                ArgEquals(Args,r.Args) &&
                ArgEquals(ArgPaths, r.ArgPaths) &&
                Arg0Namespace == r.Arg0Namespace;
        }

        private static bool ArgEquals((int index, string arg)[] one, (int index, string arg)[] two)
        {
            if (one == two) {
                return true;
            }

            if (one == null || two == null) {
                return false;
            }

            if (one.Length != two.Length) {
                return false;
            }

            for (var i = 0; i < one.Length; i++) {
                if (one[i] != two[i]) {
                    return false;
                }
            }

            return true;
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

            if (sender != null)
            {
                Append(sb, "sender", sender);
            }
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
            if (PathNamespace != null)
            {
                Append(sb, "path_namespace", PathNamespace.Value);
            }
            if (Args != null)
            {
                foreach (var item in Args)
                {
                    Append(sb, $"arg{item.index}", item.arg);
                }
            }
            if (ArgPaths != null)
            {
                foreach (var item in ArgPaths)
                {
                    Append(sb, $"arg{item.index}path", item.argPath);
                }
            }
            if (Arg0Namespace != null)
            {
                Append(sb, "arg0namespace", Arg0Namespace);
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
