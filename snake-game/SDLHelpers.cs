using static SDL2.SDL;
using static SDL2.SDL.SDL_Scancode;

namespace snake_game;

public class SDLHelpers
{
    public static Window Init(out IntPtr renderer, int windowWidth, int windowHeight, uint gridCellWidth,
        uint gridCellHeight)
    {
        if (SDL_Init(SDL_INIT_VIDEO) < 0)
            Console.WriteLine($"Could not initialize SDL. {SDL_GetError()}");

        var window = SDL_CreateWindow(
            "El Partito de Snake",
            SDL_WINDOWPOS_CENTERED,
            SDL_WINDOWPOS_CENTERED,
            windowWidth,
            windowHeight,
            SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL_WindowFlags.SDL_WINDOW_RESIZABLE
        );

        if (window == IntPtr.Zero)
            Console.WriteLine($"Window creation failed. {SDL_GetError()}");

        renderer = SDL_CreateRenderer(
            window,
            -1,
            SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
            SDL_RendererFlags.SDL_RENDERER_TARGETTEXTURE
        );

        if (renderer == IntPtr.Zero)
            Console.WriteLine($"Renderer creation failed. {SDL_GetError()}");

        return new Window(window, gridCellWidth, gridCellHeight);
    }

    public static GameState EventListen(GameState gameState)
    {
        while (SDL_PollEvent(out SDL_Event e) == 1)
        {
            switch (e.type)
            {
                case SDL_EventType.SDL_QUIT:
                    gameState.EndGame();
                    break;
                case SDL_EventType.SDL_KEYDOWN:
                    switch (e.key.keysym.scancode)
                    {
                        case SDL_SCANCODE_W:
                            gameState.InputBuffer.Add(SDL_SCANCODE_W);
                            break;

                        case SDL_SCANCODE_S:
                            gameState.InputBuffer.Add(SDL_SCANCODE_S);
                            break;

                        case SDL_SCANCODE_A:
                            gameState.InputBuffer.Add(SDL_SCANCODE_A);
                            break;

                        case SDL_SCANCODE_D:
                            gameState.InputBuffer.Add(SDL_SCANCODE_D);
                            break;

                        case SDL_SCANCODE_ESCAPE:
                            gameState.InputBuffer.Add(SDL_SCANCODE_ESCAPE);
                            break;
                    }

                    break;
            }
        }

        return gameState;
    }

    public static void Render(Window window, IntPtr renderer, GameState gameState)
    {
        // Clear render surface
        SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
        SDL_RenderClear(renderer);

        DrawGrid(window, renderer);

        foreach (Snake s in gameState.Snakes)
            DrawSnake(window, renderer, s);

        foreach (Food f in gameState.Food)
            if (f.Eaten != true)
                DrawFood(window, renderer, f);

        // Update render surface
        SDL_RenderPresent(renderer);
    }

    public static void Die(Window window, IntPtr renderer)
    {
        SDL_DestroyRenderer(renderer);
        SDL_DestroyWindow(window.WindowPtr);
        SDL_Quit();
    }

    private static void DrawGrid(Window window, IntPtr renderer)
    {
        SDL_SetRenderDrawColor(renderer, 125, 125, 125, 255);

        // Draw each square in the grid
        for (uint i = 0; i < (window.GridWidthHeight.Width); i++)
        for (uint j = 0; j < (window.GridWidthHeight.Height); j++)
        {
            var rect = new SDL_Rect
            {
                h = (int)window.GridCellHeight,
                w = (int)window.GridCellWidth,
                x = (int)(window.WindowWidthHeight.Width % window.GridCellWidth != 0
                    ? (i * window.GridCellWidth) + ((window.WindowWidthHeight.Width % window.GridCellWidth) / 2)
                    : i * window.GridCellWidth),

                y = (int)(window.WindowWidthHeight.Height % window.GridCellHeight != 0
                    ? (j * window.GridCellHeight) + ((window.WindowWidthHeight.Height % window.GridCellHeight) / 2)
                    : j * window.GridCellHeight),
            };

            SDL_RenderDrawRect(
                renderer,
                ref rect
            );
        }
    }

    private static void DrawSnake(Window window, IntPtr renderer, Snake snake)
    {
        // Draw the snake's head
        var drawPart = new SDL_Rect()
        {
            h = (int)window.GridCellHeight,
            w = (int)window.GridCellWidth,
            x = (int)((snake.X * window.GridCellWidth) +
                      ((window.WindowWidthHeight.Width % window.GridCellWidth) / 2)),
            y = (int)((snake.Y * window.GridCellHeight) +
                      ((window.WindowWidthHeight.Height % window.GridCellHeight) / 2)),
        };

        SDL_SetRenderDrawColor(renderer, 69, 178, 15, 255);

        SDL_RenderFillRect(
            renderer,
            ref drawPart
        );

        if (snake.Length < 2)
            return;

        if (snake.TurnSegments.Count == 0)
            return;
        
        // Draw a rectangle between the first turn segment and the head
        drawPart = new SDL_Rect()
        
        // Draw rectangles between all the turn segments
        for (byte i = 1; i < snake.TurnSegments.Count - 1; i++)
        { 
            drawPart = new SDL_Rect()
            {
                // Calculate the midpoint between the two points to draw the rectangle
                h = Math.Abs(
                    ((int)snake.TurnSegments[i].Y) - ((int)snake.TurnSegments[i + 1].Y)),
                w = Math.Abs(
                    ((int)snake.TurnSegments[i].X) - ((int)snake.TurnSegments[i + 1].X)),

                x = (int)((snake.TurnSegments[i].X + snake.TurnSegments[i + 1].X) / 2),
                y = (int)((snake.TurnSegments[i].Y + snake.TurnSegments[i + 1].Y) / 2)
            };

            SDL_SetRenderDrawColor(renderer, 48, 161, 56, 255);

            SDL_RenderFillRect(
                renderer,
                ref drawPart
            );
        }
    }

    private static void DrawFood(Window window, IntPtr renderer, Food food)
    {
        var foodRect = new SDL_Rect()
        {
            h = (int)window.GridCellHeight,
            w = (int)window.GridCellWidth,
            x = (int)((food.X * window.GridCellWidth) + ((window.WindowWidthHeight.Width % window.GridCellWidth) / 2)),
            y = (int)((food.Y * window.GridCellHeight) +
                      ((window.WindowWidthHeight.Height % window.GridCellHeight) / 2))
        };

        SDL_SetRenderDrawColor(renderer, 255, 0, 0, 255);

        SDL_RenderFillRect(
            renderer,
            ref foodRect
        );
    }
}