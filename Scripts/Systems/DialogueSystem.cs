using Godot;
using System;

public static class DialogueSystem
{
    public delegate void DialogueCompleteCallback();

    public delegate void DialogueStartedCallback(Conversation dialogue, int entryPoint);

    public static DialogueStartedCallback OnDialogueStarted { get; set; }
    public static DialogueCompleteCallback OnDialogueComplete { get; set; }

    public static void StartDialogue(Conversation dialogue, int entryPoint) => OnDialogueStarted?.Invoke(dialogue, entryPoint);

    public static void CompleteDialogue() => OnDialogueComplete?.Invoke();
}
