using System;
using System.Threading;
using System.Collections.Generic;

namespace ConsoleSnake
{
    class Coord : IEquatable<Coord>
    {
        public int X;
        public int Y;
        public bool Equals(Coord other)
        {
            return X == other.X && Y == other.Y;
        }
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
        private static double ySpeed = 1000.0 / 3;
        public static double DeltaTime
        {
            get
            {
                if (Direction == Orientation.Up || Direction == Orientation.Down)
                    return ySpeed;
                return ySpeed / 2;
            }
            set
            {
                ySpeed = value;
            }
        }
        public static Coord[] Field = {
            new Coord
            {
                X = (Console.WindowWidth - 41) / 2,
                Y = (Console.WindowHeight - 15) / 2
            },
            new Coord { X = 41, Y = 15 }};
        public static List<Coord> Snake = new List<Coord>
        {
            new Coord {
                X = Field[0].X + Field[1].X / 2 + 1,
                Y = Field[0].Y + Field[1].Y / 2 + 1}
        };
        public static Orientation Direction = Orientation.Up;
        public static List<Coord> Apples = new List<Coord>();
        public static object Locker = new object();
        public static bool Playing = true;
        public const string StringGameOver = "GAME OVER!";
        public static int Score;
        public static int Lifes = 3;
        public static Random Rand = new Random(DateTime.Now.Millisecond);
        public static DateTime ZeroTime;
        public static bool Pause;

        public static string Center(string s, int width, char fill = ' ')
        {
            return s.PadLeft((width + s.Length) / 2, fill).PadRight(width, fill);
        }

        public static List<Coord> GetEmpty()
        {
            var map = new List<Coord>();
            for (int y = Field[0].Y + 1; y < Field[1].Y + Field[0].Y + 1; ++y)
                for (int x = Field[0].X + 1; x < Field[1].X + Field[0].X + 1; ++x)
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

        public static void Revive()
        {
            Pause = true;
            Thread.Sleep(500);
            Snake.Clear();
            Snake.Add(new Coord
            {
                X = Field[0].X + Field[1].X / 2 + 1,
                Y = Field[0].Y + Field[1].Y / 2 + 1
            });
        }

        public static void Generate()
        {
            var gen = Apples.Count > 3 ? 0 : 3 - Apples.Count;
            var map = GetEmpty();
            while (gen > 0)
            {
                var index = Rand.Next() % map.Count;
                Apples.Add(map[index]);
                map.RemoveAt(index);
                --gen;
            }
        }

        public static void DrawStatus()
        {
            var status = $" Lifes: {Lifes} | Score: {Score} ";
            Console.SetCursorPosition(0, 0);
            var fore = Console.ForegroundColor;
            var back = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Center(status, Console.WindowWidth, '='));
            Console.ForegroundColor = fore;
            Console.BackgroundColor = back;
        }

        public static void DrawField()
        {
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
                Console.Write(((apple.X ^ apple.Y) & 0x01) == 1 ? "9" : "6");
            }
            Console.ForegroundColor = fore;
        }

        public static void DrawSnake()
        {
            var head = Snake[Snake.Count - 1];
            var newXY = Move(head);
            if (newXY.X == Field[0].X || newXY.X == Field[0].X + Field[1].X + 1
                || newXY.Y == Field[0].Y || newXY.Y == Field[0].Y + Field[1].Y + 1)
            {
                SubstractLife();
                return;
            }
            var fore = Console.ForegroundColor;
            if (Playing)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                if (!Apples.Remove(newXY))
                    Snake.RemoveAt(0);
                else
                    ++Score;
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

        public static void SubstractLife()
        {
            --Lifes;
            if (Lifes > 0)
                Revive();
            else
            {
                var GamingTime = DateTime.Now - ZeroTime;
                Console.BackgroundColor = ConsoleColor.DarkMagenta;
                Console.Clear();
                Playing = false;
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(0, Console.WindowHeight / 2 - 1);
                Console.Write(Center(StringGameOver, Console.WindowWidth));
                Console.Write(Center($"Total score: {Score} | Gaming time: " + 
                    (GamingTime.TotalMinutes >= 1.0 ? $"{(int)GamingTime.TotalMinutes} min " : "") + 
                    $"{GamingTime.Seconds} s {GamingTime.Milliseconds} ms", Console.WindowWidth));
            }
        }

        public static void HandleEvents()
        {
            ConsoleKeyInfo keyInfo;
            do
            {
                lock (Locker)
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

        public static void Draw()
        {
            Console.Clear();
            DrawField();
            DrawStatus();
            DrawSnake();
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
            ZeroTime = DateTime.Now;
            while (thread.IsAlive && Playing)
            {
                var delta = DateTime.Now - time;
                if (delta.TotalMilliseconds > DeltaTime)
                {
                    time = time.AddMilliseconds(DeltaTime);
                    Generate();
                    Draw();
                }
                if (Pause)
                {
                    Pause = false;
                    time = DateTime.Now;
                }
            }
        }
    }
}
