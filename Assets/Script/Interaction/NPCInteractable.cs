using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
	[SerializeField] private GameObject interactUI;
	[SerializeField] private DialogueData dialogue;

	private DialogueManager dialogueManager;

	private void Start()
	{
		dialogueManager = DialogueManager.Instance;
		HidePrompt();

		if (dialogueManager == null)
			Debug.LogError("NPCInteractable: DialogueManager not found in scene.", this);
	}

	public void ShowPrompt()
	{
		if (interactUI != null)
			interactUI.SetActive(true);
	}

	public void HidePrompt()
	{
		if (interactUI != null)
			interactUI.SetActive(false);
	}

	public void Interact()
	{
		HidePrompt();

		if (dialogueManager == null)
		{
			Debug.LogError("NPCInteractable: Cannot start dialogue because DialogueManager is missing.", this);
			return;
		}

		if (dialogue == null)
		{
			Debug.LogError("NPCInteractable: DialogueData is not assigned.", this);
			return;
		}

		dialogueManager.StartDialogue(dialogue);
	}
}
