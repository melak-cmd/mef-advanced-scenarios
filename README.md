# MEF with advanced scenarios, such as reloading plugins, managing dependencies, or handling exceptions during plugin loading and unloading.

- Reloading Plugins: The ComposePlugins method now includes support for reloading plugins. Before creating a new container, the previous container is disposed of to release resources. This allows you to load new or updated plugins dynamically during runtime.

- Managing Dependencies: I've added a new ILogger interface and a ConsoleLogger implementation to demonstrate dependency injection in plugins. The PluginB class depends on an ILogger instance, and MEF handles the dependency injection automatically when composing the plugins. You can define additional interfaces and implementations to support more complex dependency scenarios.

- Handling Exceptions: The LoadPlugin method is wrapped in a try-catch block to handle exceptions that may occur during plugin loading. If an exception occurs, an error message is displayed along with the specific error message.
