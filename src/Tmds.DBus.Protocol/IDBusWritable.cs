namespace Tmds.DBus.Protocol;

/// <summary>
/// Internal interface that enables serializing composite types.
/// </summary>
public interface IDBusWritable
{
    /// <summary>
    /// Writes this instance to the specified message writer.
    /// </summary>
    /// <param name="writer">The message writer to write to.</param>
    void WriteTo(ref MessageWriter writer);
}
