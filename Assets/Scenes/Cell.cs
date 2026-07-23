using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// One room in the cave. A cell tracks its own occupancy (player, Wumpus, arrow, pit, bat),
/// its position in the grid, and its links to surrounding rooms.
/// <para>
/// Two adjacency maps are maintained and they are not the same thing:
/// <see cref="neighbors"/> holds only the tunnels the player may actually walk through, while
/// <see cref="next"/> holds the full geometric adjacency used for arrow flight and Wumpus movement.
/// </para>
/// <para>The renderer tint is refreshed every frame to reflect the cell's current state.</para>
/// </summary>
public class Cell : MonoBehaviour
{
    /// <summary>Tint of a cell holding an arrow in flight.</summary>
    private static readonly Color ArrowColor = Color.yellow;

    /// <summary>Tint of a cell containing the Wumpus once it has been revealed.</summary>
    private static readonly Color WumpusColor = Color.black;

    /// <summary>Tint of the player's cell when the Wumpus is one room away.</summary>
    private static readonly Color WumpusNearbyColor = Color.red;

    /// <summary>Tint of the player's cell when it contains a bottomless pit.</summary>
    private static readonly Color PitColor = Color.cyan;

    /// <summary>Tint of the player's cell when it contains a colony of bats.</summary>
    private static readonly Color BatColor = Color.magenta;

    /// <summary>Tint of the player's cell when it is safe.</summary>
    private static readonly Color PlayerColor = Color.green;

    /// <summary>Tint of any cell the player is not currently standing in.</summary>
    private static readonly Color IdleColor = Color.white;

    /// <summary>True while the player occupies this room.</summary>
    public bool hasPlayer = false;

    /// <summary>True while the Wumpus occupies this room.</summary>
    public bool hasWumpus = false;

    /// <summary>True for the brief window an arrow is passing through this room.</summary>
    public bool hasArrow = false;

    /// <summary>True when this room contains a bottomless pit hazard.</summary>
    public bool hasPit = false;

    /// <summary>True when this room contains a bat hazard.</summary>
    public bool hasBat = false;

    /// <summary>Zero-based column of this cell in the generated grid.</summary>
    [FormerlySerializedAs("i")]
    public int columnIndex;

    /// <summary>Zero-based row of this cell in the generated grid.</summary>
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

    /// <summary>Stamps the room number onto the cell's label.</summary>
    private void Start()
    {
        GetComponentInChildren<Text>().text = GetCellIndex().ToString();
    }

    /// <summary>
    /// Room number shown to the player, derived from the cell's grid position.
    /// </summary>
    /// <returns>A one-based room number in the range 1..(width * height).</returns>
    public int GetCellIndex()
    {
        return CellGenerator.GridWidth * rowIndex + columnIndex + 1;
    }

    /// <summary>
    /// Whether the Wumpus is in an adjacent room. Drives the "I smell a wumpus!" warning, so a
    /// room that already contains the Wumpus deliberately does not count as "near".
    /// </summary>
    /// <returns>True when a neighbouring room holds the Wumpus and this one does not.</returns>
    public bool IsNearWumpus()
    {
        return !hasWumpus && AnyNeighbor(neighbor => neighbor.hasWumpus);
    }

    /// <summary>Whether any adjacent room contains a pit.</summary>
    /// <returns>True when a neighbouring room holds a pit.</returns>
    public bool IsNearPits()
    {
        return AnyNeighbor(neighbor => neighbor.hasPit);
    }

    /// <summary>Whether any adjacent room contains bats.</summary>
    /// <returns>True when a neighbouring room holds bats.</returns>
    public bool IsNearBats()
    {
        return AnyNeighbor(neighbor => neighbor.hasBat);
    }

    /// <summary>Refreshes the cell tint to match its current occupancy.</summary>
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

    /// <summary>Evaluates a predicate against all six walkable neighbours.</summary>
    private bool AnyNeighbor(Func<Cell, bool> predicate)
    {
        foreach (string direction in Direction.All)
        {
            if (predicate(neighbors[direction]))
            {
                return true;
            }
        }
        return false;
    }
}
