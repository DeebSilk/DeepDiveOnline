using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
	public static DialogueManager Instance { get; private set; }

	// Events for dialogue management
	public event Action OnDialogueStart;
	public event Action OnDialogueEnd;

	public GameObject panel;
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI dialogText;

	[Header("Choices UI")]
	public GameObject choicesPanel;
	public Button choiceButtonA;
	public Button choiceButtonB;
	public TextMeshProUGUI choiceAText;
	public TextMeshProUGUI choiceBText;

	[Header("Cinematic Framing")]
	public float dialogueHeight = 1.2f; // height offset for look target
	public float dialogueBaseDistance = 2f; // minimum distance from midpoint
	public float dialogueDistanceMultiplier = 1.0f; // scales with separation
	public float dialogueSmoothing = 0.12f;
	public float dialoguePitch = 20f; // degrees above horizon (positive looks down less)

	[System.Serializable]
	public class CameraPreset
	{
		public string presetName = "New Preset";
		public float presetDialogueHeight = 1.2f;
		public float presetBaseDistance = 2f;
		public float presetDistanceMultiplier = 1f;
		public float presetSmoothing = 0.12f;
		public float presetPitch = 20f;
		public bool useForwardOverride = false;
		public Vector3 forwardOverride = Vector3.zero;
	}

	[Header("Presets")]
	public CameraPreset[] presets;

	private DialogueData currentDialogue;
	private int index;
	private Transform cameraTarget;
	private Transform playerTransform;
	private Vector3 camVelocity;
	private Vector3 lastAppliedForward = Vector3.zero;
	public bool IsDialogueActive => currentDialogue != null;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	private void Start()
	{
		if (panel == null)
			Debug.LogWarning("DialogueManager: panel is not assigned.", this);

		SetPanelActive(false);
	}



	public void StartDialogue(DialogueData dialogue, Transform npcTarget = null, int presetIndex = -1)
	{
		if (dialogue == null || dialogue.lines == null || dialogue.lines.Count == 0)
		{
			Debug.LogWarning("DialogueManager: dialogue data is empty or null.", this);
			return;
		}

		currentDialogue = dialogue;
		index = 0;
		cameraTarget = npcTarget;

		if (presetIndex >= 0 && presets != null && presetIndex < presets.Length)
		{
			ApplyPreset(presetIndex, immediate: false);
		}

		SetPanelActive(true);
		ShowLine();
		SetCursorState(true);
		OnDialogueStart?.Invoke();
	}

	public void ApplyPreset(int index, bool immediate = false)
	{
		if (presets == null || index < 0 || index >= presets.Length)
			return;

		var p = presets[index];
		// apply values
		dialogueHeight = p.presetDialogueHeight;
		dialogueBaseDistance = p.presetBaseDistance;
		dialogueDistanceMultiplier = p.presetDistanceMultiplier;
		dialogueSmoothing = p.presetSmoothing;
		dialoguePitch = p.presetPitch;
		if (p.useForwardOverride)
			lastAppliedForward = p.forwardOverride.normalized;
		else
			lastAppliedForward = Vector3.zero;

	}

	public void NextLine()
	{
		if (currentDialogue == null)
			return;

		// If current line has choices, don't advance via NextLine
		if (currentDialogue.lines != null && index >= 0 && index < currentDialogue.lines.Count)
		{
			var curLine = currentDialogue.lines[index];
			if (curLine.choices != null && curLine.choices.Count > 0)
				return;
		}

		index++;

		if (index >= currentDialogue.lines.Count)
		{
			EndDialogue();
			return;
		}

		ShowLine();
	}

	public void EndDialogue()
	{
		SetPanelActive(false);
		currentDialogue = null;
		index = 0;
		cameraTarget = null;
		SetCursorState(false);
		OnDialogueEnd?.Invoke();
	}

	private void ShowLine()
	{
		if (currentDialogue == null || currentDialogue.lines == null || index < 0 || index >= currentDialogue.lines.Count)
			return;

		var line = currentDialogue.lines[index];
		nameText.text = string.IsNullOrEmpty(line.speaker) ? currentDialogue.npcName : line.speaker;
		dialogText.text = line.text;

		// Handle choices
		if (line.choices != null && line.choices.Count > 0)
		{
			ShowChoices(line.choices);
		}
		else
		{
			SetChoicesActive(false);
		}
	}

	private void ShowChoices(System.Collections.Generic.List<DialogueChoice> choices)
	{
		SetChoicesActive(true);
		Debug.Log($"DialogueManager: ShowChoices called, choices count = {choices?.Count}");

		// debug info about buttons and UI
		if (choiceButtonA != null)
		{
			Debug.Log($"choiceButtonA activeSelf={choiceButtonA.gameObject.activeSelf}, interactable={choiceButtonA.interactable}, targetGraphic={(choiceButtonA.targetGraphic!=null)}");
		}
		if (choiceButtonB != null)
		{
			Debug.Log($"choiceButtonB activeSelf={choiceButtonB.gameObject.activeSelf}, interactable={choiceButtonB.interactable}, targetGraphic={(choiceButtonB.targetGraphic!=null)}");
		}

		// Canvas and EventSystem info
		var canvas = panel != null ? panel.GetComponentInParent<Canvas>() : null;
		var gr = canvas != null ? canvas.GetComponent<GraphicRaycaster>() : null;
		Debug.Log($"DialogueManager: Canvas={(canvas!=null ? canvas.name : "null")}, GraphicRaycaster={(gr!=null)}, EventSystemPresent={(UnityEngine.EventSystems.EventSystem.current!=null)}");

		if (choicesPanel != null)
		{
			Debug.Log($"choicesPanel activeSelf={choicesPanel.activeSelf}, siblingIndex={choicesPanel.transform.GetSiblingIndex()}");
		}
		// Ensure choices panel is on top and interactable
		if (choicesPanel != null)
		{
			choicesPanel.transform.SetAsLastSibling();
		}

		// Setup first choice
		if (choices.Count > 0)
		{
			choiceAText.text = choices[0].text;
			choiceButtonA.onClick.RemoveAllListeners();
			int ci = 0;
			choiceButtonA.onClick.AddListener(() => OnChoiceSelected(ci));
			if (choiceButtonA != null)
				choiceButtonA.interactable = true;
		}

		// Setup second choice (optional)
		if (choices.Count > 1)
		{
			choiceBText.text = choices[1].text;
			choiceButtonB.onClick.RemoveAllListeners();
			int ci = 1;
			choiceButtonB.onClick.AddListener(() => OnChoiceSelected(ci));
			if (choiceButtonB != null)
				choiceButtonB.interactable = true;
		}
		else if (choiceButtonB != null)
		{
			choiceButtonB.onClick.RemoveAllListeners();
			choiceBText.text = string.Empty;
			choiceButtonB.interactable = false;
		}
	}

	private void OnChoiceSelected(int choiceIndex)
	{
		if (currentDialogue == null || currentDialogue.lines == null || index < 0 || index >= currentDialogue.lines.Count)
			return;

		var choices = currentDialogue.lines[index].choices;
		if (choices == null || choiceIndex < 0 || choiceIndex >= choices.Count)
			return;

		var choice = choices[choiceIndex];
		SetChoicesActive(false);

		if (choice.nextIndex >= 0 && choice.nextIndex < currentDialogue.lines.Count)
		{
			index = choice.nextIndex;
			ShowLine();
		}
		else
		{
			EndDialogue();
		}
	}

	private void SetChoicesActive(bool active)
	{
		if (choicesPanel != null)
		{
			choicesPanel.SetActive(active);
			Debug.Log($"DialogueManager: SetChoicesActive({active}) called. choicesPanel active = {choicesPanel.activeSelf}");
		}
	}

	private void SetPanelActive(bool active)
	{
		if (panel != null)
			panel.SetActive(active);
	}

	private void SetCursorState(bool show)
	{
		Cursor.visible = show;
		Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
	}
}



