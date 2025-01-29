namespace snake_game;

public readonly struct TurnSegment (uint x, uint y, Snake.Direction fromDirection, Snake.Direction toDirection)
{
    public uint X { get; } = x;
    public uint Y { get; } = y;
    
    /// <summary>
    /// The direction the snake's head was facing before the turn
    /// </summary>
    public Snake.Direction FromDirection { get; } = fromDirection;
    
    /// <summary>
    /// The direction the snake's head was facing after the turn
    /// </summary>
    public Snake.Direction ToDirection { get; } = toDirection;
}