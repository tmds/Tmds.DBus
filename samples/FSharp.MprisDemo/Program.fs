namespace FSharpMprisDemo
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

module Program =

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder(args)
        builder.Services.AddHostedService<ScrobblingWorker>() |> ignore

        builder.Build().Run()

        0 // exit code