using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private const string GameSceneName = "Game";

    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        if (settingsPanel == null)
            Debug.LogWarning("MainMenu: settingsPanel is not assigned.", this);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(GameSceneName);
    }

    public void OpenSettings()
    {
        SetSettingsPanelActive(true);
    }

    public void CloseSettings()
    {
        SetSettingsPanelActive(false);
    }

    public void ToggleSettings()
    {
        SetSettingsPanelActive(settingsPanel != null && !settingsPanel.activeSelf);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void SetSettingsPanelActive(bool isActive)
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(isActive);
    }
}