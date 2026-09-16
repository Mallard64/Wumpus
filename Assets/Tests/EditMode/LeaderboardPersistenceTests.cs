using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Covers the leaderboard bug: a player's first recorded run stored only the name, score, win flag
/// and epitaph, so the turns, coins and arrows columns showed 0 for every new entry. Updating an
/// existing entry left those three stale for the same reason.
/// </summary>
public class LeaderboardPersistenceTests
{
    private GameData gameData;
    private string savePath;
    private string backup;

    [SetUp]
    public void SetUp()
    {
        // These tests exercise the real save path, so the existing leaderboard is put back afterwards.
        savePath = Path.Combine(Application.persistentDataPath, GameData.SaveFileName);
        backup = File.Exists(savePath) ? File.ReadAllText(savePath) : null;

        gameData = new GameObject("GameData").AddComponent<GameData>();
        gameData.players.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        if (gameData != null)
        {
            Object.DestroyImmediate(gameData.gameObject);
        }

        if (backup != null)
        {
            File.WriteAllText(savePath, backup);
        }
        else if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
    }

    [Test]
    public void AddOrUpdatePlayerData_RecordsEveryColumnForANewPlayer()
    {
        gameData.AddOrUpdatePlayerData("Hunter", 91, 12, 34, 2, true, "Killed the wumpus.");

        PlayerData stored = gameData.GetPlayerData("Hunter");

        Assert.IsNotNull(stored);
        Assert.AreEqual(91, stored.score);
        Assert.AreEqual(12, stored.turns, "turns was not recorded for a new player.");
        Assert.AreEqual(34, stored.coins, "coins was not recorded for a new player.");
        Assert.AreEqual(2, stored.arrows, "arrows was not recorded for a new player.");
        Assert.IsTrue(stored.killedWumpus);
        Assert.AreEqual("Killed the wumpus.", stored.deaths);
    }

    [Test]
    public void AddOrUpdatePlayerData_RefreshesEveryColumnOnASecondRun()
    {
        gameData.AddOrUpdatePlayerData("Hunter", 50, 5, 5, 3, false, "Fell in a pit.");
        gameData.AddOrUpdatePlayerData("Hunter", 120, 40, 60, 1, true, "Killed the wumpus.");

        PlayerData stored = gameData.GetPlayerData("Hunter");

        Assert.AreEqual(1, gameData.players.Count, "The second run should update the entry, not add one.");
        Assert.AreEqual(120, stored.score);
        Assert.AreEqual(40, stored.turns, "turns was left at its previous value.");
        Assert.AreEqual(60, stored.coins, "coins was left at its previous value.");
        Assert.AreEqual(1, stored.arrows, "arrows was left at its previous value.");
        Assert.IsTrue(stored.killedWumpus, "killedWumpus was left at its previous value.");
    }

    [Test]
    public void AddOrUpdatePlayerData_KeepsPlayersApart()
    {
        gameData.AddOrUpdatePlayerData("One", 10, 1, 2, 3, false, "a");
        gameData.AddOrUpdatePlayerData("Two", 20, 4, 5, 6, true, "b");

        Assert.AreEqual(2, gameData.players.Count);
        Assert.AreEqual(1, gameData.GetPlayerData("One").turns);
        Assert.AreEqual(4, gameData.GetPlayerData("Two").turns);
    }

    [Test]
    public void SavedLeaderboard_SurvivesAReload()
    {
        gameData.AddOrUpdatePlayerData("Hunter", 77, 8, 9, 2, true, "Killed the wumpus.");

        gameData.players.Clear();
        gameData.LoadData();

        PlayerData reloaded = gameData.GetPlayerData("Hunter");
        Assert.IsNotNull(reloaded, "The entry did not come back from disk.");
        Assert.AreEqual(77, reloaded.score);
        Assert.AreEqual(8, reloaded.turns);
        Assert.AreEqual(9, reloaded.coins);
        Assert.AreEqual(2, reloaded.arrows);
    }
}
