using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Builds the cave: instantiates the hex grid, wires up adjacency, carves the random tunnels
/// that make each playthrough different, and places the Wumpus, pits and bats.
/// <para>
/// The grid is a flat-top hex layout stored column-major in <see cref="cells"/>. Every direction
/// wraps around the edges, so the cave is topologically a torus and the player can never
/// walk into a dead end at the boundary.
/// </para>
/// <para>
/// Rooms are numbered by <see cref="Cell.GetCellIndex"/> everywhere, including hazard placement
/// and Wumpus relocation, so every room number the player sees refers to the room it is printed on.
/// </para>
/// </summary>
public class CellGenerator : MonoBehaviour
{
    public const int GridWidth = 6;
    public const int GridHeight = 5;
    public const int TotalRooms = GridWidth * GridHeight;

    /// <summary>Room 1 is the player's spawn and is always kept free of hazards.</summary>
    private const int FirstHazardRoomNumber = 2;

    private const int HazardEligibleRoomCount = TotalRooms - 1;

    /// <summary>
    /// Maximum tunnels a single room may have. Capping this keeps the cave sparse and makes
    /// hazard deduction meaningful instead of turning the grid into an open field.
    /// </summary>
    private const int MaxConnectionsPerCell = 3;

    /// <summary>Tunnels every cell starts with (straight up and straight down).</summary>
    private const int BaseConnectionsPerCell = 2;

    /// <summary>Horizontal packing factor for flat-top hexes: columns overlap by one quarter.</summary>
    private const float HorizontalSpacingFactor = 0.75f;

    /// <summary>World-space X offset applied to the whole grid so it sits inside the camera.</summary>
    private const float GridOriginX = -3.5f;

    /// <summary>World-space Y offset applied to the whole grid so it sits inside the camera.</summary>
    private const float GridOriginY = -4.5f;

    /// <summary>Vertical nudge applied to odd columns to produce the staggered hex layout.</summary>
    private const float OddColumnYOffset = -0.15f;

    public GameObject hexPrefab;

    /// <summary>Prefab reserved for tunnel decoration between rooms.</summary>
    public GameObject bridgePrefab;

    /// <summary>Width of a single hex sprite in world units, used for column spacing.</summary>
    public float hexWidth;

    /// <summary>Height of a single hex sprite in world units, used for row spacing.</summary>
    public float hexHeight;

    /// <summary>All rooms, indexed as <c>[column, row]</c>.</summary>
    [FormerlySerializedAs("kids")]
    public Cell[,] cells = new Cell[GridWidth, GridHeight];

    /// <summary>World-space position of each room, indexed as <c>[column, row]</c>.</summary>
    public Vector2[,] allPositions = new Vector2[GridWidth, GridHeight];

    [FormerlySerializedAs("wumpusnum")]
    public int wumpusRoomNumber = 0;

    public Cell wumpus;

    /// <summary>Cells currently holding bats. Rebuilt on every call to <see cref="SetHazards"/>.</summary>
    public List<Cell> bats = new List<Cell>();

    /// <summary>Cells currently holding pits. Rebuilt on every call to <see cref="SetHazards"/>.</summary>
    public List<Cell> pits = new List<Cell>();

    [FormerlySerializedAs("bat1")]
    public int batRoom1 = 0;

    [FormerlySerializedAs("bat2")]
    public int batRoom2 = 0;

    [FormerlySerializedAs("cave1")]
    public int pitRoom1 = 0;

    [FormerlySerializedAs("cave2")]
    public int pitRoom2 = 0;

    [FormerlySerializedAs("upright")]
    public Sprite upRightSprite;

    [FormerlySerializedAs("upleft")]
    public Sprite upLeftSprite;

    [FormerlySerializedAs("downleft")]
    public Sprite downLeftSprite;

    [FormerlySerializedAs("downright")]
    public Sprite downRightSprite;

    private System.Random rnd = new System.Random();

    private void Start()
    {
        wumpusRoomNumber = rnd.Next(TotalRooms) + 1;
        GenerateGrid();
    }

    /// <summary>
    /// Hides every room. Used when an encounter scene is loaded additively on top of the map.
    /// </summary>
    public void makeDisappear()
    {
        SetAllCellsActive(false);
    }

    public void makeAppear()
    {
        SetAllCellsActive(true);
    }

    /// <summary>
    /// Teleports the Wumpus to a freshly drawn random room.
    /// Called after the player survives a Wumpus encounter.
    /// </summary>
    public void moveWumpus()
    {
        PlaceWumpusAt(rnd.Next(TotalRooms) + 1);
    }

