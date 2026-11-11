using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using WiringKitSOP.Models;

namespace WiringKitSOP.Services;

public static class ConfigurationService
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    public static PlcSettings LoadPlcSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Configuration", "plc-settings.json");
        return File.Exists(path) ? JsonSerializer.Deserialize<PlcSettings>(File.ReadAllText(path), Options) ?? new() : new();
    }
    public static List<SopStep> LoadSteps()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Configuration", "wiring-steps.json");
        if (!File.Exists(path)) throw new FileNotFoundException("Configuration/wiring-steps.json was not found.", path);
        var steps = JsonSerializer.Deserialize<List<SopStep>>(File.ReadAllText(path), Options) ?? new();
        if (steps.Count < 1) throw new InvalidDataException("At least one wiring stage must be configured.");
        return steps.OrderBy(x => x.Number).ToList();
    }
}
