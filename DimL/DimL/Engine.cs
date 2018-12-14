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
        private readonly float[] mat_emission = { 0.2f, 0.2f, 0.2f, 1.0f };
        private Vector3 lookFrom;
        private double lookAngleV;
        private double lookAngleH;
        private double lookMovingV;
        private double lookMovingH;
        private Font font = new Font("Inconsolata", 14, FontStyle.Bold);
        private readonly Size labelSize;
        private bool solid = true;
        private double polygonSize;

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
            labelSize = new Size(384, window.Height);
            float[] light_ambient = { 0.0f, 0.0f, 0.0f };
            float[] light_diffuse0 = { 1.0f, 0.0f, 0.0f };
            float[] light_diffuse1 = { 0.0f, 1.0f, 0.0f };
            float[] light_diffuse2 = { 0.0f, 0.0f, 1.0f };
            GL.Light(LightName.Light0, LightParameter.Ambient, light_ambient);
            GL.Light(LightName.Light0, LightParameter.Diffuse, light_diffuse0);
            GL.Light(LightName.Light1, LightParameter.Ambient, light_ambient);
            GL.Light(LightName.Light1, LightParameter.Diffuse, light_diffuse1);
            GL.Light(LightName.Light2, LightParameter.Ambient, light_ambient);
            GL.Light(LightName.Light2, LightParameter.Diffuse, light_diffuse2);
            GL.Enable(EnableCap.Light0);
            GL.Enable(EnableCap.Light1);
            GL.Enable(EnableCap.Light2);
            GL.Enable(EnableCap.Normalize);
            GL.ShadeModel(ShadingModel.Smooth);
            GL.Light(LightName.Light0, LightParameter.Position, new float[] { -5f, 5f, -5f, 0f });
            GL.Light(LightName.Light1, LightParameter.Position, new float[] { -5f, -5f, 5f, 0f });
            GL.Light(LightName.Light2, LightParameter.Position, new float[] { 5f, -5f, -5f, 0f });
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.LineWidth(2.0f);
        }

        public bool LoadFigure(string pathToJson)
        {
            bool r = Figure.Load(pathToJson);
            if (r)
            {
                foreach (var pol in Figure.Polygons)
                    polygonSize += pol.Count;
                polygonSize /= Figure.Polygons.Count;
                angles = Vector<double>.Build.Dense(NumberOfPlanes, 0.0);
                MakePlanes();
                mat_ambient[3] = (float)Math.Pow(polygonSize / 2, 2 - Dimension);
                mat_diffuse[3] = mat_ambient[3];
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, mat_ambient);
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, mat_diffuse);
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
            if (ev.Key == Key.Enter)
                solid = !solid;

            if (ev.Key == Key.Up)
                lookMovingV = 1.0;
            if (ev.Key == Key.Down)
                lookMovingV = -1.0;

            if (ev.Key == Key.Right)
                lookMovingH = 1.0;
            if (ev.Key == Key.Left)
                lookMovingH = -1.0;

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
            if (ev.Key == Key.S && ev.Control)
                ScreenShot();
        }

        private void ScreenShot()
        {
            var pixels = new byte[3 * window.Width * window.Height];
            GL.ReadPixels(0, 0, window.Width, window.Height, OpenTK.Graphics.OpenGL.PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
            var time = DateTime.Now;
            var name = string.Format("{0:0000}-{1:00}-{2:00} {3:00}-{4:00}-{5:00}.{6:000}.png", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
            var bmp = new Bitmap(window.Width, window.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            int i = 0;
            for (int y = window.Height - 1; y >= 0; --y)
                for (int x = 0; x < window.Width; ++x)
                {
                    bmp.SetPixel(x, y, Color.FromArgb(0, pixels[i + 2], pixels[i + 1], pixels[i]));
                    i += 3;
                }
            bmp.Save(name);
            bmp.Dispose();
        }

        private void KeyUp(object sender, KeyboardKeyEventArgs ev)
        {
            if (ev.Key == Key.Space)
                velocity = 0.0;
            if (ev.Key == Key.Up || ev.Key == Key.Down)
                lookMovingV = 0;
            if (ev.Key == Key.Left || ev.Key == Key.Right)
                lookMovingH = 0;
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
            // Figure
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

            // Camera
            lookAngleV += lookMovingV * speed * window.UpdateTime;
            if (lookAngleV < 0)
                lookAngleV += 2.0 * Math.PI;
            if (lookAngleV >= 2.0 * Math.PI)
                lookAngleV -= 2.0 * Math.PI;
            lookAngleH += lookMovingH * speed * window.UpdateTime;
            if (lookAngleH < 0)
                lookAngleH += 2.0 * Math.PI;
            if (lookAngleH >= 2.0 * Math.PI)
                lookAngleH -= 2.0 * Math.PI;
            lookFrom.Z = (float)(Math.Cos(lookAngleH) + Math.Sin(lookAngleH)) * (float)Math.Cos(lookAngleV);
            lookFrom.X = (float)(Math.Cos(lookAngleH) - Math.Sin(lookAngleH)) * (float)Math.Cos(lookAngleV);
            lookFrom.Y = (float)Math.Sqrt(2) * (float)Math.Sin(lookAngleV);
            lookFrom *= 5.0f;
            Matrix4 lookAtMatrix = Matrix4.LookAt(
                lookFrom.X, lookFrom.Y, lookFrom.Z,
                0, 0, 0,
                0, 1, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.LoadMatrix(ref lookAtMatrix);
        }

        private Bitmap GetLabelImage()
        {
            var bmp = new Bitmap(labelSize.Width, labelSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var graphics = Graphics.FromImage(bmp);
            var label =
                $"Target Render Frequency:{string.Format("{0,6:0.0}", window.TargetRenderFrequency)} Hz\n" +
                $"Real Render Frequency:  {string.Format("{0,6:0.0}", window.RenderFrequency)} Hz\n" +
                $"Render Delta:           {string.Format("{0,6:0.0}", window.RenderTime * 1000000)} \u00B5s\n" +
                $"Update Delta:           {string.Format("{0,6:0.0}", window.UpdateTime * 1000000)} \u00B5s\n" +
                $"Vertical Angle:         {string.Format("{0,6:0.0}", lookAngleV * 180 / Math.PI)}\n" +
                $"Horizontal Agnle:       {string.Format("{0,6:0.0}", lookAngleH * 180 / Math.PI)}\n" +
                (solid ? "Solid model" : "Wireframe model") + "\n";
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
            // Cube
            GL.Enable(EnableCap.Lighting);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            foreach (var polygon in Figure.Polygons)
            {
                if (solid)
                {
                    GL.Begin(PrimitiveType.Polygon);
                    foreach (var vertex in polygon)
                    {
                        var subvector = vertex.SubVector(0, 3).AsArray();
                        GL.Normal3(subvector);
                        GL.Vertex3(subvector);
                    }
                    GL.End();
                }
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, mat_emission);
                GL.Begin(PrimitiveType.LineLoop);
                foreach (var vertex in polygon)
                    GL.Vertex3(vertex.SubVector(0, 3).AsArray());
                GL.End();
                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, Color.Black);
            }

            // Label
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
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.LoadMatrix(ref perspectiveMatrix);
        }

        private void Load(object sender, EventArgs ev)
        {
            GL.Viewport(0, 0, window.Width, window.Height);
            float aspect = (window.Height > 0) ? (float)window.Width / window.Height : 1.0f;
            Matrix4 perspectiveMatrix = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                aspect,
                .1f, 100f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.LoadMatrix(ref perspectiveMatrix);
        }
    }
}