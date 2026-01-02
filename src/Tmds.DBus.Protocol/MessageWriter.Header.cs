namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    /// <summary>
    /// Writes a method call message header.
    /// </summary>
    /// <param name="destination">The destination bus name.</param>
    /// <param name="path">The object path.</param>
    /// <param name="interface">The interface name.</param>
    /// <param name="member">The method name.</param>
    /// <param name="signature">The method signature.</param>
    /// <param name="flags">Message flags.</param>
    public void WriteMethodCallHeader(
        string? destination = null,
        string? path = null,
        string? @interface = null,
        string? member = null,
        string? signature = null,
        MessageFlags flags = MessageFlags.None)
    {
        ArrayStart start = WriteHeaderStart(MessageType.MethodCall, flags);

        // Path.
        if (path is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Path);
            WriteVariantObjectPath(path);
        }

        // Interface.
        if (@interface is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Interface);
            WriteVariantString(@interface);
        }

        // Member.
        if (member is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Member);
            WriteVariantString(member);
        }

        // Destination.
        if (destination is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Destination);
            WriteVariantString(destination);
        }

        // Signature.
        if (signature is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Signature);
            WriteVariantSignature(signature);
        }

        WriteHeaderEnd(start);
    }

    /// <summary>
    /// Writes a method return message header.
    /// </summary>
    /// <param name="replySerial">The serial number of the method call being replied to.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="signature">The body signature.</param>
    public void WriteMethodReturnHeader(
        uint replySerial,
        ReadOnlySpan<byte> destination = default,
        string? signature = null)
    {
        ArrayStart start = WriteHeaderStart(MessageType.MethodReturn, MessageFlags.None);

        // ReplySerial
        WriteStructureStart();
        WriteByte((byte)MessageHeader.ReplySerial);
        WriteVariantUInt32(replySerial);

        // Destination.
        if (!destination.IsEmpty)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Destination);
            WriteVariantString(destination);
        }

        // Signature.
        if (signature is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Signature);
            WriteVariantSignature(signature);
        }

        WriteHeaderEnd(start);
    }

    /// <summary>
    /// Writes an error message header and optional error message.
    /// </summary>
    /// <param name="replySerial">The serial number of the method call being replied to.</param>
    /// <param name="destination">The destination name.</param>
    /// <param name="errorName">The error name.</param>
    /// <param name="errorMsg">The error message.</param>
    public void WriteError(
        uint replySerial,
        ReadOnlySpan<byte> destination = default,
        string? errorName = null,
        string? errorMsg = null)
    {
        ArrayStart start = WriteHeaderStart(MessageType.Error, MessageFlags.None);

        // ReplySerial
        WriteStructureStart();
        WriteByte((byte)MessageHeader.ReplySerial);
        WriteVariantUInt32(replySerial);

        // Destination.
        if (!destination.IsEmpty)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Destination);
            WriteVariantString(destination);
        }

        // Error.
        if (errorName is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.ErrorName);
            WriteVariantString(errorName);
        }

        // Signature.
        if (errorMsg is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Signature);
            WriteVariantSignature(Signature.String);
        }

        WriteHeaderEnd(start);

        if (errorMsg is not null)
        {
            WriteString(errorMsg);
        }
    }

    /// <summary>
    /// Writes a signal message header.
    /// </summary>
    /// <param name="destination">The destination name.</param>
    /// <param name="path">The object path.</param>
    /// <param name="interface">The interface name.</param>
    /// <param name="member">The signal name.</param>
    /// <param name="signature">The body signature.</param>
    public void WriteSignalHeader(
        string? destination = null,
        string? path = null,
        string? @interface = null,
        string? member = null,
        string? signature = null)
    {
        ArrayStart start = WriteHeaderStart(MessageType.Signal, MessageFlags.None);

        // Path.
        if (path is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Path);
            WriteVariantObjectPath(path);
        }

        // Interface.
        if (@interface is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Interface);
            WriteVariantString(@interface);
        }

        // Member.
        if (member is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Member);
            WriteVariantString(member);
        }

        // Destination.
        if (destination is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Destination);
            WriteVariantString(destination);
        }

        // Signature.
        if (signature is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Signature);
            WriteVariantSignature(signature);
        }

        WriteHeaderEnd(start);
    }

    private void WriteHeaderEnd(ArrayStart start)
    {
        WriteArrayEnd(start);
        WritePadding(ProtocolConstants.StructAlignment);
    }

    private ArrayStart WriteHeaderStart(MessageType type, MessageFlags flags)
    {
        _flags = flags;

        WriteByte(BitConverter.IsLittleEndian ? (byte)'l' : (byte)'B'); // endianness
        WriteByte((byte)type);
        WriteByte((byte)flags);
        WriteByte((byte)1); // version
        WriteUInt32((uint)0); // length placeholder
        Debug.Assert(_offset == LengthOffset + 4);
        WriteUInt32(_serial);
        Debug.Assert(_offset == SerialOffset + 4);

        // headers
        ArrayStart start = WriteArrayStart(ProtocolConstants.StructAlignment);

        // UnixFds
        WriteStructureStart();
        WriteByte((byte)MessageHeader.UnixFds);
        WriteVariantUInt32(0); // unix fd length placeholder
        Debug.Assert(_offset == UnixFdLengthOffset + 4);
        return start;
    }
}
