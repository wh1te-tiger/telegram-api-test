using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class TelegramCloudStorage
{
    [DllImport("__Internal")]
    private static extern void InitializeTelegram();

    [DllImport("__Internal")]
    private static extern void SaveData(string key, string value);

    [DllImport("__Internal")]
    private static extern void LoadData(string key);
    
    public static event Action<string, string> DataLoaded;
    public static event Action<string> DataSaved;
    public static event Action<string> SaveFailed;
    
    public bool IsInitialized => _isInitialized;
    public GameData GameData { get;  private set; }
    private bool _isInitialized;
    
    private readonly Dictionary<string, TaskCompletionSource<string>> _loadOperations = new();
    private readonly Dictionary<string, TaskCompletionSource<bool>> _saveOperations = new();
    private TaskCompletionSource<bool> _initializationTask;
    
    public async Task<bool> InitializeAsync()
    {
        if (_isInitialized) return true;

        _initializationTask = new TaskCompletionSource<bool>();
        try
        {
            InitializeTelegram();
            await LoadGameDataAsync();
            _isInitialized = true;
            _initializationTask.SetResult(true);
        }
        catch (Exception)
        {
            _initializationTask.SetResult(false);
        }

        return await _initializationTask.Task;
    }

    public async Task UpdateValueAsync(DataType dataType, int value)
    {
        await SaveProgressDataAsync(dataType.ToString(), value.ToString());
    }
    
    public async Task SaveGameDataAsync(GameData gameData)
    {
        var tasks = new List<Task>
        {
            UpdateValueAsync(DataType.Score, gameData.score),
            UpdateValueAsync(DataType.Energy, gameData.energy),
            UpdateValueAsync(DataType.Energy, gameData.boosters)
        };
        
        await Task.WhenAll(tasks);
    }
    
    private async Task SaveProgressDataAsync(string key, string value)
    {
        var tcs = new TaskCompletionSource<bool>();
        _saveOperations[key] = tcs;

        SaveData(key, value);
        
        var timeoutTask = Task.Delay(5000); 
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _saveOperations.Remove(key);
            return;
        }

        await tcs.Task;
    }
    
    public async Task LoadGameDataAsync()
    {
        var tasks = new List<Task<string>>
        {
            LoadProgressDataAsync(nameof(DataType.Score)),
            LoadProgressDataAsync(nameof(DataType.Energy)),
            LoadProgressDataAsync(nameof(DataType.Boosters))
        };

        var results = await Task.WhenAll(tasks);
        
        for (int i = 0; i < tasks.Count; i++)
        {
            if (!string.IsNullOrEmpty(results[i]))
            {
                string key = tasks[i].AsyncState as string;
                ProcessLoadedData(key, results[i]);
            }
        }
    }
    
    public async Task<string> LoadProgressDataAsync(string key)
    {
        var tcs = new TaskCompletionSource<string>();
        _loadOperations[key] = tcs;

        LoadData(key);
        
        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _loadOperations.Remove(key);
            return null;
        }

        return await tcs.Task;
    }
    
    #region Callbacks

    public void OnDataLoaded(string data)
    {
        try
        {
            string[] parts = data.Split(':');
            if (parts.Length >= 2)
            {
                string key = parts[0];
                string value = parts[1];
                
                if (_loadOperations.ContainsKey(key))
                {
                    _loadOperations[key].SetResult(value);
                    _loadOperations.Remove(key);
                }

                ProcessLoadedData(key, value);
                DataLoaded?.Invoke(key, value);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error processing loaded data: " + e.Message);
        }
    }
    
    public void OnDataSaved(string key)
    {
        if (_saveOperations.ContainsKey(key))
        {
            _saveOperations[key].SetResult(true);
            _saveOperations.Remove(key);
        }
        
        DataSaved?.Invoke(key);
    }
    
    public void OnSaveFailed(string key)
    {
        if (_saveOperations.ContainsKey(key))
        {
            _saveOperations[key].SetResult(false);
            _saveOperations.Remove(key);
        }
        
        SaveFailed?.Invoke(key);
    }

    #endregion 
    
    private void ProcessLoadedData(string key, string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        try
        {
            switch (key)
            {
                case "score":
                    GameData = new GameData(int.Parse(value), GameData.energy, GameData.boosters);
                    break;
                case "energy":
                    GameData = new GameData(GameData.score, int.Parse(value), GameData.boosters);
                    break;
                case "boosters":
                    GameData = new GameData(GameData.score, GameData.energy, int.Parse(value));
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing data for key {key}: {e.Message}");
        }
    }
}

public enum DataType
{
    Score,
    Energy,
    Boosters
}