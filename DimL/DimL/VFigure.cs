using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace DimL
{
    public class Polygon : List<Vector<double>>
    { }

    public class VFigure
    {
        string path = "";

        public int Dimension { get; set; } = 0;
        public List<Polygon> Polygons = new List<Polygon>();

        public VFigure() { }
        public VFigure(string pathToFile)
        {
            path = pathToFile;
        }

        public bool Load(string pathToFile = "")
        {
            if (pathToFile.Length > 0)
                path = pathToFile;
            else if (path.Length == 0)
                return false;
            try
            {
                StreamReader stream = new StreamReader(path);
                string data = stream.ReadToEnd();
                stream.Close();
                var source = JObject.Parse(data);
                int newDim = (int)source["Dimension"];
                if (newDim < 3)
                    throw new Exception($"The value of dimension ({newDim}) must be 3, 4 or geater!");
                if (Dimension > 0 && newDim != Dimension)
                    throw new Exception($"New dimension ({newDim}) doesn't equal to previous ({Dimension})!");
                Dimension = newDim;
                foreach (JToken polygon in source["Polygons"])
                {
                    var poly = new Polygon();
                    foreach (JToken vertex in polygon)
                    {
                        var vert = Vector<double>.Build.DenseOfEnumerable(vertex.ToObject<double[]>());
                        if (vert.Count != Dimension)
                            throw new Exception($"The vertex ({vertex.ToObject<string[]>().Aggregate((w, u) => w + ", " + u)}) has size that doesn't equal to {Dimension}");
                        poly.Add(vert);
                    }
                    Polygons.Add(poly);
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Doesn't found file at \"{path}\"!");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }
}
