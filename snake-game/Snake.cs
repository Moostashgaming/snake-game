namespace snake_game;

public struct Snake()
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right,
        Unset
    };

    public int X { get; set; }
    public int Y { get; set; }

    public uint Length { get; set; } = 1;

    public float SnakeStep { get; set; } = 0;
    
    public List<TurnSegment> TurnSegments { get; } = [];

    public Direction HeadDirection { get; set; } = Direction.Unset;
}