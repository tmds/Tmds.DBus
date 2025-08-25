module Mpris

open System
open System.Threading.Tasks
open System.Collections.Generic
open Tmds.DBus
open FSharp.Control

module Mpris =
    open System.Linq
    open FSharpMprisDemo.Constants
    open FSharpMprisDemo.Helpers

    [<DBusInterface(MPRIS_CORE_PLAYBACK_INTERFACE)>]
    type IMediaPlayer2Player =
        inherit IDBusObject
        abstract member GetAsync<'T> : (string) -> Task<'T>
        abstract member WatchPropertiesChangedAsync: (Action<PropertyChanges>) -> Task<IDisposable>

    [<AutoOpen>]
    module MediaPlayer2PlayerExtended =
        type IMediaPlayer2Player with
            member this.GetPlaybackStatus() = this.GetAsync<string> "PlaybackStatus"
            member this.MetadataAsync = this.GetAsync<IDictionary<string, obj>> "Metadata"
            member this.Position = this.GetAsync<int64> "Position"

    /// <summary>
    /// Connects to a media player via D-Bus and returns a proxy if successful.
    /// </summary>
    let private connectToMediaPlayer (con: Connection) (serviceName: string) =
        if String.IsNullOrWhiteSpace serviceName then
            Error "Service name must not be null or whitespace."
        elif not (serviceName.StartsWith MPRIS_ROOT_INTERFACE) then
            Error $"Service name must start with '{MPRIS_ROOT_INTERFACE}'."
        else
            try
                let proxy =
                    con.CreateProxy<IMediaPlayer2Player>(serviceName, ObjectPath MPRIS_OBJECT_PATH)

                Ok proxy
            with ex ->
                Error $"Failed to connect to media player: {ex.Message}"

    /// <summary>
    /// Checks asynchronously whether a given MPRIS player D-Bus name is still active.
    /// </summary>
    let private isPlayerActive (playerName: string) (con: Connection) : Async<bool> =
        async {
            try
                let! isActive = con.IsServiceActiveAsync playerName |> Async.AwaitTask
                return isActive
            with ex ->
                printfn "Error checking player '%s': %s" playerName ex.Message
                return false
        }

    /// <summary>
    /// Gets a sequence of active MPRIS media player service names, up to maxPlayers.
    /// </summary>
    let getMediaPlayers (maxPlayers: int) (con: Connection) : AsyncSeq<string array> =
        asyncSeq {
            let! names = con.ListServicesAsync() |> Async.AwaitTask

            let namesFiltered =
                names |> Array.filter (fun name -> name.StartsWith(MPRIS_ROOT_INTERFACE))

            if not (namesFiltered.Any()) then
                printfn "No running media player detected."
                printfn "Start your favotite players like 'Musikcube', 'Spotify', 'YouTube Music Web', ..."

            let! results =
                [ for p in namesFiltered ->
                      async {
                          let! isActivable = isPlayerActive p con

                          let! result =
                              match isActivable with
                              | true -> async.Return(Some p)
                              | _ -> async.Return None

                          return result
                      } ]
                |> Async.Parallel

            let resultsSomes = results |> Array.choose id


            resultsSomes
        }

    /// <summary>
    /// Given a sequence of player names, attempts to connect and yield (name, proxy) pairs.
    /// </summary>
    let private getPlayersStreams (playerNames: AsyncSeq<string>) (con: Connection) =
        asyncSeq {
            for playerName in playerNames do
                let player = connectToMediaPlayer con playerName

                match player with
                | Ok player ->
                    // Add player to watching List
                    yield (playerName, player)
                | Error err ->
                    //Ignore the player.
                    printfn "Error connecting to media player %s: %s" playerName err
        }

    /// <summary>
    /// Processes the media stream from a player, printing status and track info for debugging.
    /// </summary>
    let private processStream (playerName: string, player: IMediaPlayer2Player) =
        async {
            let! status = player.GetPlaybackStatus() |> Async.AwaitTask
            printfn "Now Playing At: %s: Player: %s Status: %s" (DateTime.UtcNow.ToString()) playerName status
            let! metadata = player.MetadataAsync |> Async.AwaitTask
            // Player id can be a hash generated value
            // let playerId =
            // metadata.Add(CUSTOM_PLAYER_ID, playerId)
            let trackInfo = extractTrackInfo metadata
            let! position = player.Position |> Async.AwaitTask
            printfn "Now Playing At:%s Position: %d %A" (DateTime.UtcNow.ToString()) position trackInfo
        }

    /// <summary>
    /// Polls all media players, processes their streams, and handles errors.
    /// </summary>
    let pollMediaPlayers (players) (con: Connection) =
        async {
            try
                do!
                    players
                    |> AsyncSeq.concatSeq
                    // get player streams
                    |> getPlayersStreams
                    <| con
                    |> AsyncSeq.groupBy (fun e -> fst e)
                    // Parallel processing for multiple player instances
                    |> AsyncSeq.mapAsyncParallel (snd >> AsyncSeq.iterAsync processStream)
                    |> AsyncSeq.iter ignore
            with e ->
                printfn "Error: %s" e.Message
        }
