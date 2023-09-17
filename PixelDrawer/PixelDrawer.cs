using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace PixelDrawer
{
    public sealed class PixelDrawer
    {
        public float ZBufferThreshold { get; set; } = 1.5f;

        private readonly Texture2D _texture2d;
        private readonly Texture2D _materialTexture2d;
        private Color[] _pixelsData;
        private Color[] _materialData;
        private float[] _zBuffer;

        public PixelDrawer(Texture2D texture, Texture2D materialTexture2d)
        {
            _texture2d = texture;
            _materialTexture2d = materialTexture2d;

            _materialData = new Color[_materialTexture2d.Width * _materialTexture2d.Height];
            _materialTexture2d.GetData(_materialData);

            _pixelsData = new Color[_texture2d.Width * _texture2d.Height];
            _zBuffer = new float[_texture2d.Width * _texture2d.Height];
        }

        public void FilledTriangle(Vector3 point0, Vector3 point1, Vector3 point2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2,
            in Color color, CullingMode cullingMode = CullingMode.Back)
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

            if (point0.Y > point1.Y)
            {
                (point0, point1) = (point1, point0);
                (uv0, uv1) = (uv1, uv0);
            }
            if (point1.Y > point2.Y)
            {
                (point1, point2) = (point2, point1);
                (uv1, uv2) = (uv2, uv1);
            }
            if (point0.Y > point1.Y)
            {
                (point0, point1) = (point1, point0);
                (uv0, uv1) = (uv1, uv0);
            }

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
                        var uv = RemapUv(x, y, point0, point1, point2,
                            uv0, uv1, uv2);
                        SetPixel(x, y, z, uv.X, uv.Y, color);
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
                        var uv = RemapUv(x, y, point0, point1, point2,
                            uv0, uv1, uv2);
                        SetPixel(x, y, z, uv.X, uv.Y, color);
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
                SetPixel(x0, y0, color);

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

        private Vector2 RemapUv(float x, float y, Vector3 point0, Vector3 point1, Vector3 point2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            var pixelPosition = new Vector3(x, y, 0);

            var s = TriangleSquare(point0, point1, point2);
            var a = TriangleSquare(pixelPosition, point1, point2);
            var b = TriangleSquare(point0, pixelPosition, point2);
            var c = TriangleSquare(point0, point1, pixelPosition);

            var alpha = a / s;
            var beta = b / s;
            var gamma = c / s;

            return new Vector2()
            {
                X = alpha * uv0.X + beta * uv1.X + gamma * uv2.X,
                Y = alpha * uv0.Y + beta * uv1.Y + gamma * uv2.Y
            };
        }

        private float TriangleSquare(Vector3 a, Vector3 b, Vector3 c)
        {
            return Math.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y)) * 0.5f;
        }

        private void SetPixel(float x, float y, float z,
            float u, float v, in Color color)
        {
            if (x >= 0 && x < _texture2d.Width
                && y >= 0 && y < _texture2d.Height)
            {
                var index = (int)y * _texture2d.Width + (int)x;
                if (_zBuffer[index] - z < ZBufferThreshold)
                {
                    _pixelsData[index] = SamplePixel(u, v);
                    _zBuffer[index] = z;
                }
            }
        }
        private void SetPixel(float x, float y, in Color color)
        {
            if (x >= 0 && x < _texture2d.Width
                && y >= 0 && y < _texture2d.Height)
            {
                var index = (int)y * _texture2d.Width + (int)x;
                _pixelsData[index] = color;
            }
        }

        private Color SamplePixel(float u, float v)
        {
            var x = u * (_materialTexture2d.Width - 1);
            var y = (1f - v) * (_materialTexture2d.Height - 1);

            var index = (int)y * _materialTexture2d.Width + (int)x;
            if (index < 0 || index >= _materialData.Length)
                return Color.Black;

            return _materialData[index];
        }
    }

    public enum CullingMode
    {
        None,
        Front,
        Back
    }
}
