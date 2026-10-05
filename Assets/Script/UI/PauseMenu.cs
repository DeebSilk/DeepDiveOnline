using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
	[Header("Panels")]
	[SerializeField] private GameObject pausePanel;
	[SerializeField] private GameObject settingsPanel;

	private PlayerControls controls;
	private GameStateManager gameStateManager => GameStateManager.Instance;

	private bool IsPaused => gameStateManager != null
		? gameStateManager.IsPaused
		: Time.timeScale == 0f;

	private void Awake()
	{
		controls = new PlayerControls();
	}

	private void OnEnable()
	{
		if (controls == null)
			return;

		controls.Enable();
		controls.UI.Pause.performed += OnPause;
	}

	private void OnDisable()
	{
		if (controls == null)
			return;

		controls.UI.Pause.performed -= OnPause;
		controls.Disable();
	}

	private void Start()
	{
		SetPanelActive(pausePanel, false);
		SetPanelActive(settingsPanel, false);
		ApplyCursorState(false);
		ApplyTimeScale(1f);
	}

	private void OnPause(InputAction.CallbackContext context)
	{
		if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
			return;

		if (IsPaused)
			Resume();
		else
			Pause();
	}

	public void Pause()
	{
		if (IsPaused)
			return;

		SetPanelActive(pausePanel, true);

		if (gameStateManager != null)
			gameStateManager.SetState(GameState.Paused);
		else
			ApplyPauseState();
	}

	public void Resume()
	{
		if (!IsPaused)
			return;

		SetPanelActive(settingsPanel, false);
		SetPanelActive(pausePanel, false);

		if (gameStateManager != null)
			gameStateManager.SetState(GameState.Gameplay);
		else
			ApplyResumeState();
	}

	public void OpenSettings()
	{
		if (settingsPanel == null)
		{
			Debug.LogWarning("PauseMenu: settingsPanel is not assigned.", this);
			return;
		}

		SetPanelActive(pausePanel, false);
		SetPanelActive(settingsPanel, true);
		ApplyCursorState(true);

		if (gameStateManager != null && !gameStateManager.IsPaused)
			gameStateManager.SetState(GameState.Paused);
		else if (gameStateManager == null)
			ApplyPauseState();
	}

	public void CloseSettings()
	{
		SetPanelActive(settingsPanel, false);
		SetPanelActive(pausePanel, true);
	}

	public void ExitGame()
	{
		ApplyResumeState();

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}

	private void ApplyPauseState()
	{
		ApplyTimeScale(0f);
		ApplyCursorState(true);
	}

	private void ApplyResumeState()
	{
		ApplyTimeScale(1f);
		ApplyCursorState(false);
	}

	private static void ApplyTimeScale(float value)
	{
		Time.timeScale = value;
	}

	private static void ApplyCursorState(bool visible)
	{
		Cursor.visible = visible;
		Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
	}

	private static void SetPanelActive(GameObject panel, bool active)
	{
		if (panel != null)
			panel.SetActive(active);
	}
}
