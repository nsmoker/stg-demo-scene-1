using Godot;
using System;

public static class DialogueSystem
{
    public delegate void DialogueCompleteCallback();
    
    public delegate void DialogueStartedCallback(Conversation dialogue, int entryPoint);

    private static DialogueStartedCallback onDialogueStarted;
    private static DialogueCompleteCallback onDialogueComplete;

    public static DialogueStartedCallback OnDialogueStarted { get => onDialogueStarted; set => onDialogueStarted = value; }
    public static DialogueCompleteCallback OnDialogueComplete { get => onDialogueComplete; set => onDialogueComplete = value; }

    public static void StartDialogue(Conversation dialogue, int entryPoint)
    {
        onDialogueStarted?.Invoke(dialogue, entryPoint);
    }

    public static void CompleteDialogue()
    {
        onDialogueComplete?.Invoke();
    }
}