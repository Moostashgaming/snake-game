namespace snake_game;

public class Snake()
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
    
    public float Speed { get; set; }
    
    public List<TurnSegment> TurnSegments { get; private set; } = [];

    public Direction HeadDirection { get; set; } = Direction.Unset;

    public Snake Copy()
    {
        Snake s = (Snake)this.MemberwiseClone();
            
        s.TurnSegments = s.TurnSegments.ConvertAll(segment => new TurnSegment()
        {
            X = segment.X,
            Y = segment.Y,
            ToDirection = segment.ToDirection,
            FromDirection = segment.FromDirection
        });

        return s;
    }
}