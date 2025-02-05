using static SDL2.SDL;
using static SDL2.SDL.SDL_Scancode;

namespace snake_game;

public class GameState(Window window)
{
    public bool Running { private set; get; } = true;

    public bool Paused { private set; get; } = false;

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
        snake.Length = 40;

        snake.Alive = true;

        InputBuffer.Clear();

        snake.TurnSegments.Clear();

        Random rand = new Random();

        snake.X = rand.Next((int)tileInset, (int)((RunningWindow.GridWidthHeight.Width + 1) - tileInset));
        snake.Y = rand.Next((int)tileInset, (int)((RunningWindow.GridWidthHeight.Height + 1) - tileInset));

        snake.HeadDirection = Snake.Direction.Unset;

        return snake;
    }

    public Food SpawnFood(Food food)
    {
        // TODO: Food can spawn on body
        food.Eaten = false;

        foreach (Snake s in Snakes)
            if (s.Alive)
                goto l_skip_return;
        return food;
        l_skip_return:

        Random rand = new Random();

        int x = rand.Next(1, (int)RunningWindow.GridWidthHeight.Width);
        int y = rand.Next(1, (int)RunningWindow.GridWidthHeight.Height);

        foreach (Snake s in Snakes)
        {
            l_retry_x:
            if (s.X != x)
                food.X = x;
            else
            {
                x = rand.Next(1, (int)RunningWindow.GridWidthHeight.Width);
                goto l_retry_x;
            }

            l_retry_y:
            if (s.Y != y)
                food.Y = y;
            else
            {
                y = rand.Next(1, (int)RunningWindow.GridWidthHeight.Height);
                goto l_retry_y;
            }
        }

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
            break;
        }

        if (
            (snake.Y >= RunningWindow.GridWidthHeight.Height || snake.Y < 0)
            ||
            (snake.X >= RunningWindow.GridWidthHeight.Width || snake.X < 0)
        )
        {
            snake.Alive = false;
            return snake;
        }

        if (snake.TurnSegments.Count == 0)
            return snake;

        // Remove old turn segments
        if (snake.TurnSegments.Count == 1)
        {
            if ((Math.Abs((int)snake.TurnSegments[0].X - snake.X) > snake.Length) ||
                (Math.Abs((int)snake.TurnSegments[0].Y - snake.Y) > snake.Length))
            {
                snake.TurnSegments.Clear();
                return snake;
            }
        }

        int len = 0;

        len += snake.TurnSegments[^1].X == snake.X
            ? Math.Abs((int)snake.TurnSegments[^1].Y - snake.Y)
            : Math.Abs((int)snake.TurnSegments[^1].X - snake.X);

        for (int i = snake.TurnSegments.Count - 1; i > 0; i--)
        {
            len += snake.TurnSegments[i].X == snake.TurnSegments[i - 1].X
                ? Math.Abs((int)snake.TurnSegments[i].Y - (int)snake.TurnSegments[i - 1].Y)
                : Math.Abs((int)snake.TurnSegments[i].X - (int)snake.TurnSegments[i - 1].X);

            if (len < snake.Length)
                continue;

            snake.TurnSegments.RemoveAt(0);
            break;
        }

        int backLen = (int)snake.Length - 1;

        if (snake.HeadDirection is Snake.Direction.Up or Snake.Direction.Down)
            backLen -= (int)Math.Abs(snake.Y - snake.TurnSegments[^1].Y);
        else
            backLen -= (int)Math.Abs(snake.X - snake.TurnSegments[^1].X);

        // Loop through every turn segment and make sure the snake's head didn't hit its body
        for (int i = snake.TurnSegments.Count - 1; i > 0; i--)
        {
            if (snake.TurnSegments[i].FromDirection is Snake.Direction.Up or Snake.Direction.Down)
                backLen -= Math.Abs(((int)snake.TurnSegments[i].Y) - ((int)snake.TurnSegments[i - 1].Y));
            else
                backLen -= Math.Abs(((int)snake.TurnSegments[i].X) - ((int)snake.TurnSegments[i - 1].X));

            if (snake.SnakeStep != 0)
                break;

            // Check if snake's head is between the two points
            if (
                (Math.Min(snake.TurnSegments[(int)i].X, snake.TurnSegments[(int)i - 1].X) <= snake.X
                 &&
                 Math.Max(snake.TurnSegments[(int)i].X, snake.TurnSegments[(int)i - 1].X) >= snake.X)
                &&
                snake.TurnSegments[i].Y == snake.Y
            )
            {
                snake.Alive = false;
                return snake;
            }

            // Check if snake's head is between the two points
            if (
                (Math.Max(snake.TurnSegments[(int)i].Y, snake.TurnSegments[(int)i - 1].Y) >= snake.Y
                 &&
                 Math.Min(snake.TurnSegments[(int)i].Y, snake.TurnSegments[(int)i - 1].Y) <= snake.Y)
                &&
                snake.TurnSegments[i].X == snake.X
            )
            {
                snake.Alive = false;
                return snake;
            }
        }

        if (backLen <= 0)
            return snake;

        switch (snake.TurnSegments[0].FromDirection)
        {
            case Snake.Direction.Up:
                if ((snake.TurnSegments[^1].Y + backLen >= snake.Y && snake.TurnSegments[^1].Y <= snake.Y)
                    &&
                    snake.X == snake.TurnSegments[^1].X)
                    snake.Alive = false;
                break;

            case Snake.Direction.Down:
                if ((snake.TurnSegments[^1].Y - backLen >= snake.Y && snake.TurnSegments[^1].Y <= snake.Y)
                    &&
                    snake.X == snake.TurnSegments[^1].X)
                    snake.Alive = false;
                break;

            case Snake.Direction.Left:
                if ((snake.TurnSegments[^1].X + backLen >= snake.X && snake.TurnSegments[^1].X <= snake.X)
                    &&
                    snake.Y == snake.TurnSegments[^1].Y)
                    snake.Alive = false;
                break;

            case Snake.Direction.Right:
                if ((snake.TurnSegments[^1].X - backLen >= snake.X && snake.TurnSegments[^1].X <= snake.X)
                    &&
                    snake.Y == snake.TurnSegments[^1].Y)
                    snake.Alive = false;
                break;
        }

        return snake;
    }

    private Snake MoveSnake(Snake.Direction d, Snake snake, float speed)
    {
        // TODO: Make speed proportional to grid size
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

        // Respawn out of bounds food
        for (int i = 0; i < Food.Count; i++)
        {
            if (
                ((Food[i].X >= RunningWindow.GridWidthHeight.Width) || (Food[i].X < 0))
                ||
                ((Food[i].Y >= RunningWindow.GridWidthHeight.Height) || (Food[i].Y < 0)))
            {
                Food f = Food[i];
                f.Eaten = true;
                Food[i] = f;
            }
        }

        for (byte i = 0; i < Food.Count; i++)
            if (Food[i].Eaten)
                Food[i] = SpawnFood(Food[i]);

        if (InputBuffer.Count != 0)
        {
            switch (InputBuffer[0])
            {
                case SDL_SCANCODE_W:
                    if (((snake.Length != 1) && (snake.HeadDirection == Snake.Direction.Down)) ||
                        (snake.HeadDirection == Snake.Direction.Up))
                        break;

                    snake.TurnSegments.Add(new TurnSegment((uint)snake.X, (uint)snake.Y, snake.HeadDirection,
                        Snake.Direction.Up));
                    snake.HeadDirection = Snake.Direction.Up;
                    break;

                case SDL_SCANCODE_S:
                    if (((snake.Length != 1) && (snake.HeadDirection == Snake.Direction.Up)) ||
                        (snake.HeadDirection == Snake.Direction.Down))
                        break;

                    snake.TurnSegments.Add(new TurnSegment((uint)snake.X, (uint)snake.Y, snake.HeadDirection,
                        Snake.Direction.Down));
                    snake.HeadDirection = Snake.Direction.Down;
                    break;

                case SDL_SCANCODE_A:
                    if (((snake.Length != 1) && (snake.HeadDirection == Snake.Direction.Right)) ||
                        (snake.HeadDirection == Snake.Direction.Left))
                        break;

                    snake.TurnSegments.Add(new TurnSegment((uint)snake.X, (uint)snake.Y, snake.HeadDirection,
                        Snake.Direction.Left));
                    snake.HeadDirection = Snake.Direction.Left;
                    break;

                case SDL_SCANCODE_D:
                    if (((snake.Length != 1) && (snake.HeadDirection == Snake.Direction.Left)) ||
                        (snake.HeadDirection == Snake.Direction.Right))
                        break;

                    snake.TurnSegments.Add(new TurnSegment((uint)snake.X, (uint)snake.Y, snake.HeadDirection,
                        Snake.Direction.Right));
                    snake.HeadDirection = Snake.Direction.Right;
                    break;
            }

            InputBuffer.RemoveAt(0);
        }

        if (snake.HeadDirection != Snake.Direction.Unset)
            snake = MoveSnake(snake.HeadDirection, snake, .5F);

        snake = UpdateSnake(snake);

        return snake;
    }

    public void EndGame() => Running = false;

    public void PauseGame() => Paused = true;
    public void ResumeGame() => Paused = false;
}