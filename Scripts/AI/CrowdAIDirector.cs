using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[GlobalClass]
[Tool]
/// <summary>
/// A crowd AI Director cycles a group of NPCs through a defined set of tasks with durations and draw probabilities.
/// </summary>
public partial class CrowdAIDirector : Resource
{
    public struct CrowdAICharacterState
    {
        public CrowdAITask Task;
        public double RemainingDuration;
    }

    [Export]
    public Godot.Collections.Array<CrowdAITask> PossibleTasks;

    [Export]
    public float WalkSpeed = 20.0f;

    private List<Character> _managedCharacters = [];
    private Dictionary<ulong, CrowdAICharacterState> _states = [];
    private Random _random = new();

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
        switch (task.Type)
        {
            case CrowdAITaskType.WalkToRandomPoint:
                {
                    var targetPoint = character.ToLocal(area.GetRandomTraversablePoint());
                    character.WalkToPoint(targetPoint, onComplete: () =>
                    {
                        SetState(character.GetInstanceId(), new CrowdAICharacterState
                        {
                            Task = task,
                            RemainingDuration = 0.0f
                        });
                    }, speed: WalkSpeed);

                    SetState(character.GetInstanceId(), new CrowdAICharacterState
                    {
                        Task = task,
                        RemainingDuration = task.Duration
                    });

                    break;
                }
            case CrowdAITaskType.Idle:
                {
                    character.SetIdle();
                    SetState(character.GetInstanceId(), new CrowdAICharacterState
                    {
                        Task = task,
                        RemainingDuration = task.Duration
                    });
                    break;
                }
            case CrowdAITaskType.TalkToPartner:
                {
                    // Do not interrupt current conversations.
                    var possibleConversationPartners = _managedCharacters.Where(c => GetTask(c.GetInstanceId()).Type != CrowdAITaskType.TalkToPartner).ToList();
                    if (possibleConversationPartners.Count > 0)
                    {
                        int partnerIndex = _random.Next(0, possibleConversationPartners.Count);
                        Character partnerInstance = possibleConversationPartners[partnerIndex];
                        CrowdAICharacterState partnerState = GetState(partnerInstance.GetInstanceId());
                        // Don't pick ourselves as a conversation partner
                        if (partnerInstance.GetInstanceId().Equals(character.GetInstanceId()))
                        {
                            partnerIndex = (partnerIndex + 1) % possibleConversationPartners.Count;
                            partnerInstance = possibleConversationPartners[partnerIndex];
                            partnerState = GetState(partnerInstance.GetInstanceId());
                        }
                        var talkState = new CrowdAICharacterState
                        {
                            Task = task,
                            RemainingDuration = task.Duration
                        };
                        SetState(partnerInstance.GetInstanceId(), talkState);
                        SetState(character.GetInstanceId(), talkState);

                        partnerInstance.WalkToCharacter(character, () =>
                        {
                            partnerInstance.SetTalking();
                            partnerInstance.SetFacing(partnerInstance.ToLocal(character.GlobalPosition));
                        }, WalkSpeed, 12.0f);
                        character.WalkToCharacter(partnerInstance, () =>
                        {
                            character.SetTalking();
                            character.SetFacing(character.ToLocal(partnerInstance.GlobalPosition));
                        }, WalkSpeed, 12.0f);
                    }

                    break;
                }
            default:
                break;
        }
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
