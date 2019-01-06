using System.Windows.Media.Media3D;

namespace DameGUI.Data
{
    public class DameGeometry
    {
        private double depth = 1.0;
        private double height = 1.0;
        private Point3D origin = new Point3D();
        private double width = 1.0;

        ////////////////////////////////////////////////////////////////////////////////////////
        public double Depth { get { return depth; } set { depth = value; } }

        public double Height { get { return height; } set { height = value; } }
        public MeshGeometry3D Mesh3D { get { return GetMesh3D(); } }
        public Point3D Origin { get { return origin; } set { origin = value; } }
        public double Width { get { return width; } set { width = value; } }

        ////////////////////////////////////////////////////////////////////////////////////////
        private MeshGeometry3D GetMesh3D()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            Point3D pb = origin;
            Point3D pt = new Point3D(0, 0, height) + (Vector3D)origin;

            //-------------------------------------------------------------------------------------

            mesh.Positions.Add(new Point3D(-width, 0, height) + (Vector3D)origin);
            mesh.Positions.Add(new Point3D(0, -depth, height) + (Vector3D)origin);
            mesh.Positions.Add(new Point3D(width, 0, height) + (Vector3D)origin);
            mesh.Positions.Add(new Point3D(0, depth, height) + (Vector3D)origin);

            mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1));

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            //-------------------------------------------------------------------------------------

            mesh.Positions.Add(new Point3D(-width, 0, height) + (Vector3D)origin);
            mesh.Positions.Add(new Point3D(0, -depth, height) + (Vector3D)origin);
            mesh.Positions.Add(new Point3D(width, 0, height) + (Vector3D)origin);
            mesh.Positions.Add(new Point3D(0, depth, height) + (Vector3D)origin);

            mesh.Positions.Add(new Point3D(-width, 0, 0) + (Vector3D)origin);
            mesh.Positions.Add(new Point3D(0, -depth, 0) + (Vector3D)origin);
            mesh.Positions.Add(new Point3D(width, 0, 0) + (Vector3D)origin);
            mesh.Positions.Add(new Point3D(0, depth, 0) + (Vector3D)origin);

            mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(8); mesh.TriangleIndices.Add(9);
            mesh.TriangleIndices.Add(9); mesh.TriangleIndices.Add(5); mesh.TriangleIndices.Add(4);

            mesh.TriangleIndices.Add(5); mesh.TriangleIndices.Add(9); mesh.TriangleIndices.Add(10);
            mesh.TriangleIndices.Add(10); mesh.TriangleIndices.Add(6); mesh.TriangleIndices.Add(5);

            mesh.TriangleIndices.Add(6); mesh.TriangleIndices.Add(10); mesh.TriangleIndices.Add(11);
            mesh.TriangleIndices.Add(11); mesh.TriangleIndices.Add(7); mesh.TriangleIndices.Add(6);

            mesh.TriangleIndices.Add(7); mesh.TriangleIndices.Add(11); mesh.TriangleIndices.Add(8);
            mesh.TriangleIndices.Add(8); mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(7);

            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));
            mesh.Normals.Add(new Vector3D(0, 0, -1));

            //-------------------------------------------------------------------------------------

            mesh.Freeze();
            return mesh;
        }
    }
}