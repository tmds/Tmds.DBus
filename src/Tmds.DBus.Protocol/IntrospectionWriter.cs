namespace Tmds.DBus.Protocol
{
    internal class IntrospectionWriter
    {
        private readonly StringBuilder _sb = new();

        public void WriteDocType()
        {
            _sb.Append("<!DOCTYPE node PUBLIC \"-//freedesktop//DTD D-BUS Object Introspection 1.0//EN\"\n");
            _sb.Append("\"http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd\">\n");
        }

        public void WriteIntrospectableInterface()
        {
            WriteInterfaceStart("org.freedesktop.DBus.Introspectable");

            WriteMethodStart("Introspect");
            WriteOutArg("data", new Signature("s"));
            WriteMethodEnd();

            WriteInterfaceEnd();
        }

        public void WriteInterfaceStart(string name)
        {
            _sb.AppendFormat("  <interface name=\"{0}\">\n", name);
        }

        public void WriteInterfaceEnd()
        {
            _sb.Append("  </interface>\n");
        }

        public void WriteMethodStart(string name)
        {
            _sb.AppendFormat("    <method name=\"{0}\">\n", name);
        }

        public void WriteMethodEnd()
        {
            _sb.Append("    </method>\n");
        }

        public void WriteOutArg(string name, Signature signature)
        {
            _sb.AppendFormat("      <arg direction=\"out\" name=\"{0}\" type=\"{1}\"/>\n", name, signature);
        }

        public void WriteNodeStart(string name)
        {
            _sb.AppendFormat("<node name=\"{0}\">\n", name);
        }

        public void WriteNodeEnd()
        {
            _sb.Append("</node>\n");
        }

        public void WriteChildNode(string name)
        {
            _sb.AppendFormat("  <node name=\"{0}\"/>\n", name);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
