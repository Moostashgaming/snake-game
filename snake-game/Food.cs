namespace snake_game;

public readonly struct Food()
{
    public int X { get; init; }
    public int Y { get; init; }
    
    public bool Branching { get; init; } = false;
}