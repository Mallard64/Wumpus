using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// One room in the cave: its occupancy, its position in the grid, and its links to other rooms.
/// <para>
/// Two adjacency maps are maintained and they are not the same thing:
/// <see cref="neighbors"/> holds only the tunnels the player may actually walk through, while
/// <see cref="next"/> holds the full geometric adjacency used for arrow flight and Wumpus movement.
/// </para>
/// </summary>
public class Cell : MonoBehaviour
{
    private static readonly Color ArrowColor = Color.yellow;
    private static readonly Color WumpusColor = Color.black;
    private static readonly Color WumpusNearbyColor = Color.red;
    private static readonly Color PitColor = Color.cyan;
    private static readonly Color BatColor = Color.magenta;
    private static readonly Color PlayerColor = Color.green;
    private static readonly Color IdleColor = Color.white;

    public bool hasPlayer = false;
    public bool hasWumpus = false;

    /// <summary>True only for the brief window an arrow is passing through this room.</summary>
    public bool hasArrow = false;

    public bool hasPit = false;
    public bool hasBat = false;

    [FormerlySerializedAs("i")]
    public int columnIndex;

    [FormerlySerializedAs("j")]
    public int rowIndex;

    /// <summary>
    /// Walkable tunnels out of this room, keyed by <see cref="Direction"/>.
    /// A direction that maps back to this same cell represents a wall.
    /// </summary>
    public Dictionary<string, Cell> neighbors = new Dictionary<string, Cell>();

    /// <summary>
    /// Full geometric adjacency (with wraparound), keyed by <see cref="Direction"/>.
    /// Used by arrows and the Wumpus, which ignore walls.
    /// </summary>
    public Dictionary<string, Cell> next = new Dictionary<string, Cell>();

    /// <summary>
    /// Number of tunnels carved into this room so far. The generator caps this to keep
    /// the cave from degenerating into a fully connected grid.
    /// </summary>
    public int numConnections = 0;

    private SpriteRenderer cachedRenderer;

    private void Start()
    {
        GetComponentInChildren<Text>().text = GetCellIndex().ToString();
    }

    /// <summary>
    /// The room number shown to the player. This is the project's single room-numbering scheme:
    /// every other system (hazard placement, Wumpus relocation, hint text) resolves rooms through
    /// this method, so a number on screen always refers to the room the player sees it on.
    /// </summary>
    /// <returns>A one-based room number in the range 1..<see cref="CellGenerator.TotalRooms"/>.</returns>
    public int GetCellIndex()
    {
        return ToRoomNumber(columnIndex, rowIndex);
    }

    /// <summary>Room number for a grid position, without needing a live cell.</summary>
    /// <param name="column">Zero-based column.</param>
    /// <param name="row">Zero-based row.</param>
    /// <returns>A one-based room number.</returns>
    public static int ToRoomNumber(int column, int row)
    {
        return CellGenerator.GridWidth * row + column + 1;
    }

    /// <summary>
    /// Whether the Wumpus is in an adjacent room. Drives the "I smell a wumpus!" warning, so a
    /// room that already contains the Wumpus deliberately does not count as "near".
    /// </summary>
    public bool IsNearWumpus()
    {
        return !hasWumpus && AnyNeighbor(neighbor => neighbor.hasWumpus);
    }

    public bool IsNearPits()
    {
        return AnyNeighbor(neighbor => neighbor.hasPit);
    }

    public bool IsNearBats()
    {
        return AnyNeighbor(neighbor => neighbor.hasBat);
    }

    private void Update()
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<SpriteRenderer>();
        }

        cachedRenderer.color = ResolveDisplayColor();
    }

    /// <summary>
    /// Picks the tint for the cell's current state. Arrow flight takes priority over the player,
    /// and any cell without the player stays neutral so hazards are not given away.
    /// </summary>
    private Color ResolveDisplayColor()
    {
        if (hasArrow)
        {
            return hasWumpus ? WumpusColor : ArrowColor;
        }

        if (!hasPlayer)
        {
            return IdleColor;
        }

        if (IsNearWumpus())
        {
            return WumpusNearbyColor;
        }
        if (hasWumpus)
        {
            return WumpusColor;
        }
        if (hasPit)
        {
            return PitColor;
        }
        if (hasBat)
        {
            return BatColor;
        }
        return PlayerColor;
    }

    /// <summary>
    /// Evaluates a predicate against the walkable neighbours. Directions that were never wired up
    /// are skipped rather than throwing, so a partially built cell is still safe to query.
    /// </summary>
    private bool AnyNeighbor(Func<Cell, bool> predicate)
    {
        foreach (string direction in Direction.All)
        {
            if (neighbors.TryGetValue(direction, out Cell neighbor) && neighbor != null && predicate(neighbor))
            {
                return true;
            }
        }
        return false;
    }
}
