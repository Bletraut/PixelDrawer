using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace PixelDrawer
{
    public class SimplePlain
    {
        private readonly Vector3[] _verticies;
        private readonly Vector3[] _positions;

        public SimplePlain(float scale = 10) 
        {
            _verticies = new Vector3[4];
            _positions = new Vector3[_verticies.Length];

            _verticies[0] = new Vector3(-1, -1, 0) * scale;
            _verticies[1] = new Vector3(1, -1, 0) * scale;
            _verticies[2] = new Vector3(1, 1, 0) * scale;
            _verticies[3] = new Vector3(-1, 1, 0) * scale;
        }

        public void Draw(PixelDrawer drawer, Matrix viewMatrix)
        {
            for (int i = 0; i < _verticies.Length; i++)
            {
                var v = _verticies[i];
                var vt = Vector4.Transform(new Vector4(v, 1f), viewMatrix);
                _positions[i] = new Vector3(vt.X, vt.Y, vt.Z) / vt.W;

                _positions[i] *= new Vector3(320, 240, 0);
                _positions[i] += new Vector3(320, 240, 0);
            }

            drawer.Line(_positions[0], _positions[1], Color.Red);
            drawer.Line(_positions[1], _positions[3], Color.Red);
            drawer.Line(_positions[3], _positions[0], Color.Red);

            drawer.Line(_positions[1], _positions[2], Color.Blue);
            drawer.Line(_positions[2], _positions[3], Color.Blue);
            drawer.Line(_positions[3], _positions[1], Color.Blue);
        }
    }
}
