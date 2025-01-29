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
                    25,
                    25
                );

            GameState gameState = new GameState(window);

            gameState.Food.Add(new Food());
            gameState.Snakes.Add(new Snake());

            gameState.Snakes[0] = gameState.SpawnSnake(gameState.Snakes[0], 2);

            for (byte i = 0; i <= gameState.Food.Count - 1; i++)
                gameState.Food[i] = gameState.SpawnFood(gameState.Food[i]);
            
            while (gameState.Running)
            {
                gameState = SDLHelpers.EventListen(gameState);
                
                gameState.Snakes[0] = gameState.Update(gameState.Snakes[0]);
                SDLHelpers.Render(window, renderer, gameState);
                
                Thread.Sleep(33);
            }

            SDLHelpers.Die(window, renderer);
        }
    }
}