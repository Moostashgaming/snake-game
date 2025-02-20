namespace snake_game;

public readonly struct TurnSegment
{
    public uint X { get; init; }
    public uint Y { get; init; }
    
    /// <summary>
    /// The direction the snake's head was facing before the turn
    /// </summary>
    public Snake.Direction FromDirection { get; init; }
    
    /// <summary>
    /// The direction the snake's head was facing after the turn
    /// </summary>
    public Snake.Direction ToDirection { get; init; }
}