    /// <summary>
    /// Moves the Wumpus to a specific room, clearing it out of the room it currently occupies.
    /// Split out from <see cref="moveWumpus"/> so relocation can be exercised without randomness.
    /// </summary>
    /// <param name="roomNumber">One-based target room, as numbered by <see cref="Cell.GetCellIndex"/>.</param>
    public void PlaceWumpusAt(int roomNumber)
    {
        Cell destination = CellAtRoomNumber(roomNumber);
        if (destination == null)
        {
            return;
        }

        if (wumpus != null)
        {
            wumpus.hasWumpus = false;
        }

        destination.hasWumpus = true;
        wumpus = destination;
        wumpusRoomNumber = roomNumber;
    }

    /// <summary>
    /// Resolves a room number straight to its cell. Room numbering is row-major, so the column
    /// and row fall out of a divide and a remainder rather than a scan of the grid.
    /// </summary>
    /// <param name="roomNumber">One-based room number.</param>
    /// <returns>The matching cell, or <c>null</c> when the number is out of range.</returns>
    public Cell CellAtRoomNumber(int roomNumber)
    {
        if (roomNumber < 1 || roomNumber > TotalRooms)
        {
            return null;
        }

        int zeroBased = roomNumber - 1;
        return cells[zeroBased % GridWidth, zeroBased / GridWidth];
    }

    /// <summary>
    /// Shuffles the Wumpus one room in a random direction. Called occasionally after the player
    /// fires an arrow, so a miss does not leave the Wumpus pinned in place.
    /// </summary>
    public void moveWumpusAdj()
    {
        wumpus.hasWumpus = false;

        string direction = Direction.All[rnd.Next(Direction.All.Length)];
        Cell destination = wumpus.next[direction];

        destination.hasWumpus = true;
        wumpus = destination;
        wumpusRoomNumber = destination.GetCellIndex();
    }

    /// <summary>
    /// Clears the previous hazards and scatters two pits and two bat colonies across the cave,
    /// guaranteeing all four land in distinct rooms and never in the player's spawn room.
    /// </summary>
    public void SetHazards()
    {
        ClearExistingHazards();
        ChooseHazardRooms();
        ApplyHazardsToCells();
    }

    private void SetAllCellsActive(bool isActive)
    {
        for (int column = 0; column < GridWidth; column++)
        {
            for (int row = 0; row < GridHeight; row++)
            {
                cells[column, row].gameObject.SetActive(isActive);
            }
        }
    }

    /// <summary>
    /// Builds the full geometric adjacency map, then carves the walkable tunnels on top of it.
    /// Every room starts with a straight-up and straight-down tunnel; the diagonals are earned
    /// through <see cref="GenerateRandomConnections"/>.
    /// </summary>
    private void GenerateNeighbors()
    {
        for (int column = 0; column < GridWidth; column++)
        {
            for (int row = 0; row < GridHeight; row++)
            {
                Cell cell = cells[column, row];
                cell.numConnections += BaseConnectionsPerCell;
                cell.neighbors[Direction.Up] = CellAbove(column, row);
                cell.neighbors[Direction.Down] = CellBelow(column, row);
            }
        }

        for (int column = 0; column < GridWidth; column++)
        {
            for (int row = 0; row < GridHeight; row++)
            {
                Cell cell = cells[column, row];
                cell.next[Direction.UpRight] = DiagonalNeighbor(column, row, Direction.UpRight);
                cell.next[Direction.DownRight] = DiagonalNeighbor(column, row, Direction.DownRight);
                cell.next[Direction.UpLeft] = DiagonalNeighbor(column, row, Direction.UpLeft);
                cell.next[Direction.DownLeft] = DiagonalNeighbor(column, row, Direction.DownLeft);
                cell.next[Direction.Up] = CellAbove(column, row);
                cell.next[Direction.Down] = CellBelow(column, row);
            }
        }

        GenerateRandomConnections();
    }

    /// <summary>
    /// Carves exactly one extra diagonal tunnel out of each column, retrying with a different
    /// source room whenever the chosen room's neighbours are already at their connection cap.
    /// </summary>
    private void GenerateRandomConnections()
    {
        for (int column = 0; column < GridWidth; column++)
        {
            while (!TryCarveTunnelFromColumn(column))
            {
                // The room drawn had no neighbour with capacity left; draw another one.
            }
        }
    }

