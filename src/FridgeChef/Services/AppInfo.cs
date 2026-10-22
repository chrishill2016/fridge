using System.Reflection;

namespace FridgeChef.Services;

public interface IAppInfo
{
    string Version { get; }
}

public sealed class AppInfo : IAppInfo
{
    public string Version { get; } = FromAssembly(typeof(AppInfo).Assembly);

    internal static string FromAssembly(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        // Strip any "+commit" build metadata.
        return informational?.Split('+')[0] ?? assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
