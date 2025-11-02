using halloween.Simulation;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace halloween;

public static class Importer
{
    private const string UNITS_PATH = @"Data/units.json";

    private static List<Unit> _units;

    public static void ImportAll()
    {
        ImportUnits();
    }

    private static void ImportUnits()
    {
        string json_content = File.ReadAllText(UNITS_PATH);

        _units = new();

        var dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json_content);
        
        foreach(var keyValuePair in dict)
        {
            _units.Add(Unit.FromDictionary(keyValuePair.Key, keyValuePair.Value));
        }
    }
}
