using System;
using System.Windows.Forms;
using Ameath.DesktopPet.Core;

namespace Ameath.DesktopPet.Controllers;

public sealed class BehaviorController
{
    private readonly PetStateMachine _stateMachine;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Random _random = new();
    private DateTime _lastUserInteraction = DateTime.UtcNow;
    private DateTime _stateEndTime = DateTime.UtcNow;
    private bool _overrideActive;

    public BehaviorController(PetStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        _timer = new System.Windows.Forms.Timer { Interval = 500 };
        _timer.Tick += (_, _) => Tick();
    }

    public void Start()
    {
        _stateEndTime = DateTime.UtcNow + GetRandomDuration(_stateMachine.CurrentState);
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void RegisterInteraction()
    {
        _lastUserInteraction = DateTime.UtcNow;
    }

    public void TriggerInteract()
    {
        RegisterInteraction();
        _stateMachine.ChangeState(PetState.Interact);
        _stateEndTime = DateTime.UtcNow + TimeSpan.FromSeconds(2);
    }

    public void BeginDrag()
    {
        RegisterInteraction();
        _overrideActive = true;
        _stateMachine.ChangeState(PetState.Drag);
    }

    public void EndDrag()
    {
        _overrideActive = false;
        _stateMachine.ChangeState(PetState.Idle);
        _stateEndTime = DateTime.UtcNow + GetRandomDuration(PetState.Idle);
    }

    private void Tick()
    {
        if (_overrideActive)
        {
            return;
        }

        if (_stateMachine.CurrentState == PetState.Interact && DateTime.UtcNow < _stateEndTime)
        {
            return;
        }

        if (DateTime.UtcNow < _stateEndTime)
        {
            return;
        }

        var nextState = PickNextState();
        _stateMachine.ChangeState(nextState);
        _stateEndTime = DateTime.UtcNow + GetRandomDuration(nextState);
    }

    private PetState PickNextState()
    {
        var roll = _random.NextDouble();
        if (roll < 0.5)
        {
            return PetState.Idle;
        }

        if (roll < 0.85)
        {
            return PetState.Wander;
        }

        return PetState.Idle;
    }

    private TimeSpan GetRandomDuration(PetState state)
    {
        return state switch
        {
            PetState.Wander => TimeSpan.FromSeconds(_random.Next(4, 8)),
            PetState.Sleep => TimeSpan.FromSeconds(_random.Next(6, 12)),
            PetState.Interact => TimeSpan.FromSeconds(2),
            PetState.Drag => TimeSpan.FromSeconds(1),
            _ => TimeSpan.FromSeconds(_random.Next(3, 6))
        };
    }
}
