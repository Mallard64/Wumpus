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
    /// <summary>Name of the leaderboard file inside <see cref="Application.persistentDataPath"/>.</summary>
    public const string SaveFileName = "gamedata.json";

    /// <summary>The surviving instance, set on first <c>Awake</c>.</summary>
    public static GameData instance;

    /// <summary>Every player recorded on the leaderboard.</summary>
    public List<PlayerData> players = new List<PlayerData>();

    private string filePath;

    /// <summary>Enforces the singleton and resolves the save path.</summary>
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

        filePath = Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    /// <summary>Loads any previously saved leaderboard.</summary>
    private void Start()
    {
        LoadData();
    }

    /// <summary>Writes the in-memory leaderboard to disk, overwriting the previous save.</summary>
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

    /// <summary>
    /// Reads the leaderboard from disk. Leaves the in-memory list untouched when no save exists.
    /// </summary>
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

    /// <summary>
    /// Records the result of a run, replacing the existing entry for that player if there is one,
    /// then saves to disk.
    /// </summary>
    /// <param name="playerName">Name the run is recorded under.</param>
    /// <param name="score">Final score.</param>
    /// <param name="turns">Rooms the player moved through.</param>
    /// <param name="coins">Coins held at the end of the run.</param>
    /// <param name="arrows">Arrows left at the end of the run.</param>
    /// <param name="killedWumpus">True when the run ended in a win.</param>
    /// <param name="deaths">Short epitaph describing how the run ended.</param>
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

    /// <summary>Looks up a player's leaderboard entry.</summary>
    /// <param name="playerName">Name to search for.</param>
    /// <returns>The player's entry, or <c>null</c> when they have no recorded run.</returns>
    public PlayerData GetPlayerData(string playerName)
    {
        return players.FirstOrDefault(p => p.playerName == playerName);
    }

    /// <summary>Wipes the leaderboard and saves the empty result.</summary>
    public void ResetData()
    {
        players = new List<PlayerData>();
        SaveData();
    }

    /// <remarks>
    /// NOTE: this writes the whole leaderboard to disk on every frame. It is preserved as-is to
    /// keep behaviour identical; see the README's "Known issues" section.
    /// </remarks>
    public void Update()
    {
        SaveData();
    }
}

/// <summary>One player's best recorded run.</summary>
[System.Serializable]
public class PlayerData
{
    /// <summary>Name the run was recorded under.</summary>
    public string playerName;

    /// <summary>Final score.</summary>
    public int score;

    /// <summary>Short epitaph describing how the run ended.</summary>
    public string deaths;

    /// <summary>Rooms the player moved through.</summary>
    public int turns;

    /// <summary>Coins held at the end of the run.</summary>
    public int coins;

    /// <summary>Arrows left at the end of the run.</summary>
    public int arrows;

    /// <summary>True when the run ended in a win.</summary>
    public bool killedWumpus;
}

/// <summary>Serialization wrapper for the on-disk leaderboard.</summary>
[System.Serializable]
public class GameDataFile
{
    /// <summary>Every recorded player.</summary>
    public List<PlayerData> players;
}
