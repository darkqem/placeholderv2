using System;
using System.Collections.Generic;

namespace Systems.Dialogue
{
    [Serializable]
    public class DialogueEntry
    {
        public string dialogueID;
        public string speaker;
        public string text;
        public float typingSpeed = 0.05f; // seconds per character
        public string audioClip; // clip key for typing sound
    }

    [Serializable]
    public class DialogueConfig
    {
        public List<DialogueEntry> dialogues = new List<DialogueEntry>();
    }
}


