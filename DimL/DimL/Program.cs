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
            Engine engine = new Engine();
            engine.LoadFigure("Cube3.json");
            engine.Run();
        }
    }
}
