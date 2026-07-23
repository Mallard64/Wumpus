using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Renders the saved leaderboard on the win and lose screens, highest score first.
/// Each entry shows the player's name, score, epitaph, turns, coins, arrows and win flag.
/// </summary>
public class Leaderboard : MonoBehaviour
{
    /// <summary>Vertical gap between consecutive leaderboard rows.</summary>
    private const int RowSpacing = 50;

    /// <summary>Vertical offset applied to every row so the list clears the page heading.</summary>
    private const float HeaderOffset = 100f;

    /// <summary>Position the first leaderboard row is instantiated at.</summary>
    public Vector3 leaderboardContainer = new Vector3(0, -50, 0);

    /// <summary>Prefab holding the seven labels that make up one leaderboard row.</summary>
    public GameObject leaderboardEntryPrefab;

    /// <summary>Accumulated vertical offset for the row currently being laid out.</summary>
    public int inc = 0;

    private string filePath;

    /// <summary>Resolves the save path and draws the leaderboard.</summary>
    private void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, GameData.SaveFileName);
        DisplayLeaderboard();
    }

    /// <summary>Loads the saved runs, sorts them by score and spawns a row for each.</summary>
    private void DisplayLeaderboard()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No save file found at " + filePath);
            return;
        }

        string json = File.ReadAllText(filePath);
        GameDataFile data = JsonUtility.FromJson<GameDataFile>(json);

        data.players.Sort((left, right) => right.score.CompareTo(left.score));

        foreach (PlayerData player in data.players)
        {
            SpawnEntry(player);
            inc += RowSpacing;
        }
    }

    /// <summary>
    /// Instantiates one leaderboard row, fills in its seven labels and slides it into position.
    /// </summary>
    /// <param name="player">The run to display.</param>
    private void SpawnEntry(PlayerData player)
    {
        GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContainer, Quaternion.identity);
        Text[] labels = entry.GetComponentsInChildren<Text>();

        string[] values =
        {
            player.playerName,
            player.score.ToString(),
            player.deaths,
            "Turns: " + player.turns,
            "Coins: " + player.coins,
            "Arrows: " + player.arrows,
            "Killed wumpus? " + (player.killedWumpus ? "yes" : "no")
        };

        for (int i = 0; i < values.Length; i++)
        {
            labels[i].text = values[i];
            Vector3 position = labels[i].transform.position;
            labels[i].transform.position = new Vector3(position.x, position.y - HeaderOffset - inc, position.z);
        }
    }
}