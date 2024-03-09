#nullable enable
namespace Tmds.DBus.Tool.Diagnostics;

/// <summary>
/// Converts between <see cref="string"/> indexes and <see cref="TextLocation"/>s.
/// </summary>
internal class TextLocationConverter
{
    private readonly string _str;

    private TextLocationConverter(string str)
    {
        _str = str;
    }

    public override string ToString()
    {
        return _str;
    }

    public int IndexOf(TextLocation location)
    {
        int seekLine = 0;
        int seekColumn = 0;

        for (int i = 0; i < _str.Length; i++)
        {
            if (seekLine == location.Line && seekColumn >= location.Column || seekLine > location.Line)
            {
                return i;
            }

            if (_str[i] == '\n')
            {
                seekLine++;
                seekColumn = 0;
            }
            else
            {
                seekColumn++;
            }
        }

        return _str.Length;
    }

    public TextLocation LocationOf(int index)
    {
        int seekLine = 0;
        int seekColumn = 0;

        for (int i = 0; i < index; i++)
        {
            if (_str[i] == '\n')
            {
                seekLine++;
                seekColumn = 0;
            }
            else
            {
                seekColumn++;
            }
        }

        return new TextLocation(seekLine, seekColumn);
    }

    public static implicit operator TextLocationConverter(string text) => new(text);
    public static implicit operator string(TextLocationConverter textLocationConverter) => textLocationConverter._str;
}