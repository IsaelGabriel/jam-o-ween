namespace halloween.Simulation;

public enum BattleAction
{
    ATTACK
}

public enum BattleResult
{
    TEAM_1,
    TEAM_2,
    DRAW
}

public struct BattleEvent(Unit source, BattleAction action, int ammount, Unit[] affected)
{
    Unit source = source;
    BattleAction action = action;
    int ammount = ammount;
    Unit[] affected = affected;
}

public struct BattleReport((Team t1, Team t2) teams, List<BattleEvent> events, BattleResult result)
{
    (Team t1, Team t2) teams = teams;
    List<BattleEvent> events = events;
    BattleResult result = result;
}

