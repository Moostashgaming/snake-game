namespace snake_game;

public struct Food()
{
    public uint X { get; set; }
    public uint Y { get; set; }
    
    public bool Eaten { get; set; } = false;
}