using System.IO;
using System.Text.Json;
using DJConnect.Windows.Models;

namespace DJConnect.Windows.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public SettingsStore()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DJConnect");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "settings.json");
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_path))
        {
            return new AppSettings();
        }

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, Options) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, settings, Options);
    }
}
