using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace PixelDrawer
{
    public sealed class PixelDrawer
    {
        public float ZBufferThreshold { get; set; } = 1.5f;

        private readonly Texture2D _texture2d;
        private Color[] _pixelsData;
        private float[] _zBuffer;

        public PixelDrawer(Texture2D texture)
        {
            _texture2d = texture;
            _pixelsData = new Color[_texture2d.Width * _texture2d.Height];
            _zBuffer = new float[_texture2d.Width * _texture2d.Height];
        }

        public void FilledTriangle(Vector3 point0, Vector3 point1, Vector3 point2, 
            Color color, CullingMode cullingMode = CullingMode.Back)
        {
            // Culling
            if (cullingMode != CullingMode.None)
            {
                var deltaA = point1 - point0;
                var deltaB = point2 - point0;

                var crossProduct = deltaA.X * deltaB.Y - deltaA.Y * deltaB.X;
                if ((cullingMode == CullingMode.Back && crossProduct < 0)
                    || (cullingMode == CullingMode.Front && crossProduct > 0))
                    return;
            }

            if (point0.Y > point1.Y) (point0, point1) = (point1, point0);
            if (point1.Y > point2.Y) (point1, point2) = (point2, point1);
            if (point0.Y > point1.Y) (point0, point1) = (point1, point0);

            var totalHeight = point2.Y - point0.Y;
            var topSegmentHeight = point1.Y - point0.Y;
            var bottomSegmentHeight = point2.Y - point1.Y;

            var step02 = (point2 - point0) * 1f / totalHeight;
            var step01 = (point1 - point0) * 1f / topSegmentHeight;
            var step12 = (point2 - point1) * 1f / bottomSegmentHeight;

            Vector3 x02 = point0, x01 = point0, x12 = point1;
            for (var y = point0.Y; y <= point2.Y; y++)
            {
                var start = x02;
                if (y <= point1.Y)
                {
                    var end = x01;
                    if (start.X > end.X) (start, end) = (end, start);

                    var z = start.Z;
                    var stepZ = (end.Z - start.Z) / (end.X - start.X);

                    for (int x = (int)start.X; x <= end.X; x++)
                    {
                        SetPixel(x, y, z, color);
                        z += stepZ;
                    }
                    x01 += step01;
                }
                else
                {
                    var end = x12;
                    if (start.X > end.X) (start, end) = (end, start);

                    var z = start.Z;
                    var stepZ = (end.Z - start.Z) / (end.X - start.X);

                    for (int x = (int)start.X; x <= end.X; x++)
                    {
                        SetPixel(x, y, z, color);
                        z += stepZ;
                    }
                    x12 += step12;
                }
                x02 += step02;
            }
        }

        public void Triangle(Vector3 point1, Vector3 point2, Vector3 point3, in Color color)
            => Triangle(point1.X, point1.Y, point2.X, point2.Y, point3.X, point3.Y, color);
        public void Triangle(Vector2 point1, Vector2 point2, Vector2 point3, in Color color)
            => Triangle(point1.X, point1.Y, point2.X, point2.Y, point3.X, point3.Y, color);
        public void Triangle(float x0, float y0,
            float x1, float y1,
            float x2, float y2,
            in Color color)
        {
            Line(x0, y0, x1, y1, color);
            Line(x1, y1, x2, y2, color);
            Line(x2, y2, x0, y0, color);
        }

        public void Line(Vector2 from, Vector2 to, in Color color)
            => Line(from.X, from.Y, to.X, to.Y, color);
        public void Line(float x0, float y0, float x1, float y1, in Color color)
        {
            var deltaX = x1 - x0;
            var deltaY = y1 - y0;

            var step = 1f / MathF.Max(Math.Abs(deltaX), MathF.Abs(deltaY));
            var stepX = deltaX * step;
            var stepY = deltaY * step;

            for (float i = 0; i <= 1f; i += step)
            {
                SetPixel(x0, y0, 0, color);

                x0 += stepX;
                y0 += stepY;
            }
        }

        public void Fill(in Color color)
        {
            for (int i = 0; i < _pixelsData.Length; i++)
                _pixelsData[i] = color;
        }

        public void ClearZBuffer()
        {
            for (int i = 0; i < _zBuffer.Length; i++)
                _zBuffer[i] = float.NegativeInfinity;
        }

        public void ApplyPixels()
        {
            _texture2d.SetData(_pixelsData);
        }

        private void SetPixel(float x, float y, float z, in Color color)
        {
            if (x >= 0 && x < _texture2d.Width
                && y >= 0 && y < _texture2d.Height)
            {
                var index = (int)y * _texture2d.Width + (int)x;
                if (_zBuffer[index] - z < ZBufferThreshold)
                {
                    _pixelsData[index] = color;
                    _zBuffer[index] = z;
                }
            }
        }
    }

    public enum CullingMode
    {
        None,
        Front,
        Back
    }
}
