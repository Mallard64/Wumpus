using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a <see cref="CellGenerator"/> whose grid is populated with bare <see cref="Cell"/>
/// components.
/// <para>
/// The real generator instantiates a prefab per room, which needs assets and a running scene.
/// These tests only care about room numbering and occupancy bookkeeping, so the grid is assembled
/// directly and the prefab step is skipped.
/// </para>
/// </summary>
internal static class TestCave
{
    /// <summary>Creates a generator with a fully populated grid. Dispose with <see cref="Destroy"/>.</summary>
    public static CellGenerator Build()
    {
        CellGenerator generator = new GameObject("CellGenerator").AddComponent<CellGenerator>();
        generator.cells = new Cell[CellGenerator.GridWidth, CellGenerator.GridHeight];

        for (int column = 0; column < CellGenerator.GridWidth; column++)
        {
            for (int row = 0; row < CellGenerator.GridHeight; row++)
            {
                Cell cell = new GameObject($"Cell {column},{row}").AddComponent<Cell>();
                cell.columnIndex = column;
                cell.rowIndex = row;
                generator.cells[column, row] = cell;
            }
        }

        return generator;
    }

    /// <summary>
    /// Wires every cell's geometric adjacency so movement code has all six keys available.
    /// The links are deliberately simple rather than a faithful hex layout: the tests that use
    /// this assert bookkeeping invariants, not the geometry, which
    /// <see cref="CellGenerator"/> builds itself at runtime.
    /// </summary>
    public static void WireAdjacency(CellGenerator generator)
    {
        for (int column = 0; column < CellGenerator.GridWidth; column++)
        {
            for (int row = 0; row < CellGenerator.GridHeight; row++)
            {
                Cell cell = generator.cells[column, row];
                int up = (row - 1 + CellGenerator.GridHeight) % CellGenerator.GridHeight;
                int down = (row + 1) % CellGenerator.GridHeight;
                int left = (column - 1 + CellGenerator.GridWidth) % CellGenerator.GridWidth;
                int right = (column + 1) % CellGenerator.GridWidth;

                cell.next[Direction.Up] = generator.cells[column, up];
                cell.next[Direction.Down] = generator.cells[column, down];
                cell.next[Direction.UpLeft] = generator.cells[left, up];
                cell.next[Direction.DownLeft] = generator.cells[left, down];
                cell.next[Direction.UpRight] = generator.cells[right, up];
                cell.next[Direction.DownRight] = generator.cells[right, down];

                foreach (string direction in Direction.All)
                {
                    cell.neighbors[direction] = cell;
                }
            }
        }
    }

    /// <summary>Every cell in the grid, in no particular order.</summary>
    public static IEnumerable<Cell> AllCells(CellGenerator generator)
    {
        for (int column = 0; column < CellGenerator.GridWidth; column++)
        {
            for (int row = 0; row < CellGenerator.GridHeight; row++)
            {
                yield return generator.cells[column, row];
            }
        }
    }

    /// <summary>Number of rooms currently flagged as holding the Wumpus.</summary>
    public static int CountWumpusRooms(CellGenerator generator)
    {
        int count = 0;
        foreach (Cell cell in AllCells(generator))
        {
            if (cell.hasWumpus)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>Tears down a generator built by <see cref="Build"/>.</summary>
    public static void Destroy(CellGenerator generator)
    {
        if (generator == null)
        {
            return;
        }

        foreach (Cell cell in AllCells(generator))
        {
            if (cell != null)
            {
                Object.DestroyImmediate(cell.gameObject);
            }
        }

        Object.DestroyImmediate(generator.gameObject);
    }
}
