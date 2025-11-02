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

        List<BattleReport> reports = [];

        foreach (var battle in battles)
        {
            List<BattleEvent> events = [];
            BattleResult result;
            int current1 = 0;
            int current2 = 0;
            while (current1 < Team.MAX_TEAM_SIZE && current2 < Team.MAX_TEAM_SIZE)
            {
                bool skip = false;
                Unit u1 = battle.t1.units[current1];
                Unit u2 = battle.t2.units[current2];
                if (u1 == null || u1.health <= 0)
                {
                    skip = true;
                    current1++;
                }
                if (u2 == null || u2.health <= 0)
                {
                    skip = true;
                    current2++;
                }
                if (skip)
                {
                    continue;
                }

                Unit[] order = [u1, u2];
                order = order.OrderByDescending(u => u.speed).ToArray();

                events.Add(order[0].Attack(order[1]));
                if(order[1].health > 0)
                {
                    events.Add(order[1].Attack(order[0]));
                }

            }
            if (current1 >= Team.MAX_TEAM_SIZE && current2 >= Team.MAX_TEAM_SIZE)
            {
                result = BattleResult.DRAW;
            }
            else if (current1 >= Team.MAX_TEAM_SIZE)
            {
                result = BattleResult.TEAM_2;
            }
            else
            {
                result = BattleResult.TEAM_1;
            }
            reports.Add(new(battle, events, result));
        }
    }
}