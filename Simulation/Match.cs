namespace halloween.Simulation;

public class Match
{
    public List<Team> teams;
    private Dictionary<string, MatchState> _states = [];
    private string _currentState;

    public Match(string startingState)
    {
        MatchState.CurrentMatch = this;
        foreach (MatchState state in _states.Values)
        {
            state.Init();
        }

        SetState(startingState);
    }

    public void Update()
    {
        if(IsValidState(_currentState))
        {
            _states[_currentState].UpdateState();
        }
    }

    public void SetState(string state)
    {
        state = state.ToLower();

        if(!IsValidState(state))
        {
            return;
        }

        if (IsValidState(_currentState))
        {
            _states[_currentState].ExitState();
        }

        _currentState = state;

        _states[_currentState].EnterState();
    }
    
    private bool IsValidState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }
        if(!_states.ContainsKey(stateName.ToLower()))
        {
            return false;
        }
        return true;
    }

}