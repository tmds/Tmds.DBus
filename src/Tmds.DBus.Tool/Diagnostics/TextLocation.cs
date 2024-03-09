#nullable enable
using System;
using System.Diagnostics;
using System.Xml;

namespace Tmds.DBus.Tool.Diagnostics;

/// <summary>
/// Represents a location in a text file given by a line and column number.
/// </summary>
/// <remarks>
/// First line and column number begin at (0,0) unlike most text editors which begin at (1,1).
/// </remarks>
[DebuggerDisplay("({Line},{Column})")]
internal readonly struct TextLocation
{
    public int Line { get; }
    public int Column { get; }

    public static TextLocation From(XmlReader xmlReader, XmlNodeType? nodeType = null) => TryFrom(xmlReader, nodeType) ?? throw new NullReferenceException();

    public static TextLocation? TryFrom(XmlReader xmlReader, XmlNodeType? nodeType = null) =>
        xmlReader is IXmlLineInfo lineInfo && lineInfo.HasLineInfo() ? TryFrom(lineInfo, nodeType) : null;

    public static TextLocation? TryFrom(IXmlLineInfo lineInfo, XmlNodeType? nodeType = null)
    {
        if (!lineInfo.HasLineInfo())
        {
            return null;
        }

        int leadInLength = nodeType switch
        {
            XmlNodeType.Element => "<".Length,
            XmlNodeType.EndElement => "</".Length,
            _ => 0
        };

        return new TextLocation(lineInfo.LineNumber - 1, lineInfo.LinePosition - leadInLength - 1);
    }

    public TextLocation(int line, int column)
    {
        Line = line;
        Column = column;
    }

    #region Equality members

    public bool Equals(TextLocation other)
    {
        return Line == other.Line && Column == other.Column;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Line, Column);
    }

    public static bool operator ==(TextLocation a, TextLocation b)
    {
        return a.Line == b.Line && a.Column == b.Column;
    }

    public static bool operator !=(TextLocation a, TextLocation b)
    {
        return a.Line != b.Line || a.Column != b.Column;
    }

    #endregion
}