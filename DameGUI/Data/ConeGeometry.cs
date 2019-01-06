using System;
using System.Windows.Media.Media3D;

namespace DameGUI.Data
{
    public class ConeGeometry
    {
        #region Fields

        private Point3D center = new Point3D();
        private double height = 1.0;
        private double rbottom = 1.0;
        private double rtop = 1.0;
        private int thetaDiv = 20;

        #endregion Fields

        ////////////////////////////////////////////////////////////////////////////////////////

        #region Properties

        public Point3D Center { get { return center; } set { center = value; } }
        public double Height { get { return height; } set { height = value; } }
        public MeshGeometry3D Mesh3D { get { return GetMesh3D(); } }
        public double Rbottom { get { return rbottom; } set { rbottom = value; } }
        public double Rtop { get { return rtop; } set { rtop = value; } }
        public int ThetaDiv { get { return thetaDiv; } set { thetaDiv = value; } }

        #endregion Properties

        ////////////////////////////////////////////////////////////////////////////////////////

        #region GetMesh3D

        private MeshGeometry3D GetMesh3D()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            if (ThetaDiv < 2) { return null; }

            double h = Height / 2;

            Point3D pt = new Point3D(0, 0, h) + (Vector3D)Center;
            Point3D pb = new Point3D(0, 0, -h) + (Vector3D)Center;

            Point3D[] pts = new Point3D[ThetaDiv];
            Point3D[] pbs = new Point3D[ThetaDiv];

            for (int i = 0; i < ThetaDiv; i++)
            {
                pts[i] = GetPosition(Rtop, i * 360 / (ThetaDiv - 1), h);
                pbs[i] = GetPosition(Rbottom, i * 360 / (ThetaDiv - 1), -h);
            }

            mesh.Positions.Add(pt);
            mesh.Normals.Add(new Vector3D(0, 0, 1));

            for (int i = 0; i < ThetaDiv - 1; i++)
            {
                // Top surface:
                mesh.Positions.Add(pts[i + 1]);
                mesh.Positions.Add(pts[i]);

                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(mesh.Positions.Count - 2); // 10 * i + 1
                mesh.TriangleIndices.Add(mesh.Positions.Count - 1); // 10 * i + 2

                mesh.Normals.Add(new Vector3D(0, 0, 1));
                mesh.Normals.Add(new Vector3D(0, 0, 1));
                //-
                mesh.Positions.Add(pts[i + 1]);
                mesh.Positions.Add(pbs[i + 1]);
                mesh.Positions.Add(pbs[i]);

                mesh.TriangleIndices.Add(mesh.Positions.Count - 3);
                mesh.TriangleIndices.Add(mesh.Positions.Count - 2); // 10 * i + 1
                mesh.TriangleIndices.Add(mesh.Positions.Count - 1); // 10 * i + 2

                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));

                mesh.Positions.Add(pts[i + 1]);
                mesh.Positions.Add(pbs[i]);
                mesh.Positions.Add(pts[i]);

                mesh.TriangleIndices.Add(mesh.Positions.Count - 3);
                mesh.TriangleIndices.Add(mesh.Positions.Count - 2); // 10 * i + 1
                mesh.TriangleIndices.Add(mesh.Positions.Count - 1); // 10 * i + 2

                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));
                mesh.Normals.Add(new Vector3D(0, 0, -1));

                /*// Bottom surface:
                mesh.Positions.Add(pb);
                mesh.Positions.Add(pbs[i + 1]);
                mesh.Positions.Add(pbs[i]);
                mesh.TriangleIndices.Add(10 * i + 3);
                mesh.TriangleIndices.Add(10 * i + 4);
                mesh.TriangleIndices.Add(10 * i + 5);*/
            }
            mesh.Freeze();
            return mesh;
        }

        #endregion GetMesh3D

        ////////////////////////////////////////////////////////////////////////////////////////

        #region GetPosition

        private Point3D GetPosition(double radius, double theta, double y)
        {
            double sn = Math.Sin(theta * Math.PI / 180);
            double cn = Math.Cos(theta * Math.PI / 180);

            Point3D pt = new Point3D() { X = radius * cn, Y = -radius * sn, Z = y } + (Vector3D)Center;
            return pt;
        }

        #endregion GetPosition
    }
}