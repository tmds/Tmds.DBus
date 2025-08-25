namespace FSharpMprisDemo

open System
open System.Collections.Generic
open FSharpMprisDemo.Constants
open FSharpMprisDemo

module Helpers =
    open System.Text
    open System.Security.Cryptography
    let normalize (s: string) =
        s.Trim().ToLowerInvariant()

    let hashString (input: string) =
        using (SHA256.Create()) (fun sha ->
            input
            |> Encoding.UTF8.GetBytes
            |> sha.ComputeHash
            |> Convert.ToBase64String
        )

    let getTrackId (track: TrackInfo) : string =
        match track with
        | _ ->
            let artist = defaultArg (Some track.Artist) [DEFAULT_ARTIST]
            let title = defaultArg (Some track.Title) DEFAULT_TITLE
            let album = defaultArg track.Album DEFAULT_ALBUM
            let url = defaultArg track.Url DEFAULT_URL
            let duration = 
                match track.Length with
                | Some d -> d.TotalSeconds.ToString("F0")
                | None -> DEFAULT_DURATION

            let raw = $"""{normalize (String.Join(", ", artist))}|{normalize title}|{normalize url}|{normalize album}|{duration}"""
            hashString raw

    let extractTrackInfo (metadata: IDictionary<string, obj>) =
        try
            let getStringValue (key: string) =
                match metadata.TryGetValue(key) with
                | true, value -> Some(value.ToString())
                | _ -> None

            let getStringArrayValue (key: string) =
                match metadata.TryGetValue(key) with
                | true, value ->
                    match value with
                    | :? (string[]) as arr -> Array.toList arr
                    | _ -> []
                | _ -> []

            let getMicrosecondsValue (key: string) =
                match metadata.TryGetValue(key) with
                | true, value ->
                    match Int64.TryParse(value.ToString()) with
                    | true, microseconds -> Some(TimeSpan.FromMilliseconds(float microseconds / 1000.0))
                    | _ -> None
                | _ -> None


            let getStringFromArrayValue value =
                match value with
                | [] -> None 
                | _ ->
                value
                |> List.ofSeq 
                |> List.fold (fun acc item -> acc + item)  ","
                |> Some  

            let artists = getStringArrayValue XESAM_ARTIST
            let title = getStringValue XESAM_TITLE
            let album = getStringValue XESAM_ALBUM
            let length = getMicrosecondsValue MPRIS_LENGTH
            let url = getStringValue XESAM_URL
            let genres = getStringArrayValue XESAM_GENRE
            let playerId = getStringValue CUSTOM_PLAYER_ID

            let metadataMapper = 
                Map.ofList [
                    nameof XESAM_ARTIST, defaultArg (getStringFromArrayValue artists) DEFAULT_ARTIST
                    nameof XESAM_TITLE, defaultArg title DEFAULT_TITLE
                    nameof XESAM_ALBUM, defaultArg album DEFAULT_ALBUM
                    nameof XESAM_URL, defaultArg url DEFAULT_URL
                    nameof XESAM_GENRE, defaultArg (getStringFromArrayValue genres) DEFAULT_GENRE
                ]
            
            match title with
            | Some title ->
                let trackinfo = 
                    { Id = ""
                      Artist = if List.isEmpty artists then [ DEFAULT_ARTIST ] else artists
                      Title = title
                      Album = album
                      Length = length
                      Url= url
                      ReplayCount =0us
                      PlayerId = Option.defaultValue DEFAULT_PLAYER playerId
                      MetadataMap = metadataMapper } 
                Some {trackinfo with Id= getTrackId trackinfo}
            | None -> None
        with _ ->
            None