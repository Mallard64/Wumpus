using NUnit.Framework;

/// <summary>
/// Covers the <c>moveWumpus</c> bug: the search loop never advanced its room counter, so the
/// comparison against the drawn room number only ever matched when that number was 1. In practice
/// the Wumpus either vanished from the board entirely or, in the one case that did match, was
/// flagged into every room at once.
/// </summary>
public class WumpusRelocationTests
{
    private CellGenerator generator;

    [SetUp]
    public void SetUp()
    {
        generator = TestCave.Build();
        generator.PlaceWumpusAt(1);
    }

    [TearDown]
    public void TearDown()
    {
        TestCave.Destroy(generator);
    }

    [Test]
    public void PlaceWumpusAt_MovesTheWumpusToEveryRoomInTurn()
    {
        for (int roomNumber = 1; roomNumber <= CellGenerator.TotalRooms; roomNumber++)
        {
            generator.PlaceWumpusAt(roomNumber);

            Assert.AreEqual(roomNumber, generator.wumpus.GetCellIndex(), "Wumpus landed in the wrong room.");
            Assert.IsTrue(generator.wumpus.hasWumpus, "The destination room was not flagged.");
            Assert.AreEqual(roomNumber, generator.wumpusRoomNumber, "The reported room number is stale.");
        }
    }

    [Test]
    public void PlaceWumpusAt_LeavesExactlyOneWumpusOnTheBoard()
    {
        for (int roomNumber = 1; roomNumber <= CellGenerator.TotalRooms; roomNumber++)
        {
            generator.PlaceWumpusAt(roomNumber);

            Assert.AreEqual(
                1,
                TestCave.CountWumpusRooms(generator),
                $"Moving to room {roomNumber} left the wrong number of Wumpus rooms.");
        }
    }

    [Test]
    public void PlaceWumpusAt_Room1_DoesNotFlagEveryRoom()
    {
        // Room 1 is the case the old counter bug did match on, and it matched on every iteration.
        generator.PlaceWumpusAt(14);
        generator.PlaceWumpusAt(1);

        Assert.AreEqual(1, TestCave.CountWumpusRooms(generator));
        Assert.AreEqual(1, generator.wumpus.GetCellIndex());
    }

    [Test]
    public void PlaceWumpusAt_ClearsTheRoomItCameFrom()
    {
        generator.PlaceWumpusAt(7);
        Cell previous = generator.wumpus;

        generator.PlaceWumpusAt(19);

        Assert.IsFalse(previous.hasWumpus, "The Wumpus is still flagged in its previous room.");
        Assert.AreNotSame(previous, generator.wumpus);
    }

    [Test]
    public void PlaceWumpusAt_IgnoresRoomNumbersOutsideTheGrid()
    {
        generator.PlaceWumpusAt(12);
        Cell before = generator.wumpus;

        generator.PlaceWumpusAt(0);
        generator.PlaceWumpusAt(CellGenerator.TotalRooms + 1);

        Assert.AreSame(before, generator.wumpus, "An out-of-range room moved the Wumpus.");
        Assert.AreEqual(1, TestCave.CountWumpusRooms(generator));
    }

    [Test]
    public void MoveWumpus_AlwaysLeavesExactlyOneWumpus()
    {
        for (int i = 0; i < 200; i++)
        {
            generator.moveWumpus();

            Assert.AreEqual(1, TestCave.CountWumpusRooms(generator), "Random relocation lost or duplicated the Wumpus.");
            Assert.AreEqual(generator.wumpusRoomNumber, generator.wumpus.GetCellIndex());
        }
    }

    [Test]
    public void MoveWumpus_EventuallyReachesMoreThanOneRoom()
    {
        // The old bug made relocation a no-op for all but one drawn number; over this many
        // attempts a working implementation visits many different rooms.
        System.Collections.Generic.HashSet<int> visited = new System.Collections.Generic.HashSet<int>();

        for (int i = 0; i < 200; i++)
        {
            generator.moveWumpus();
            visited.Add(generator.wumpus.GetCellIndex());
        }

        Assert.Greater(visited.Count, 1, "The Wumpus never relocated.");
    }

    [Test]
    public void MoveWumpusAdj_KeepsTheReportedRoomInSyncWithTheCell()
    {
        TestCave.WireAdjacency(generator);

        for (int i = 0; i < 200; i++)
        {
            generator.moveWumpusAdj();

            Assert.AreEqual(1, TestCave.CountWumpusRooms(generator));
            Assert.AreEqual(generator.wumpusRoomNumber, generator.wumpus.GetCellIndex());
            Assert.IsTrue(generator.wumpus.hasWumpus);
        }
    }
}
