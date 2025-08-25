namespace FSharpMprisDemo
open System

type TrackInfo =
    { Id: string
      Artist: string list
      Title: string
      Album: string option
      Length: TimeSpan option
      Url: string option
      ReplayCount: uint16
      PlayerId: string
      MetadataMap: Map<string, string> }
