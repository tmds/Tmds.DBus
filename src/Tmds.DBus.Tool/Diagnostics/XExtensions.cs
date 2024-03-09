#nullable enable
using System;
using System.Linq;
using System.Xml.Linq;

namespace Tmds.DBus.Tool.Diagnostics;

internal static class XExtensions
{
    /// <summary>
    /// Get the absolute XPath to a given XElement or XAttribute
    /// (e.g. "/people/person[6]/name[1]/last[1]").
    /// </summary>
    /// <remarks>
    /// Inspired by: <see href="https://stackoverflow.com/questions/451950/get-the-xpath-to-an-xelement/454597#454597"/> 
    /// </remarks>
    /// 
    public static string GetAbsoluteXPath(this XObject obj)
    {
        string result = obj switch
        {
            XElement element => GetAbsoluteXPath(element),
            XAttribute attr => GetAbsoluteXPath(attr),
            _ => string.Empty
        };
        return result;
    }

    private static string GetAbsoluteXPath(XAttribute attribute)
    {
        var path = GetAbsoluteXPath(attribute.Parent ?? throw new InvalidOperationException("Attribute has been removed from its parent"));

        if (attribute.Name.Namespace == XNamespace.None)
        {
            path += $"/@{attribute.Name.LocalName}";
        }
        else
        {
            path += $"/*[local-name()='{attribute.Name.LocalName}']";
        }

        return path;
    }

    private static string GetAbsoluteXPath(XElement element)
    {
        var pathElements = element
            .AncestorsAndSelf()
            .Reverse()
            .Select(e =>
            {
                string name = e.Name.Namespace == XNamespace.None
                    ? e.Name.LocalName
                    : $"*[local-name()='{e.Name.LocalName}']";

                int index = GetIndexOf(e);
                var indexStr = index >= 0 ? $"[{index}]" : string.Empty;

                return $"/{name}{indexStr}";
            });

        return string.Concat(pathElements);
    }

    /// <summary>
    /// Get the index of the given XElement relative to its
    /// siblings with identical names. If the given element is
    /// the root, -1 is returned or -2 if element has no sibling elements.
    /// </summary>
    /// <param name="element">The element to get the index of.</param>
    private static int GetIndexOf(XElement element)
    {
        if (element.Parent == null)
        {
            // Element is root
            return -1;
        }

        if (element.Parent.Elements(element.Name).Count() == 1)
        {
            // Element has no sibling elements
            return -2;
        }

        int i = 1; // Indexes for nodes start at 1, not 0

        foreach (var sibling in element.Parent.Elements(element.Name))
        {
            if (sibling == element)
            {
                return i;
            }

            i++;
        }

        throw new InvalidOperationException("Element has been removed from its parent");
    }
}