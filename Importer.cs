using halloween.Simulation;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace halloween;

public static class Importer
{
    public const string UNITS_PATH = @"Data/units.json";

    private static Dictionary<string, Unit> _units;

    public static List<string> UnitNames
    {
        get => [.. _units.Keys];
    }

    public static void ImportAll()
    {
        ImportUnits(File.ReadAllText(UNITS_PATH));
    }


    public static void ImportUnits(string json_content)
    {
        _units = new();

        var dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json_content);

        foreach (var keyValuePair in dict)
        {
            _units.Add(keyValuePair.Key, Unit.FromDictionary(keyValuePair.Key, keyValuePair.Value));
        }
    }

    public static Unit GetNewUnit(string key)
    {
        return _units[key];
    }
}
