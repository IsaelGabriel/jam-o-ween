namespace halloween.Simulation;

public abstract class MatchState
{
    public static Match CurrentMatch;
    public string name = "state";

    public delegate void StateTransition(string nextState);

    public abstract void Init();
    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}