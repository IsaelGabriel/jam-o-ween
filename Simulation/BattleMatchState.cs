namespace halloween.Simulation;

public class BattleMatchState : MatchState
{
    private static Random rng = new();
    private void CalculateBattles()
    {
        List<Tuple<string, string>> brackets = CurrentMatch.nextBattles.OrderBy(_ => rng.Next()).ToList();
        List<string> teamsAlreadyInBracket = [];


        List<(Team t1, Team t2)> battles = [];

        foreach (var bracket in brackets)
        {
            Team team1, team2;
            if (!teamsAlreadyInBracket.Contains(bracket.Item1))
            {
                team1 = CurrentMatch.teams[bracket.Item1];
                teamsAlreadyInBracket.Add(bracket.Item1);
            }
            else
            {
                team1 = CurrentMatch.teams[bracket.Item1].CreateCopy(true);
            }
            if (!teamsAlreadyInBracket.Contains(bracket.Item2))
            {
                team2 = CurrentMatch.teams[bracket.Item2];
                teamsAlreadyInBracket.Add(bracket.Item2);
            }
            else
            {
                team2 = CurrentMatch.teams[bracket.Item2].CreateCopy(true);
            }

            battles.Add((team1, team2));
        }
        

    }
}