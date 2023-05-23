using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConsoleApp
{
    public interface IPlugin
    {
        void Run();
    }

    [Export(typeof(IPlugin))]
    public class PluginA : IPlugin
    {
        public void Run()
        {
            Console.WriteLine("Plugin A is running.");
        }
    }

    [Export(typeof(IPlugin))]
    public class PluginB : IPlugin
    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public PluginB(ILogger logger)
        {
            this.logger = logger;
        }

        public void Run()
        {
            logger.Log("Plugin B is running.");
        }
    }

    public interface ILogger
    {
        void Log(string message);
    }

    [Export(typeof(ILogger))]
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine($"[LOG] {message}");
        }
    }

    class Program
    {
        private CompositionContainer container;
        private List<IPlugin> loadedPlugins;

        private void ComposePlugins()
        {
            var catalog = new DirectoryCatalog(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var batch = new CompositionBatch();
            loadedPlugins = new List<IPlugin>();

            // Add support for reloading plugins
            if (container != null)
            {
                container.Dispose();
            }

            container = new CompositionContainer(catalog);
            batch.AddPart(this);
            container.Compose(batch);
        }

        private void LoadPlugin(string pluginAssemblyPath)
        {
            try
            {
                var catalog = new AssemblyCatalog(pluginAssemblyPath);
                var batch = new CompositionBatch();
                batch.AddPart(catalog);
                container.Compose(batch);

                var plugins = catalog.Parts.Select(part => part.CreatePart()).OfType<IPlugin>().ToList();
                loadedPlugins.AddRange(plugins);

                Console.WriteLine($"Plugin(s) loaded successfully from assembly: {Path.GetFileName(pluginAssemblyPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load plugin from assembly: {Path.GetFileName(pluginAssemblyPath)}");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void UnloadPlugin(IPlugin plugin)
        {
            if (loadedPlugins.Contains(plugin))
            {
                loadedPlugins.Remove(plugin);
                Console.WriteLine($"Plugin {plugin.GetType().Name} unloaded successfully.");
            }
            else
            {
                Console.WriteLine("The plugin is not loaded.");
            }
        }

        private void RunPlugins()
        {
            foreach (var plugin in loadedPlugins)
            {
                plugin.Run();
            }
        }

        static void Main(string[] args)
        {
            var program = new Program();
            program.ComposePlugins();
            program.RunPlugins();

            Console.WriteLine("Enter 'load' to load a plugin, 'unload' to unload a plugin, or 'exit' to exit the program.");
            string input;
            while ((input = Console.ReadLine()) != "exit")
            {
                if (input == "load")
                {
                    Console.Write("Enter the path to the plugin assembly: ");
                    string pluginPath = Console.ReadLine();
                    program.LoadPlugin(pluginPath);
                }
                else if (input == "unload")
                {
                    Console.Write("Enter the index of the plugin to unload: ");
                    if (int.TryParse(Console.ReadLine(), out int index) && index >= 0 && index < program.loadedPlugins.Count)
                    {
                        var plugin = program.loadedPlugins[index];
                        program.UnloadPlugin(plugin);
                    }
                    else
                    {
                        Console.WriteLine("Invalid index.");
                    }
                }
            }
        }
    }
}