    /// <summary>
    /// Picks a room in the given column that still has capacity and tries to link it rightwards.
    /// </summary>
    /// <param name="column">Column to carve a tunnel out of.</param>
    /// <returns>True when a tunnel was carved; false when the caller should retry.</returns>
    private bool TryCarveTunnelFromColumn(int column)
    {
        int row = rnd.Next(GridHeight);
        while (cells[column, row].numConnections >= MaxConnectionsPerCell)
        {
            row = rnd.Next(GridHeight);
        }

        Cell source = cells[column, row];
        Cell upRightTarget = DiagonalNeighbor(column, row, Direction.UpRight);
        Cell downRightTarget = DiagonalNeighbor(column, row, Direction.DownRight);

        // Prefer the up-right neighbour on a coin flip, and fall back to the down-right one.
        if (rnd.Next(2) == 0 && HasCapacity(upRightTarget))
        {
            ConnectRooms(source, upRightTarget, Direction.UpRight, Direction.DownLeft, upRightSprite, downLeftSprite);
            return true;
        }

        if (HasCapacity(downRightTarget))
        {
            ConnectRooms(source, downRightTarget, Direction.DownRight, Direction.UpLeft, downRightSprite, upLeftSprite);
            return true;
        }

        return false;
    }

    private static bool HasCapacity(Cell cell)
    {
        return cell.numConnections < MaxConnectionsPerCell;
    }

    /// <summary>
    /// Opens a two-way walkable tunnel between two rooms and swaps in the matching tunnel sprites.
    /// </summary>
    /// <param name="source">Room the tunnel is carved from.</param>
    /// <param name="target">Room the tunnel leads to.</param>
    /// <param name="outwardDirection">Direction from <paramref name="source"/> to <paramref name="target"/>.</param>
    /// <param name="inwardDirection">Direction from <paramref name="target"/> back to <paramref name="source"/>.</param>
    /// <param name="sourceSprite">Sprite showing the tunnel on the source room.</param>
    /// <param name="targetSprite">Sprite showing the tunnel on the target room.</param>
    private static void ConnectRooms(
        Cell source,
        Cell target,
        string outwardDirection,
        string inwardDirection,
        Sprite sourceSprite,
        Sprite targetSprite)
    {
        target.numConnections++;
        source.numConnections++;
        source.neighbors[outwardDirection] = target;
        target.neighbors[inwardDirection] = source;
        source.GetComponent<SpriteRenderer>().sprite = sourceSprite;
        target.GetComponent<SpriteRenderer>().sprite = targetSprite;
    }

    /// <summary>
    /// Resolves a diagonal neighbour with wraparound. Odd and even columns stagger vertically,
    /// so the row offset differs between them.
    /// </summary>
    /// <param name="column">Source column.</param>
    /// <param name="row">Source row.</param>
    /// <param name="direction">One of the four diagonal <see cref="Direction"/> values.</param>
    /// <returns>The adjacent cell in that diagonal direction.</returns>
    private Cell DiagonalNeighbor(int column, int row, string direction)
    {
        bool isEvenColumn = column % 2 == 0;
        int rightColumn = (column + 1) % GridWidth;
        int leftColumn = (column - 1 + GridWidth) % GridWidth;
        int upperRow = (row - 1 + GridHeight) % GridHeight;
        int lowerRow = (row + 1) % GridHeight;

        switch (direction)
        {
            case Direction.UpRight:
                return isEvenColumn ? cells[rightColumn, upperRow] : cells[rightColumn, row];
            case Direction.DownRight:
                return isEvenColumn ? cells[rightColumn, row] : cells[rightColumn, lowerRow];
            case Direction.UpLeft:
                return isEvenColumn ? cells[leftColumn, upperRow] : cells[leftColumn, row];
            default:
                return isEvenColumn ? cells[leftColumn, row] : cells[leftColumn, lowerRow];
        }
    }

    /// <summary>Cell directly above, wrapping to the bottom row at the top edge.</summary>
    private Cell CellAbove(int column, int row)
    {
        return cells[column, (row - 1 + GridHeight) % GridHeight];
    }

    /// <summary>Cell directly below, wrapping to the top row at the bottom edge.</summary>
    private Cell CellBelow(int column, int row)
    {
        return cells[column, (row + 1) % GridHeight];
    }

