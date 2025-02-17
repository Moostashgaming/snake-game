using static SDL2.SDL;

namespace snake_game;

public readonly struct Window(IntPtr window, uint gridCellWidth, uint gridCellHeight)
{
    public IntPtr WindowPtr { get; } = window;

    public uint GridCellWidth { get; } = gridCellWidth;
    public uint GridCellHeight { get; } = gridCellHeight;

    public int GridOffsetX => (int) (WindowWidthHeight.Width % GridCellWidth) / 2;
    public int GridOffsetY => (int) (WindowWidthHeight.Height % GridCellHeight) / 2;

    public WidthHeight WindowWidthHeight
    {
        get
        {
            SDL_GetWindowSize(window, out var windowWidth, out var windowHeight);
            return new WidthHeight()
            {
                Width = (uint)windowWidth,
                Height = (uint)windowHeight
            };
        }
    }

    public WidthHeight GridWidthHeight =>
        new()
        {
            Width = WindowWidthHeight.Width % GridCellWidth == 0 
                ? WindowWidthHeight.Width / GridCellWidth 
                : (WindowWidthHeight.Width / GridCellWidth) - ((WindowWidthHeight.Width % GridCellWidth) / GridCellWidth),
            Height = WindowWidthHeight.Height % GridCellHeight == 0
                ? WindowWidthHeight.Height / GridCellHeight
                : (WindowWidthHeight.Height / GridCellHeight) - ((WindowWidthHeight.Height % GridCellHeight) / GridCellHeight)
        };

    public struct WidthHeight
    {
        public uint Width { get; init; }
        public uint Height { get; init; }
    }
}