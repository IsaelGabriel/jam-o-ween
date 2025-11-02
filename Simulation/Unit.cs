namespace halloween.Simulation;

public class Unit
{
    public string name = "Unit";
    public int speed = 10;
    public int power = 10;


    public static Unit FromDictionary(string name, Dictionary<string, string> dict)
    {
        Unit unit = new();

        unit.name = name;
        unit.power = Int32.Parse(dict["power"]);
        unit.speed = Int32.Parse(dict["speed"]);

        return unit;
    }

}