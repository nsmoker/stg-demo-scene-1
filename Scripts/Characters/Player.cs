using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.Characters;

public partial class Player : Character
{
    public override void _Ready()
    {
        base._Ready();
        foreach (Quest q in CharacterData.Journal)
        {
            QuestSystem.AddQuest(q);
        }
    }
}

