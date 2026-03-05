using STGDemoScene1.Addons.Edi.Scripts;

namespace STGDemoScene1.Scripts.Systems;

public static class DialogueSystem
{
    private static bool s_inDialogue = false;

    public delegate void DialogueCompleteCallback();

    public delegate void DialogueStartedCallback(Conversation dialogue, int entryPoint);

    public static DialogueStartedCallback OnDialogueStarted { get; set; }
    public static DialogueCompleteCallback OnDialogueComplete { get; set; }

    public static void StartDialogue(Conversation dialogue, int entryPoint)
    {
        if (dialogue != null)
        {
            s_inDialogue = true;
            OnDialogueStarted?.Invoke(dialogue, entryPoint);
        }
    }

    public static void CompleteDialogue()
    {
        s_inDialogue = false;
        OnDialogueComplete?.Invoke();
    }

    public static bool IsInDialogue() => s_inDialogue;
}
