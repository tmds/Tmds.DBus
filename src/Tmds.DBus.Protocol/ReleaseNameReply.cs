namespace Tmds.DBus.Protocol;

enum ReleaseNameReply : uint
{
    ReplyReleased = 1,
    NonExistent,
    NotOwner
}
