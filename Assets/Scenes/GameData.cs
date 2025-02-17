using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// GameData loads and saves the leaderboard scores
// The scores are saved to a json file on the local machine
// The default player name is read from the machine name
public class GameData : MonoBehaviour
{
    public static GameData instance;

    // List of player data
    public List<PlayerData> players = new List<PlayerData>();

    private string filePath;

    void Awake()
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

        // Set the file path for the JSON file
        filePath = Path.Combine(Application.persistentDataPath, "gamedata.json");
    }

    void Start()
    {
        // Load data from JSON file if it exists
        LoadData();
    }

    public void SaveData()
    {
        GameDataFile data = new GameDataFile
        {
            players = this.players
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Data saved to " + filePath);
    }

    public void LoadData()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            GameDataFile data = JsonUtility.FromJson<GameDataFile>(json);

            this.players = data.players ?? new List<PlayerData>();
            Debug.Log("Data loaded from " + filePath);
        }
        else
        {
            Debug.LogWarning("No save file found at " + filePath);
        }
    }

    public void AddOrUpdatePlayerData(string playerName, int score, int turns, int coins, int arrows, bool killedWumpus, string deaths)
    {
        PlayerData playerData = players.FirstOrDefault(p => p.playerName == playerName);
        if (playerData == null)
        {
            playerData = new PlayerData { playerName = playerName, score = score, killedWumpus = killedWumpus, deaths = deaths};
            players.Add(playerData);
        }
        else
        {
            playerData.score = score;
            playerData.deaths = deaths;
        }
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

    public void Update()
    {
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
