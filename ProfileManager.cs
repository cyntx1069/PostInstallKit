using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PostInstallKit;

public static class ProfileManager
{
    // Profiles are stored next to the exe, so they travel with the app (USB stick friendly)
    private static string Folder => Path.Combine(AppContext.BaseDirectory, "Profiles");

    private static string PathFor(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return Path.Combine(Folder, name + ".json");
    }

    public static List<string> GetProfileNames()
    {
        if (!Directory.Exists(Folder)) return new List<string>();
        return Directory.GetFiles(Folder, "*.json")
                        .Select(Path.GetFileNameWithoutExtension)
                        .Where(n => n != null)
                        .Select(n => n!)
                        .OrderBy(n => n)
                        .ToList();
    }

    public static void Save(string name, IEnumerable<string> wingetIds)
    {
        Directory.CreateDirectory(Folder);
        var json = JsonSerializer.Serialize(wingetIds.ToList(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(PathFor(name), json);
    }

    public static HashSet<string> Load(string name)
    {
        var path = PathFor(name);
        if (!File.Exists(path)) return new HashSet<string>();
        var ids = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path)) ?? new List<string>();
        return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
    }

    public static void Delete(string name)
    {
        var path = PathFor(name);
        if (File.Exists(path)) File.Delete(path);
    }
}