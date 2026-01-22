using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArkhamHunters.Scripts;
using Godot;

[GlobalClass]
[Tool]
public partial class TalkToPartnerTask : CrowdAITask
{
    [Export]
    public float WalkSpeed;

    public override void StartTask(Character character, StagfootScreen area, double duration, CrowdAIDirector director)
    {
        var state = new CrowdAICharacterState
        {
            Task = this,
            RemainingDuration = Duration
        };
         var possibleConversationPartners = director.ManagedCharacters.Where(c =>
        {
            var t = director.GetTask(c.GetInstanceId());
            var s = director.GetState(c.GetInstanceId());
            return t is not TalkToPartnerTask || s.RemainingDuration <= 0.0f;
        }).ToList();
        if (possibleConversationPartners.Count > 0)
        {
            int partnerIndex = director.DirectorRandom.Next(0, possibleConversationPartners.Count);
            Character partnerInstance = possibleConversationPartners[partnerIndex];
            CrowdAICharacterState partnerState = director.GetState(partnerInstance.GetInstanceId());
            // Don't pick ourselves as a conversation partner
            if (partnerInstance.GetInstanceId().Equals(character.GetInstanceId()))
            {
                partnerIndex = (partnerIndex + 1) % possibleConversationPartners.Count;
                partnerInstance = possibleConversationPartners[partnerIndex];
                partnerState = director.GetState(partnerInstance.GetInstanceId());
            }
            partnerState.OnComplete?.Invoke();
            director.SetState(partnerInstance.GetInstanceId(), state);

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
    }
}