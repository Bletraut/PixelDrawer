using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PixelDrawer
{
    public class Model3D
    {
        // Static.
        public static Model3D LoadModel(string filePath)
        {
            var file = File.ReadAllText(filePath);
            var lines = file.Split('\n').ToArray();

            var verticies = lines.Where(line => line.StartsWith("v "))
                .Select(vertexInfo =>
                {
                    var values = vertexInfo.Split(" ");
                    var x = float.Parse(values[1], CultureInfo.InvariantCulture);
                    var y = float.Parse(values[2], CultureInfo.InvariantCulture);
                    var z = float.Parse(values[3], CultureInfo.InvariantCulture);

                    return new Vector3(x, y, z);
                }).ToArray();

            var indexes = lines.Where(line => line.StartsWith("f"))
                .SelectMany(triangleInfo =>
                {
                    var indexes = triangleInfo.Trim().Split(" ")
                        .Skip(1).Select(value => int.Parse(value.Split(@"/").First()) - 1);
                    return indexes;
                }).ToArray();

            return new Model3D(verticies, indexes);
        }

        // Class.
        public Vector3[] Verticies { get; private set; }
        public int[] Indexes { get; private set; }

        public Model3D(Vector3[] verticies, int[] indexes)
        {
            Verticies = verticies;
            Indexes = indexes;
        }
    }
}
