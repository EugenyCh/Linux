using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
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
        private readonly double speed = Math.PI / 2;
        private readonly List<int[]> planes = new List<int[]>();
        private int activePlane = 0;
        private readonly float[] mat_ambient = { 0.2f, 0.4f, 0.6f, 1.0f };
        private readonly float[] mat_diffuse = { 0.5f, 0.8f, 1.0f, 1.0f };
        private Vector3 lookFrom = new Vector3(5.0f, 5.0f, 5.0f);
        private Font font = new Font("Inconsolata", 14, FontStyle.Bold);
        private readonly Size labelSize = new Size(384, 640);

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
            window.UpdateFrame += Update;
            window.Resize += Resize;
            window.Load += Load;
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
            GL.Light(LightName.Light0, LightParameter.Position, new float[] { -5f, 5f, 0f, 1f });
            GL.Material(MaterialFace.Front, MaterialParameter.Ambient, mat_ambient);
            GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, mat_diffuse);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
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
            window.TargetRenderFrequency = 60.0;
            window.Run();
        }

        private int PlaneComparator(int[] x, int[] y)
        {
            return (Math.Pow(2, x[0]) + Math.Pow(2, x[1])).CompareTo(Math.Pow(2, y[0]) + Math.Pow(2, y[1]));
        }

        private void Close()
        {
            window.Close();
            window.ProcessEvents();
            window.Dispose();
        }

        private void KeyDown(object sender, KeyboardKeyEventArgs ev)
        {

            if (ev.Key == Key.BracketRight)
                activePlane = (activePlane + 1) % NumberOfPlanes;
            if (ev.Key == Key.BracketLeft)
                activePlane = (NumberOfPlanes + activePlane - 1) % NumberOfPlanes;
            if (ev.Control)
                direction = -1.0;
            else
                direction = 1.0;
            if (ev.Shift)
                step = 2.0;
            else
                step = 1.0;
            if (ev.Key == Key.Space)
                velocity = 1.0;
            if (ev.Key == Key.Escape)
                Close();
            //if (ev.Code == Keyboard.Key.F5)
            //    ScreenShot();
        }

        private void KeyUp(object sender, KeyboardKeyEventArgs ev)
        {
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
            var matrix = MakePlaneRotationMatrix(deltaAngle, planes[activePlane][0], planes[activePlane][1]);
            int size = Figure.Polygons.Count;
            for (int c = 0; c < size; ++c)
            {
                var polygon = Figure.Polygons[c];
                for (int a = 0; a < polygon.Count; ++a)
                    polygon[a] *= matrix;
            }
        }

        private void Update(object sender, EventArgs ev)
        {
            double deltaAngle = velocity * speed * step * direction * window.UpdateTime;
            angles[activePlane] += deltaAngle;
            for (int i = 0; i < angles.Count; ++i)
            {
                if (angles[i] < 0)
                    angles[i] += 2.0 * Math.PI;
                if (angles[i] >= 2.0 * Math.PI)
                    angles[i] -= 2.0 * Math.PI;
            }
            Rotate(deltaAngle);
        }

        private Bitmap GetLabelImage()
        {
            var bmp = new Bitmap(labelSize.Width, labelSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var graphics = Graphics.FromImage(bmp);
            var label =
                $"Target Render Frequency:{string.Format("{0,6:0.0}", window.TargetRenderFrequency)} Hz\n" +
                $"Real Render Frequency:  {string.Format("{0,6:0.0}", window.RenderFrequency)} Hz\n" +
                $"Render Delta:           {string.Format("{0,6:0.0}", window.RenderTime * 1000000)} \u00B5s\n" +
                $"Update Delta:           {string.Format("{0,6:0.0}", window.UpdateTime * 1000000)} \u00B5s\n";
            for (int i = 0; i < NumberOfPlanes; ++i)
            {
                var str = $"Angle (X{planes[i][0] + 1}, X{planes[i][1] + 1})";
                if (activePlane == i)
                    str = "[" + str + "]";
                else
                    str = " " + str + " ";
                label += str.PadLeft(23) + ":" + string.Format("{0,6:0.0}", angles[i] * 180 / Math.PI) + "\n";
            }
            graphics.DrawString(label, font, Brushes.GreenYellow, new PointF(0, 0));
            graphics.Flush();
            return bmp;
        }

        private int LoadTexture(Bitmap bmp)
        {
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height,
                0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            return id;
        }

        private void Render(object sender, EventArgs ev)
        {
            // CUBE
            GL.Enable(EnableCap.Lighting);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
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

            // LABEL
            GL.Disable(EnableCap.Lighting);
            GL.Enable(EnableCap.Texture2D);
            var texture = LoadTexture(GetLabelImage());
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(0, window.Width, 0, window.Height, -100, 100);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();
            //GL.Color4(1.0, 0.5, 0.5, 1.0);
            GL.Begin(PrimitiveType.Polygon);
            GL.TexCoord2(0, 1);
            GL.Vertex2(0, window.Height - labelSize.Height);
            GL.TexCoord2(1, 1);
            GL.Vertex2(labelSize.Width, window.Height - labelSize.Height);
            GL.TexCoord2(1, 0);
            GL.Vertex2(labelSize.Width, window.Height);
            GL.TexCoord2(0, 0);
            GL.Vertex2(0, window.Height);
            GL.End();
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.DeleteTexture(texture);
            GL.Disable(EnableCap.Texture2D);
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
                lookFrom.X, lookFrom.Y, lookFrom.Z,
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
                lookFrom.X, lookFrom.Y, lookFrom.Z,
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