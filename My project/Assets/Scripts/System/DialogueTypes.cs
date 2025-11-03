using System;
using System.Collections.Generic;

namespace Systems.Dialogue
{
    [Serializable]
    public class DialogueLine
    {
        public string text;
        public string speaker; // Optional: can override dialogue-level speaker
        public float typingSpeed = 0.05f; // seconds per character
        public string audioClip; // clip key for typing sound
        public string waitForEvent; // Optional: event name to wait for before showing this line
    }

    [Serializable]
    public class DialogueEntry
    {
        public string dialogueID;
        public string speaker; // Default speaker for all lines
        public List<DialogueLine> lines = new List<DialogueLine>(); // Multiple lines of dialogue
        public float typingSpeed = 0.05f; // Default typing speed
        public string audioClip; // Default audio clip
        
        // Legacy support: if text is set, convert it to a single line
        [Obsolete("Use 'lines' instead")]
        public string text;
    }

    [Serializable]
    public class DialogueConfig
    {
        public List<DialogueEntry> dialogues = new List<DialogueEntry>();
    }
}


