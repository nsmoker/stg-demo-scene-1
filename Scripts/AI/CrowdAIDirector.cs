using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[GlobalClass]
[Tool]
/// <summary>
/// A crowd AI Director cycles a group of NPCs through a defined set of tasks. 
/// </summary>
public partial class CrowdAIDirector : Resource
{
    public class CrowdAICharacterState
    {
        public Character Instance;
        public CrowdAITask Task;
        public bool TaskStarted;
        public bool TaskComplete;
        public double RemainingDuration;
    }

    [Export]
    public Godot.Collections.Array<CrowdAITask> PossibleTasks;

    [Export]
    public float WalkSpeed = 20.0f;

    private List<Character> _managedCharacters = [];
    private List<CrowdAICharacterState> _states = [];
    private Random _random = new Random();

    public List<Character> ManagedCharacters { get { return _managedCharacters; } set
        {
            _managedCharacters = [ ..value];
            _states.Clear();
            foreach (Character c in _managedCharacters)
            {
                CrowdAITask task = DrawRandomTask();
                var state = new CrowdAICharacterState
                {
                    Instance = c,
                    Task = task,
                    TaskStarted = false,
                    TaskComplete = false,
                    RemainingDuration = task.Duration
                };
                _states.Add(state);
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

    public void StartTask(CrowdAITask task, Character character, StagfootScreen area)
    {
        switch (task.Type)
        {
            case CrowdAITaskType.WalkToRandomPoint:
                {
                    var targetPoint = character.ToLocal(area.GetRandomTraversablePoint());
                    character.WalkToPoint(targetPoint, onComplete: () =>
                    {
                        var state = _states[_states.FindIndex(x => x.Instance.GetInstanceId() == character.GetInstanceId())];
                        state.TaskComplete = true;
                    }, speed: WalkSpeed);

                    break;
                }
            case CrowdAITaskType.Idle:
                {
                    character.SetIdle();
                    break;
                }
            default:
                break;
        }

        var state = _states[_states.FindIndex(x => x.Instance.GetInstanceId() == character.GetInstanceId())];
        state.TaskStarted = true;
    }

    public void Process(double delta, StagfootScreen area)
    {
        foreach (CrowdAICharacterState state in _states)
        {
            // End the task no matter what if the alloted duration has passed.
            state.RemainingDuration -= delta;
            state.TaskComplete = state.TaskComplete || state.RemainingDuration <= 0;

            if (!state.TaskStarted)
            {
                StartTask(state.Task, state.Instance, area);
            } else if (state.TaskComplete)
            {
                state.Task = DrawRandomTask();
                state.RemainingDuration = state.Task.Duration;
                state.TaskStarted = false;
                state.TaskComplete = false;
            }
        }
    }
}
