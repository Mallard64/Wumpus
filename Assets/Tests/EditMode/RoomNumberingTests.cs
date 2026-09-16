using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Covers the room-numbering bug: hazards used to be placed against a column-major counter while
/// <see cref="Cell.GetCellIndex"/> numbered rooms row-major, so a hint naming a room could point
/// at a different room than the one carrying that number on screen.
/// </summary>
public class RoomNumberingTests
{
    private CellGenerator generator;

    [SetUp]
    public void SetUp()
    {
        generator = TestCave.Build();
    }

    [TearDown]
    public void TearDown()
    {
        TestCave.Destroy(generator);
    }

    [Test]
    public void GetCellIndex_NumbersEveryRoomExactlyOnce()
    {
        HashSet<int> seen = new HashSet<int>();

        foreach (Cell cell in TestCave.AllCells(generator))
        {
            Assert.IsTrue(seen.Add(cell.GetCellIndex()), $"Room {cell.GetCellIndex()} was numbered twice.");
        }

        Assert.AreEqual(CellGenerator.TotalRooms, seen.Count);
        for (int roomNumber = 1; roomNumber <= CellGenerator.TotalRooms; roomNumber++)
        {
            Assert.IsTrue(seen.Contains(roomNumber), $"Room {roomNumber} was never assigned.");
        }
    }

    [Test]
    public void GetCellIndex_AgreesWithToRoomNumber()
    {
        foreach (Cell cell in TestCave.AllCells(generator))
        {
            Assert.AreEqual(Cell.ToRoomNumber(cell.columnIndex, cell.rowIndex), cell.GetCellIndex());
        }
    }

    [Test]
    public void CellAtRoomNumber_RoundTripsWithGetCellIndex()
    {
        for (int roomNumber = 1; roomNumber <= CellGenerator.TotalRooms; roomNumber++)
        {
            Cell cell = generator.CellAtRoomNumber(roomNumber);

            Assert.IsNotNull(cell, $"Room {roomNumber} did not resolve to a cell.");
            Assert.AreEqual(roomNumber, cell.GetCellIndex());
        }
    }

    [Test]
    public void CellAtRoomNumber_ReturnsNullOutsideTheGrid()
    {
        Assert.IsNull(generator.CellAtRoomNumber(0));
        Assert.IsNull(generator.CellAtRoomNumber(-1));
        Assert.IsNull(generator.CellAtRoomNumber(CellGenerator.TotalRooms + 1));
    }

    [Test]
    public void Room1_IsThePlayerSpawn()
    {
        Assert.AreSame(generator.cells[0, 0], generator.CellAtRoomNumber(1));
    }

    [Test]
    public void SetHazards_PlacesHazardsOnTheRoomNumbersItReports()
    {
        // The regression: these room numbers are what hint text prints, so the cells actually
        // flagged must be the cells carrying those numbers.
        for (int attempt = 0; attempt < 50; attempt++)
        {
            generator.SetHazards();

            Assert.AreEqual(2, generator.pits.Count, "Expected exactly two pits.");
            Assert.AreEqual(2, generator.bats.Count, "Expected exactly two bat colonies.");

            foreach (Cell pit in generator.pits)
            {
                Assert.IsTrue(
                    pit.GetCellIndex() == generator.pitRoom1 || pit.GetCellIndex() == generator.pitRoom2,
                    $"A pit sits in room {pit.GetCellIndex()} but the generator reports pits in "
                    + $"{generator.pitRoom1} and {generator.pitRoom2}.");
            }

            foreach (Cell bat in generator.bats)
            {
                Assert.IsTrue(
                    bat.GetCellIndex() == generator.batRoom1 || bat.GetCellIndex() == generator.batRoom2,
                    $"Bats sit in room {bat.GetCellIndex()} but the generator reports bats in "
                    + $"{generator.batRoom1} and {generator.batRoom2}.");
            }
        }
    }

    [Test]
    public void SetHazards_NeverPlacesAHazardInTheSpawnRoom()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            generator.SetHazards();

            Cell spawn = generator.CellAtRoomNumber(1);
            Assert.IsFalse(spawn.hasPit, "A pit was placed in the spawn room.");
            Assert.IsFalse(spawn.hasBat, "Bats were placed in the spawn room.");
        }
    }

    [Test]
    public void SetHazards_ChoosesFourDistinctRooms()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            generator.SetHazards();

            HashSet<int> rooms = new HashSet<int>
            {
                generator.pitRoom1, generator.pitRoom2, generator.batRoom1, generator.batRoom2
            };
            Assert.AreEqual(4, rooms.Count, "Two hazards were assigned to the same room.");
        }
    }

    [Test]
    public void SetHazards_ClearsTheHazardsItPlacedLastTime()
    {
        generator.SetHazards();
        generator.SetHazards();

        int pitCells = 0;
        int batCells = 0;
        foreach (Cell cell in TestCave.AllCells(generator))
        {
            if (cell.hasPit)
            {
                pitCells++;
            }
            if (cell.hasBat)
            {
                batCells++;
            }
        }

        Assert.AreEqual(2, pitCells, "Pits from the previous layout were left behind.");
        Assert.AreEqual(2, batCells, "Bats from the previous layout were left behind.");
    }
}
