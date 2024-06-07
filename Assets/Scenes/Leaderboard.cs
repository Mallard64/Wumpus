using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

// Show the list of (player, scores) sorted by the highest score
// In addition to the score, it also shows turns / coins / arrows
public class Leaderboard : MonoBehaviour
{
    public Vector3 leaderboardContainer = new Vector3(0,-50,0);
    public GameObject leaderboardEntryPrefab;
    private string filePath;

    public int inc = 0;

    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "gamedata.json");
        DisplayLeaderboard();
    }

    void DisplayLeaderboard()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            GameDataFile data = JsonUtility.FromJson<GameDataFile>(json);

            // Sort players by score
            data.players.Sort((x, y) => y.score.CompareTo(x.score));

            // Display each player's data
            foreach (PlayerData player in data.players)
            {
                Debug.Log(player.playerName);
                GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContainer, Quaternion.identity);
                
                Text[] texts = entry.GetComponentsInChildren<Text>();
                texts[0].text = player.playerName;
                texts[0].transform.position = new Vector3(texts[0].transform.position.x, texts[0].transform.position.y-100-inc, texts[0].transform.position.z);
                texts[1].text = player.score.ToString();
                texts[1].transform.position = new Vector3(texts[1].transform.position.x, texts[1].transform.position.y - 100-inc, texts[1].transform.position.z);
                texts[2].text = player.deaths.ToString();
                texts[2].transform.position = new Vector3(texts[2].transform.position.x, texts[2].transform.position.y - 100-inc, texts[2].transform.position.z);
                texts[3].text = "Turns: " + player.turns.ToString();
                texts[3].transform.position = new Vector3(texts[3].transform.position.x, texts[3].transform.position.y - 100 - inc, texts[3].transform.position.z);
                texts[4].text = "Coins: " + player.coins.ToString();
                texts[4].transform.position = new Vector3(texts[4].transform.position.x, texts[4].transform.position.y - 100 - inc, texts[4].transform.position.z);
                texts[5].text = "Arrows: " + player.arrows.ToString();
                texts[5].transform.position = new Vector3(texts[5].transform.position.x, texts[5].transform.position.y - 100 - inc, texts[5].transform.position.z);
                texts[6].text = "Killed wumpus? " + (player.killedWumpus ? "yes" : "no");
                texts[6].transform.position = new Vector3(texts[6].transform.position.x, texts[6].transform.position.y - 100 - inc, texts[6].transform.position.z);
                inc += 50;
            }
        }
        else
        {
            Debug.LogWarning("No save file found at " + filePath);
        }
    }
}