using UnityEngine;

public class NPCController : MonoBehaviour
{
	[SerializeField] private DialogueData dialogue;
	[SerializeField] private GameObject interactUI;

	private DialogueManager dialogueManager;

	private void Awake()
	{
		dialogueManager = DialogueManager.Instance;

		if (dialogueManager == null)
			dialogueManager = FindAnyObjectByType<DialogueManager>();
	}

	private void Start()
	{
		if (interactUI != null)
			interactUI.SetActive(false);

		if (dialogueManager == null)
			Debug.LogError("NPCController: DialogueManager not found in scene. Add a DialogueManager object with the DialogueManager component to your scene.", this);
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
			Debug.LogError("NPCController: Cannot start dialogue because DialogueManager is missing.", this);
			return;
		}

		if (dialogue == null)
		{
			Debug.LogError("NPCController: DialogueData is not assigned.", this);
			return;
		}

		dialogueManager.StartDialogue(dialogue, transform);
	}
}
