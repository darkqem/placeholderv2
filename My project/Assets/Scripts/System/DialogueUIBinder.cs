using TMPro;
using UnityEngine;

namespace Systems
{
    public class DialogueUIBinder : MonoBehaviour
    {
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject panelRoot;

        private bool listenersRegistered;

        // Public method to force registration (can be called from DialogueManager)
        public void ForceRegisterListeners()
        {
            Debug.Log($"[DialogueUIBinder] ForceRegisterListeners called on {gameObject.name} (active: {gameObject.activeInHierarchy})");
            RegisterListeners();
        }

        private void Awake()
        {
            Debug.Log($"[DialogueUIBinder] Awake called on {gameObject.name} (active: {gameObject.activeInHierarchy})");
            // Try to register in Awake - this is called even for inactive objects
            RegisterListeners();
        }

        private void OnEnable()
        {
            Debug.Log("[DialogueUIBinder] OnEnable called.");
            RegisterListeners();
        }

        private void Start()
        {
            Debug.Log("[DialogueUIBinder] Start called.");
            // Also try to register in Start, in case DialogueManager is created after OnEnable
            RegisterListeners();
        }

        private void OnDisable()
        {
            UnregisterListeners();
        }

        private void RegisterListeners()
        {
            Debug.Log($"[DialogueUIBinder] RegisterListeners called. listenersRegistered={listenersRegistered}, DialogueManager.Instance={DialogueManager.Instance != null}");
            
            if (listenersRegistered)
            {
                Debug.Log("[DialogueUIBinder] Listeners already registered, skipping.");
                return;
            }
            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("[DialogueUIBinder] DialogueManager.Instance is null! Cannot register listeners. Make sure DialogueManager exists in the scene.");
                return;
            }

            Debug.Log("[DialogueUIBinder] Registering event listeners...");
            DialogueManager.Instance.OnSpeakerChanged.AddListener(OnSpeakerChanged);
            DialogueManager.Instance.OnTextUpdated.AddListener(OnTextUpdated);
            DialogueManager.Instance.OnDialogueStarted.AddListener(OnDialogueStarted);
            DialogueManager.Instance.OnDialogueFinished.AddListener(OnDialogueFinished);
            
            listenersRegistered = true;
            Debug.Log("[DialogueUIBinder] Event listeners registered successfully!");
            
            // Log how many listeners are registered
            int startedCount = DialogueManager.Instance.OnDialogueStarted.GetPersistentEventCount();
            Debug.Log($"[DialogueUIBinder] OnDialogueStarted has {startedCount} persistent listeners and our delegate should be added.");
        }

        private void UnregisterListeners()
        {
            if (!listenersRegistered) return;
            if (DialogueManager.Instance == null) return;

            DialogueManager.Instance.OnSpeakerChanged.RemoveListener(OnSpeakerChanged);
            DialogueManager.Instance.OnTextUpdated.RemoveListener(OnTextUpdated);
            DialogueManager.Instance.OnDialogueStarted.RemoveListener(OnDialogueStarted);
            DialogueManager.Instance.OnDialogueFinished.RemoveListener(OnDialogueFinished);
            
            listenersRegistered = false;
        }

        private void OnDialogueStarted()
        {
            Debug.Log("[DialogueUIBinder] OnDialogueStarted called. Activating panel root.");
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                Debug.Log("[DialogueUIBinder] Panel root activated.");
            }
            else
            {
                Debug.LogWarning("[DialogueUIBinder] Panel Root is null! Cannot show dialogue panel.");
            }
        }

        private void OnDialogueFinished()
        {
            Debug.Log("[DialogueUIBinder] OnDialogueFinished called. Deactivating panel root.");
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnSpeakerChanged(string value)
        {
            Debug.Log($"[DialogueUIBinder] OnSpeakerChanged called with: '{value}'");
            if (speakerText != null)
            {
                speakerText.SetText(value);
                Debug.Log("[DialogueUIBinder] Speaker text updated.");
            }
            else
            {
                Debug.LogWarning("[DialogueUIBinder] Speaker Text is null! Cannot update speaker name.");
            }
        }

        private void OnTextUpdated(string value)
        {
            if (bodyText != null)
            {
                bodyText.SetText(value);
            }
            else
            {
                Debug.LogWarning("[DialogueUIBinder] Body Text is null! Cannot update dialogue text.");
            }
        }
    }
}


