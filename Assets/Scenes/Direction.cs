/// <summary>
/// Canonical keys for the six movement directions on the flat-top hex grid.
/// <para>
/// <see cref="Cell.neighbors"/> and <see cref="Cell.next"/> are keyed by these strings, so using
/// the constants instead of string literals keeps lookups from silently missing on a typo.
/// </para>
/// </summary>
public static class Direction
{
    /// <summary>Straight up (same column, previous row).</summary>
    public const string Up = "up";

    /// <summary>Straight down (same column, next row).</summary>
    public const string Down = "down";

    /// <summary>Up and to the left.</summary>
    public const string UpLeft = "upleft";

    /// <summary>Up and to the right.</summary>
    public const string UpRight = "upright";

    /// <summary>Down and to the left.</summary>
    public const string DownLeft = "downleft";

    /// <summary>Down and to the right.</summary>
    public const string DownRight = "downright";

    /// <summary>All six directions, in the order used by random Wumpus movement.</summary>
    public static readonly string[] All = { Up, UpLeft, DownLeft, UpRight, DownRight, Down };
}
