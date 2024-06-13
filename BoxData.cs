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
  /// Вспомогательный объект для получения с web-сервера данных о боксах и других фигурах, десериализации из JSON.
  /// Задает данные для создания солидов-деталей
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  BoxData
  {
    /// <summary>
    /// Форма солида: Box, Cone, Cylinder, Pyramid, Sphere. По умолчанию - бокс
    /// </summary>
    [DataMember]
    public string Shape { get; set; } = "Box";

    /// <summary>
    /// Точка вставки солида внутрь сборки. 
    /// Бокс будет создан в положительную сторону от этой точки по всем осям.
    /// Остальные фигуры - точка центра основания или центр сферы.
    /// В координатах выложенного исходного солида. 
    /// То есть для стены - высота сборки выложена вдоль X
    /// </summary>
    [DataMember]
    public double X { get; set; }

    [DataMember]
    public double Y { get; set; }

    [DataMember]
    public double Z { get; set; }

    /// <summary>
    /// Размеры солида по осям выложенной сборки. 
    /// То есть для стены - высота сборки выложена вдоль X
    /// </summary>
    [DataMember]
    public double SizeX { get; set; }

    [DataMember]
    public double SizeY { get; set; }

    [DataMember]
    public double SizeZ { get; set; }

    /// <summary>
    /// Развороты детали вокруг трех осей. Центр вращения - в точке вставки. 
    /// Повототы делаются снача вокруг X, потом Y, потом Z.
    /// В градусах.
    /// </summary>
    [DataMember]
    public double RotateX { get; set; }

    [DataMember]
    public double RotateY { get; set; }

    [DataMember]
    public double RotateZ { get; set; }

    /// <summary>
    /// Имя цвета (как он отображается в Палитре Свойств AVC). 
    /// По умолчанию - текущий цвет чертежа.
    /// </summary>
    [DataMember]
    public string Color { get; set; }

    /// <summary>
    /// Если задан слой, а его нет в чертеже, то будет взят из шаблона или создан новый.
    /// По умолчанию - текущий слой
    /// </summary>
    [DataMember]
    public string Layer { get; set; }

    /// <summary>
    /// Если задан материал, а его нет в чертеже, то будет взят из шаблона или создан новый. 
    /// </summary>
    [DataMember]
    public string Material { get; set; }

    /// <summary>
    /// В какую сборку добавить этот солид (блок или именованная группа в зависимости от настроек)
    /// ПОКА НЕ ИСПОЛЬЗУЕТСЯ
    /// </summary>
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
