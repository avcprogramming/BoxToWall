// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Colors;
using Bricscad.EditorInput;
using Teigha.Geometry;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Rt = Autodesk.AutoCAD.Runtime;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AVC
{
  /// <summary>
  /// Вспомогательный объект для получения с web-сервера данных о боксах и других фигурах, десериализации из JSON 
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  BoxData
  {
    /// <summary>
    /// Box, Cone, Cylinder, Pyramid, Sphere
    /// </summary>
    [DataMember]
    public string Shape { get; set; } = "Box";
    [DataMember]
    public double X { get; set; }
    [DataMember]
    public double Y { get; set; }
    [DataMember]
    public double Z { get; set; }
    [DataMember]
    public double SizeX { get; set; }
    [DataMember]
    public double SizeY { get; set; }
    [DataMember]
    public double SizeZ { get; set; }
    [DataMember]
    public double RotateX { get; set; }
    [DataMember]
    public double RotateY { get; set; }
    [DataMember]
    public double RotateZ { get; set; }
    [DataMember]
    public string Color { get; set; }
    [DataMember]
    public string Layer { get; set; } 
    [DataMember]
    public string Material { get; set; }
    [DataMember]
    public string Group { get; set; }

    /// <summary>
    /// Любой размер меньше STol.EqPoint
    /// </summary>
    internal bool 
    IsZeroSize => SizeX < STol.EqPoint || SizeY < STol.EqPoint || SizeZ < STol.EqPoint;

    public BoxData() { }

    internal
    BoxData(AvcSolid solid)
    {
      if (solid == null) return;
      X = solid.BasePoint.X;
      Y = solid.BasePoint.Y;
      Z = solid.BasePoint.Z;
      SizeX = solid.Metric.Length;
      SizeY = solid.Metric.Width;
      SizeZ = solid.Metric.Thickness;
      Color = solid.ColorName;
      Layer = solid.ActualLayer?.Name;
      Material = solid.ActualMaterial?.Name;
    }

    public Solid3d
    CreateSolid(Database db, Transaction tr)
    {
      if (IsZeroSize || db is null || tr is null) return null;
      Solid3d solid = new();

      switch (Shape)
      {
        case "Cone":
          solid.CreateFrustum(SizeZ, SizeX * 0.5, SizeY * 0.5, 0.0);
          solid.TransformBy(Matrix3d.Displacement(new Vector3d(X, Y, Z + SizeZ * 0.5)));
          break;
        case "Cylinder":
          if (Abs(SizeX - SizeY) < STol.EqPoint)
          using (Circle circ = new(new Point3d(X, Y, Z), Vector3d.ZAxis, SizeX * 0.5))
            solid.CreateExtrudedSolid(circ, Vector3d.ZAxis, new SweepOptions());
          else
          {
            solid.CreateFrustum(SizeZ, SizeX * 0.5, SizeY * 0.5, SizeX * 0.5);
            solid.TransformBy(Matrix3d.Displacement(new Vector3d(X, Y, Z + SizeZ * 0.5)));
          }
          break;
        case "Pyramid":
          solid.CreatePyramid(SizeZ, 4, SizeY, 0.0);
          solid.TransformBy(Matrix3d.Displacement(new Vector3d(X, Y, Z + SizeZ * 0.5)));
          break;
        case "Sphere":
          solid.CreateSphere(SizeX);
          solid.TransformBy(Matrix3d.Displacement(new Vector3d(X, Y, Z)));
          break;
        default: // Box
          solid.CreateBox(SizeX, SizeY, SizeZ);
          solid.TransformBy(Matrix3d.Displacement(new Vector3d(X + SizeX * 0.5, Y + SizeY * 0.5, Z + SizeZ * 0.5)));
          break;
      }

      if (RotateX != 0.0)
        solid.TransformBy(Matrix3d.Rotation(RotateX / 180.0 * PI, Vector3d.XAxis, new Point3d(X, Y, Z)));
      if (RotateY != 0.0)
        solid.TransformBy(Matrix3d.Rotation(RotateY / 180.0 * PI, Vector3d.YAxis, new Point3d(X, Y, Z)));
      if (RotateZ != 0.0)
        solid.TransformBy(Matrix3d.Rotation(RotateZ / 180.0 * PI, Vector3d.ZAxis, new Point3d(X, Y, Z)));

      solid.SetDatabaseDefaults(db);
      if (!IsNullOrWhiteSpace(Layer))
      {
        LayerManager lm = new(db, tr);
        ObjectId layerId = lm.GetOrCreate(Layer, LayerEnum.Visible);
        if (!layerId.IsNull) solid.LayerId = layerId;
      }
      Color color = null;
      if (!IsNullOrWhiteSpace(Color))
        if (!ColorExt.TryParseColor(Color, out color)) Cns.Info(BoxFromTableL.ColorErr, Color);
                
      ObjectId materialId = IsNullOrWhiteSpace(Material) ? ObjectId.Null
        : MaterialExt.GetOrCreate(Material, ObjectId.Null, MatUseLike.Sheet, db, tr);
      solid.SetMaterialOrColor(materialId, color, tr);
      return solid;
    }

    public override string
    ToString() => IsZeroSize ? "Zero" : $"{Shape} {SizeX.ApproxSize()}x{SizeY.ApproxSize()}x{SizeZ.ApproxSize()}";

  }
}
