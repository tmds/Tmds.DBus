namespace Tmds.DBus.Protocol;

class MessageFormatter
{
    public static void FormatMessage(in Message msg, StringBuilder sb)
    {
        // Header.
        Append(sb, msg.MessageType);
        sb.Append(" serial=");
        sb.Append(msg.Serial);
        if (msg.ReplySerial.HasValue)
        {
            sb.Append(" rserial=");
            sb.Append(msg.ReplySerial.Value);
        }
        Append(sb, " err", msg.ErrorName);
        Append(sb, " path", msg.Path);
        Append(sb, " memb", msg.Member);
        Append(sb, " body", msg.Signature);
        Append(sb, " src", msg.Sender);
        Append(sb, " dst", msg.Destination);
        Append(sb, " ifac", msg.Interface);
        if (msg.UnixFdCount != 0)
        {
            sb.Append(" fds=");
            sb.Append(msg.UnixFdCount);
        }

        sb.AppendLine();

        // Body.
        int indent = 2;
        Reader reader = msg.GetBodyReader();
        ReadData(sb, ref reader, msg.Signature, indent);

        // Remove final newline.
        sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length);
    }

    private static void ReadData(StringBuilder sb, ref Reader reader, Utf8Span signature, int indent)
    {
        var sigReader = new SignatureReader(signature);
        while (sigReader.TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature))
        {
            if (type == DBusType.Invalid)
            {
                // TODO something is wrong, complain loudly.
                break;
            }
            sb.Append(' ', indent);
            switch (type)
            {
                // case DBusType.Byte:
                //     sb.AppendLine($"byte   {msg.ReadByte()}");
                //     break;
                // case DBusType.Bool:
                //     sb.AppendLine($"bool   {msg.ReadBool()}");
                //     break;
                // case DBusType.Int16:
                //     sb.AppendLine($"int16  {msg.ReadInt16()}");
                //     break;
                // case DBusType.UInt16:
                //     sb.AppendLine($"uint16 {msg.ReadUInt16()}");
                //     break;
                // case DBusType.Int32:
                //     sb.AppendLine($"int32  {msg.ReadInt32()}");
                //     break;
                case DBusType.UInt32:
                    sb.AppendLine($"uint32 {reader.ReadUInt32()}");
                    break;
                // case DBusType.Int64:
                //     sb.Append($"int64  {msg.ReadInt64()}");
                //     break;
                // case DBusType.UInt64:
                //     sb.Append($"uint64 {msg.ReadUInt64()}");
                //     break;
                // case DBusType.Double:
                //     sb.Append($"double {msg.ReadDouble()}");
                //     break;
                case DBusType.UnixFd:
                    sb.AppendLine($"fd     {reader.ReadHandleRaw()}");
                    break;
                case DBusType.String:
                    sb.Append("string ");
                    sb.AppendUTF8(reader.ReadObjectPathAsSpan()); // TODO: handle long strings without allocating.
                    sb.AppendLine();
                    break;
                case DBusType.ObjectPath:
                    sb.Append("path   ");
                    sb.AppendUTF8(reader.ReadObjectPathAsSpan()); // TODO: handle long strings without allocating.
                    sb.AppendLine();
                    break;
                case DBusType.Signature:
                    sb.Append("sig    ");
                    sb.AppendUTF8(reader.ReadSignature());
                    sb.AppendLine();
                    break;
                case DBusType.Array:
                    sb.AppendLine("array  [");
                    ArrayEnd itEnd = reader.ReadArrayStart((DBusType)innerSignature[0]);
                    while (reader.HasNext(itEnd))
                    {
                        ReadData(sb, ref reader, innerSignature, indent + 2);
                    }
                    sb.Append(' ', indent);
                    sb.AppendLine("]");
                    break;
                case DBusType.Struct:
                    sb.AppendLine("struct (");
                    ReadData(sb, ref reader, innerSignature, indent + 2);
                    sb.Append(' ', indent);
                    sb.AppendLine(")");
                    break;
                case DBusType.Variant:
                    sb.AppendLine("var   ("); // TODO: merge with next line
                    ReadData(sb, ref reader, reader.ReadSignature(), indent + 2);
                    sb.Append(' ', indent);
                    sb.AppendLine(")");
                    break;
                case DBusType.DictEntry:
                    sb.AppendLine("dicte (");
                    ReadData(sb, ref reader, innerSignature, indent + 2);
                    sb.Append(' ', indent);
                    sb.AppendLine(")");
                    break;
            }
        }
        // TODO: complain if there is still data left.
    }

    private static void Append(StringBuilder sb, MessageType type)
    {
        switch (type)
        {
            case MessageType.MethodCall:
                sb.Append("call"); break;
            case MessageType.MethodReturn:
                sb.Append("ret "); break;
            case MessageType.Error:
                sb.Append("err "); break;
            case MessageType.Signal:
                sb.Append("sig "); break;
            default:
                sb.Append($"?{type}"); break;
        }
    }

    private static void Append(StringBuilder sb, string field, Utf8Span value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        sb.Append(field);
        sb.Append('=');
        sb.AppendUTF8(value);
    }
}