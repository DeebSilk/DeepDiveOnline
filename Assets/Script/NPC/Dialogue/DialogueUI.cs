using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
	public GameObject panel;
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI dialogText;

	private int index;
	private DialogueData currentDialogue;

	void Start()
	{
		panel.SetActive(false);
	}

	public void StartDialogue(DialogueData data)
	{
		currentDialogue = data;
		index = 0;

		panel.SetActive(true);
		ShowLine();
	}

	public void NextLine()
	{
		if (currentDialogue == null)
			return;

		index++;

		if (index >= currentDialogue.lines.Count)
		{
			EndDialogue();
			return;
		}

		ShowLine();
	}

	void ShowLine()
	{
		var line = currentDialogue.lines[index];

		nameText.text = line.speaker;
		dialogText.text = line.text;
	}

	public void EndDialogue()
	{
		panel.SetActive(false);
		currentDialogue = null;
	}
}
