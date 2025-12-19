
# FSharp MPRIS Demo

This is a sample `F#` program that uses [`Tmds.DBus`](https://github.com/tmds/Tmds.DBus) to communicate with **MPRIS-compatible** media players over **D-Bus**.
The program contains a background service that polls the **Now Playing** music information and prints the state to standard output.

## Design Considerations

The program is designed around two main event streams, handled using `F# Async Expressions` and `FSharp.Control.AsyncSeq`:

1. **Media Player Discovery Stream**
    - Monitors MPRIS-compatible media players connecting/disconnecting
    - Handles dynamic player registration and removal

2. **Player State Stream**
    - Each media player maintains its own state stream
    - Tracks current playback status, metadata, and position

These streams are processed asynchronously to provide real-time updates about the media playback state.  
**Note:** This demo implements a subset of MPRIS functionality for demonstration purposes. The event streams above outline the overall architecture, though not all features are implemented in this sample project.


## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- A Linux system with D-Bus and an MPRIS-compatible media player (e.g., [`Musikcube`](https://github.com/clangen/musikcube), YouTube Music On [`Zen Browser`](https://github.com/zen-browser/desktop))

## Usage

**1. Start a media player**

Play music with an MPRIS-compatible media player (such as Musikcube, YouTube Music on Web browser, etc.):


**2. Run the program**

```sh
dotnet run FSharp.MprisDemo
```

**3. View output**

Check the terminal for the captured "Now Playing" information. You should see output similar to:

```txt
Now Playing At: 8/25/2025 9:04:50 AM: Player: org.mpris.MediaPlayer2.firefox.instance_1_271 Status: Playing
Now Playing At:8/25/2025 9:04:50 AM Position: 11000000 Some
  { Id = "RUPuYxtpQ4m9q+mgYOx7fH0ITOABu38zZOjyVQ6Ygxk="
    Artist = ["Picture This & JORIS"]
    Title = "Heart over Head"
    Album = Some ""
    Length = Some 00:03:26
    Url =
     Some
       "https://music.youtube.com/watch?v=c3I8sgXyGCs&list=OLAK5uy_mkWbjeHOkVy1wmHeV3IodOnlAcpfNEXqw"
    ReplayCount = 0us
    PlayerId = "unknownplayer"
    MetadataMap =
     map
       [("XESAM_ALBUM", ""); ("XESAM_ARTIST", ",Picture This & JORIS");
        ("XESAM_GENRE", "unknowngenre"); ("XESAM_TITLE", "Heart over Head");
        <Snip>
```

## Useful Commands
Here are some helpful commands to interact with MPRIS:

```shell
# List all MPRIS-compatible players
playerctl -l

# Now playing info, Include specified tags.
playerctl metadata --format  "Now Playing: {{ title }} - {{ artist }}" --follow
```

## Next Steps

Explore the available APIs and experiment further!
- [Tmds.DBus Docs](https://github.com/tmds/Tmds.DBus/tree/main/docs)
- [MPRIS D-Bus Interface Specification](https://specifications.freedesktop.org/mpris-spec/latest/index.html)

For a complete end-to-end **F#** example, check the:
 **FScrobble** [GitHub repository](https://github.com/alibaghernejad/fscrobble/).  
 
