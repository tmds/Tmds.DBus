#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Tmds.DBus.Tool.Diagnostics;

internal static class XmlErrorHighlighter
{
    public static void WriteToConsole(string fullXml, XObject highlightObject, ConsoleColor highlightColor)
    {
        if (FindStartEndPositions(fullXml, highlightObject) is { } range)
        {
            var texts = SplitText(fullXml, new[] { range.Start, range.End }).ToList();

            Console.Write(texts[0]);

            var originalColor = Console.BackgroundColor;
            Console.BackgroundColor = highlightColor;
            Console.Write(texts[1]);
            Console.BackgroundColor = originalColor;

            Console.Write(texts[2]);
            Console.Write(Environment.NewLine);
        }
        else
        {
            //This should never happen (unless XML is mistakenly loaded without LoadOptions.SetLineInfo)
            Console.WriteLine(fullXml);
            Console.WriteLine("(Unable to highlight faulty XML)");
        }
    }

    /// <summary>
    /// Find the text positions in <see cref="fullXml">fullXml</see> where the <see cref="obj">xObject</see> starts and ends
    /// </summary>
    private static (TextLocation Start, TextLocation End)? FindStartEndPositions(string fullXml, XObject obj)
    {
        if (TextLocation.TryFrom(obj) is { } pos)
        {
            using var textReader = new StringReader(fullXml);
            using var xmlReader = XmlReader.Create(textReader, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            while (xmlReader.Read())
            {
                //Search in elements
                {
                    var seekPos = TextLocation.From(xmlReader);
                    if (seekPos == pos)
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            var start = TextLocation.From(xmlReader, xmlReader.NodeType);
                            xmlReader.Skip();
                            var end = TextLocation.From(xmlReader, xmlReader.NodeType);
                            return (start, end);
                        }

                        //Location for obj could not be determined
                        return null;
                    }
                }

                //Search in attributes
                while (xmlReader.MoveToNextAttribute())
                {
                    var seekPos = TextLocation.From(xmlReader);
                    if (seekPos == pos)
                    {
                        //Preserve the correct quoteChar before moving on
                        var quoteChar = xmlReader.QuoteChar;

                        //Move to following attribute or element
                        if (!xmlReader.MoveToNextAttribute())
                        {
                            xmlReader.Read();
                        }

                        //Set the end location as start of following element and rewind until the attribute's quoteChar is found
                        var end = TextLocation.From(xmlReader, xmlReader.NodeType);
                        var lct = (TextLocationConverter)fullXml;
                        var idx = lct.IndexOf(end);
                        while (fullXml[idx-1] != quoteChar) idx--;
                        end = lct.LocationOf(idx);

                        //Done
                        return (seekPos, end);
                    }
                }
            }
        }

        //Location for obj could not be determined
        return null;
    }

    private static IEnumerable<string> SplitText(string text, IEnumerable<TextLocation> positions)
    {
        int line = 0;
        int column = 0;
        int consumedUntilIdx = 0;
        var queue = new Queue<TextLocation>(positions);
        var split = queue.Dequeue();
        var lastSplitFound = false;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (line == split.Line && column == split.Column || line > split.Line)
            {
                yield return text[consumedUntilIdx..i];
                consumedUntilIdx = i;
                if (queue.Count != 0)
                {
                    split = queue.Dequeue();
                }
                else
                {
                    lastSplitFound = true;
                    break;
                }
            }

            if (c == '\n')
            {
                line++;
                column = 0;
            }
            else
            {
                column++;
            }
        }

        yield return text[consumedUntilIdx..];
        if (!lastSplitFound) yield return string.Empty;
    }
}