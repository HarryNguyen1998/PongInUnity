using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public enum GameState
{
    kMainMenu,
    kGameplay,
    kSettingsMenu,
    kGameOverMenu,
    kQuit,
}

/// <summary>
/// Update the scores, handle state change logic, and raise UI event when state change.
/// Singleton only works when put into Preload scene.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    // References
    [SerializeField] GameSettings _gameSettingsSO;

    // Members
    public event Action<GameState> GameStateChanged;
    public event Action<bool> RoundWasOver;
    public GameplaySettingsPOD CurrentSettings { get; set; } = new GameplaySettingsPOD();
    public int ScoreLeft { get; private set; }
    public int ScoreRight { get; private set; }
    public bool LeftWon { get; private set; }
    public bool IsInGame { get; set; }
    public GameState CurrentState { get; private set; }
    public static GameManager Instance { get; private set; }

    bool _isPaused;
    bool _hasGameStarted;

    void Awake()
    {
        if (_hasGameStarted)
            return;

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _isPaused = false;
        LoadFromFile();

        _hasGameStarted = true;

#if UNITY_EDITOR
        if (DebugPreload.otherScene > 0)
        {
            Debug.Log($"---------Loading current scene {DebugPreload.otherScene}---------");
            SceneManager.LoadSceneAsync(DebugPreload.otherScene);
            return;
        }
#endif

        SceneManager.LoadSceneAsync("PongClone", LoadSceneMode.Additive);
    }

    public void ResetSettings()
    {
        CurrentSettings = _gameSettingsSO.DefaultSettings.DeepCopy();
        SaveToFile();
    }

    public void SaveToFile()
    {
#if !UNITY_WEBGL
        FileManager.TryWriteFile("SettingsData.dat", CurrentSettings.ToJson());
#endif
    }

    public void LoadFromFile()
    {
#if !UNITY_WEBGL
        // File doesn't exist
        if (!FileManager.TryReadFile("SettingsData.dat", out string json))
        {
            FileManager.TryWriteFile("SettingsData.dat", _gameSettingsSO.DefaultSettings.ToJson());
            CurrentSettings = _gameSettingsSO.DefaultSettings.DeepCopy();
        }
        else
        {
            CurrentSettings.FromJson(json);
        }
#else
        CurrentSettings = _gameSettingsSO.DefaultSettings.DeepCopy();
#endif
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        if (newState == GameState.kSettingsMenu)
        {
            Pause();
        }
        else
        {
            if (_isPaused)
                Resume();
        }

        switch (newState)
        {
            case GameState.kMainMenu:
            {
                DOTween.KillAll();
                IsInGame = false;
                SceneManager.LoadSceneAsync("PongClone");
                break;
            }
            case GameState.kGameplay:
            {
                // Transition between gameplay and settings won't reload the Scene.
#if UNITY_WEBGL
                CurrentSettings.IsFullScreen = Screen.fullScreen;
#endif
                if (!IsInGame)
                {
                    ScoreLeft = 0;
                    ScoreRight = 0;
                    DOTween.KillAll();
                    SceneManager.LoadSceneAsync("PongClone");
                }
                IsInGame = true;
                break;
            }
            case GameState.kGameOverMenu:
            {
                IsInGame = false;
                break;
            }
            case GameState.kQuit:
            {
                Quit();
                break;
            }
        }

        GameStateChanged?.Invoke(CurrentState);
    }

    public void IncrementScore(bool leftWon)
    {
        LeftWon = leftWon;

        if (leftWon)
            ++ScoreLeft;
        else
            ++ScoreRight;

        RoundWasOver?.Invoke(leftWon);

        // We don't want a gameover in main menu
        if (CurrentState == GameState.kGameplay &&
            (ScoreLeft >= CurrentSettings.RoundCnt || ScoreRight >= CurrentSettings.RoundCnt))
            ChangeState(GameState.kGameOverMenu);

    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1.0f;
    }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0.0f;
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}