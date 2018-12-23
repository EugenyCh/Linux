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

    class MainClass
    {
        public static double DeltaTime = 1000.0 / 60.0;
        public static Coord[] Field = { new Coord { X = 0, Y = 0 }, new Coord { X = 40, Y = 15 } };
        public static List<Coord> Snake = new List<Coord>();
        public static object locker = new object();

        public static void DrawField()
        {
            Console.SetCursorPosition(Field[0].X, Field[0].Y);
            Console.Write("+" + new string('-', Field[1].X) + "+");
            Console.SetCursorPosition(Field[0].X, Field[1].Y + 1);
            Console.Write("+" + new string('-', Field[1].X) + "+");
            Console.SetCursorPosition(Field[0].X, Field[0].Y + 1);
            for (int line = 0; line < Field[1].Y; ++line)
                Console.WriteLine("|".PadRight(Field[1].X + 1) + "|");
        }

        public static void HandleEvents()
        {
            ConsoleKeyInfo keyInfo;
            do
            {
                lock (locker)
                {
                    keyInfo = Console.ReadKey(true);
                }
            } while (keyInfo.Key != ConsoleKey.Escape);
        }

        public static void Main(string[] args)
        {
            Console.CursorVisible = false;
            var time = DateTime.Now;
            var thread = new Thread(HandleEvents);
            thread.Start();
            while (thread.IsAlive)
            {
                var delta = DateTime.Now - time;
                if (delta.TotalMilliseconds > DeltaTime)
                {
                    time = time.AddMilliseconds(DeltaTime);
                    DrawField();
                }
            }
        }
    }
}
