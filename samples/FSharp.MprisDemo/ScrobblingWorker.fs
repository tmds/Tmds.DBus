namespace FSharpMprisDemo
open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Tmds.DBus

type ScrobblingWorker(logger: ILogger<ScrobblingWorker>) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        task {
            try
                use con = new Connection(Address.Session)
                let connectionStateChangedEventHandler (eArgs: ConnectionStateChangedEventArgs) =
                    printfn $"Connection state changed to {eArgs.State}"

                con.StateChanged.Add connectionStateChangedEventHandler
                if con = null then
                    printfn "Failed to create DBus connection."
                else
                    // Can only connect once
                    let! connectionInfo = con.ConnectAsync() |> Async.AwaitTask
                    printfn $"Connected to Localname:{connectionInfo.LocalName}, RemoteName: {connectionInfo.RemoteIsBus}"
                let players = Mpris.Mpris.getMediaPlayers 1 con
                while not ct.IsCancellationRequested do
                    do! Task.Delay(5000, ct)
                    do! Mpris.Mpris.pollMediaPlayers players con

            with
            | :? OperationCanceledException ->
                // Task was cancelled, exit gracefully
                ()
            | ex ->
                Printf.eprintf "Error in %s daemon service, %s" (nameof ScrobblingWorker) ex.Message
                return raise ex
        }