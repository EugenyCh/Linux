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
        private static double ySpeed = 1000.0 / 4;
        public static double DeltaTime
        {
            get
            {
                if (Direction == Orientation.Up || Direction == Orientation.Down)
                    return ySpeed;
                return ySpeed / 1.666;
            }
            set
            {
                ySpeed = value;
            }
        }
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
        public static List<Coord> Apples = new List<Coord>();
        public static object locker = new object();
        public static bool Playing = true;
        public const string StringGameOver = "GAME OVER!"; 

        public static List<Coord> GetEmpty()
        {
            var map = new List<Coord>();
            for (int y = Field[0].Y + 1; y < Field[1].Y + Field[0].Y; ++y)
                for (int x = Field[0].X + 1; x < Field[1].X + Field[0].X; ++x)
                    map.Add(new Coord { X = x, Y = y });
            foreach (var coord in Snake)
                map.Remove(coord);
            foreach (var coord in Apples)
                map.Remove(coord);
            return map;
        }

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

        public static void Generate()
        {
            var gen = Apples.Count > 3 ? 0 : 3 - Apples.Count;
            var map = GetEmpty();
            var random = new Random(DateTime.Now.Millisecond);
            while (gen > 0)
            {
                var index = random.Next() % map.Count;
                Apples.Add(map[index]);
                map.RemoveAt(index);
                --gen;
            }
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
            var fore = Console.ForegroundColor;
            foreach (var apple in Apples)
            {
                Console.SetCursorPosition(apple.X, apple.Y);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Q");
            }
            Console.ForegroundColor = fore;
        }

        public static void DrawSnake()
        {
            var head = Snake[Snake.Count - 1];
            var newXY = Move(head);
            if (newXY.X == Field[0].X || newXY.X == Field[0].X + Field[1].X)
                GameOver();
            else if (newXY.Y == Field[0].Y || newXY.Y == Field[0].Y + Field[1].Y)
                GameOver();
            var fore = Console.ForegroundColor;
            if (Playing)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Snake.RemoveAt(0);
                foreach (var coord in Snake)
                {
                    Console.SetCursorPosition(coord.X, coord.Y);
                    Console.Write("#");
                }
                Snake.Add(newXY);
                Console.SetCursorPosition(newXY.X, newXY.Y);
                Console.Write("O");
            }
            Console.ForegroundColor = fore;
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
                    Generate();
                    DrawField();
                    DrawSnake();
                }
            }
        }
    }
}
