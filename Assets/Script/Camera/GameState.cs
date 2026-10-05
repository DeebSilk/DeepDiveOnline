using System;
using UnityEngine;

public enum GameState
{
	Gameplay,
	Paused
}

public class GameStateManager : MonoBehaviour
{
	public static GameStateManager Instance { get; private set; }

	public event Action<GameState> StateChanged;

	[SerializeField]
	private GameState initialState = GameState.Gameplay;

	public GameState CurrentState { get; private set; } = (GameState)(-1);

	public bool IsGameplay => CurrentState == GameState.Gameplay;
	public bool IsPaused => CurrentState == GameState.Paused;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		SetState(initialState);
	}

	public void SetState(GameState state)
	{
		if (CurrentState == state)
			return;

		CurrentState = state;
		StateChanged?.Invoke(state);

		switch (state)
		{
			case GameState.Gameplay:
				EnterGameplay();
				break;

			case GameState.Paused:
				EnterPaused();
				break;
	}
	}

	private void EnterGameplay()
	{
		Time.timeScale = 1f;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void EnterPaused()
	{
		Time.timeScale = 0f;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public void TogglePause()
	{
		SetState(IsGameplay ? GameState.Paused : GameState.Gameplay);
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}
}
