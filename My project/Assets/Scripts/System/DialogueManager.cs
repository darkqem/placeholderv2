using System.Collections;
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

            Debug.Log($"[DialogueManager] Found dialogue entry: speaker='{entry.speaker}', text length={entry.text.Length}, typingSpeed={entry.typingSpeed}");
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(TypeRoutine(entry));
        }

        private IEnumerator TypeRoutine(DialogueEntry entry)
        {
            Debug.Log($"[DialogueManager] TypeRoutine started. OnDialogueStarted has {OnDialogueStarted.GetPersistentEventCount()} persistent listeners (runtime listeners count unavailable).");
            Debug.Log("[DialogueManager] Invoking OnDialogueStarted event.");
            OnDialogueStarted?.Invoke();
            Debug.Log($"[DialogueManager] Invoking OnSpeakerChanged with: '{entry.speaker}'. OnSpeakerChanged has {OnSpeakerChanged.GetPersistentEventCount()} persistent listeners (runtime listeners count unavailable).");
            OnSpeakerChanged?.Invoke(entry.speaker);

            StringBuilder sb = new StringBuilder();
            int tickCounter = 0;

            // Start typing loop sound if provided
            if (!string.IsNullOrEmpty(entry.audioClip) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PrepareTypingClip(entry.audioClip);
            }

            foreach (char c in entry.text)
            {
                // Check for right mouse button click to skip dialogue
                if (Input.GetMouseButtonDown(1)) // 1 = right mouse button
                {
                    Debug.Log("[DialogueManager] Right mouse button clicked. Skipping dialogue.");
                    // Show full text immediately
                    OnTextUpdated?.Invoke(entry.text);
                    break;
                }

                sb.Append(c);
                OnTextUpdated?.Invoke(sb.ToString());

                tickCounter++;
                if (tickCounter % Mathf.Max(1, typingTickEveryNChars) == 0)
                {
                    if (!string.IsNullOrEmpty(entry.audioClip) && AudioManager.Instance != null)
                    {
                        AudioManager.Instance.TickType();
                    }
                }

                yield return new WaitForSeconds(entry.typingSpeed);
            }

            // Wait for right mouse button click to close dialogue if text is fully displayed
            while (!Input.GetMouseButtonDown(1)) // Wait until right mouse button is clicked
            {
                yield return null;
            }
            
            Debug.Log("[DialogueManager] Right mouse button clicked. Closing dialogue.");

            Debug.Log("[DialogueManager] TypeRoutine finished. Invoking OnDialogueFinished event.");
            OnDialogueFinished?.Invoke();
            running = null;
        }
    }
}


