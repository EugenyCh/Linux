using System;
using System.Threading;
using System.Collections.Generic;

namespace ConsoleSnake
{
    class Coord
    {
        public int X;
        public int Y;
    }

    enum Orientation
    {
        Left,
        Right,
        Up,
        Down
    }

    class MainClass
    {
        public static double DeltaTime = 1000.0 / 4;
        public static Coord[] Field = {
            new Coord
            {
                X = (Console.WindowWidth - 40) / 2,
                Y = (Console.WindowHeight - 15) / 2
            },
            new Coord { X = 40, Y = 15 }};
        public static List<Coord> Snake = new List<Coord>
        {
            new Coord {
                X = (Field[0].X + Field[1].X) / 2,
                Y = (Field[1].Y + Field[1].Y) / 2}
        };
        public static Orientation Direction = Orientation.Up;
        public static object locker = new object();
        public static bool Playing = true;
        public const string StringGameOver = "GAME OVER!"; 

        public static Coord Move(Coord coord)
        {
            switch (Direction)
            {
                case Orientation.Right:
                    return new Coord { X = coord.X + 1, Y = coord.Y };
                case Orientation.Left:
                    return new Coord { X = coord.X - 1, Y = coord.Y };
                case Orientation.Up:
                    return new Coord { X = coord.X, Y = coord.Y - 1 };
                case Orientation.Down:
                    return new Coord { X = coord.X, Y = coord.Y + 1 };
            }
            return coord;
        }

        public static void DrawField()
        {
            Console.Clear();
            Console.SetCursorPosition(Field[0].X, Field[0].Y);
            Console.Write("+" + new string('-', Field[1].X) + "+");
            Console.SetCursorPosition(Field[0].X, Field[0].Y + Field[1].Y + 1);
            Console.Write("+" + new string('-', Field[1].X) + "+");
            Console.SetCursorPosition(0, Field[0].Y + 1);
            for (int line = 0; line < Field[1].Y; ++line)
                Console.WriteLine(new string(' ', Field[0].X) + "|".PadRight(Field[1].X + 1) + "|");
        }

        public static void DrawSnake()
        {
            var head = Snake[Snake.Count - 1];
            var newXY = Move(head);
            if (newXY.X == Field[0].X || newXY.X == Field[0].X + Field[1].X)
                GameOver();
            else if (newXY.Y == Field[0].Y || newXY.Y == Field[0].Y + Field[1].Y)
                GameOver();
            if (Playing)
            {
                Snake.Add(newXY);
                Snake.RemoveAt(0);
                foreach (var coord in Snake)
                {
                    Console.SetCursorPosition(coord.X, coord.Y);
                    Console.Write("#");
                }
            }
        }

        public static void GameOver()
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.Clear();
            Playing = false;
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition((Console.WindowWidth - StringGameOver.Length) / 2, Console.WindowHeight / 2);
            Console.WriteLine(StringGameOver);
        }

        public static void HandleEvents()
        {
            ConsoleKeyInfo keyInfo;
            do
            {
                lock (locker)
                {
                    keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.W || keyInfo.Key == ConsoleKey.UpArrow)
                        Direction = Orientation.Up;
                    else if (keyInfo.Key == ConsoleKey.S || keyInfo.Key == ConsoleKey.DownArrow)
                        Direction = Orientation.Down;
                    else if (keyInfo.Key == ConsoleKey.D || keyInfo.Key == ConsoleKey.RightArrow)
                        Direction = Orientation.Right;
                    else if (keyInfo.Key == ConsoleKey.A || keyInfo.Key == ConsoleKey.LeftArrow)
                        Direction = Orientation.Left;
                }
            } while (keyInfo.Key != ConsoleKey.Escape);
        }

        public static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorVisible = false;
            var time = DateTime.Now;
            var thread = new Thread(HandleEvents);
            thread.Start();
            while (thread.IsAlive && Playing)
            {
                var delta = DateTime.Now - time;
                if (delta.TotalMilliseconds > DeltaTime)
                {
                    time = time.AddMilliseconds(DeltaTime);
                    DrawField();
                    DrawSnake();
                }
            }
        }
    }
}
