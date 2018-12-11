using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace DimL
{
    public class Engine
    {
        private readonly GameWindow window;
        private Vector<double> angles;
        private double velocity;
        private double direction = 1.0;
        private double step = 1.0;
        private readonly double speed = Math.PI / 3;
        private readonly List<int[]> planes = new List<int[]>();
        private int activePlane = 0;
        private readonly float[] mat_ambient = { 0.2f, 0.4f, 0.6f };
        private readonly float[] mat_diffuse = { 0.5f, 0.8f, 1.0f };

        public VFigure Figure { get; set; } = new VFigure();
        public int Dimension => Figure.Dimension;
        public int NumberOfPlanes => (Dimension * (Dimension - 1)) >> 1;

        public Engine()
        {
            GraphicsMode mode = new GraphicsMode(
                new ColorFormat(8, 8, 8, 8),
                24,
                8,
                4);
            window = new GameWindow(DisplayDevice.Default.Width, DisplayDevice.Default.Height, mode, "ND-Render", GameWindowFlags.Fullscreen);
            window.VSync = VSyncMode.On;
            window.RenderFrame += Render;
            window.Resize += Resize;
            window.Load += Load;
            window.Closed += Closed;
            window.KeyDown += KeyDown;
            window.KeyUp += KeyUp;
            float[] light_ambient = { 0.0f, 0.0f, 0.0f, 1.0f };
            float[] light_diffuse = { 1.0f, 1.0f, 1.0f, 1.0f };
            GL.Light(LightName.Light0, LightParameter.Ambient, light_ambient);
            GL.Light(LightName.Light0, LightParameter.Diffuse, light_diffuse);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.Enable(EnableCap.Light0);
            GL.Enable(EnableCap.Normalize);
            GL.ShadeModel(ShadingModel.Smooth);
            GL.Material(MaterialFace.Front, MaterialParameter.Ambient, mat_ambient);
            GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, mat_diffuse);
            GL.Light(LightName.Light0, LightParameter.Position, new float[] { -5f, 5f, 0f, 1f });
        }

        public bool LoadFigure(string pathToJson)
        {
            bool r = Figure.Load(pathToJson);
            if (r)
            {
                angles = Vector<double>.Build.Dense(NumberOfPlanes, 0.0);
                MakePlanes();
                return true;
            }
            return false;
        }

        public void Run()
        {
            window.Run(1.0 / 60.0);
        }

        private void Closed(object sender, EventArgs ev)
        {
            window.Close();
        }

        private int PlaneComparator(int[] x, int[] y)
        {
            return (Math.Pow(2, x[0]) + Math.Pow(2, x[1])).CompareTo(Math.Pow(2, y[0]) + Math.Pow(2, y[1]));
        }

        private void KeyDown(object sender, KeyboardKeyEventArgs ev)
        {
            if (ev.Key.ToString() == "]")
                activePlane = (activePlane + 1) % NumberOfPlanes;
            if (ev.Key.ToString() == "[")
                activePlane = (NumberOfPlanes + activePlane - 1) % NumberOfPlanes;
            if (ev.Alt)
                direction = -1.0;
            if (ev.Shift)
                step = 2.0;
            if (ev.Key == Key.Space)
                velocity = 1.0;
            if (ev.Key == Key.Escape)
                window.Close();
            //if (ev.Code == Keyboard.Key.F5)
            //    ScreenShot();
        }

        private void KeyUp(object sender, KeyboardKeyEventArgs ev)
        {
            if (ev.Alt)
                direction = 1.0;
            if (ev.Shift)
                step = 1.0;
            if (ev.Key == Key.Space)
                velocity = 0.0;
        }

        private Matrix<double> MakePlaneRotationMatrix(double angle, int xa, int xb)
        {
            if (Math.Min(xa, xb) < 0 || Math.Max(xa, xb) > Dimension || Dimension < 2)
                return Matrix<double>.Build.Dense(1, 1, Math.Cos(angle));
            var matrix = Matrix<double>.Build.DenseDiagonal(Dimension, Dimension, 1.0);
            matrix[xa, xa] = Math.Cos(angle);
            matrix[xb, xb] = matrix[xa, xa];
            matrix[xa, xb] = ((xa < xb) ? -1.0 : 1.0) * Math.Sin(angle);
            matrix[xb, xa] = -matrix[xa, xb];
            return matrix;
        }

        private void MakePlanes()
        {
            for (int k1 = 0; k1 < Dimension - 1; ++k1)
                for (int k2 = k1 + 1; k2 < Dimension; ++k2)
                {
                    if (((k1 + k2) & 1) == 1)
                        planes.Add(new int[] { k1, k2 });
                    else
                        planes.Add(new int[] { k2, k1 });
                }
            planes.Sort(PlaneComparator);
        }

        private int[] GetPlane(int index)
        {
            return planes[index];
        }

        private void Rotate(double deltaAngle)
        {
            var matrix = MakePlaneRotationMatrix(angles[activePlane], planes[activePlane][0], planes[activePlane][1]);
            int size = Figure.Polygons.Count;
            for (int c = 0; c < size; ++c)
            {
                var polygon = Figure.Polygons[c];
                for (int a = 0; a < polygon.Count; ++a)
                    polygon[a] *= matrix;
            }
        }

        private void Update()
        {
            double deltaAngle = velocity * speed * direction;
            angles[activePlane] += deltaAngle;
            for (int i = 0; i < angles.Count; ++i)
                if (Math.Abs(angles[i]) >= 2.0 * Math.PI)
                    angles[i] -= Math.Floor(angles[i] / (2.0 * Math.PI)) * (2.0 * Math.PI);
            Rotate(deltaAngle);
        }

        private void Render(object sender, EventArgs ev)
        {
            GL.Enable(EnableCap.Lighting);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            foreach (var polygon in Figure.Polygons)
            {
                GL.Begin(PrimitiveType.Polygon);
                foreach (var vertex in polygon)
                {
                    GL.Normal3(vertex.AsArray());
                    GL.Vertex3(vertex.AsArray());
                }
                GL.End();
            }
            GL.Disable(EnableCap.Lighting);
            GL.MatrixMode(MatrixMode.Projection);
            GL.Color3(1.0, 1.0, 1.0);
            GL.PointSize(32);
            GL.Begin(PrimitiveType.Points);
            GL.Vertex3(4, 4, 4);
            GL.End();
            window.SwapBuffers();
        }

        private void Resize(object sender, EventArgs ev)
        {
            GL.Viewport(0, 0, window.Width, window.Height);
            float aspect = (window.Height > 0) ? (float)window.Width / window.Height : 1.0f;
            Matrix4 perspectiveMatrix = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                aspect,
                .1f, 100f);
            Matrix4 lookAtMatrix = Matrix4.LookAt(
                5.0f, 5.0f, 5.0f,
                0, 0, 0,
                0, 1, 0);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.LoadMatrix(ref perspectiveMatrix);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.LoadMatrix(ref lookAtMatrix);
        }

        private void Load(object sender, EventArgs ev)
        {
            GL.Viewport(0, 0, window.Width, window.Height);
            float aspect = (window.Height > 0) ? (float)window.Width / window.Height : 1.0f;
            Matrix4 perspectiveMatrix = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                aspect,
                .1f, 100f);
            Matrix4 lookAtMatrix = Matrix4.LookAt(
                5.0f, 5.0f, 5.0f,
                0, 0, 0,
                0, 1, 0);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.LoadMatrix(ref perspectiveMatrix);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity(); 
            GL.LoadMatrix(ref lookAtMatrix);
        }
    }
}