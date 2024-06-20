// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;
using System.Collections.Generic;

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
  /// Так же может содержать сведения о блоке.
  /// Задает данные для создания солидов-деталей
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  BoxData
  {
    internal static readonly HashSet<string>
    Shapes = new() { "Box", "Cone", "Cylinder", "Pyramid", "Sphere" };

    /// <summary>
    /// Форма солида: Box, Cone, Cylinder, Pyramid, Sphere. По умолчанию - бокс.
    /// Для блоков - "Block"
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
    /// То есть для стены - высота сборки выложена вдоль X.
    /// Для блока - масштабы и зеркальность (когда < 0)
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
    /// В какую сборку добавить этот солид (в блок или именованная группа в зависимости от настроек)
    /// По умолчанию Model
    /// </summary>
    [DataMember]
    public string Owner { get; set; }

    /// <summary>
    /// Имя для солида. Для блока - это обязательный столбец с именем существующего блока в данном чертеже или в DWT шаблоне
    /// </summary>
    [DataMember]
    public string Name { get; set; }

    /// <summary>
    /// Тип или вид для солида. Не используется у блоков
    /// </summary>
    [DataMember]
    public string Kind { get; set; }

    /// <summary>
    /// Описание для солида. Не используется у блоков
    /// </summary>
    [DataMember]
    public string Info { get; set; }

    /// <summary>
    /// Не задан владелец или Model
    /// </summary>
    internal bool
    ToModel
    {
      get
      {
        if (IsNullOrWhiteSpace(Owner)) return true;
        string up = Owner.ToUpper();
        return up == "MODEL" || up == "*MODEL_SPACE";
      }
    }

    /// <summary>
    /// Любой размер меньше STol.EqPoint
    /// </summary>
    internal bool
    IsZeroSize => SizeX < STol.EqPoint || SizeY < STol.EqPoint || SizeZ < STol.EqPoint;

    internal bool
    IsBlock => Shape == "Block";

    internal bool
    AllowedShape => Shapes.Contains(Shape);

    internal bool
    IsNull => IsBlock ? IsNullOrWhiteSpace(Name) : !AllowedShape || IsZeroSize;

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
      Name = solid.Name;
      Kind = solid.Kind;
      Info = solid.Info;
      Owner = solid.BlockName;
    }

    /// <summary>
    /// Количество столбцов таблицы для преобразования в BoxData
    /// </summary>
    internal const int
    ColumnCount = 17;

    internal
    BoxData(object[] columns)
    {
      if (columns.Length < 10) return;
      if (columns[0] is string str) Shape = str; 
      if (!IsBlock && !AllowedShape) return;
      if (columns[1] is double x || AvcSettings.LenStyle.ParseDistance(columns[1].ToString(), out x)) X = x; else return;
      if (columns[2] is double y || AvcSettings.LenStyle.ParseDistance(columns[2].ToString(), out y)) Y = y; else return;
      if (columns[3] is double z || AvcSettings.LenStyle.ParseDistance(columns[3].ToString(), out z)) Z = z; else return;
      if (columns[4] is double sx || AvcSettings.LenStyle.ParseDistance(columns[4].ToString(), out sx)) SizeX = sx; else return;
      if (columns[5] is double sy || AvcSettings.LenStyle.ParseDistance(columns[5].ToString(), out sy)) SizeY = sy; else return;
      if (columns[6] is double sz || AvcSettings.LenStyle.ParseDistance(columns[6].ToString(), out sz)) SizeZ = sz; else return;
      if (columns[7] is double rx || AvcSettings.LenStyle.ParseDistance(columns[7].ToString(), out rx)) RotateX = rx; else return;
      if (columns[8] is double ry || AvcSettings.LenStyle.ParseDistance(columns[8].ToString(), out ry)) RotateY = ry; else return;
      if (columns[9] is double rz || AvcSettings.LenStyle.ParseDistance(columns[9].ToString(), out rz)) RotateZ = rz; else return;
      if (columns.Length > 10 && columns[10] is string l) Layer = l;
      if (columns.Length > 11 && columns[11] is string c) Color = c;
      if (columns.Length > 12 && columns[12] is string m) Material = m;
      if (columns.Length > 13 && columns[13] is string g) Owner = g;
      if (columns.Length > 14 && columns[14] is string n) Name = n;
      if (columns.Length > 15 && columns[15] is string k) Kind = k;
      if (columns.Length > 16 && columns[16] is string i) Info = i;
    }

    public Solid3d
    CreateSolid(Database db, Transaction tr)
    {
      if (!AllowedShape || IsZeroSize || db is null || tr is null) return null;
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
              solid.CreateExtrudedSolid(circ, Vector3d.ZAxis * SizeZ, new SweepOptions());
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

      XDataNames xd = new(Name, SolidFlags.None, Kind, Info);
      xd.SaveTo(solid, tr);

      solid.SetDatabaseDefaults(db);
      if (!IsNullOrWhiteSpace(Layer))
      {
        LayerManager lm = new(db, tr);
        LayerEnum? layerStd = LayerManager.IsStdName(Layer);
        if (layerStd is null) layerStd = LayerEnum.Visible;
        ObjectId layerId = lm.GetOrCreate(Layer, layerStd.Value);
        if (!layerId.IsNull) solid.LayerId = layerId;
      }
      Color color = null;
      if (!IsNullOrWhiteSpace(Color))
        if (!ColorExt.TryParseColor(Color, out color)) 
          Cns.Info(BoxFromTableL.ColorErr, Color);

      ObjectId materialId = IsNullOrWhiteSpace(Material) ? ObjectId.Null
        : MaterialExt.GetOrCreate(Material, ObjectId.Null, MatUseLike.Sheet, db, tr);
      solid.SetMaterialOrColor(materialId, color, tr);

      return solid;
    }

    public BlockReference
    CreateBlock(Database db, Transaction tr)
    {
      if (IsZeroSize || db is null || tr is null || !IsBlock) return null;
      Point3d point = new (X, Y, Z);
      BlockManager bm = new (db, tr);
      BlockReference br = bm.CreateBlockReference(Name, point);
      if (br is null)
      {
        Cns.Info(BlockL.BlockNotFound, Name);
        return null;
      }

      if (SizeX == 0.0) SizeX = 1.0;
      if (SizeY == 0.0) SizeY = 1.0;
      if (SizeZ == 0.0) SizeZ = 1.0;
      if (SizeX != 1.0 || SizeY != 1.0 || SizeZ != 1.0)
        br.ScaleFactors = new Scale3d(SizeX, SizeY, SizeZ); 

      if (RotateX != 0.0)
        br.TransformBy(Matrix3d.Rotation(RotateX / 180.0 * PI, Vector3d.XAxis, point));
      if (RotateY != 0.0)
        br.TransformBy(Matrix3d.Rotation(RotateY / 180.0 * PI, Vector3d.YAxis, point));
      if (RotateZ != 0.0)
        br.TransformBy(Matrix3d.Rotation(RotateZ / 180.0 * PI, Vector3d.ZAxis, point));

      if (!IsNullOrWhiteSpace(Layer))
      {
        LayerManager lm = new(db, tr);
        LayerEnum? layerStd = LayerManager.IsStdName(Layer);
        if (layerStd is null) layerStd = LayerEnum.Visible;
        ObjectId layerId = lm.GetOrCreate(Layer, layerStd.Value);
        if (!layerId.IsNull) br.LayerId = layerId;
      }

      if (!IsNullOrWhiteSpace(Color))
        if (ColorExt.TryParseColor(Color, out Color color))
          br.Color = color; 
        else Cns.Info(BoxFromTableL.ColorErr, Color);

      ObjectId materialId = IsNullOrWhiteSpace(Material) ? ObjectId.Null
        : MaterialExt.GetOrCreate(Material, ObjectId.Null, MatUseLike.Sheet, db, tr);
      if (!materialId.IsNull) br.MaterialId = materialId;

      return br;
    }

    public override string
    ToString() => IsNull ? "Null" : IsBlock ? Name : IsZeroSize ? "Zero"
      : $"{Shape} {SizeX.ApproxSize()}x{SizeY.ApproxSize()}x{SizeZ.ApproxSize()}";

  }
}
