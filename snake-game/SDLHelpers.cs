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
                            if (!gameState.Paused)
                                gameState.InputBuffer.Add(SDL_SCANCODE_W);
                            break;

                        case SDL_SCANCODE_S:
                            if (!gameState.Paused)
                                gameState.InputBuffer.Add(SDL_SCANCODE_S);
                            break;

                        case SDL_SCANCODE_A:
                            if (!gameState.Paused)
                                gameState.InputBuffer.Add(SDL_SCANCODE_A);
                            break;

                        case SDL_SCANCODE_D:
                            if (!gameState.Paused)
                                gameState.InputBuffer.Add(SDL_SCANCODE_D);
                            break;

                        case SDL_SCANCODE_ESCAPE:
                            if (!gameState.Paused)
                            {
                                gameState.PauseGame();
                                break;
                            }

                            gameState.ResumeGame();
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

        foreach (TurnSegment t in gameState.Snakes[0].TurnSegments)
        {
            var rect = new SDL_Rect()
            {
                h = (int)window.GridCellHeight,
                w = (int)window.GridCellWidth,
                x = (int)(t.X * window.GridCellWidth) + window.GridOffsetX,
                y = (int)(t.Y * window.GridCellHeight) + window.GridOffsetY
            };

            SDL_SetRenderDrawColor(renderer, 195, 63, 182, 255);
            SDL_RenderFillRect(renderer, ref rect);
        }

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
                    ? (i * window.GridCellWidth) + window.GridOffsetX
                    : i * window.GridCellWidth),

                y = (int)(window.WindowWidthHeight.Height % window.GridCellHeight != 0
                    ? (j * window.GridCellHeight) + window.GridOffsetY
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
        // TODO: Watch these calculations (uInts could cause problems)
        // Draw the snake's head
        var drawPart = new SDL_Rect()
        {
            h = (int)window.GridCellHeight,
            w = (int)window.GridCellWidth,
            x = (int)(snake.X * window.GridCellWidth) + window.GridOffsetX,
            y = (int)(snake.Y * window.GridCellHeight) + window.GridOffsetY
        };

        SDL_SetRenderDrawColor(renderer, 69, 178, 15, 255);

        SDL_RenderFillRect(
            renderer,
            ref drawPart
        );

        if (snake.Length < 2)
            return;
        
        if (snake.TurnSegments.Count == 0)
        {
            // Draw a rectangle from the head to back of the snake
            drawPart = new SDL_Rect()
            {
                // If the head is facing up the rectangle should be one grid square in width and Snake.Length * GridCellHeight in length and vice versa
                h = (int)(snake.HeadDirection is Snake.Direction.Up or Snake.Direction.Down
                    ? (snake.Length - 1) * window.GridCellHeight
                    : window.GridCellHeight),
                w = (int)(snake.HeadDirection is Snake.Direction.Left or Snake.Direction.Right
                    ? (snake.Length - 1) * window.GridCellWidth
                    : window.GridCellWidth),

                // If the head is facing left the body needs to be drawn to the right and vice versa
                x = (int)((snake.HeadDirection switch
                {
                    Snake.Direction.Left => snake.X + 1,
                    Snake.Direction.Right => snake.X - (snake.Length - 1),
                    _ => snake.X
                } * (window.GridCellWidth)) + (window.GridOffsetX)),

                y = (int)((snake.HeadDirection switch
                {
                    Snake.Direction.Up => snake.Y + 1,
                    Snake.Direction.Down => snake.Y - (snake.Length - 1),
                    _ => snake.Y
                } * (window.GridCellHeight)) + (window.GridOffsetY)),
            };
            
            SDL_SetRenderDrawColor(renderer, 48, 161, 56, 255);

            SDL_RenderFillRect(
                renderer,
                ref drawPart
            );
            
            return;
        }
        
        int lenToDraw = (int)snake.Length - 1;

        if (snake.HeadDirection is Snake.Direction.Up or Snake.Direction.Down)
            lenToDraw -= (int)Math.Abs(snake.Y - snake.TurnSegments[^1].Y);
        else
            lenToDraw -= (int)Math.Abs(snake.X - snake.TurnSegments[^1].X);
            
        // Draw a rectangle between the first turn segment and the head
        drawPart = new SDL_Rect()
        {
            // If the head is facing up the rectangle should be one grid square in width and the distance between the turn segment and the head * GridCellHeight in length and vice versa
            h = (int)(snake.HeadDirection is Snake.Direction.Up or Snake.Direction.Down
                ? Math.Abs(snake.Y - snake.TurnSegments[^1].Y) * window.GridCellHeight
                : window.GridCellHeight),
            w = (int)(snake.HeadDirection is Snake.Direction.Left or Snake.Direction.Right
                ? Math.Abs(snake.X - snake.TurnSegments[^1].X) * window.GridCellWidth
                : window.GridCellWidth),

            // If the head is facing left the body needs to be drawn to the right and vice versa
            x = (int)((snake.HeadDirection switch
            {
                Snake.Direction.Left => snake.X + 1,
                Snake.Direction.Right => snake.X - (Math.Abs(snake.X - snake.TurnSegments[0].X)),
                _ => snake.X
            } * window.GridCellWidth) + window.GridOffsetX),
            y = (int)((snake.HeadDirection switch
            {
                Snake.Direction.Up => snake.Y + 1,
                Snake.Direction.Down => snake.Y - (Math.Abs(snake.Y - snake.TurnSegments[0].Y)),
                _ => snake.Y
            } * window.GridCellHeight) + window.GridOffsetY),
        };

        SDL_SetRenderDrawColor(renderer, 48, 161, 56, 255);

        SDL_RenderFillRect(
            renderer,
            ref drawPart
        );

        // Draw rectangles between all the turn segments
        for (int i = snake.TurnSegments.Count - 1; i > 0; i--)
        {
            if (snake.TurnSegments[i].FromDirection is Snake.Direction.Up or Snake.Direction.Down)
                lenToDraw -= Math.Abs(((int)snake.TurnSegments[i].Y) - ((int)snake.TurnSegments[i - 1].Y));
            else
                lenToDraw -= Math.Abs(((int)snake.TurnSegments[i].X) - ((int)snake.TurnSegments[i - 1].X));
            
            drawPart = new SDL_Rect()
            {
                h = (int)(snake.TurnSegments[i].FromDirection is Snake.Direction.Up or Snake.Direction.Down
                    ? Math.Abs(
                          ((int)snake.TurnSegments[i].Y) - ((int)snake.TurnSegments[i - 1].Y)) * window.GridCellHeight
                    : window.GridCellHeight),
                w = (int)(snake.TurnSegments[i].FromDirection is Snake.Direction.Left or Snake.Direction.Right
                    ? Math.Abs(
                          ((int)snake.TurnSegments[i].X) - ((int)snake.TurnSegments[i - 1].X)) * window.GridCellWidth
                    : window.GridCellWidth),

                x = (int)((snake.TurnSegments[i].FromDirection switch
                {
                    // Draw the segment one over from the previous turn segment
                    Snake.Direction.Left => snake.TurnSegments[i].X + 1,
                    Snake.Direction.Right => snake.TurnSegments[i].X -
                                             (Math.Abs(snake.TurnSegments[i].X - snake.TurnSegments[i - 1].X)),
                    _ => snake.TurnSegments[i].X
                } * window.GridCellWidth) + window.GridOffsetX),
                y = (int)((snake.TurnSegments[i].FromDirection switch
                {
                    Snake.Direction.Up => snake.TurnSegments[i].Y + 1,
                    Snake.Direction.Down => snake.TurnSegments[i].Y -
                                            (Math.Abs(snake.TurnSegments[i].Y - snake.TurnSegments[i - 1].Y)),
                    _ => snake.TurnSegments[i].Y
                } * window.GridCellHeight) + window.GridOffsetY),
            };

            SDL_SetRenderDrawColor(renderer, 0, 0, 255, 255);

            SDL_RenderFillRect(
                renderer,
                ref drawPart
            );
        }
        
        if (lenToDraw < 0)
            lenToDraw = 0;
        
        // Draw from the last turn segment to the back
        drawPart = new SDL_Rect()
        {
            h = (int)(snake.TurnSegments[0].FromDirection is Snake.Direction.Down or Snake.Direction.Up
            ? lenToDraw * window.GridCellHeight
            : window.GridCellHeight),
            w = (int)(snake.TurnSegments[0].FromDirection is Snake.Direction.Left or Snake.Direction.Right
            ? lenToDraw * window.GridCellWidth
            : window.GridCellWidth),
            
            x = (int)((snake.TurnSegments[0].FromDirection switch
            {
                Snake.Direction.Left => snake.TurnSegments[0].X + 1,
                Snake.Direction.Right => snake.TurnSegments[0].X - lenToDraw,
                _ => snake.TurnSegments[0].X
            } * window.GridCellWidth) + window.GridOffsetX),
            y = (int)((snake.TurnSegments[0].FromDirection switch
            {
                Snake.Direction.Up => snake.TurnSegments[0].Y + 1,
                Snake.Direction.Down => snake.TurnSegments[0].Y - lenToDraw,
                _ => snake.TurnSegments[0].Y
            } * window.GridCellHeight) + window.GridOffsetY)
        };
        
        SDL_SetRenderDrawColor(renderer, 200, 200, 0, 255);

        SDL_RenderFillRect(
            renderer,
            ref drawPart
        );
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