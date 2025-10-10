using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private MainScreen MainScreen;
    
    private TelegramCloudStorage _telegramCloudStorage;
    private PlayerState _playerState;
    private void Awake()
    {
        _telegramCloudStorage = new TelegramCloudStorage();
    }

    private async void Start()
    {
        bool initialized = await _telegramCloudStorage.InitializeAsync();
        
        if (initialized)
        {
            StartGame();
        }
        else
        {
            Debug.LogError("Failed to initialize Telegram storage");
        }
    }

    void StartGame()
    {
        var data = _telegramCloudStorage.GameData;
        
        _playerState = new PlayerState(data);
        MainScreen.Initialize(_playerState);
    }
    
    public async void OnApplicationQuit()
    {
        try
        {
            await _telegramCloudStorage.SaveGameDataAsync(_playerState.GetGameData());
            Debug.Log("Game progress saved on exit");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save on exit: {e.Message}");
        }
    }
}