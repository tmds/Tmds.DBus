using System;
using System.CommandLine;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Tool;

class MonitorCommand : Command
{
    public MonitorCommand() : base("monitor")
    {
        Description = "Monitor DBus messages";

        Option<string> busOption = CommandHelpers.CreateBusOption();
        Add(busOption);

        this.SetAction((parseResult) =>
        {
            string bus = parseResult.GetValue(busOption)!;
            string address = CommandHelpers.ParseBusAddress(bus);
            MonitorBusAsync(address).Wait();
            return 0;
        });
    }

    private static async Task MonitorBusAsync(string address)
    {
        StringBuilder sb = new StringBuilder();

        await foreach (DisposableMessage dmsg in Tmds.DBus.Protocol.Connection.MonitorBusAsync(address))
        {
            using var _ = dmsg;
            Message msg = dmsg.Message;

            FormatMessage(sb, msg);

            Print(sb);

            sb.Clear();
        }
    }

    static void Print(StringBuilder sb)
    {
        foreach (ReadOnlyMemory<char> chunk in sb.GetChunks())
        {
            Console.Write(chunk);
        }
    }

    private const string ItemPrefix = "- ";

    static void FormatMessage(StringBuilder sb, Message msg)
    {
        switch (msg.MessageType)
        {
            case MessageType.MethodCall:
                sb.Append("CAL ");
                break;
            case MessageType.MethodReturn:
                sb.Append("RET ");
                break;
            case MessageType.Error:
                sb.Append("ERR ");
                break;
            case MessageType.Signal:
                sb.Append("SIG ");
                break;
            default:
                throw new ArgumentOutOfRangeException(msg.MessageType.ToString());
        }

        sb.Append(msg.SenderAsString);
        sb.Append($"({msg.Serial})");
        sb.Append("->");
        if (msg.DestinationIsSet)
        {
            sb.Append(msg.DestinationAsString);
        }
        if (msg.ReplySerial.HasValue)
        {
            sb.Append($"({msg.ReplySerial.Value})");
        }
        sb.Append(' ');

        if (msg.PathIsSet)
        {
            sb.Append(msg.PathAsString);
            sb.Append(' ');
        }

        if (msg.InterfaceIsSet)
        {
            sb.Append(msg.InterfaceAsString);
            sb.Append('.');
        }
        if (msg.MemberIsSet)
        {
            sb.Append(msg.MemberAsString);
            sb.Append(' ');
        }

        if (msg.ErrorNameIsSet)
        {
            sb.Append(msg.ErrorNameAsString);
            sb.Append(' ');
        }

        if (msg.SignatureIsSet)
        {
            sb.Append(msg.SignatureAsString);
        }
        sb.AppendLine();

        SignatureReader sigReader = new(msg.Signature);
        Reader reader = msg.GetBodyReader();
        while (sigReader.TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature))
        {
            int indent = 2;
            Indent(sb, indent);
            sb.Append(ItemPrefix);
            AppendValue(sb, indent + ItemPrefix.Length, ref reader, type, innerSignature);
        }
    }

    static void AppendValue(StringBuilder sb, int indent, ref Reader reader, DBusType type, ReadOnlySpan<byte> innerSignature, bool addNewLine = true, bool isDictEntryValue = false)
    {
        SignatureReader sigReader;
        switch (type)
        {
            case DBusType.Byte:
                sb.Append(reader.ReadByte());
                break;
            case DBusType.Bool:
                sb.Append(reader.ReadBool());
                break;
            case DBusType.Int16:
                sb.Append(reader.ReadInt16());
                break;
            case DBusType.UInt16:
                sb.Append(reader.ReadUInt16());
                break;
            case DBusType.Int32:
                sb.Append(reader.ReadInt32());
                break;
            case DBusType.UInt32:
                sb.Append(reader.ReadUInt32());
                break;
            case DBusType.Int64:
                sb.Append(reader.ReadInt64());
                break;
            case DBusType.UInt64:
                sb.Append(reader.ReadUInt64());
                break;
            case DBusType.Double:
                sb.Append(reader.ReadDouble());
                break;
            case DBusType.String:
                sb.Append(reader.ReadString());
                break;
            case DBusType.ObjectPath:
                sb.Append(reader.ReadObjectPath());
                break;
            case DBusType.Signature:
                sb.Append(reader.ReadSignatureAsString());
                break;
            case DBusType.UnixFd:
                sb.Append($"(fd)[{reader.ReadUInt32()}]");
                break;
            case DBusType.Array:
                sigReader = new(innerSignature);
                sigReader.TryRead(out type, out innerSignature);
                bool isDictionary = type == DBusType.DictEntry;

                // Print these types on a single line.
                bool printSingleLine = type is DBusType.Byte or
                                                DBusType.Bool or
                                                DBusType.Int16 or
                                                DBusType.UInt16 or
                                                DBusType.Int32 or
                                                DBusType.UInt32 or
                                                DBusType.Int64 or
                                                DBusType.UInt64 or
                                                DBusType.Double or
                                                DBusType.UnixFd;

                // Only print first 16 elements of an array.
                int remaining = isDictionary ? int.MaxValue : 16;

                ArrayEnd arrayEnd = reader.ReadArrayStart(type);
                bool isEmpty = true;
                while (reader.HasNext(arrayEnd))
                {
                    if (printSingleLine)
                    {
                        if (isEmpty) // first
                        {
                            sb.Append("[");
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                    }
                    else
                    {
                        if (isEmpty) // first
                        {
                            if (isDictEntryValue)
                            {
                                sb.AppendLine();
                                Indent(sb, indent);
                            }
                        }
                        else
                        {
                            Indent(sb, indent);
                        }

                        if (!isDictionary)
                        {
                            sb.Append(ItemPrefix);
                        }
                    }
                    isEmpty = false;

                    if (remaining-- == 0)
                    {
                        if (printSingleLine)
                        {
                            sb.Append("...");
                        }
                        else
                        {
                            sb.AppendLine("...");
                        }
                        reader.SkipTo(arrayEnd);
                        break;
                    }

                    AppendValue(sb, isDictionary ? indent : indent + ItemPrefix.Length, ref reader, type, innerSignature, addNewLine: !printSingleLine);
                }
                if (isEmpty)
                {
                    sb.AppendLine(isDictionary ? "{}" : "[]");
                }
                else if (printSingleLine)
                {
                    sb.AppendLine("]");
                }

                addNewLine = false;
                break;
            case DBusType.Struct:
                if (isDictEntryValue)
                {
                    sb.AppendLine();
                    Indent(sb, indent);
                }

                reader.AlignStruct();
                sigReader = new(innerSignature);

                bool isFirst = true;
                while (sigReader.TryRead(out type, out innerSignature))
                {
                    if (!isFirst)
                    {
                        Indent(sb, indent);
                    }
                    isFirst = false;

                    sb.Append(ItemPrefix);
                    AppendValue(sb, indent + ItemPrefix.Length, ref reader, type, innerSignature);
                }

                addNewLine = false;
                break;
            case DBusType.Variant:
                innerSignature = reader.ReadSignatureAsSpan();
                sigReader = new(innerSignature);
                sigReader.TryRead(out type, out innerSignature);
                AppendValue(sb, indent, ref reader, type, innerSignature, isDictEntryValue: isDictEntryValue);

                addNewLine = false;
                break;
            case DBusType.DictEntry:
                reader.AlignStruct();
                sigReader = new(innerSignature);

                sigReader.TryRead(out type, out innerSignature);
                AppendValue(sb, indent, ref reader, type, innerSignature, addNewLine: false);

                sb.Append(": ");

                sigReader.TryRead(out type, out innerSignature);
                AppendValue(sb, indent + 2 /* indent value */, ref reader, type, innerSignature, isDictEntryValue: true);

                addNewLine = false;
                break;
            default:
                throw new InvalidOperationException();
        }

        if (addNewLine)
        {
            sb.AppendLine();
        }
    }

    private static void Indent(StringBuilder sb, int indent)
    {
        sb.Append(' ', indent);
    }
}
