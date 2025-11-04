using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Systems.Dialogue;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Systems
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Events")] 
        public UnityEvent OnDialogueStarted;
        public UnityEvent OnDialogueFinished;
        public UnityEvent<string> OnSpeakerChanged;
        public UnityEvent<string> OnTextUpdated;

        [Tooltip("Optional: typing tick will play every N characters")] 
        [SerializeField] private int typingTickEveryNChars = 2;

        private Coroutine running;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[DialogueManager] Instance created and registered.");
            
            // Subscribe to scene loaded event to find UI binders in new scenes
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Find and register all DialogueUIBinders in the current scene (including inactive ones)
            FindAndRegisterUIBinders();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[DialogueManager] Scene loaded: {scene.name}. Searching for DialogueUIBinders...");
            // Find and register UI binders in the newly loaded scene
            FindAndRegisterUIBinders();
        }

        private void FindAndRegisterUIBinders()
        {
            // Find all DialogueUIBinder components, including inactive ones
            DialogueUIBinder[] binders = Resources.FindObjectsOfTypeAll<DialogueUIBinder>();
            Debug.Log($"[DialogueManager] Found {binders.Length} DialogueUIBinder component(s) in the scene.");
            
            foreach (var binder in binders)
            {
                if (binder == null) continue;
                
                Debug.Log($"[DialogueManager] Attempting to register DialogueUIBinder on object: {binder.gameObject.name} (active: {binder.gameObject.activeInHierarchy})");
                
                // Force registration - DialogueUIBinder will handle the check
                binder.ForceRegisterListeners();
            }
            
            // Also find and register DialogueInputBlocker components
            DialogueInputBlocker[] blockers = Resources.FindObjectsOfTypeAll<DialogueInputBlocker>();
            Debug.Log($"[DialogueManager] Found {blockers.Length} DialogueInputBlocker component(s) in the scene.");
            
            foreach (var blocker in blockers)
            {
                if (blocker == null) continue;
                
                Debug.Log($"[DialogueManager] Attempting to register DialogueInputBlocker on object: {blocker.gameObject.name} (active: {blocker.gameObject.activeInHierarchy})");
                
                // Force registration by calling the public method directly
                blocker.RegisterToDialogueManager();
            }
        }

        public void PlayDialogueFromResources(string resourcePath, string dialogueId)
        {
            Debug.Log($"[DialogueManager] PlayDialogueFromResources called: path='{resourcePath}', id='{dialogueId}'");
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[DialogueManager] Dialogue JSON not found at Resources/{resourcePath}");
                return;
            }
            Debug.Log($"[DialogueManager] JSON loaded successfully, length: {asset.text.Length} characters");
            PlayDialogueFromJson(asset.text, dialogueId);
        }

        public void PlayDialogueFromJson(string json, string dialogueId)
        {
            Debug.Log($"[DialogueManager] PlayDialogueFromJson called: dialogueId='{dialogueId}', json length={json.Length}");
            DialogueConfig config = JsonUtility.FromJson<DialogueConfig>(json);
            if (config == null || config.dialogues == null || config.dialogues.Count == 0)
            {
                Debug.LogWarning("[DialogueManager] Dialogue JSON is empty or invalid");
                return;
            }

            Debug.Log($"[DialogueManager] Found {config.dialogues.Count} dialogue entries in JSON");
            DialogueEntry entry = config.dialogues.FirstOrDefault(d => d.dialogueID == dialogueId);
            if (entry == null)
            {
                Debug.LogWarning($"[DialogueManager] Dialogue id not found: {dialogueId}. Available IDs: {string.Join(", ", config.dialogues.Select(d => d.dialogueID))}");
                return;
            }

            // Convert legacy format (single text) to new format (lines)
            if (entry.lines == null || entry.lines.Count == 0)
            {
                // Legacy support: convert single text to lines
                if (!string.IsNullOrEmpty(entry.text))
                {
                    entry.lines = new List<DialogueLine>
                    {
                        new DialogueLine
                        {
                            text = entry.text,
                            speaker = entry.speaker,
                            typingSpeed = entry.typingSpeed,
                            audioClip = entry.audioClip
                        }
                    };
                    Debug.Log("[DialogueManager] Converted legacy dialogue format to new format.");
                }
                else
                {
                    Debug.LogWarning("[DialogueManager] Dialogue entry has no lines and no legacy text.");
                    return;
                }
            }

            Debug.Log($"[DialogueManager] Found dialogue entry: speaker='{entry.speaker}', lines count={entry.lines.Count}");
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(TypeRoutine(entry));
        }

        private IEnumerator TypeRoutine(DialogueEntry entry)
        {
            Debug.Log($"[DialogueManager] TypeRoutine started. OnDialogueStarted has {OnDialogueStarted.GetPersistentEventCount()} persistent listeners (runtime listeners count unavailable).");
            Debug.Log("[DialogueManager] Invoking OnDialogueStarted event.");
            OnDialogueStarted?.Invoke();

            // Process each line of dialogue
            for (int lineIndex = 0; lineIndex < entry.lines.Count; lineIndex++)
            {
                DialogueLine line = entry.lines[lineIndex];
                
                // Wait for event if specified
                if (!string.IsNullOrEmpty(line.waitForEvent))
                {
                    Debug.Log($"[DialogueManager] Waiting for event: {line.waitForEvent}");
                    yield return StartCoroutine(WaitForEvent(line.waitForEvent));
                }

                // Determine speaker for this line (use line-specific or fall back to entry default)
                string currentSpeaker = !string.IsNullOrEmpty(line.speaker) ? line.speaker : entry.speaker;
                OnSpeakerChanged?.Invoke(currentSpeaker);

                // Determine typing speed and audio clip (use line-specific or fall back to entry default)
                float currentTypingSpeed = line.typingSpeed > 0 ? line.typingSpeed : entry.typingSpeed;
                string currentAudioClip = !string.IsNullOrEmpty(line.audioClip) ? line.audioClip : entry.audioClip;

                // Start typing loop sound if provided
                if (!string.IsNullOrEmpty(currentAudioClip) && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PrepareTypingClip(currentAudioClip);
                }

                StringBuilder sb = new StringBuilder();
                int tickCounter = 0;
                bool textSkipped = false;

                // Type out the text character by character
                foreach (char c in line.text)
                {
                    // Check for left mouse button click (LMB) to skip to next line
                    if (Input.GetMouseButtonDown(0)) // 0 = left mouse button
                    {
                        Debug.Log("[DialogueManager] Left mouse button clicked. Showing full text.");
                        // Show full text immediately
                        OnTextUpdated?.Invoke(line.text);
                        textSkipped = true;
                        break;
                    }

                    sb.Append(c);
                    OnTextUpdated?.Invoke(sb.ToString());

                    tickCounter++;
                    if (tickCounter % Mathf.Max(1, typingTickEveryNChars) == 0)
                    {
                        if (!string.IsNullOrEmpty(currentAudioClip) && AudioManager.Instance != null)
                        {
                            AudioManager.Instance.TickType();
                        }
                    }

                    yield return new WaitForSeconds(currentTypingSpeed);
                }

                // Wait for left mouse button click to proceed to next line (or close if last line)
                bool isLastLine = (lineIndex == entry.lines.Count - 1);
                
                while (!Input.GetMouseButtonDown(0)) // Wait until left mouse button is clicked
                {
                    yield return null;
                }
                
                Debug.Log($"[DialogueManager] Left mouse button clicked. {(isLastLine ? "Closing dialogue." : "Proceeding to next line.")}");
            }

            Debug.Log("[DialogueManager] TypeRoutine finished. Invoking OnDialogueFinished event.");
            OnDialogueFinished?.Invoke();
            running = null;
        }

        private IEnumerator WaitForEvent(string eventName)
        {
            bool eventTriggered = false;
            UnityAction eventHandler = () => { eventTriggered = true; };

            // Subscribe to the event
            if (EventManager.Instance != null)
            {
                EventManager.Instance.SubscribeToEvent(eventName, eventHandler);
            }
            else
            {
                Debug.LogWarning("[DialogueManager] EventManager.Instance is null! Cannot wait for event.");
                yield break;
            }

            // Wait until event is triggered
            while (!eventTriggered)
            {
                yield return null;
            }

            // Unsubscribe from the event
            if (EventManager.Instance != null)
            {
                EventManager.Instance.UnsubscribeFromEvent(eventName, eventHandler);
            }

            Debug.Log($"[DialogueManager] Event '{eventName}' was triggered. Continuing dialogue.");
        }
    }
}


