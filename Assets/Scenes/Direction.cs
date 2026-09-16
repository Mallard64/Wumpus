/// <summary>
/// Canonical keys for the six movement directions on the flat-top hex grid.
/// <para>
/// <see cref="Cell.neighbors"/> and <see cref="Cell.next"/> are keyed by these strings, so using
/// the constants instead of string literals keeps lookups from silently missing on a typo.
/// </para>
/// </summary>
public static class Direction
{
    public const string Up = "up";
    public const string Down = "down";
    public const string UpLeft = "upleft";
    public const string UpRight = "upright";
    public const string DownLeft = "downleft";
    public const string DownRight = "downright";

    /// <summary>All six directions, in the order used by random Wumpus movement.</summary>
    public static readonly string[] All = { Up, UpLeft, DownLeft, UpRight, DownRight, Down };
}
