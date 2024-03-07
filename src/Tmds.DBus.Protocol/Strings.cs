namespace Tmds.DBus.Protocol;

static class Strings
{
    public const string AddTypeReaderMethodObsolete = "AddTypeReader methods are obsolete. Remove the call to this method.";
    public const string AddTypeWriterMethodObsolete = "AddTypeWriter methods are obsolete. Remove the call to this method.";
    public const string UseNonGenericWriteArray = $"Use a non-generic overload of '{nameof(MessageWriter.WriteArray)}' if it exists for the item type, and otherwise write out the elements separately surrounded by a call to '{nameof(MessageWriter.WriteArrayStart)}' and '{nameof(MessageWriter.WriteArrayStart)}'.";
    public const string UseNonGenericReadArray = $"Use a '{nameof(Reader.ReadArray)}Of*' method if it exists for the item type, and otherwise read out the elements in a while loop using '{nameof(Reader.ReadArrayStart)}' and '{nameof(Reader.HasNext)}'.";
}