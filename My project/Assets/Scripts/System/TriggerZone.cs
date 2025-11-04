using UnityEngine;

namespace Systems
{
    [RequireComponent(typeof(Collider))]
    public class TriggerZone : MonoBehaviour
    {
        [Header("Activation")]
        [SerializeField] private bool oneShot = true;
        [SerializeField] private string playerTag = "Player";

        [Header("Dialogue")]
        [Tooltip("Drag & drop JSON TextAsset here, or use Resource Path below")]
        [SerializeField] private TextAsset dialogueJson;
        [Tooltip("Resources path to the JSON asset (e.g., Dialogues/scene1_intro)")]
        [SerializeField] private string dialogueResourcePath;
        [SerializeField] private string dialogueId;
        [SerializeField] private bool advanceSceneOnDialogueFinish = true;

        [Header("Visuals")] 
        [SerializeField] private ParticleSystem glow;

        private bool activated;

        private void Reset()
        {
            Collider c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activated && oneShot) return;
            if (!other.CompareTag(playerTag)) return;

            Activate();
        }

        private void Activate()
        {
            activated = true;
            if (glow != null) glow.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            bool hasDialogueId = !string.IsNullOrEmpty(dialogueId);
            bool hasTextAsset = dialogueJson != null && !string.IsNullOrEmpty(dialogueJson.text);
            bool hasResourcePath = !string.IsNullOrEmpty(dialogueResourcePath);

            Debug.Log($"[TriggerZone] Activated! DialogueManager.Instance: {DialogueManager.Instance != null}, hasDialogueId: {hasDialogueId}, hasTextAsset: {hasTextAsset}, hasResourcePath: {hasResourcePath}, dialogueId: '{dialogueId}', resourcePath: '{dialogueResourcePath}'");

            if (DialogueManager.Instance != null && hasDialogueId && (hasTextAsset || hasResourcePath))
            {
                DialogueManager.Instance.OnDialogueFinished.AddListener(OnDialogueFinished);
                if (hasTextAsset)
                {
                    Debug.Log($"[TriggerZone] Playing dialogue from TextAsset with ID: {dialogueId}");
                    DialogueManager.Instance.PlayDialogueFromJson(dialogueJson.text, dialogueId);
                }
                else
                {
                    Debug.Log($"[TriggerZone] Playing dialogue from Resources path: {dialogueResourcePath}, ID: {dialogueId}");
                    DialogueManager.Instance.PlayDialogueFromResources(dialogueResourcePath, dialogueId);
                }
            }
            else
            {
                if (DialogueManager.Instance == null)
                {
                    Debug.LogWarning("[TriggerZone] DialogueManager.Instance is null! Make sure DialogueManager exists in the scene.");
                }
                if (!hasDialogueId)
                {
                    Debug.LogWarning("[TriggerZone] Dialogue Id is empty! Please set it in the inspector.");
                }
                if (!hasTextAsset && !hasResourcePath)
                {
                    Debug.LogWarning("[TriggerZone] Neither Dialogue Json nor Resource Path is set! Please set one of them in the inspector.");
                }
                
                if (advanceSceneOnDialogueFinish && GameManager.Instance != null)
                {
                    Debug.Log("[TriggerZone] No dialogue configured, advancing scene immediately.");
                    GameManager.Instance.LoadNextStage();
                }
            }
        }

        private void OnDialogueFinished()
        {
            DialogueManager.Instance.OnDialogueFinished.RemoveListener(OnDialogueFinished);
            if (advanceSceneOnDialogueFinish && GameManager.Instance != null)
            {
                GameManager.Instance.LoadNextStage();
            }
        }
    }
}


