namespace Tmds.DBus.Protocol;

enum RequestNameReply : uint
{
    PrimaryOwner = 1,
    InQueue,
    Exists,
    AlreadyOwner,
}
