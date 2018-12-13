using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace DimL
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
                return;
            Engine engine = new Engine();
            engine.LoadFigure(args[0]);
            engine.Run();
            return;
        }
    }
}
