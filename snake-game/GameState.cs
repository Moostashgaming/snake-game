using static SDL2.SDL;
using static SDL2.SDL.SDL_Scancode;

namespace snake_game;

public class GameState(Window window)
{
    public bool Running { private set; get; } = true;

    private Window RunningWindow { get; } = window;

    public List<Snake> Snakes { get; } = [];

    public List<Food> Food { get; } = [];

    public List<SDL_Scancode> InputBuffer { get; } = [];

    /// <summary>
    /// Spawn the snake in a random location inset from the edge tileInset tiles
    /// </summary>
    /// <param name="tileInset">How many tiles to inset the snake's spawn area from the edge</param>
    public Snake SpawnSnake(Snake snake, uint tileInset)
    {
        snake.Length = 1;

        snake.Alive = true;

        InputBuffer.Clear();

        snake.TurnSegments.Clear();

        Random rand = new Random();

        snake.X = rand.Next((int)tileInset, (int)((RunningWindow.GridWidthHeight.Width + 1) - tileInset));
        snake.Y = rand.Next((int)tileInset, (int)((RunningWindow.GridWidthHeight.Height + 1) - tileInset));

        snake.HeadDirection = Snake.Direction.Unset;

        Console.WriteLine($"Snake spawn X: {snake.X}, Y: {snake.Y}");

        return snake;
    }

    public Food SpawnFood(Food food)
    {
        food.Eaten = false;

        foreach (Snake s in Snakes)
            if (s.Alive)
                goto l_skip_return;
        return food;
        l_skip_return:

        Random rand = new Random();

        uint x = (uint)rand.Next(1, (int)RunningWindow.GridWidthHeight.Width + 1);
        uint y = (uint)rand.Next(1, (int)RunningWindow.GridWidthHeight.Height + 1);

        foreach (Snake s in Snakes)
        {
            l_retry_x:
            if (s.X != x)
                food.X = x;
            else
            {
                x = (uint)rand.Next(1, (int)RunningWindow.GridWidthHeight.Width + 1);
                goto l_retry_x;
            }

            l_retry_y:
            if (s.Y != y)
                food.Y = y;
            else
            {
                y = (uint)rand.Next(1, (int)RunningWindow.GridWidthHeight.Height + 1);
                goto l_retry_y;
            }
        }

        Console.WriteLine($"Food Spawn X: {food.X}, Y: {food.Y}");

        return food;
    }

    private Snake UpdateSnake(Snake snake)
    {
        for (byte i = 0; i < Food.Count; i++)
        {
            if (Food[i].X != snake.X || Food[i].Y != snake.Y)
                continue;

            Food f = Food[i];
            f.Eaten = true;
            Food[i] = f;
            snake.Length++;
        }

        if (
            (snake.Y > RunningWindow.GridWidthHeight.Height || snake.Y < 0)
            ||
            (snake.X > RunningWindow.GridWidthHeight.Width || snake.X < 0)
        )
        {
            snake.Alive = false;
            return snake;
        }

        // Loop through every turn segment and make sure the snake's head didn't hit its body
        for (uint i = 0; i < snake.TurnSegments.Count - 1; i++)
        {
            if (snake.SnakeStep == 0)
                break;
                
            // Check if the snake's head is not coplanar with two turn segments (A segment of its body)
            if (
                (snake.TurnSegments[(int)i].X != snake.X)
                &&
                (snake.TurnSegments[(int)i + 1].X != snake.X)
            )
            {
                continue;
            }

            // Check if snake's head is between the two points
            if (
                Math.Min(snake.TurnSegments[(int)i].X, snake.TurnSegments[(int)i + 1].X) <= snake.X
                &&
                Math.Max(snake.TurnSegments[(int)i].X, snake.TurnSegments[(int)i + 1].X) >= snake.X
            )
            {
                snake.Alive = false;
                return snake;
            }

            // Check if the snake's head is NOT coplanar with two turn segments (A segment of its body)
            if (
                (snake.TurnSegments[(int)i].Y != snake.Y)
                &&
                (snake.TurnSegments[(int)i + 1].Y != snake.Y)
            )
            {
                continue;
            }

            // Check if snake's head is between the two points
            if (
                Math.Max(snake.TurnSegments[(int)i].Y, snake.TurnSegments[(int)i + 1].Y) >= snake.Y
                &&
                Math.Min(snake.TurnSegments[(int)i].Y, snake.TurnSegments[(int)i + 1].Y) <= snake.Y
            )
            {
                snake.Alive = false;
                return snake;
            }
        }

        return snake;
    }

    private Snake MoveSnake(Snake.Direction d, Snake snake, float speed)
    {
        snake.SnakeStep += speed;
        
        if (snake.SnakeStep >= 1)
        {
            switch (d)
            {
                case Snake.Direction.Up:
                    snake.Y -= 1;
                    break;

                case Snake.Direction.Down:
                    snake.Y += 1;
                    break;

                case Snake.Direction.Left:
                    snake.X -= 1;
                    break;

                case Snake.Direction.Right:
                    snake.X += 1;
                    break;
            }
            
            snake.SnakeStep = 0;
        }

        return snake;
    }

    public Snake Update(Snake snake)
    {
        if (!snake.Alive)
        {
            snake = SpawnSnake(snake, 2);

            for (byte i = 0; i < Food.Count; i++)
            {
                Food f = Food[i];
                f.Eaten = true;
                Food[i] = f;
            }

            return snake;
        }

        Console.WriteLine($"Snake Length: {snake.Length}");
        Console.WriteLine($"Snake Turn Segments: {snake.TurnSegments.Count}");


        if (InputBuffer.Count != 0)
        {
            switch (InputBuffer[0])
            {
                case SDL_SCANCODE_W:
                    if ((snake.Length != 1) && (snake.HeadDirection == Snake.Direction.Down))
                        break;

                    snake.TurnSegments.Add(new TurnSegment((uint)snake.X, (uint)snake.Y, snake.HeadDirection, Snake.Direction.Up));
                    snake.HeadDirection = Snake.Direction.Up;
                    break;

                case SDL_SCANCODE_S:
                    if ((snake.Length != 1) && (snake.HeadDirection == Snake.Direction.Up))
                        break;

                    snake.TurnSegments.Add(new TurnSegment((uint)snake.X, (uint)snake.Y, snake.HeadDirection, Snake.Direction.Down));
                    snake.HeadDirection = Snake.Direction.Down;
                    break;

                case SDL_SCANCODE_A:
                    if ((snake.Length != 1) && (snake.HeadDirection == Snake.Direction.Right))
                        break;

                    snake.TurnSegments.Add(new TurnSegment((uint)snake.X, (uint)snake.Y, snake.HeadDirection, Snake.Direction.Left));
                    snake.HeadDirection = Snake.Direction.Left;
                    break;

                case SDL_SCANCODE_D:
                    if ((snake.Length != 1) && (snake.HeadDirection == Snake.Direction.Left))
                        break;

                    snake.TurnSegments.Add(new TurnSegment((uint)snake.X, (uint)snake.Y, snake.HeadDirection, Snake.Direction.Right));
                    snake.HeadDirection = Snake.Direction.Right;
                    break;
            }

            InputBuffer.RemoveAt(0);
        }

        if (snake.HeadDirection != Snake.Direction.Unset)
            snake = MoveSnake(snake.HeadDirection, snake, .5F);

        snake = UpdateSnake(snake);

        for (byte i = 0; i < Food.Count; i++)
            if (Food[i].Eaten)
                Food[i] = SpawnFood(Food[i]);

        return snake;
    }

    public void EndGame()
    {
        Running = false;
    }
}