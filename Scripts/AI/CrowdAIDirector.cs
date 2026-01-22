using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public struct CrowdAICharacterState
{
    public CrowdAITask Task;
    public double RemainingDuration;
    public Action OnComplete = () => { };

    public CrowdAICharacterState() { }
}

[GlobalClass]
[Tool]
/// <summary>
/// A crowd AI Director cycles a group of NPCs through a defined set of tasks with durations and draw probabilities.
/// </summary>
public partial class CrowdAIDirector : Resource
{
    [Export]
    private Godot.Collections.Array<CrowdAITask> _possibleTasks;

    public Godot.Collections.Array<CrowdAITask> PossibleTasks
    {
        get { return _possibleTasks; }
        set
        {
            foreach (var character in _managedCharacters)
            {
                var state = _states[character.GetInstanceId()];
                _states[character.GetInstanceId()] = new CrowdAICharacterState
                {
                    Task = state.Task,
                    RemainingDuration = 0.0f,
                    OnComplete = state.OnComplete
                };
            }
            _possibleTasks = value;
        }
    }

    [Export]
    public float WalkSpeed = 20.0f;

    private List<Character> _managedCharacters = [];
    private Dictionary<ulong, CrowdAICharacterState> _states = [];
    private Dictionary<ulong, bool> _chairMap = [];
    private readonly Random _random = new();

    public Random DirectorRandom { get { return _random; }}

    public Dictionary<ulong, bool> ChairMap { get { return _chairMap; }}

    public List<Character> ManagedCharacters { get { return _managedCharacters; } set
        {
            _managedCharacters = [ ..value];
            _states.Clear();
            foreach (Character c in _managedCharacters)
            {
                CrowdAITask task = DrawRandomTask();
                var state = new CrowdAICharacterState
                {
                    Task = task,
                    RemainingDuration = 0.0f
                };
                _states.Add(c.GetInstanceId(), state);
            }
        }
    }

    private CrowdAITask DrawRandomTask()
    {
        float draw = _random.NextSingle();
        CrowdAITask returnTask = new();
        foreach (CrowdAITask task in PossibleTasks)
        {
            draw -= task.Probability;
            if (draw <= 0)
            {
                returnTask = task;
                break;
            }
        }

        return returnTask;
    }

    public CrowdAITask GetTask(ulong instanceId)
    {
        return _states[instanceId].Task;
    }

    public void SetState(ulong instanceId, CrowdAICharacterState state)
    {
        _states[instanceId] = state;
    }

    public CrowdAICharacterState GetState(ulong instanceId)
    {
        return _states[instanceId];
    }

    public void StartTask(CrowdAITask task, Character character, StagfootScreen area, double duration)
    {
        var state = new CrowdAICharacterState
        {
            Task = task,
            RemainingDuration = task.Duration
        };
        task.StartTask(character, area, duration, this);

        SetState(character.GetInstanceId(), state);
    }

    public void Process(double delta, StagfootScreen area)
    {
        foreach (Character c in _managedCharacters)
        {
            var state = GetState(c.GetInstanceId());
            state.RemainingDuration -= delta;

            if (state.RemainingDuration <= 0)
            {
                state.Task = DrawRandomTask();
                state.RemainingDuration = state.Task.Duration;
                StartTask(state.Task, c, area, state.Task.Duration);
            }
            else
            {
                SetState(c.GetInstanceId(), state);
            }
        }
    }
}
