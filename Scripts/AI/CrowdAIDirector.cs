using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        public Action OnComplete = () => { };

        public CrowdAICharacterState() { }
    }

    [Export]
    private Godot.Collections.Array<CrowdAITask> _possibleTasks;

    public Godot.Collections.Array<CrowdAITask> PossibleTasks
    {
        get => _possibleTasks;
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
    private readonly Dictionary<ulong, CrowdAICharacterState> _states = [];
    private readonly Dictionary<ulong, bool> _chairMap = [];
    private readonly Random _random = new();

    public List<Character> ManagedCharacters
    {
        get => _managedCharacters;
        set
        {
            _managedCharacters = [.. value];
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

    public CrowdAITask GetTask(ulong instanceId) => _states[instanceId].Task;

    public void SetState(ulong instanceId, CrowdAICharacterState state) => _states[instanceId] = state;

    public CrowdAICharacterState GetState(ulong instanceId) => _states[instanceId];

    public void StartTask(CrowdAITask task, Character character, StagfootScreen area, double duration)
    {
        var state = new CrowdAICharacterState
        {
            Task = task,
            RemainingDuration = task.Duration
        };
        switch (task.Type)
        {
            case CrowdAITaskType.WalkToRandomPoint:
                {
                    var targetPoint = character.ToLocal(area.GetRandomTraversablePoint());
                    character.WalkToPoint(targetPoint, onComplete: () => SetState(character.GetInstanceId(), new CrowdAICharacterState
                    {
                        Task = task,
                        RemainingDuration = 0.0f
                    }), speed: WalkSpeed);
                    break;
                }
            case CrowdAITaskType.Idle:
                {
                    character.SetIdle();
                    break;
                }
            case CrowdAITaskType.FindOpenSeat:
                {
                    if (!character.IsSeated())
                    {
                        var openChairs = area.GetUnnamedFurnitureProps().Where(chair => chair is FurnitureProp furniture && !furniture.Occupied
                            && (!_chairMap.ContainsKey(chair.GetInstanceId()) || !_chairMap[chair.GetInstanceId()]));
                        if (openChairs.Any())
                        {
                            var chair = openChairs.MinBy(x => x.GlobalPosition.DistanceTo(character.GlobalPosition));
                            _chairMap[chair.GetInstanceId()] = true;
                            character.WalkToPoint(chair.GlobalPosition, () => character.SitOn((Prop) chair), speed: WalkSpeed);
                            state.OnComplete = () => _chairMap[chair.GetInstanceId()] = false;
                        }
                    }
                    break;
                }
            case CrowdAITaskType.TalkToPartner:
                {
                    // Do not interrupt current conversations.
                    var possibleConversationPartners = _managedCharacters.Where(c =>
                    {
                        var t = GetTask(c.GetInstanceId());
                        return t.Type != CrowdAITaskType.TalkToPartner || _states[c.GetInstanceId()].RemainingDuration <= 0.0f;
                    }).ToList();
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
                        partnerState.OnComplete?.Invoke();
                        SetState(partnerInstance.GetInstanceId(), state);

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
            case CrowdAITaskType.FollowCrowdFlow:
                {
                    var randomField = area.FlowFields[task.Tag];
                    var flow = randomField.SampleFlowField(area.ToLocal(character.GlobalPosition));
                    character.WalkToPoint(character.GlobalPosition + flow.Normalized() * WalkSpeed, () =>
                    {
                        var state = GetState(character.GetInstanceId());
                        StartTask(new CrowdAITask
                        {
                            Type = CrowdAITaskType.FollowCrowdFlow,
                            Duration = state.RemainingDuration,
                            Tag = task.Tag
                        }, character, area, state.RemainingDuration);
                        if (!area.CheckBounds(character.Position))
                        {
                            character.GlobalPosition = area.ToGlobal(area.GetEdge() + new Vector2(0.0f, -100.0f + _random.NextSingle() * 200.0f));
                        }
                    }, 20.0f);
                    break;
                }
            default:
                break;
        }

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
