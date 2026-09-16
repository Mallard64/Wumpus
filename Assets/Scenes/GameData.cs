using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Persists the leaderboard to a JSON file in the platform's persistent data directory.
/// <para>
/// Survives scene loads as a singleton, so a run's result written on the map is still readable
/// from the win and lose screens.
/// </para>
/// </summary>
public class GameData : MonoBehaviour
{
    public const string SaveFileName = "gamedata.json";

    public static GameData instance;
    public List<PlayerData> players = new List<PlayerData>();

    private string filePath;

    /// <summary>
    /// Resolved on first use rather than in <c>Awake</c>, so saving and loading work even when
    /// the component has not been through the normal Unity lifecycle (as in edit-mode tests).
    /// </summary>
    private string SaveFilePath => filePath ??= Path.Combine(Application.persistentDataPath, SaveFileName);

    private void Awake()
    {
        // Ensure that only one instance of this object exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadData();
    }

    public void SaveData()
    {
        GameDataFile data = new GameDataFile
        {
            players = this.players
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log("Data saved to " + SaveFilePath);
    }

    /// <summary>
    /// Reads the leaderboard from disk. Leaves the in-memory list untouched when no save exists.
    /// </summary>
    public void LoadData()
    {
        if (File.Exists(SaveFilePath))
        {
            string json = File.ReadAllText(SaveFilePath);
            GameDataFile data = JsonUtility.FromJson<GameDataFile>(json);

            this.players = data.players ?? new List<PlayerData>();
            Debug.Log("Data loaded from " + SaveFilePath);
        }
        else
        {
            Debug.LogWarning("No save file found at " + SaveFilePath);
        }
    }

    /// <summary>
    /// Records the result of a run, replacing the existing entry for that player if there is one,
    /// then saves to disk.
    /// </summary>
    public void AddOrUpdatePlayerData(string playerName, int score, int turns, int coins, int arrows, bool killedWumpus, string deaths)
    {
        PlayerData playerData = players.FirstOrDefault(p => p.playerName == playerName);
        if (playerData == null)
        {
            playerData = new PlayerData { playerName = playerName };
            players.Add(playerData);
        }

        playerData.score = score;
        playerData.deaths = deaths;
        playerData.turns = turns;
        playerData.coins = coins;
        playerData.arrows = arrows;
        playerData.killedWumpus = killedWumpus;

        SaveData();
    }

    public PlayerData GetPlayerData(string playerName)
    {
        return players.FirstOrDefault(p => p.playerName == playerName);
    }

    public void ResetData()
    {
        players = new List<PlayerData>();
        SaveData();
    }
}

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int score;
    public string deaths;
    public int turns;
    public int coins;
    public int arrows;
    public bool killedWumpus;
}

[System.Serializable]
public class GameDataFile
{
    public List<PlayerData> players;
}
