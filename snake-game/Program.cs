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
                    40,
                    40
                );

            GameState gameState = new GameState(window);

            gameState.Snakes.Add(gameState.SpawnSnake(2));
            gameState.Food.Add(gameState.SpawnFood(0, 0));
            
            l_pause:
            while (gameState.Running)
            {
                // TODO: Poll inputs more often than frames
                gameState = SDLHelpers.EventListen(gameState);

                if (gameState.Paused)
                {
                    SDLHelpers.Render(window, renderer, gameState);
                    Thread.Sleep(11);
                    goto l_pause;
                }

                gameState.Update();
                SDLHelpers.Render(window, renderer, gameState);
                
                Thread.Sleep(33);
            }

            SDLHelpers.Die(window, renderer);
        }
    }
}