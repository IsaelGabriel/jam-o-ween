namespace halloween.Simulation;

public abstract class MatchState
{
    public static Match CurrentMatch;
    public string name = "state";

    public delegate void StateTransition(string nextState);

    public virtual void Init() { }
    public virtual void EnterState() { }
    public virtual void UpdateState() { }
    public virtual void ExitState() { }
}