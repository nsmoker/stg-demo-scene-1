using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts;
using Godot;

[GlobalClass]
[Tool]
public partial class FindOpenSeatTask : CrowdAITask
{
    [Export]
    public float WalkSpeed;

    public override void StartTask(Character character, StagfootScreen area, double duration, CrowdAIDirector director)
    {
        if (!character.IsSeated())
        {
            var openChairs = area.GetUnnamedFurnitureProps().Where(chair => chair is FurnitureProp furniture && !furniture.Occupied
                && (!director.ChairMap.ContainsKey(chair.GetInstanceId()) || !director.ChairMap[chair.GetInstanceId()]));
            if (openChairs.Any())
            {
                var chair = openChairs.MinBy(x => x.GlobalPosition.DistanceTo(character.GlobalPosition));
                director.ChairMap[chair.GetInstanceId()] = true;
                character.WalkToPoint(chair.GlobalPosition, () =>
                {
                    character.SitOn((Prop) chair);
                }, speed: WalkSpeed);
            }
        }
    }
}