    /// <summary>
    /// Instantiates every room, records its world position, then wires up adjacency,
    /// scatters hazards and drops the player into room 1.
    /// </summary>
    private void GenerateGrid()
    {
        for (int column = 0; column < GridWidth; column++)
        {
            for (int row = 0; row < GridHeight; row++)
            {
                Vector2 position = CalculateCellPosition(column, row);
                Cell cell = InstantiateCell(column, row, position);

                if (cell.GetCellIndex() == wumpusRoomNumber)
                {
                    cell.hasWumpus = true;
                    wumpus = cell;
                }

                allPositions[column, row] = position;
            }
        }

        GenerateNeighbors();
        SetHazards();
        cells[0, 0].hasPlayer = true;
    }

    /// <summary>
    /// Computes the world-space position of a room in the staggered flat-top hex layout.
    /// </summary>
    /// <param name="column">Zero-based column.</param>
    /// <param name="row">Zero-based row.</param>
    /// <returns>The room's position in world units.</returns>
    private Vector2 CalculateCellPosition(int column, int row)
    {
        float xPos = (column + 1) * hexWidth * HorizontalSpacingFactor + GridOriginX;
        float stagger = column % 2 == 1 ? OddColumnYOffset : hexHeight / 2;
        float yPos = -1 * row * hexHeight + stagger + GridOriginY;
        return new Vector2(xPos, yPos);
    }

    /// <summary>
    /// Spawns one room prefab, registers it in the grid, and seeds its walkable neighbours to
    /// itself so that an uncarved direction reads as a wall rather than a null reference.
    /// </summary>
    /// <param name="column">Zero-based column the cell occupies.</param>
    /// <param name="row">Zero-based row the cell occupies.</param>
    /// <param name="position">World position to spawn the room at.</param>
    /// <returns>The newly created cell.</returns>
    private Cell InstantiateCell(int column, int row, Vector2 position)
    {
        GameObject hexGO = Instantiate(hexPrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
        hexGO.transform.parent = transform;

        Cell cell = hexGO.GetComponent<Cell>();
        cells[column, row] = cell;
        cell.columnIndex = column;
        cell.rowIndex = row;

        foreach (string direction in Direction.All)
        {
            cell.neighbors[direction] = cell;
        }

        return cell;
    }

    private void ClearExistingHazards()
    {
        ForEachRoom((cell, roomNumber) =>
        {
            if (pitRoom1 == roomNumber || pitRoom2 == roomNumber)
            {
                cell.hasPit = false;
            }
            else if (batRoom1 == roomNumber || batRoom2 == roomNumber)
            {
                cell.hasBat = false;
            }
        });

        pits.Clear();
        bats.Clear();
    }

    /// <summary>
    /// Draws four distinct hazard room numbers, none of which can be the player's spawn room.
    /// </summary>
    private void ChooseHazardRooms()
    {
        pitRoom1 = DrawHazardRoomNumber();

        pitRoom2 = pitRoom1;
        while (pitRoom2 == pitRoom1)
        {
            pitRoom2 = DrawHazardRoomNumber();
        }

        batRoom1 = pitRoom1;
        while (batRoom1 == pitRoom1 || batRoom1 == pitRoom2)
        {
            batRoom1 = DrawHazardRoomNumber();
        }

        batRoom2 = batRoom1;
        while (batRoom2 == pitRoom1 || batRoom2 == pitRoom2 || batRoom2 == batRoom1)
        {
            batRoom2 = DrawHazardRoomNumber();
        }
    }

    private int DrawHazardRoomNumber()
    {
        return rnd.Next(HazardEligibleRoomCount) + FirstHazardRoomNumber;
    }

    private void ApplyHazardsToCells()
    {
        ForEachRoom((cell, roomNumber) =>
        {
            if (pitRoom1 == roomNumber || pitRoom2 == roomNumber)
            {
                cell.hasPit = true;
                pits.Add(cell);
            }
            else if (batRoom1 == roomNumber || batRoom2 == roomNumber)
            {
                cell.hasBat = true;
                bats.Add(cell);
            }
        });
    }

    /// <summary>
    /// Walks the grid, handing each cell its room number as the player sees it. Using
    /// <see cref="Cell.GetCellIndex"/> here is what keeps hazard placement and hint text agreeing.
    /// </summary>
    private void ForEachRoom(Action<Cell, int> action)
    {
        for (int column = 0; column < GridWidth; column++)
        {
            for (int row = 0; row < GridHeight; row++)
            {
                Cell cell = cells[column, row];
                action(cell, cell.GetCellIndex());
            }
        }
    }
}
