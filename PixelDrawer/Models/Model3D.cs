using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PixelDrawer
{
    public class Model3D
    {
        // Static.
        public static Model3D LoadPlaneModel()
        {
            var verticies = new Vector3[]
            {
                new Vector3(-100, -100, -100),
                new Vector3(100, -100, 0),
                new Vector3(100, 100, 0),
                new Vector3(-100, 100, 0),
            };

            var uvs = new Vector2[]
            {
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0),
                new Vector2(0, 0)
            };

            var indexes = new int[]
            {
                0,0,
                1,1,
                3,3,

                1,1,
                2,2,
                3,3,
            };

            return new Model3D(verticies, uvs, indexes);
        }

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

            var uvs = lines.Where(line => line.StartsWith("vt"))
                .Select(vertexInfo =>
                {
                    var values = vertexInfo.Split(" ");
                    var u = float.Parse(values[1], CultureInfo.InvariantCulture);
                    var v = float.Parse(values[2], CultureInfo.InvariantCulture);

                    return new Vector2(u, v);
                }).ToArray();

            var indexes = lines.Where(line => line.StartsWith("f"))
                .SelectMany(triangleInfo =>
                {
                    var indexes = triangleInfo.Trim().Split(" ");

                    var f1 = indexes[1].Split("/").Select(n => int.Parse(n) - 1).ToArray();
                    var f2 = indexes[2].Split("/").Select(n => int.Parse(n) - 1).ToArray();
                    var f3 = indexes[3].Split("/").Select(n => int.Parse(n) - 1).ToArray();
                    var triangles = new List<int>(6)
                    {
                        f1[0], f1[1],
                        f2[0], f2[1],
                        f3[0], f3[1],
                    };

                    // Quads to tris.
                    if (indexes.Length >= 5)
                    {
                        var f4 = indexes[4].Split("/").Select(n => int.Parse(n) - 1).ToArray();

                        triangles.Add(f1[0]);
                        triangles.Add(f1[1]);
                        triangles.Add(f3[0]);
                        triangles.Add(f3[1]);
                        triangles.Add(f4[0]);
                        triangles.Add(f4[1]);
                    }

                    return triangles;
                }).ToArray();

            return new Model3D(verticies, uvs, indexes);
        }

        // Class.
        public Vector3[] Verticies { get; private set; }
        public Vector2[] UVs { get; private set; }
        public int[] Indexes { get; private set; }

        public Model3D(Vector3[] verticies, Vector2[] uvs, int[] indexes)
        {
            Verticies = verticies;
            UVs = uvs;
            Indexes = indexes;
        }
    }
}
