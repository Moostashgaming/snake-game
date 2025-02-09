using SDL2;

namespace snake_game
{
    class Program
    {
        public static void Main(string[] args)
        {
            Window window =
                SDLHelpers.Init(
                    out IntPtr renderer,
                    640,
                    480,
                    50,
                    50
                );

            GameState gameState = new GameState(window);

            gameState.Food.Add(new Food());
            gameState.Snakes.Add(new Snake());

            gameState.Snakes[0] = gameState.SpawnSnake(gameState.Snakes[0], 2);

            for (byte i = 0; i <= gameState.Food.Count - 1; i++)
                gameState.Food[i] = gameState.SpawnFood(gameState.Food[i]);
            
            
            
            l_pause:
            while (gameState.Running)
            {
                // TODO: Poll inputs more often than frames
                gameState = SDLHelpers.EventListen(gameState);

                if (gameState.Paused)
                {
                    SDLHelpers.Render(window, renderer, gameState);
                    goto l_pause;
                }

                gameState.Update(gameState.Snakes);
                SDLHelpers.Render(window, renderer, gameState);
                
                Thread.Sleep(33);
            }

            SDLHelpers.Die(window, renderer);
        }
    }
}