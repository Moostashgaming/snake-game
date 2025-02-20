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
    public Snake SpawnSnake(uint tileInset)
    {
        if (RunningWindow.GridWidthHeight.Width <= tileInset
            ||
            RunningWindow.GridWidthHeight.Height <= tileInset)
            tileInset = 0;

        Food.Clear();
        InputBuffer.Clear();

        Random rand = new Random();

        return new Snake()
        {
            Length = 1,
            X = rand.Next((int)tileInset, (int)((RunningWindow.GridWidthHeight.Width + 1) - tileInset)),
            Y = rand.Next((int)tileInset, (int)((RunningWindow.GridWidthHeight.Height + 1) - tileInset)),
            Speed = .7F
        };
    }

    public Food SpawnFood(short xInset, short yInset)
    {
        ulong retryTimeout = 0;

        Random rand = new Random();

        // Roll for branching
        bool branching = rand.Next(0, 50) == 25;

        // Branching fruit can't spawn on the corners otherwise the game is unwinnable
        short x = (short)rand.Next(
            branching && xInset == 0 ? 1 : xInset,
            branching && yInset == 0
                ? (int)RunningWindow.GridWidthHeight.Width - 1
                : (int)RunningWindow.GridWidthHeight.Width - xInset);

        short y = (short)rand.Next(
            branching && yInset == 0 ? 1 : yInset,
            branching && yInset == 0
                ? (int)RunningWindow.GridWidthHeight.Height - 1
                : (int)RunningWindow.GridWidthHeight.Height - yInset);

        foreach (Snake s in Snakes)
        {
            l_retry:
            retryTimeout++;

            if (retryTimeout == 4 * (RunningWindow.GridWidthHeight.Width * RunningWindow.GridWidthHeight.Height))
                return new Food()
                {
                    X = 0,
                    Y = 0,
                    Branching = false
                };

            x = (short)rand.Next(xInset, (int)RunningWindow.GridWidthHeight.Width - xInset);

            if (s.X == x)
                goto l_retry;

            y = (short)rand.Next(yInset, (int)RunningWindow.GridWidthHeight.Height - yInset);

            if (s.Y == y)
                goto l_retry;

            // Check if the apple is on another existing apple
            foreach (Food f in Food)
                if (x == f.X && y == f.Y)
                    goto l_retry;

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
                            goto l_retry;
                        break;

                    case Snake.Direction.Down:
                        if (((y <= Math.Max(s.Y, s.Y - s.Length - 1))
                             &&
                             (y >= Math.Min(s.Y, s.Y - s.Length - 1))
                             &&
                             x == s.X))
                            goto l_retry;
                        break;

                    case Snake.Direction.Left:
                        if (((x <= Math.Max(s.X, s.X + s.Length - 1))
                             &&
                             (x >= Math.Min(s.X, s.X + s.Length - 1))
                             &&
                             x == s.Y))
                            goto l_retry;
                        break;

                    case Snake.Direction.Right:
                        if (((x <= Math.Max(s.X, s.X - s.Length - 1))
                             &&
                             (x >= Math.Min(s.X, s.X - s.Length - 1))
                             &&
                             x == s.Y))
                            goto l_retry;
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
                        goto l_retry;
                    break;

                case Snake.Direction.Down:
                    if (((y <= Math.Max(s.Y, s.TurnSegments[^1].Y - Math.Abs(s.TurnSegments[^1].Y - s.Y)))
                         &&
                         (y >= Math.Min(s.Y, s.TurnSegments[^1].Y - Math.Abs(s.TurnSegments[^1].Y - s.Y))))
                        &&
                        x == s.X)
                        goto l_retry;
                    break;

                case Snake.Direction.Left:
                    if (((x <= Math.Max(s.X, s.TurnSegments[^1].X + Math.Abs(s.TurnSegments[^1].X - s.X)))
                         &&
                         (x >= Math.Min(s.X, s.TurnSegments[^1].X + Math.Abs(s.TurnSegments[^1].X - s.X))))
                        &&
                        y == s.Y)
                        goto l_retry;
                    break;

                case Snake.Direction.Right:
                    if (((x <= Math.Max(s.X, s.TurnSegments[^1].X - Math.Abs(s.TurnSegments[^1].X - s.X)))
                         &&
                         (x >= Math.Min(s.X, s.TurnSegments[^1].X - Math.Abs(s.TurnSegments[^1].X - s.X))))
                        &&
                        y == s.Y)
                        goto l_retry;
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
                    goto l_retry;


                // Check if apple is between the two points
                if (
                    (Math.Max(s.TurnSegments[i].Y, s.TurnSegments[i - 1].Y) >= y
                     &&
                     Math.Min(s.TurnSegments[i].Y, s.TurnSegments[i - 1].Y) <= y)
                    &&
                    s.TurnSegments[i].X == x
                )
                {
                    goto l_retry;
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
                        goto l_retry;
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
                        goto l_retry;
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
                        goto l_retry;
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
                        goto l_retry;
                    }

                    break;
            }
        }

        return new Food()
        {
            Branching = branching,
            X = x,
            Y = y
        };
    }

    private Snake CleanTurnsegments(Snake snake)
    {
        if (snake.TurnSegments.Count == 0)
            return snake;

        if (snake.SnakeStep != 0)
            return snake;

        // Remove old turn segments
        if (snake.TurnSegments.Count == 1)
        {
            if ((Math.Abs((int)snake.TurnSegments[0].X - snake.X) >= snake.Length) ||
                (Math.Abs((int)snake.TurnSegments[0].Y - snake.Y) >= snake.Length))
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

            if (len <= snake.Length)
                continue;

            snake.TurnSegments.RemoveRange(0, i);
            break;
        }

        return snake;
    }

    private Snake MoveSnake(Snake.Direction d, Snake snake)
    {
        // TODO: Make speed proportional to grid size
        snake.SnakeStep += snake.Speed;

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

    private bool CheckSnakeDied(Snake snake)
    {
        if (snake.SnakeStep != 0)
            return false;

        if (
            (snake.Y >= RunningWindow.GridWidthHeight.Height || snake.Y < 0)
            ||
            (snake.X >= RunningWindow.GridWidthHeight.Width || snake.X < 0)
        )
        {
            return true;
        }

        foreach (Snake other in Snakes)
        {
            if ((other.X == snake.X && other.Y == snake.Y) && !snake.Equals(other))
            {
                // If the snake just branched there'll be two turnsegments on top of each other
                // In this case we should not check if the heads collided as they haven't been given time to separate
                return !(other.TurnSegments[^1].X == snake.TurnSegments[^1].X
                         &&
                         other.TurnSegments[^1].Y == snake.TurnSegments[^1].Y);
            }

            if (other.Length == 1)
                return false;

            if (other.TurnSegments.Count == 0 || snake.TurnSegments.Count == 0)
            {
                switch (other.HeadDirection)
                {
                    case Snake.Direction.Up:
                        if (
                            (other.Y < snake.Y
                             &&
                             other.Y + (other.Length - 1) >= snake.Y)
                            &&
                            snake.X == other.X)
                        {
                            return true;
                        }

                        break;

                    case Snake.Direction.Down:
                        if (
                            (other.Y - (other.Length - 1) <= snake.Y
                             &&
                             other.Y > snake.Y)
                            &&
                            snake.X == other.X)
                        {
                            return true;
                        }

                        break;

                    case Snake.Direction.Left:
                        if (
                            (other.X < snake.X
                             &&
                             other.X + (other.Length - 1) >= snake.X)
                            &&
                            snake.Y == other.Y)
                        {
                            return true;
                        }

                        break;

                    case Snake.Direction.Right:
                        if (
                            (other.X - (other.Length - 1) <= snake.X
                             &&
                             other.X > snake.X)
                            &&
                            snake.Y == other.Y)
                        {
                            return true;
                        }

                        break;
                }

                continue;
            }


            if ((other.TurnSegments[^1].X == snake.TurnSegments[^1].X
                 &&
                 other.TurnSegments[^1].Y == snake.TurnSegments[^1].Y)
                &&
                (snake.X == other.TurnSegments[^1].X
                 &&
                 snake.Y == other.TurnSegments[^1].Y))
                return false;

            int backLen = (int)other.Length - 1;

            if (other.HeadDirection is Snake.Direction.Up or Snake.Direction.Down)
                backLen -= (int)Math.Abs(other.Y - other.TurnSegments[^1].Y);
            else
                backLen -= (int)Math.Abs(other.X - other.TurnSegments[^1].X);

            // Loop through every turn segment and make sure the snake's head didn't hit its body
            for (int i = other.TurnSegments.Count - 1; i > 0; i--)
            {
                if (other.TurnSegments[i].FromDirection is Snake.Direction.Up or Snake.Direction.Down)
                    backLen -= Math.Abs(((int)other.TurnSegments[i].Y) - ((int)other.TurnSegments[i - 1].Y));
                else
                    backLen -= Math.Abs(((int)other.TurnSegments[i].X) - ((int)other.TurnSegments[i - 1].X));

                // Check if snake's head is between the two points
                if (
                    (Math.Min(other.TurnSegments[i].X, other.TurnSegments[i - 1].X) <= snake.X
                     &&
                     Math.Max(other.TurnSegments[i].X, other.TurnSegments[i - 1].X) >= snake.X)
                    &&
                    other.TurnSegments[i].Y == snake.Y
                )
                {
                    return true;
                }

                // Check if snake's head is between the two points
                if (
                    (Math.Max(other.TurnSegments[i].Y, other.TurnSegments[i - 1].Y) >= snake.Y
                     &&
                     Math.Min(other.TurnSegments[i].Y, other.TurnSegments[i - 1].Y) <= snake.Y)
                    &&
                    other.TurnSegments[i].X == snake.X
                )
                {
                    return true;
                }
            }

            // If there's no back to the snake, we don't need to check if we hit it
            if (backLen <= 0)
                continue;

            // Check if we hit the very back of the snake
            switch (other.TurnSegments[0].FromDirection)
            {
                case Snake.Direction.Up:
                    if (
                        (other.TurnSegments[0].Y <= snake.Y
                         &&
                         other.TurnSegments[0].Y + backLen >= snake.Y)
                        &&
                        snake.X == other.TurnSegments[0].X)
                    {
                        return true;
                    }

                    break;

                case Snake.Direction.Down:
                    if (
                        (other.TurnSegments[0].Y - backLen <= snake.Y
                         &&
                         other.TurnSegments[0].Y >= snake.Y)
                        &&
                        snake.X == other.TurnSegments[0].X)
                    {
                        return true;
                    }

                    break;

                case Snake.Direction.Left:
                    if (
                        (other.TurnSegments[0].X <= snake.X
                         &&
                         other.TurnSegments[0].X + backLen >= snake.X)
                        &&
                        snake.Y == other.TurnSegments[0].Y)
                    {
                        return true;
                    }

                    break;

                case Snake.Direction.Right:
                    if (
                        (other.TurnSegments[0].X - backLen <= snake.X
                         &&
                         other.TurnSegments[0].X >= snake.X)
                        &&
                        snake.Y == other.TurnSegments[0].Y)
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    public void Update()
    {
        for (short n = 0; n < Snakes.Count; n++)
        {
            // Check if this snake died, and if it did we need to respawn it and the food
            if (CheckSnakeDied(Snakes[n]))
            {
                Snakes.Clear();
            
                Snakes.Add(SpawnSnake(2));
            
                for (byte i = 0; i < Snakes.Count; i++)
                    Food.Add(SpawnFood(0, 0));
            
                return;
            }
            
            // Check if the snake ate the food, and perform the necessary functions if it did
            for (byte i = 0; i < Food.Count; i++)
            {
                // If the snake is not on the food check the next one
                if (Food[i].X != Snakes[n].X || Food[i].Y != Snakes[n].Y)
                    continue;

                Snakes[n].Length++;
                
                if (Food[i].Branching)
                {
                    // Place another snake on the board
                    Snakes.Add(Snakes[n].Copy());

                    Random rand = new Random();

                    // Randomize the new snake's speed
                    Snakes[^1].Speed = ((float) rand.Next(1, 11) / 10);
                    
                    // Set the head direction of the new snake
                    Snakes[^1].HeadDirection = Snakes[n].HeadDirection switch
                    {
                        Snake.Direction.Up or Snake.Direction.Down => Snake.Direction.Left,
                        Snake.Direction.Left or Snake.Direction.Right => Snake.Direction.Up
                    };

                    // Add a turn segment at the branching point
                    Snakes[^1].TurnSegments.Add(
                        new TurnSegment
                        {
                            X = (uint)Snakes[n].X,
                            Y = (uint)Snakes[n].Y,
                            FromDirection = Snakes[n].HeadDirection,
                            ToDirection = Snakes[^1].HeadDirection
                        }
                    );

                    
                    // Set the old snakes head direction opposite of the new one
                    Snake.Direction newDirection = Snakes[^1].HeadDirection == Snake.Direction.Left
                        ? Snake.Direction.Right
                        : Snake.Direction.Down;

                    // Add a turn segment at the branching point
                    Snakes[n].TurnSegments.Add(
                        new TurnSegment
                        {
                            X = (uint)Snakes[n].X,
                            Y = (uint)Snakes[n].Y,
                            FromDirection = Snakes[n].HeadDirection,
                            ToDirection = newDirection
                        }
                    );

                    Snakes[n].HeadDirection = newDirection;

                    // Place another food for the extra snake
                    Food.Add(SpawnFood(0, 0));
                }

                Food[i] = SpawnFood(0, 0);

                break;
            }

            // Respawn out of bounds food
            for (int i = 0; i < Food.Count; i++)
            {
                if (
                    ((Food[i].X >= RunningWindow.GridWidthHeight.Width) || (Food[i].X < 0))
                    ||
                    ((Food[i].Y >= RunningWindow.GridWidthHeight.Height) || (Food[i].Y < 0)))
                {
                    Food[i] = SpawnFood(0, 0);
                }
            }

            if (InputBuffer.Count == 0)
                goto l_skip_input;

            // Set head direction and add a turnsegment according to InputBuffer
            switch (InputBuffer[0])
            {
                case SDL_SCANCODE_W:
                    if (((Snakes[n].Length != 1) && (Snakes[n].HeadDirection == Snake.Direction.Down)) ||
                        (Snakes[n].HeadDirection == Snake.Direction.Up))
                        break;

                    Snakes[n].TurnSegments.Add(new TurnSegment
                    {
                        X = (uint)Snakes[n].X,
                        Y = (uint)Snakes[n].Y,
                        FromDirection = Snakes[n].HeadDirection,
                        ToDirection = Snake.Direction.Up
                    });

                    Snakes[n].HeadDirection = Snake.Direction.Up;
                    break;

                case SDL_SCANCODE_S:
                    if (((Snakes[n].Length != 1) && (Snakes[n].HeadDirection == Snake.Direction.Up)) ||
                        (Snakes[n].HeadDirection == Snake.Direction.Down))
                        break;

                    Snakes[n].TurnSegments.Add(new TurnSegment
                    {
                        X = (uint)Snakes[n].X,
                        Y = (uint)Snakes[n].Y,
                        FromDirection = Snakes[n].HeadDirection,
                        ToDirection = Snake.Direction.Down
                    });

                    Snakes[n].HeadDirection = Snake.Direction.Down;
                    break;

                case SDL_SCANCODE_A:
                    if (((Snakes[n].Length != 1) && (Snakes[n].HeadDirection == Snake.Direction.Right)) ||
                        (Snakes[n].HeadDirection == Snake.Direction.Left))
                        break;

                    Snakes[n].TurnSegments.Add(new TurnSegment
                    {
                        X = (uint)Snakes[n].X,
                        Y = (uint)Snakes[n].Y,
                        FromDirection = Snakes[n].HeadDirection,
                        ToDirection = Snake.Direction.Left
                    });

                    Snakes[n].HeadDirection = Snake.Direction.Left;
                    break;

                case SDL_SCANCODE_D:
                    if (((Snakes[n].Length != 1) && (Snakes[n].HeadDirection == Snake.Direction.Left)) ||
                        (Snakes[n].HeadDirection == Snake.Direction.Right))
                        break;

                    Snakes[n].TurnSegments.Add(new TurnSegment
                    {
                        X = (uint)Snakes[n].X,
                        Y = (uint)Snakes[n].Y,
                        FromDirection = Snakes[n].HeadDirection,
                        ToDirection = Snake.Direction.Right
                    });

                    Snakes[n].HeadDirection = Snake.Direction.Right;
                    break;
            }

            l_skip_input:

            Snakes[n] = MoveSnake(Snakes[n].HeadDirection, Snakes[n]);
            Snakes[n] = CleanTurnsegments(Snakes[n]);
        }

        if (InputBuffer.Count != 0)
            InputBuffer.RemoveAt(0);
    }

    public void EndGame() => Running = false;

    public void PauseGame() => Paused = true;
    public void ResumeGame() => Paused = false;
}