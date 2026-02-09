using System;

namespace Ameath.DesktopPet.Core;

public sealed class PetStateMachine
{
    public PetState CurrentState { get; private set; }

    public event Action<PetState>? StateEntered;
    public event Action<PetState>? StateExited;

    public void Initialize(PetState initialState)
    {
        CurrentState = initialState;
        StateEntered?.Invoke(initialState);
    }

    public void ChangeState(PetState nextState)
    {
        if (nextState == CurrentState)
        {
            return;
        }

        var previous = CurrentState;
        StateExited?.Invoke(previous);
        CurrentState = nextState;
        StateEntered?.Invoke(nextState);
    }
}
