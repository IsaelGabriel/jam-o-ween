namespace halloween.Simulation;

public class Team
{
    public const int MAX_TEAM_SIZE = 5;

    public string name = "";

    public Unit?[] units = new Unit[MAX_TEAM_SIZE];

    public Team CreateCopy(bool markAsCopy)
    {
        Team team = new();

        team.name = this.name;
        if(markAsCopy)
        {
            team.name += " (Copy)";
        }
        
        for(int i = 0; i < MAX_TEAM_SIZE; i++)
        {
            team.units[i] = units[i]?.Copy();
        }

        return team;
    }
}