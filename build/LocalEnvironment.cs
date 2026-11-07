using Microsoft.Extensions.Configuration;
namespace GestaoComercial.Hosting;

// Shared host bootstrap source; not compiled into Domain, Application or Contracts.
public static class LocalEnvironment
{
    public static IReadOnlyDictionary<string, string?> Read(string path)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var separator = line.IndexOf('=');
            if (separator < 1) throw new InvalidOperationException("Formato inválido no arquivo .env.");
            var key = line[..separator].Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(key, "^[A-Za-z_][A-Za-z0-9_]*$")) throw new InvalidOperationException("Nome de variável inválido no arquivo .env.");
            var value = line[(separator + 1)..].Trim();
            if (value.Length > 0 && (value.StartsWith('"') || value[0] == (char)39))
            {
                if (value.Length < 2 || value[^1] != value[0]) throw new InvalidOperationException("Aspas inválidas no arquivo .env.");
                value = value[1..^1];
            }
            values[key.Replace("__", ":")] = value;
        }
        return values;
    }

    public static void AddDevelopmentFile(ConfigurationManager config, string environment, string startDirectory)
    {
        if (environment != "Development") return;
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var file = Path.Combine(directory.FullName, ".env");
            if (!File.Exists(file)) continue;
            // Existing process variables take precedence; the file is never changed.
            config.AddInMemoryCollection(Read(file)).AddEnvironmentVariables();
            return;
        }
    }
}
