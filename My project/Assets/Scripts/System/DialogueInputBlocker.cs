using System.Collections.Generic;
using UnityEngine;

namespace Systems
{
	public class DialogueInputBlocker : MonoBehaviour
	{
		[Header("Auto-find components on Player")]
		[Tooltip("If enabled, will automatically find PlayerMovement and MouseLook components")]
		[SerializeField] private bool autoFindComponents = true;
		
		[Header("Manual component references")]
		[Tooltip("Drag components here if auto-find is disabled")]
		[SerializeField] private PlayerMovement playerMovement;
		[SerializeField] private MonoBehaviour mouseLook;
		[SerializeField] private List<MonoBehaviour> additionalComponents = new List<MonoBehaviour>();

		private List<MonoBehaviour> allComponents = new List<MonoBehaviour>();
		private CharacterController characterController;
		private bool wasBlocked;
		private static bool inputBlocked = false;
		
		public static bool IsInputBlocked => inputBlocked;

		private void InitializeComponents()
		{
			if (autoFindComponents)
			{
				// Auto-find PlayerMovement on this object
				if (playerMovement == null)
				{
					playerMovement = GetComponent<PlayerMovement>();
				}
				
				// Auto-find MouseLook - try on camera (MouseLook is usually on camera)
				if (mouseLook == null)
				{
					Camera cam = GetComponentInChildren<Camera>();
					if (cam != null)
					{
						// Try to find MouseLook component by name/type
						MonoBehaviour[] camComponents = cam.GetComponents<MonoBehaviour>();
						foreach (var comp in camComponents)
						{
							if (comp != null && comp.GetType().Name == "MouseLook")
							{
								mouseLook = comp;
								break;
							}
						}
					}
				}
			}

			// Collect all components
			if (playerMovement != null && !allComponents.Contains(playerMovement))
			{
				allComponents.Add(playerMovement);
			}
			if (mouseLook != null && !allComponents.Contains(mouseLook))
			{
				allComponents.Add(mouseLook);
			}
			foreach (var comp in additionalComponents)
			{
				if (comp != null && !allComponents.Contains(comp))
				{
					allComponents.Add(comp);
				}
			}
			
			// Get CharacterController for stopping movement
			characterController = GetComponent<CharacterController>();
			if (characterController == null && playerMovement != null)
			{
				characterController = playerMovement.GetComponent<CharacterController>();
			}
			
			Debug.Log($"[DialogueInputBlocker] Found {allComponents.Count} component(s) to block: PlayerMovement={playerMovement != null}, MouseLook={mouseLook != null}, CharacterController={characterController != null}");
		}

		private void Start()
		{
			// Initialize components first
			InitializeComponents();
			
			// Also try to register to DialogueManager
			RegisterToDialogueManager();
		}

		private void OnEnable()
		{
			RegisterToDialogueManager();
		}

		public void RegisterToDialogueManager()
		{
			if (DialogueManager.Instance != null)
			{
				// Remove listeners first to avoid duplicates
				DialogueManager.Instance.OnDialogueStarted.RemoveListener(BlockControls);
				DialogueManager.Instance.OnDialogueFinished.RemoveListener(UnblockControls);
				
				// Add listeners
				DialogueManager.Instance.OnDialogueStarted.AddListener(BlockControls);
				DialogueManager.Instance.OnDialogueFinished.AddListener(UnblockControls);
				Debug.Log($"[DialogueInputBlocker] Successfully registered to DialogueManager events on {gameObject.name}.");
			}
			else
			{
				Debug.LogWarning($"[DialogueInputBlocker] DialogueManager.Instance is null! Cannot register events. Will try again later. (Object: {gameObject.name})");
			}
		}

		private void OnDisable()
		{
			if (DialogueManager.Instance != null)
			{
				DialogueManager.Instance.OnDialogueStarted.RemoveListener(BlockControls);
				DialogueManager.Instance.OnDialogueFinished.RemoveListener(UnblockControls);
			}
		}

		private void BlockControls()
		{
			if (wasBlocked)
			{
				Debug.Log("[DialogueInputBlocker] Already blocked, skipping.");
				return;
			}
			
			Debug.Log($"[DialogueInputBlocker] Blocking controls on {gameObject.name}...");
			
			// Block input globally
			inputBlocked = true;
			Debug.Log($"[DialogueInputBlocker] inputBlocked set to TRUE. IsInputBlocked = {IsInputBlocked}");
			
			// Disable components
			SetComponentsEnabled(false);
			
			// Stop movement immediately by stopping CharacterController
			if (characterController != null)
			{
				// Stop any ongoing movement
				characterController.Move(Vector3.zero);
				Debug.Log("[DialogueInputBlocker] CharacterController.Move(Vector3.zero) called.");
			}
			else
			{
				Debug.LogWarning("[DialogueInputBlocker] CharacterController is null! Cannot stop movement directly.");
			}
			
			wasBlocked = true;
		}

		private void UnblockControls()
		{
			if (!wasBlocked) return;
			
			Debug.Log("[DialogueInputBlocker] Unblocking controls...");
			
			// Unblock input globally
			inputBlocked = false;
			
			// Re-enable components
			SetComponentsEnabled(true);
			
			wasBlocked = false;
		}

		private void SetComponentsEnabled(bool enabled)
		{
			foreach (MonoBehaviour component in allComponents)
			{
				if (component == null) continue;
				component.enabled = enabled;
				Debug.Log($"[DialogueInputBlocker] {(enabled ? "Enabled" : "Disabled")} component: {component.GetType().Name}");
			}
		}
	}
}


