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
        snake.Length = 1;

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
        food.Eaten = false;

        Random rand = new Random();

        short x = (short)rand.Next(1, (int)RunningWindow.GridWidthHeight.Width);
        short y = (short)rand.Next(1, (int)RunningWindow.GridWidthHeight.Height);
        
        foreach (Snake s in Snakes)
        {
            l_retry_all:
            l_retry_x:
            x = (short)rand.Next(1, (int)RunningWindow.GridWidthHeight.Width);

            if (s.X == x)
                goto l_retry_x;

            l_retry_y:
            y = (short)rand.Next(1, (int)RunningWindow.GridWidthHeight.Height);

            if (s.Y == y)
                goto l_retry_y;

            if (s.Length == 1)
                continue;

            // Check if the apple is between the head and the back of the snake
            if (s.TurnSegments.Count == 0)
            {
                switch (s.HeadDirection)
                {
                    case Snake.Direction.Up:
                        if (((y <= Math.Max(s.Y, s.Y + s.Length - 1))
                             &&
                             (y >= Math.Min(s.Y, s.Y + s.Length - 1))
                            &&
                            x == s.X))
                            goto l_retry_all;
                        break;
                
                    case Snake.Direction.Down:
                        if (((y <= Math.Max(s.Y, s.Y - s.Length - 1))
                             &&
                             (y >= Math.Min(s.Y, s.Y - s.Length - 1))
                             &&
                             x == s.X))
                            goto l_retry_all;
                        break;
                
                    case Snake.Direction.Left:
                        if (((x <= Math.Max(s.X, s.X + s.Length - 1))
                             &&
                             (x >= Math.Min(s.X, s.X + s.Length - 1))
                             &&
                             x == s.Y))
                            goto l_retry_all;
                        break;
                
                    case Snake.Direction.Right:
                        if (((x <= Math.Max(s.X, s.X - s.Length - 1))
                             &&
                             (x >= Math.Min(s.X, s.X - s.Length - 1))
                             &&
                             x == s.Y))
                            goto l_retry_all;
                        break;
                }

                continue;
            }
            
            // Check if the apple is between the first turn segment and the head
            switch (s.HeadDirection)
            {
                case Snake.Direction.Up:
                    // The mathematics
                    // I tried to make this readable, this is still hell
                    if (((y <= Math.Max(s.Y, s.TurnSegments[^1].Y + Math.Abs(s.TurnSegments[^1].Y - s.Y)))
                         &&
                         (y >= Math.Min(s.Y, s.TurnSegments[^1].Y + Math.Abs(s.TurnSegments[^1].Y - s.Y))))
                        &&
                        x == s.X)
                        goto l_retry_all;
                    break;
                
                case Snake.Direction.Down:
                    if (((y <= Math.Max(s.Y, s.TurnSegments[^1].Y - Math.Abs(s.TurnSegments[^1].Y - s.Y)))
                         &&
                         (y >= Math.Min(s.Y, s.TurnSegments[^1].Y - Math.Abs(s.TurnSegments[^1].Y - s.Y))))
                        &&
                        x == s.X)
                        goto l_retry_all;
                    break;
                
                case Snake.Direction.Left:
                    if (((x <= Math.Max(s.X, s.TurnSegments[^1].X + Math.Abs(s.TurnSegments[^1].X - s.X)))
                         &&
                         (x >= Math.Min(s.X, s.TurnSegments[^1].X + Math.Abs(s.TurnSegments[^1].X - s.X))))
                        &&
                        y == s.Y)
                        goto l_retry_all;
                    break;
                
                case Snake.Direction.Right:
                    if (((x <= Math.Max(s.X, s.TurnSegments[^1].X - Math.Abs(s.TurnSegments[^1].X - s.X)))
                         &&
                         (x >= Math.Min(s.X, s.TurnSegments[^1].X - Math.Abs(s.TurnSegments[^1].X - s.X))))
                        &&
                        y == s.Y)
                        goto l_retry_all;
                    break;
            }

            int backLen = (int)s.Length - 1;

            if (s.HeadDirection is Snake.Direction.Up or Snake.Direction.Down)
                backLen -= (int)Math.Abs(s.Y - s.TurnSegments[^1].Y);
            else
                backLen -= (int)Math.Abs(s.X - s.TurnSegments[^1].X);

            // Check if the apple is between any of the turn segments
            for (int i = s.TurnSegments.Count - 1; i > 0; i--)
            {
                if (s.TurnSegments[i].FromDirection is Snake.Direction.Up or Snake.Direction.Down)
                    backLen -= Math.Abs(((int)s.TurnSegments[i].Y) - ((int)s.TurnSegments[i - 1].Y));
                else
                    backLen -= Math.Abs(((int)s.TurnSegments[i].X) - ((int)s.TurnSegments[i - 1].X));

                // Check if apple is between the two points
                if (
                    (Math.Min(s.TurnSegments[i].X, s.TurnSegments[i - 1].X) <= x
                     &&
                     Math.Max(s.TurnSegments[i].X, s.TurnSegments[i - 1].X) >= x)
                    &&
                    s.TurnSegments[i].Y == y
                )
                    goto l_retry_all;


                // Check if apple is between the two points
                if (
                    (Math.Max(s.TurnSegments[i].Y, s.TurnSegments[i - 1].Y) >= y
                     &&
                     Math.Min(s.TurnSegments[i].Y, s.TurnSegments[i - 1].Y) <= y)
                    &&
                    s.TurnSegments[i].X == x
                )
                {
                    goto l_retry_all;
                }
            }

            if (backLen <= 0)
                continue;

            // Check if the apple is on the very end
            switch (s.TurnSegments[0].FromDirection)
            {
                case Snake.Direction.Up:
                    if (
                        (Math.Min(s.TurnSegments[0].Y + backLen, s.TurnSegments[0].Y) <= y
                         &&
                         Math.Max(s.TurnSegments[0].Y + backLen, s.TurnSegments[0].Y) >= y)
                        &&
                        x == s.TurnSegments[0].X)
                    {
                        goto l_retry_all;
                    }

                    break;

                case Snake.Direction.Down:
                    if (
                        (Math.Min(s.TurnSegments[0].Y - backLen, s.TurnSegments[0].Y) <= y
                         &&
                         Math.Max(s.TurnSegments[0].Y - backLen, s.TurnSegments[0].Y) >= y)
                        &&
                        x == s.TurnSegments[0].X)
                    {
                        goto l_retry_all;
                    }

                    break;

                case Snake.Direction.Left:
                    if (
                        (Math.Min(s.TurnSegments[0].X + backLen, s.TurnSegments[0].X) <= x
                         &&
                         Math.Max(s.TurnSegments[0].X + backLen, s.TurnSegments[0].X) >= x)
                        &&
                        y == s.TurnSegments[0].Y)
                    {
                        goto l_retry_all;
                    }

                    break;

                case Snake.Direction.Right:
                    if (
                        (Math.Min(s.TurnSegments[0].X - backLen, s.TurnSegments[0].X) <= x
                         &&
                         Math.Max(s.TurnSegments[0].X - backLen, s.TurnSegments[0].X) >= x)
                        &&
                        y == s.TurnSegments[0].Y)
                    {
                        goto l_retry_all;
                    }

                    break;
            }
        }

        food.X = x;
        food.Y = y;
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

        if (snake.SnakeStep != 0)
            return snake;


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

            // Check if snake's head is between the two points
            if (
                (Math.Min(snake.TurnSegments[i].X, snake.TurnSegments[i - 1].X) <= snake.X
                 &&
                 Math.Max(snake.TurnSegments[i].X, snake.TurnSegments[i - 1].X) >= snake.X)
                &&
                snake.TurnSegments[i].Y == snake.Y
            )
            {
                snake.Alive = false;
                return snake;
            }

            // Check if snake's head is between the two points
            if (
                (Math.Max(snake.TurnSegments[i].Y, snake.TurnSegments[i - 1].Y) >= snake.Y
                 &&
                 Math.Min(snake.TurnSegments[i].Y, snake.TurnSegments[i - 1].Y) <= snake.Y)
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
                if (
                    (Math.Min(snake.TurnSegments[0].Y + backLen, snake.TurnSegments[0].Y) <= snake.Y
                     &&
                     Math.Max(snake.TurnSegments[0].Y + backLen, snake.TurnSegments[0].Y) >= snake.Y)
                    &&
                    snake.X == snake.TurnSegments[0].X)
                {
                    snake.Alive = false;
                }

                break;

            case Snake.Direction.Down:
                if (
                    (Math.Min(snake.TurnSegments[0].Y - backLen, snake.TurnSegments[0].Y) <= snake.Y
                     &&
                     Math.Max(snake.TurnSegments[0].Y - backLen, snake.TurnSegments[0].Y) >= snake.Y)
                    &&
                    snake.X == snake.TurnSegments[0].X)
                {
                    snake.Alive = false;
                }

                break;

            case Snake.Direction.Left:
                if (
                    (Math.Min(snake.TurnSegments[0].X + backLen, snake.TurnSegments[0].X) <= snake.X
                     &&
                     Math.Max(snake.TurnSegments[0].X + backLen, snake.TurnSegments[0].X) >= snake.X)
                    &&
                    snake.Y == snake.TurnSegments[0].Y)
                {
                    snake.Alive = false;
                }

                break;

            case Snake.Direction.Right:
                if (
                    (Math.Min(snake.TurnSegments[0].X - backLen, snake.TurnSegments[0].X) <= snake.X
                     &&
                     Math.Max(snake.TurnSegments[0].X - backLen, snake.TurnSegments[0].X) >= snake.X)
                    &&
                    snake.Y == snake.TurnSegments[0].Y)
                {
                    snake.Alive = false;
                }

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

    public void Update(List<Snake> snakes)
    {
        for (short n = 0; n < snakes.Count; n++)
        {
            if (!snakes[n].Alive)
            {
                snakes[n] = SpawnSnake(snakes[n], 2);

                for (byte i = 0; i < Food.Count; i++)
                {
                    Food f = Food[i];
                    f.Eaten = true;
                    Food[i] = f;
                }

                return;
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

            if (InputBuffer.Count == 0)
                goto l_skip_input_check;

            Snake s;

            switch (InputBuffer[0])
            {
                case SDL_SCANCODE_W:
                    if (((snakes[n].Length != 1) && (snakes[n].HeadDirection == Snake.Direction.Down)) ||
                        (snakes[n].HeadDirection == Snake.Direction.Up))
                        break;

                    snakes[n].TurnSegments.Add(
                        new TurnSegment((uint)snakes[n].X,
                            (uint)snakes[n].Y,
                            snakes[n].HeadDirection,
                            Snake.Direction.Up));

                    s = snakes[n];
                    s.HeadDirection = Snake.Direction.Up;
                    snakes[n] = s;
                    break;

                case SDL_SCANCODE_S:
                    if (((snakes[n].Length != 1) && (snakes[n].HeadDirection == Snake.Direction.Up)) ||
                        (snakes[n].HeadDirection == Snake.Direction.Down))
                        break;

                    snakes[n].TurnSegments.Add(new TurnSegment((uint)snakes[n].X, (uint)snakes[n].Y,
                        snakes[n].HeadDirection,
                        Snake.Direction.Down));

                    s = snakes[n];
                    s.HeadDirection = Snake.Direction.Down;
                    snakes[n] = s;
                    break;

                case SDL_SCANCODE_A:
                    if (((snakes[n].Length != 1) && (snakes[n].HeadDirection == Snake.Direction.Right)) ||
                        (snakes[n].HeadDirection == Snake.Direction.Left))
                        break;

                    snakes[n].TurnSegments.Add(new TurnSegment((uint)snakes[n].X, (uint)snakes[n].Y,
                        snakes[n].HeadDirection,
                        Snake.Direction.Left));

                    s = snakes[n];
                    s.HeadDirection = Snake.Direction.Left;
                    snakes[n] = s;
                    break;

                case SDL_SCANCODE_D:
                    if (((snakes[n].Length != 1) && (snakes[n].HeadDirection == Snake.Direction.Left)) ||
                        (snakes[n].HeadDirection == Snake.Direction.Right))
                        break;

                    snakes[n].TurnSegments.Add(new TurnSegment((uint)snakes[n].X, (uint)snakes[n].Y,
                        snakes[n].HeadDirection,
                        Snake.Direction.Right));

                    s = snakes[n];
                    s.HeadDirection = Snake.Direction.Right;
                    snakes[n] = s;
                    break;
            }
            
            l_skip_input_check:

            snakes[n] = MoveSnake(snakes[n].HeadDirection, snakes[n], .5F);
            snakes[n] = UpdateSnake(snakes[n]);
        }

        if (InputBuffer.Count != 0)
            InputBuffer.RemoveAt(0);
    }

    public void EndGame() => Running = false;

    public void PauseGame() => Paused = true;
    public void ResumeGame() => Paused = false;
}