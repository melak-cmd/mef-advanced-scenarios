open System
open System.Collections.Generic
open System.ComponentModel.Composition
open System.ComponentModel.Composition.Hosting
open System.IO
open System.Linq
open System.Reflection

type IPlugin =
    abstract member Run : unit -> unit

[<Export(typeof<IPlugin>)>]
type PluginA() =
    interface IPlugin with
        member this.Run() = printfn "Plugin A is running."

[<Export(typeof<IPlugin>)>]
type PluginB(logger: ILogger) =
    interface IPlugin with
        member this.Run() = logger.Log("Plugin B is running.")

type ILogger =
    abstract member Log : string -> unit

[<Export(typeof<ILogger>)>]
type ConsoleLogger() =
    interface ILogger with
        member this.Log(message) = printfn "[LOG] %s" message

type Program() =
    let mutable container: CompositionContainer = null
    let mutable loadedPlugins: List<IPlugin> = null

    member private this.ComposePlugins() =
        let catalog = DirectoryCatalog(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
        let batch = CompositionBatch()
        loadedPlugins <- List<IPlugin>()

        // Add support for reloading plugins
        match container with
        | null -> container <- CompositionContainer(catalog)
        | _ -> container.Dispose(); container <- CompositionContainer(catalog)

        batch.AddPart(this)
        container.Compose(batch)

    member private this.LoadPlugin(pluginAssemblyPath: string) =
        try
            let catalog = AssemblyCatalog(pluginAssemblyPath)
            let batch = CompositionBatch()
            batch.AddPart(catalog)
            container.Compose(batch)

            let plugins = catalog.Parts |> Seq.map (fun part -> part.CreatePart() :?> IPlugin) |> List.ofSeq
            loadedPlugins <- loadedPlugins @ plugins

            printfn "Plugin(s) loaded successfully from assembly: %s" (Path.GetFileName(pluginAssemblyPath))
        with
        | ex ->
            printfn "Failed to load plugin from assembly: %s" (Path.GetFileName(pluginAssemblyPath))
            printfn "Error: %s" ex.Message

    member private this.UnloadPlugin(plugin: IPlugin) =
        if loadedPlugins.Contains(plugin) then
            loadedPlugins <- loadedPlugins |> List.filter ((<>) plugin)
            printfn "Plugin %s unloaded successfully." plugin.GetType().Name
        else
            printfn "The plugin is not loaded."

    member private this.RunPlugins() =
        for plugin in loadedPlugins do
            plugin.Run()

    member this.Main(args: string[]) =
        this.ComposePlugins()
        this.RunPlugins()

        printfn "Enter 'load' to load a plugin, 'unload' to unload a plugin, or 'exit' to exit the program."
        let rec loop() =
            match Console.ReadLine() with
            | "exit" -> ()
            | "load" ->
                printf "Enter the path to the plugin assembly: "
                let pluginPath = Console.ReadLine()
                this.LoadPlugin(pluginPath)
                loop()
            | "unload" ->
                printf "Enter the index of the plugin to unload: "
                match Int32.TryParse(Console.ReadLine()) with
                | true, index when index >= 0 && index < loadedPlugins.Count ->
                    let plugin = loadedPlugins.[index]
                    this.UnloadPlugin(plugin)
                    loop()
                | _ ->
                    printfn "Invalid index."
                    loop()
            | _ -> loop()

let program = Program()
program.Main(Array.empty)
