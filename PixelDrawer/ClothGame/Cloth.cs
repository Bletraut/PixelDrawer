using Microsoft.Xna.Framework;

namespace PixelDrawer
{
    public class Cloth
    {
        private readonly VerletSolver _verletSolver;
        private VerletBody[] _bodies;
        private Vector3[] _positions;

        private int _width;
        private int _height;

        public Cloth(VerletSolver verletSolver,
            //int width = 100, int height = 50,
            int width = 50, int height = 20,
            float segmentWidth = 20, float segmentHeight = 20)
        {
            _width = width;
            _height = height;

            _verletSolver = verletSolver;
            GenerateCloth(_width, _height, segmentWidth, segmentHeight);
        }

        public void Draw(PixelDrawer drawer, Matrix viewMatrix)
        {
            for (int i = 0; i < _bodies.Length; i++)
                _positions[i] = Vector3.Transform(_bodies[i].CurrentPosition, viewMatrix);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var bodyPosition = _positions[y * _width + x];

                    if (y < _height - 1)
                    {
                        var otherBodyPosition = _positions[(y + 1) * _width + x];
                        drawer.Line(bodyPosition, otherBodyPosition, Color.White);
                    }

                    if (x < _width - 1)
                    {
                        var otherBodyPosition = _positions[y * _width + x + 1];
                        drawer.Line(bodyPosition, otherBodyPosition, Color.White);
                    }
                }
            }
        }

        private void GenerateCloth(int width, int height,
            float segmentWidth, float segmentHeight)
        {
            _bodies = new VerletBody[width * height];
            _positions = new Vector3[_bodies.Length];

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
                    _bodies[y * _width + x] = body;
                    _verletSolver.AddBody(body);
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
        }
    }
}
