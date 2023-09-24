using System;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace PixelDrawer
{
    public class Cloth
    {
        private readonly VerletSolver _verletSolver;
        private readonly Vector3 _gameHalfSize;

        private VerletBody[] _bodies;
        private Vector3[] _positions;
        private Vector2[] _uvs;
        private int[] _indices;

        private readonly int _width;
        private readonly int _height;

        private readonly Vector3 _lightDirection;

        private readonly int _renderTasksCount = 8;
        private readonly Task[] _renderTasks;

        public Cloth(VerletSolver verletSolver,
            Rectangle gameSize,
            int width = 50, int height = 20,
            float segmentWidth = 20, float segmentHeight = 20)
        {
            _verletSolver = verletSolver;
            _gameHalfSize = new Vector3(gameSize.Width / 2f, gameSize.Height / 2f, 1f);

            _width = width;
            _height = height;

            _lightDirection = new Vector3(0, 0, 1);

            GenerateCloth(_width, _height, segmentWidth, segmentHeight);

            _renderTasks = new Task[_renderTasksCount];
        }

        public void Draw(PixelDrawer drawer, Matrix viewMatrix)
        {
            for (int i = 0; i < _bodies.Length; i++)
            {
                var v = Vector4.Transform(new Vector4(_bodies[i].CurrentPosition, 1f), viewMatrix);
                _positions[i] = new Vector3(v.X, v.Y, v.Z) / v.W;
                _positions[i].Z = v.Z;
                _positions[i] *= _gameHalfSize;
                _positions[i] += _gameHalfSize;
            }

            for (int t = 0; t < _renderTasksCount; t++)
            {
                var taskId = t;
                _renderTasks[taskId] = new Task(() =>
                {
                    for (int i = 0; i < _indices.Length; i += 6)
                    {
                        if (i % _renderTasksCount != taskId)
                            continue;

                        var v1 = _positions[_indices[i]];
                        var v2 = _positions[_indices[i + 2]];
                        var v3 = _positions[_indices[i + 4]];

                        var uv1 = _uvs[_indices[i + 1]];
                        var uv2 = _uvs[_indices[i + 3]];
                        var uv3 = _uvs[_indices[i + 5]];

                        var shadowColor = Color.White;
                        var normal = Vector3.Cross(v2 - v1, v3 - v1);
                        normal.Normalize();
                        var lightness = shadowColor * Math.Clamp(Vector3.Dot(normal, _lightDirection), 0.35f, 1f);
                        lightness.A = 255;

                        drawer.FilledTriangle(v1, v2, v3, uv1, uv2, uv3, lightness, CullingMode.None);
                    }
                });

                _renderTasks[taskId].Start();
            }

            Task.WaitAll(_renderTasks);
        }

        private void GenerateCloth(int width, int height,
            float segmentWidth, float segmentHeight)
        {
            _bodies = new VerletBody[width * height];
            _positions = new Vector3[_bodies.Length];
            _uvs = new Vector2[_bodies.Length];

            var trianglesCount = (width - 1) * (height - 1) * 2;
            _indices = new int[trianglesCount * 6];

            var clothSize = new Vector3()
            {
                X = width * segmentWidth,
                Y = height * segmentHeight
            };
            var startPosition = -clothSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var isStatic = y == 0;
                    var position = startPosition + new Vector3()
                    {
                        X = x * segmentWidth,
                        Y = y * segmentHeight
                    };

                    var body = new VerletBody()
                    {
                        IsStatic = isStatic,
                        CurrentPosition = position,
                        LastPosition = position,
                    };

                    var bodyIndex = y * _width + x;
                    _bodies[bodyIndex] = body;
                    _verletSolver.AddBody(body);

                    _uvs[bodyIndex] = new Vector2((float)x / width, 1f - (float)y / height);
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var body = _bodies[y * width + x];

                    if (y < height - 1)
                    {
                        var otherBody = _verletSolver.Bodies[(y + 1) * width + x];
                        _verletSolver.AddLineConstraint(new LineConstraint()
                        {
                            Length = segmentHeight,
                            Body1 = body,
                            Body2 = otherBody
                        });
                    }

                    if (x < width - 1)
                    {
                        var otherBody = _bodies[y * width + x + 1];
                        _verletSolver.AddLineConstraint(new LineConstraint()
                        {
                            Length = segmentWidth,
                            Body1 = body,
                            Body2 = otherBody
                        });
                    }
                }
            }

            var index = 0;
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var leftTop = y * width + x;
                    var rightTop = leftTop + 1;
                    var leftBottom = (y + 1) * width + x;
                    var rightBottom = leftBottom + 1;

                    _indices[index] = _indices[index + 1] = leftTop;
                    _indices[index + 2] = _indices[index + 3] = rightTop;
                    _indices[index + 4] = _indices[index + 5] = leftBottom;
                    index += 6;

                    _indices[index] = _indices[index + 1] = rightTop;
                    _indices[index + 2] = _indices[index + 3] = rightBottom;
                    _indices[index + 4] = _indices[index + 5] = leftBottom;
                    index += 6;
                }
            }
        }
    }
}
