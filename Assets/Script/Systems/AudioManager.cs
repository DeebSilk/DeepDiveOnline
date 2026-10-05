using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Systems")]
    [SerializeField] private MusicManager music;
    [SerializeField] private SFXManager sfx;
    [SerializeField] private AmbientManager ambient;
    [SerializeField] private AudioSettingsManager settings;

    [Header("Scene Music")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Game";

    public MusicManager Music => music;
    public SFXManager SFX => sfx;
    public AmbientManager Ambient => ambient;
    public AudioSettingsManager Settings => settings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        ValidateReferences();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Определяем музыку для первой загруженной сцены
        SetMusicForCurrentScene();
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        SetMusicForScene(scene.name);
    }

    private void SetMusicForCurrentScene()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();

        SetMusicForScene(
            currentScene.name
        );
    }

    private void SetMusicForScene(
        string sceneName
    )
    {
        if (music == null)
        {
            Debug.LogWarning(
                "AudioManager: MusicManager is missing.",
                this
            );

            return;
        }

        // Главное меню
        if (sceneName == mainMenuSceneName)
        {
            music.SetState(
                MusicManager.MusicState.Menu
            );

            return;
        }

        // Игровой мир
        if (sceneName == gameSceneName)
        {
            music.SetState(
                MusicManager.MusicState.Gameplay
            );

            return;
        }

        Debug.Log(
            $"AudioManager: No music state configured for scene '{sceneName}'.",
            this
        );
    }

    private void ValidateReferences()
    {
        if (music == null)
            music = GetComponentInChildren<MusicManager>(true);

        if (sfx == null)
            sfx = GetComponentInChildren<SFXManager>(true);

        if (ambient == null)
            ambient = GetComponentInChildren<AmbientManager>(true);

        if (settings == null)
            settings = GetComponentInChildren<AudioSettingsManager>(true);

        if (music == null)
            Debug.LogError(
                "AudioManager: MusicManager is missing.",
                this
            );

        if (sfx == null)
            Debug.LogError(
                "AudioManager: SFXManager is missing.",
                this
            );

        if (ambient == null)
            Debug.LogError(
                "AudioManager: AmbientManager is missing.",
                this
            );

        if (settings == null)
            Debug.LogError(
                "AudioManager: AudioSettingsManager is missing.",
                this
            );
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }
}