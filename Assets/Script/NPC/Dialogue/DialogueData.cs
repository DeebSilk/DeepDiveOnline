using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
	public string speaker;
	[TextArea(2, 4)] public string text;
	public List<DialogueChoice> choices;
}

[CreateAssetMenu(fileName = "Dialogue", menuName = "Game/Dialogue")]
public class DialogueData : ScriptableObject
{
	public string npcName = "NPC";
	public List<DialogueLine> lines = new();
}

[System.Serializable]
public class DialogueChoice
{
	public string text;
	// index in DialogueData.lines to jump to (-1 to end dialogue)
	public int nextIndex = -1;
}
