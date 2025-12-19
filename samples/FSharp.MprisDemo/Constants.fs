namespace FSharpMprisDemo

module Constants =
    [<Literal>]
    let MPRIS_ROOT_INTERFACE = "org.mpris.MediaPlayer2"

    [<Literal>]
    let MPRIS_CORE_PLAYBACK_INTERFACE = "org.mpris.MediaPlayer2.Player"

    /// Used for filtering purposes.
    [<Literal>]
    let MPRIS_INTERFACE_PREFIX = "org.mpris.MediaPlayer2."

    [<Literal>]
    let MPRIS_OBJECT_PATH = "/org/mpris/MediaPlayer2"

    // Metadata keys used in MPRIS
    [<Literal>]
    let XESAM_ARTIST = "xesam:artist"

    [<Literal>]
    let XESAM_TITLE = "xesam:title"

    [<Literal>]
    let XESAM_ALBUM = "xesam:album"

    [<Literal>]
    let XESAM_URL = "xesam:url"

    [<Literal>]
    let MPRIS_LENGTH = "mpris:length"

    [<Literal>]
    let XESAM_GENRE = "xesam:genre"

    [<Literal>]
    let CUSTOM_PLAYER_ID = "custom:playerid"

    // Default values for missing metadata
    [<Literal>] 
    let DEFAULT_ARTIST = "unknownartist"
    [<Literal>] 
    let DEFAULT_TITLE = "untitled"
    [<Literal>] 
    let DEFAULT_ALBUM = "noalbum"
    [<Literal>] 
    let DEFAULT_URL = "nourl"
    [<Literal>] 
    let DEFAULT_DURATION = "noduration"
    [<Literal>] 
    let DEFAULT_PLAYER = "unknownplayer"
    [<Literal>] 
    let DEFAULT_GENRE = "unknowngenre"
