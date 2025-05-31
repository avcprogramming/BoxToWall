// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/
// Ignore Spelling: sunion sint ddj tabslot dri msl crs omo fp ic cint cunion osl reducew azone

using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;
using System.Collections.Generic;
using System.Linq;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Colors;
using Bricscad.EditorInput;
using Teigha.Geometry;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
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
    SolidShapes = new() { "Box", "Cone", "Cylinder", "Pyramid", "Sphere" };

    internal static readonly HashSet<string>
    CurveShapes = new() { "Arc", "Line", "Rectangle", "Circle", "Ellipse" };

    /// <summary>
    /// Форма солида: Box, Cone, Cylinder, Pyramid, Sphere. По умолчанию - бокс.
    /// Для блоков - "Block",
    /// Для плоских фигур - "Line", "Rectangle", "Circle", "Ellipse",
    /// Для мультитекстов - "Text"
    /// </summary>
    [DataMember]
    public string
    Shape
    { get; set; } = "Box";

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
    /// Для блока - масштабы и зеркальность (когда < 0).
    /// Для дуги - относ конечной точки от начальной в плоскости XY
    /// </summary>
    [DataMember]
    public double SizeX { get; set; }

    /// <summary>
    /// Размеры солида по осям выложенной сборки. 
    /// То есть для стены - высота сборки выложена вдоль X.
    /// Для блока - масштабы и зеркальность (когда < 0).
    /// Для дуги - относ конечной точки от начальной в плоскости XY
    /// </summary>
    [DataMember]
    public double SizeY { get; set; }

    /// <summary>
    /// Размеры солида по осям выложенной сборки. 
    /// То есть для стены - высота сборки выложена вдоль X.
    /// Для блока - масштабы и зеркальность (когда < 0).
    /// У линии Z может отличаться от 0, тогда линия сразу прочертиться в 3D и не нужно будет разворотов
    /// Для дуги SizeZ - это не размер, а параметр кривизны дуги Bulge:
    ///   0 - линия.
    ///   >0 - дуга против часовой стрелки, <0 - по часовой. 
    ///   1 - дуга в 180 градусов.
    ///   Вычисляется как Bulge = Tan(arc / 4), где arc - угол дуги в радианах.
    ///   Радиус кривизны дуги Radius = Length / (2.0 * Sin(Atan(Abs(Bulge)) * 2.0)), где Length - расстояние от начала до конца сегмента по прямой
    ///   Автокад не понимает Bulge меньше чем 1e-6
    /// Для остальных плоских фигур SizeZ не используется.
    /// Для текстов используется только SizeY как высота шрифта.
    /// </summary>
    [DataMember]
    public double SizeZ { get; set; }

    /// <summary>
    /// Развороты детали вокруг трех осей. Центр вращения - в точке вставки. 
    /// Повороты делаются сначала вокруг X, потом Y, потом Z.
    /// Для текстов поворот RotateZ применяется первым, как свойство текста Rotation
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
    /// По умолчанию - текущий слой. Для текстов по умолчанию Annotation
    /// Вес и тип линий задавайте настройками слоя.
    /// </summary>
    [DataMember]
    public string Layer { get; set; }

    /// <summary>
    /// Если задан материал, а его нет в чертеже, то будет взят из шаблона или создан новый. 
    /// Для текстов это стиль.
    /// </summary>
    [DataMember]
    public string Material { get; set; }

    /// <summary>
    /// Направление текстуры материала.
    /// Допустимы варианты: x,y,z,along,across,"". 
    /// Названия along,across,"" - зависят от локализации и настраиваются в Общих настройках.
    /// Любой другой текст - текстура будет назначена в зависимости от Grain материла.
    /// </summary>
    [DataMember]
    public string Texture { get; set; }

    /// <summary>
    /// В какую сборку добавить этот солид (в блок или именованная группа в зависимости от настроек)
    /// Пустое свойство или null - создать сборку по шаблону AsmCreate. Для записи в модель = Model
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
    /// Описание для солида. Не используется у блоков.
    /// Для текста отсюда берется Контент.
    /// </summary>
    [DataMember]
    public string Info { get; set; }

    /// <summary>
    /// Список команд для пост-обработки этого объекта. Реализованы следующие команды: sint, sunion, ddj, tabslot, dri.
    /// Команды перечисляются через запятую без пробелов. 
    /// Выполняются для всех помеченных объектов в блоке одновременно.
    /// Порядок перечисления команд не важен, будут выполнены как запрограммировано (порядок указан выше).
    /// </summary>
    [DataMember]
    public string Commands { get; set; }

    /// <summary>
    /// Owner = Model
    /// </summary>
    internal bool
    ToModel => IsModel(Owner);

    /// <summary>
    /// Любой размер меньше STol.EqPoint
    /// </summary>
    internal bool
    IsZeroSize => SizeX < STol.EqPoint || SizeY < STol.EqPoint || SizeZ < STol.EqPoint;

    internal bool
    IsBlock => Shape == "Block";

    internal bool
    IsText => Shape == "Text";

    internal bool
    AllowedSolidShape => SolidShapes.Contains(Shape);

    internal bool
    AllowedCurveShape => CurveShapes.Contains(Shape);

    internal bool
    AllowedShape => AllowedSolidShape || IsBlock || AllowedCurveShape || IsText;

    internal bool
    IsNull => IsBlock ? IsNullOrWhiteSpace(Name) :
      IsText ? IsNullOrEmpty(Info) :
      Shape == "Line" ? SizeX < STol.EqPoint && SizeY < STol.EqPoint && SizeZ < STol.EqPoint :
      AllowedCurveShape ? SizeX < STol.EqPoint && SizeY < STol.EqPoint :
      !AllowedSolidShape || IsZeroSize;

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
      Texture = SolidTexture.GetTextureName(solid.Texture);
    }

    /// <summary>
    /// Количество столбцов таблицы для преобразования в BoxData
    /// </summary>
    internal const int
    ColumnCount = 19;

    internal
    BoxData(object[] columns)
    {
      if (columns.Length < 10) return;
      if (columns[0] is string str) Shape = str;
      if (!AllowedShape) return;
      if (columns[1] is double x || AvcSettings.LenStyle.ParseDistance(columns[1].ToString(), out x)) X = x;
      else { Cns.Info(BoxFromTableL.ColumnReadError, 'B'); Shape = "Error"; return; }
      if (columns[2] is double y || AvcSettings.LenStyle.ParseDistance(columns[2].ToString(), out y)) Y = y;
      else { Cns.Info(BoxFromTableL.ColumnReadError, 'C'); Shape = "Error"; return; }
      if (columns[3] is double z || AvcSettings.LenStyle.ParseDistance(columns[3].ToString(), out z)) Z = z;
      else { Cns.Info(BoxFromTableL.ColumnReadError, 'D'); Shape = "Error"; return; }
      if (columns[4] is double sx || AvcSettings.LenStyle.ParseDistance(columns[4].ToString(), out sx)) SizeX = sx;
      else if (!IsText) { Cns.Info(BoxFromTableL.ColumnReadError, 'E'); Shape = "Error"; return; }
      if (columns[5] is double sy || AvcSettings.LenStyle.ParseDistance(columns[5].ToString(), out sy)) SizeY = sy;
      else if (!IsText) { Cns.Info(BoxFromTableL.ColumnReadError, 'F'); Shape = "Error"; return; }
      if (columns[6] is double sz || AvcSettings.LenStyle.ParseDistance(columns[6].ToString(), out sz)) SizeZ = sz;
      else if (!IsText && !AllowedCurveShape) { Cns.Info(BoxFromTableL.ColumnReadError, 'G'); Shape = "Error"; return; }
      if (columns[7] is double rx || AvcSettings.LenStyle.ParseDistance(columns[7].ToString(), out rx)) RotateX = rx;
      else { Cns.Info(BoxFromTableL.ColumnReadError, 'H'); Shape = "Error"; return; }
      if (columns[8] is double ry || AvcSettings.LenStyle.ParseDistance(columns[8].ToString(), out ry)) RotateY = ry;
      else { Cns.Info(BoxFromTableL.ColumnReadError, 'I'); Shape = "Error"; return; }
      if (columns[9] is double rz || AvcSettings.LenStyle.ParseDistance(columns[9].ToString(), out rz)) RotateZ = rz;
      else { Cns.Info(BoxFromTableL.ColumnReadError, 'J'); Shape = "Error"; return; }
      if (columns.Length > 10 && columns[10] is string g) Owner = g;
      if (columns.Length > 11 && columns[11] is string l) Layer = l;
      if (columns.Length > 12 && columns[12] is string c) Color = c;
      if (columns.Length > 13 && columns[13] is string m) Material = m;
      if (columns.Length > 14 && columns[14] is string t) Texture = t;
      if (columns.Length > 15 && columns[15] is string n) Name = n;
      if (columns.Length > 16 && columns[16] is string k) Kind = k;
      if (columns.Length > 17 && columns[17] is string i) Info = i;
      if (columns.Length > 18 && columns[18] is string cmd) Commands = cmd;
    }

    /// <summary>
    /// Создать солид по BoxData.
    /// Shape должно быть одно из Shapes.
    /// Готовый солид не вставляется в чертеж (Owner не используется в этой процедуре)
    /// </summary>
    /// <returns>может вернуть null</returns>
    public Solid3d
    CreateSolid(Database db, Transaction tr)
    {
      if (!AllowedSolidShape || IsZeroSize || db is null || tr is null) return null;
      Solid3d solid = new();

      switch (Shape)
      {
        case "Cone":
          solid.CreateFrustum(SizeZ, SizeX * 0.5, SizeY * 0.5, 0.0);
          solid.Move(X, Y, Z + SizeZ * 0.5);
          break;
        case "Cylinder":
          if (Abs(SizeX - SizeY) < STol.EqPoint)
            using (Circle circ = new(new Point3d(X, Y, Z), Vector3d.ZAxis, SizeX * 0.5))
              solid.CreateExtrudedSolid(circ, Vector3d.ZAxis * SizeZ, new SweepOptions());
          else
          {
            solid.CreateFrustum(SizeZ, SizeX * 0.5, SizeY * 0.5, SizeX * 0.5);
            solid.Move(X, Y, Z + SizeZ * 0.5);
          }
          solid.CleanBody(); // иначе остается стык на поверхности цилиндра
          break;
        case "Pyramid":
          solid.CreatePyramid(SizeZ, 4, SizeY, 0.0);
          solid.Move(X, Y, Z + SizeZ * 0.5);
          break;
        case "Sphere":
          solid.CreateSphere(SizeX);
          solid.Move(X, Y, Z);
          break;
        default: // Box
          solid.CreateBox(SizeX, SizeY, SizeZ);
          solid.Move(X + SizeX * 0.5, Y + SizeY * 0.5, Z + SizeZ * 0.5);
          break;
      }

      if (RotateX != 0.0)
        solid.TransformBy(Matrix3d.Rotation(RotateX / 180.0 * PI, Vector3d.XAxis, new Point3d(X, Y, Z)));
      if (RotateY != 0.0)
        solid.TransformBy(Matrix3d.Rotation(RotateY / 180.0 * PI, Vector3d.YAxis, new Point3d(X, Y, Z)));
      if (RotateZ != 0.0)
        solid.TransformBy(Matrix3d.Rotation(RotateZ / 180.0 * PI, Vector3d.ZAxis, new Point3d(X, Y, Z)));

      XDataNames xd = new(Name, SolidFlags.None, Kind, Info);
      string lc = Texture?.ToLower();
      TextureAlong t;
      if (lc == "x" || lc == "y" || lc == "z")
        t = LengthAxis() == lc ? TextureAlong.Along : TextureAlong.Across;
      else
        t = SolidTexture.GetTextureFromName(Texture);
      if (t != TextureAlong.Indeterminate)
        xd.Texture = t;
      xd.SaveTo(solid, db, tr);
      bool hasTexture = t == TextureAlong.Across || t == TextureAlong.Along;

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
        : MaterialExt.GetOrCreate(Material, ObjectId.Null,
        AvcPaletteStyle.DefMaterialUseLike, hasTexture ? MaterialEnum.Grain : 0, db, tr);
      solid.SetMaterialOrColor(materialId, color, tr);

      return solid;
    }

    /// <summary>
    /// По какой оси изначально (до разворотов) была самая длинная сторона бокса
    /// </summary>
    /// <returns></returns>
    private string
    LengthAxis()
    {
      double max = Max(Max(SizeX, SizeY), SizeZ);
      if (max == SizeX) return "x";
      if (max == SizeY) return "y";
      return "z";
    }

    public Curve
    CreateCurve(Database db, Transaction tr)
    {
      if (IsNull || db is null || tr is null) return null;

      if (Shape == "Rectangle" && (SizeX < STol.EqPoint || SizeY < STol.EqPoint))
        Shape = "Line";
      else if (Shape == "Circle" && Abs(SizeX - SizeY) > STol.ZeroSize)
        Shape = "Ellipse";
      else if (Shape == "Ellipse" && Abs(SizeX - SizeY) < STol.ZeroSize)
        Shape = "Circle";
      else if (Shape == "Arc" && Abs(SizeZ) < 1e-6)
      { Shape = "Line"; SizeZ = Z; }

      Point3d basePoint = new(X, Y, Z);
      Curve curve;
      switch (Shape)
      {
        case "Arc":
          PSegment seg = new(new(X, Y), new(X + SizeX, Y + SizeY), SizeZ, ObjectId.Null);
          curve = seg.ToCurve();
          curve.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * Z));
          break;
        case "Line":
          curve = new Line(basePoint, new(X + SizeX, Y + SizeY, Z + SizeZ));
          break;
        case "Rectangle":
          Polyline pl = new(4);
          pl.AddVertexAt(0, new Point2d(X, Y), 0, 0, 0);
          pl.AddVertexAt(1, new Point2d(X + SizeX, Y), 0, 0, 0);
          pl.AddVertexAt(2, new Point2d(X + SizeX, Y + SizeY), 0, 0, 0);
          pl.AddVertexAt(3, new Point2d(X, Y + SizeY), 0, 0, 0);
          pl.Closed = true;
          pl.TransformBy(Matrix3d.Displacement(Vector3d.ZAxis * Z));
          curve = pl;
          break;
        case "Circle":
          curve = new Circle(basePoint, Vector3d.ZAxis, SizeX * 0.5);
          break;
        case "Ellipse":
          curve = new Ellipse(basePoint, Vector3d.ZAxis, SizeX * 0.5 * Vector3d.XAxis, SizeX / SizeY, 0, 2 * PI);
          break;
        default:
          return null;
      }

      curve.SetDatabaseDefaults(db);

      if (RotateX != 0.0)
        curve.TransformBy(Matrix3d.Rotation(RotateX / 180.0 * PI, Vector3d.XAxis, basePoint));
      if (RotateY != 0.0)
        curve.TransformBy(Matrix3d.Rotation(RotateY / 180.0 * PI, Vector3d.YAxis, basePoint));
      if (RotateZ != 0.0)
        curve.TransformBy(Matrix3d.Rotation(RotateZ / 180.0 * PI, Vector3d.ZAxis, basePoint));

      if (!IsNullOrWhiteSpace(Layer))
      {
        LayerManager lm = new(db, tr);
        ObjectId layerId = lm.GetOrCreate(Layer, LayerEnum.Visible);
        if (!layerId.IsNull) curve.LayerId = layerId;
      }
      if (!IsNullOrWhiteSpace(Color))
        if (ColorExt.TryParseColor(Color, out Color color)) curve.Color = color;
        else Cns.Info(BoxFromTableL.ColorErr, Color);

      if (!IsNullOrWhiteSpace(Material))
      {
        ObjectId materialId = MaterialExt.GetOrCreate(Material, ObjectId.Null,
          MatUseLike.Rod, 0, db, tr);
        if (!materialId.IsNull) curve.MaterialId = materialId;
      }

      XDataNames xd = new(Name, SolidFlags.None, Kind, Info);
      xd.SaveTo(curve, db, tr);

      return curve;
    }

    /// <summary>
    /// Создать BlockReference по данным из BoxData.
    /// Shape должно быть Block.
    /// Name должно указывать на имя существующего блока из чертежа _db или из шаблона.
    /// SizeX,SizeY,SizeZ используются как масштаб блока (обычно 1)
    /// Kind и Info игнорируются.
    /// BlockReference не вставляется в чертеж (Owner не используется в этой процедуре)
    /// </summary>
    /// <returns>может вернуть null</returns>
    public BlockReference
    CreateBlock(Database db, Transaction tr)
    {
      if (!IsBlock || IsNullOrWhiteSpace(Name) || db is null || tr is null) return null;
      Point3d point = new(X, Y, Z);
      BlockManager bm = new(db, tr, AvcSettings.TemplateFile);
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
        : MaterialExt.GetOrCreate(Material, ObjectId.Null,
        AvcPaletteStyle.DefMaterialUseLike, 0, db, tr);
      if (!materialId.IsNull) br.MaterialId = materialId;

      return br;
    }

    public MText
    CreateText(Database db, Transaction tr)
    {
      if (IsNullOrEmpty(Info)) return null;
      MText text = MTextExt.CreateText(Info, Material, SizeY, false, db, tr);
      if (text is null) return null;
      if (IsNullOrEmpty(Material)) text.Attachment = AttachmentPoint.BottomLeft;
      text.Location = new Point3d(X, Y, Z);
      text.Rotation = RotateZ / 180 * PI;

      if (RotateX != 0.0)
        text.TransformBy(Matrix3d.Rotation(RotateX / 180.0 * PI, Vector3d.XAxis, text.Location));
      if (RotateY != 0.0)
        text.TransformBy(Matrix3d.Rotation(RotateY / 180.0 * PI, Vector3d.YAxis, text.Location));

      LayerManager lm = new(db, tr);
      if (!IsNullOrWhiteSpace(Layer))
      {
        ObjectId layerId = lm.GetOrCreate(Layer, LayerEnum.Visible);
        if (!layerId.IsNull) text.LayerId = layerId;
      }
      if (AvcSettings.CommonFlags.HasFlag(AvcEnum.ManageLayers))
        text.LayerId = lm.GetOrCreate(LayerEnum.Annotation);

      if (!IsNullOrWhiteSpace(Color))
        if (ColorExt.TryParseColor(Color, out Color color)) text.Color = color;
        else Cns.Info(BoxFromTableL.ColorErr, Color);

      return text;
    }

    public static bool
    IsModel(string blockName)
    {
      if (IsNullOrWhiteSpace(blockName)) return false;
      string up = blockName.ToUpper();
      return up == "MODEL" || up == "*MODEL_SPACE";
    }

    public override string
    ToString()
    {
      return IsNull ? "Null" :
      IsBlock ? $"{Shape} {Name}" :
      IsText ? $"{Shape} {Info}" :
      $"{Shape} {SizeX.ApproxSize()}x{SizeY.ApproxSize()}x{SizeZ.ApproxSize()}";
    }

    /// <summary>
    /// Группировка по BoxData.Owner
    /// </summary>
    internal static Dictionary<string, List<BoxData>>
    Grouping(IEnumerable<BoxData> boxes)
    {
      Dictionary<string, List<BoxData>> res = new();
      if (boxes != null)
        foreach (BoxData item in boxes)
        {
          if (IsNullOrWhiteSpace(item.Owner)) item.Owner = ""; // объединить null и "" в одну группу
          if (IsModel(item.Owner)) item.Owner = "Model"; // объединить разные названия модели
          if (res.TryGetValue(item.Owner, out List<BoxData> list)) list.Add(item);
          else res.Add(item.Owner, new List<BoxData> { item });
        }
      return res;
    }

    /// <summary>
    /// Создаем солиды-детали и ставим их вертикально
    /// </summary>
    private static CreateBoxResult
    CreateBoxes(BoxData[] boxes, Database db, ObjectId spaceId, Matrix3d transform)
    {
      using Transaction tr = db.TransactionManager.StartTransaction();
      BlockTableRecord space = tr.GetObject(spaceId, OpenMode.ForWrite) as BlockTableRecord;
      CreateBoxResult results = new();

      foreach (BoxData box in boxes)
      {
        LongOperationManager.TryTickOrEsc();
        if (box.IsNullMessage()) continue;

        using Entity entity =
          box.IsBlock ? box.CreateBlock(db, tr) :
          box.IsText ? box.CreateText(db, tr) :
          box.AllowedCurveShape ? box.CreateCurve(db, tr) :
          box.CreateSolid(db, tr);
        if (entity is null) continue;

        entity.TransformBy(transform);
        //Transient.DebugDraw(entity, 2);
        ObjectId boxId = space.AppendEntity(entity);
        if (boxId.IsNull) continue;
        results.Add(boxId, box.Commands);
        tr.AddNewlyCreatedDBObject(entity, true);
      }

      tr.Commit();
      return results;
    }

    public bool
    IsNullMessage()
    {
      if (AllowedSolidShape && IsZeroSize)
      {
        Cns.Info(BoxFromTableL.ZeroSizeSolidError);
        return true;
      }
      else if (IsNull)
      {
        Cns.Info(BoxFromTableL.BoxIsNull);
        return true;
      }
      else return false;
    }

    /// <summary>
    /// Создать солиды и боксы по данным из boxes и трансформировать по transform. BoxData.Owner игнорируется.
    /// Вызвать Drill и другие команды.
    /// Вставить их в группу или блок blockName.
    /// </summary>
    /// <returns>возвращает id BTR сборки или любой 1 ID </returns>
    private static ObjectId
    CreateWall(BoxData[] boxes, string blockName, Matrix3d transform, CreateBoxEnum flags, Database db)
    {
      bool toModel = IsModel(blockName);
      ObjectId modelId = db.GetModelId();

      // Создаем солиды-детали и ставим их вертикально
      CreateBoxResult results = CreateBoxes(boxes, db, modelId, transform);
      if (results is null || results.IsNull)
      {
        if (!toModel) Cns.Info(BoxFromTableL.ZeroSolidListError, blockName);
        return ObjectId.Null;
      }

      // выполнение команд над готовыми боксами и, возможно, замена id на новые
      RunCommands(ref results, boxes, flags, db, modelId);
      if (results is null || results.IsNull)
      {
        if (!toModel) Cns.Info(BoxFromTableL.ZeroSolidListError, blockName);
        return ObjectId.Null;
      }

      ObjectId[] boxIdArray = results.ToArray();
      ObjectId retId = boxIdArray[0]; // для возврата признака, что все сработало, но сборка не сделана
      if (!toModel && (flags.HasFlag(CreateBoxEnum.MakeBlock) || flags.HasFlag(CreateBoxEnum.MakeGroup)))
      {
        AssemblyStyle style = AssemblyStyle.GetCurrent();
        using Transaction tr = db.TransactionManager.StartTransaction();
        string newName = IsNullOrWhiteSpace(blockName) ?
          style.NewName(boxIdArray, db, tr) : DatabaseExt.ValidName(blockName);
        if (flags.HasFlag(CreateBoxEnum.MakeBlock)) // создание блока
        {
          // Подправим стиль создания блоков
          BlockCreateEnum createOpt = style.CreateOptions &
            ~(BlockCreateEnum.LayerZero | BlockCreateEnum.MeshToSolid | BlockCreateEnum.Unite | BlockCreateEnum.Insert)
            | BlockCreateEnum.BlockMetric;

          // создание блока в начале координат без вставки
          CreateBlockResult ret = BlockCreate.CreateNewBlock(
            boxIdArray, newName, createOpt, BlockBasePointEnum.WCS, BlockRotationEnum.WCS, Point3dExt.Null,
            Matrix3d.Identity, style.LabelHeight, tr);
          if (ret.IsNull) return ObjectId.Null;
          boxIdArray.EraseAll(tr);
          retId = ret.BtrId; // возврат BTR сборки для выставки сборок
        }
        else if (flags.HasFlag(CreateBoxEnum.MakeGroup)) // создание группы
        {
          Cns.Info(BoxFromTableL.CreateGroup, newName);
          Group group;
          DBDictionary dic = tr.GetObject(db.GroupDictionaryId, OpenMode.ForWrite) as DBDictionary;
          if (dic is null) return ObjectId.Null;
          if (dic.Contains(newName)) // старая группа - заменяем все объекты
          {
            retId = dic.GetAt(newName);
            group = tr.GetObject(retId, OpenMode.ForWrite) as Group;
            if (group is null) return ObjectId.Null;
            ObjectId[] oldIds = group.GetAllEntityIds();
            foreach (ObjectId oldId in oldIds)
              group.Remove(oldId);
          }
          else // новая группа
          {
            group = new(AvcWeb.AvcADLink, true);
            retId = dic.SetAt(newName, group);
            tr.AddNewlyCreatedDBObject(group, true);
          }

          foreach (ObjectId entId in boxIdArray)
            group.Append(entId);
        }
        tr.Commit();
      }

      return retId;
    }

    private static void
    RunCommands(ref CreateBoxResult result, BoxData[] boxes, CreateBoxEnum flags, Database db, ObjectId spaceId)
    {
      if (result.IsNull) return;

      foreach (var pair in result.Commands)
        switch (pair.Key)
        {
          case "dri": break;
          case "sint": break;
          case "sunion": break;
          case "ddj": break;
          case "tabslot": break;
          case "crs": Cns.Info(BoxFromTableL.NotImplementedCommand, pair.Key.ToUpper());  break;
          case "fp": Cns.Info(BoxFromTableL.NotImplementedCommand, pair.Key.ToUpper());  break;
          case "ic": Cns.Info(BoxFromTableL.NotImplementedCommand, pair.Key.ToUpper()); break;  // только для кривых
          case "cunion": Cns.Info(BoxFromTableL.NotImplementedCommand, pair.Key.ToUpper()); break;
          case "cint": Cns.Info(BoxFromTableL.NotImplementedCommand, pair.Key.ToUpper()); break;
          case "msl": Cns.Info(BoxFromTableL.NotImplementedCommand, pair.Key.ToUpper()); break;
          case "osl": Cns.Info(BoxFromTableL.NotImplementedCommand, pair.Key.ToUpper()); break;
          case "reducew": Cns.Info(BoxFromTableL.NotImplementedCommand, pair.Key.ToUpper()); break;
          case "azone": Cns.Info(BoxFromTableL.NotImplementedCommand, pair.Key.ToUpper()); break;
          default: Cns.Info(BoxFromTableL.UnknownCommand, pair.Key.ToUpper()); break;
        }

      try
      {
        if (result.Commands.TryGetValue("sint", out HashSet<ObjectId> sint) && sint.Count > 0)
        {
#if RENT || VARS
          if (!LicenseCheck.HasLicenseFor("SInt"))
            throw new CancelException("");
#endif
          Cns.Info(CommandL.SolidInt);
          SolidSubStyle subStyle = SolidSubStyle.GetCurrent();
           HashSet<ObjectId> intResults = SolidIntCmd.Intersect(sint, db, new PointOfView(), false);
          result.Replace(sint, intResults);
        }

        if (result.Commands.TryGetValue("sunion", out HashSet<ObjectId> sunion) && sunion.Count > 0)
        {
#if RENT || VARS
          if (!LicenseCheck.HasLicenseFor("SUnion"))
            throw new CancelException("");
#endif
          Cns.Info(CommandL.SolidUnion);
          SolidSubStyle subStyle = SolidSubStyle.GetCurrent();
          HashSet<ObjectId> unionResults = SolidUnionCmd.Union(sunion, db, new PointOfView(), false);
          result.Replace(sunion, unionResults);
        }

        if (result.Commands.TryGetValue("ddj", out HashSet<ObjectId> ddj) && ddj.Count > 0)
        {
#if RENT || VARS
          if (!LicenseCheck.HasLicenseFor("DDJ"))
            throw new CancelException("");
#endif
          Cns.Info(CommandL.DDJ);
          DadoJointStyle style = DadoJointStyle.GetCurrent();
          DadoJoint.CreateJoints(ddj, style, db, false); // не меняет id солидов (но могут распадаться на части)
          result.ClearErased();
        }

        if (result.Commands.TryGetValue("tabslot", out HashSet<ObjectId> tabslot) && tabslot.Count > 0)
        {
#if RENT || VARS
          if (!LicenseCheck.HasLicenseFor("TabSlot"))
            throw new CancelException("");
#endif
          Cns.Info(CommandL.TabSlot);
          TabSlotCmd.CreateTabSlots(tabslot, db); // не меняет id солидов (но могут распадаться на части)
          result.ClearErased();
        }

        // Сверловка
        int count = 0;
        if (result.Commands.TryGetValue("dri", out HashSet<ObjectId> dri) && dri.Count > 0)
          count = DrillHoles(dri.ToArray(), null, db);
        else if (flags.HasFlag(CreateBoxEnum.Drill))
          count = DrillHoles(result.ToArray(), boxes, db);
        // Удалим ID дырок
        if (count > 0)
          result.ClearErased();
      }
      catch (WarningException ex) { Cns.Warning(ex.Message); }
    }

    private static int
    DrillHoles(ObjectId[] boxIdArray, BoxData[] boxes, Database db)
    {
      string hl = Drill.GetHolesLayer(boxIdArray, db);
      if (IsNullOrEmpty(hl)) return 0;

      HashSet<string> holeLayers;
      using (Transaction tr = db.TransactionManager.StartTransaction())
      {
        LayerManager lm = new(db, tr);
        holeLayers = lm.GetRealLayers(hl);
        tr.Commit();
      }

      if (holeLayers.Count == 0)
      {
        Cns.Info(DrillL.LayerNotSpecified);
        return 0;
      }

      // Поищем, есть ли среди боксов дырки для сверления
      bool hasHoles = false;
      if (boxes is null) hasHoles = true; // не проверяем
      else
        foreach (BoxData boxData in boxes)
          if (holeLayers.Contains(boxData.Layer) || boxData.IsBlock)
          { hasHoles = true; break; }
      if (!hasHoles) return 0;

#if RENT || VARS
      DocExt.ThirdTest = db.BlockTableId; // проверка в Subtraction
      if (!LicenseCheck.HasLicenseFor(DbCommand.DrillCmdName))
        throw new CancelException("");
#endif

      try
      {
        Cns.Info(DrillL.Drilling);
        return Drill.MakeDrill(boxIdArray, hl);
      }
      catch (WarningException ex) { Cns.Info(ex.Message); }

      return 0;
    }

    /// <summary>
    /// Создание солидов и блоков
    /// </summary>
    internal static int
    CreateWalls(Dictionary<string, List<BoxData>> groups, CreateBoxEnum flags, Matrix3d transform, Database db, out List<ObjectId> btrs)
    {
      btrs = new();
      int count = 0;
      List<BoxData> model = null; // нельзя создавать блоки в модели до того как созданы все остальные блоки (которые возможно надо вставить в модель)
      foreach (KeyValuePair<string, List<BoxData>> group in groups)
      {
        LongOperationManager.TryTickOrEsc();
        if (IsModel(group.Key)) model = group.Value;
        else
        {
          ObjectId blockId = CreateWall(group.Value.ToArray(), group.Key, transform, flags, db);
          if (blockId.IsNull) continue;
          if (blockId.ObjectClass == BlockExt.dbBTR) btrs.Add(blockId);
          count++;
        }
      }

      if (model is not null && model.Count > 0)
      {
        ObjectId blockId = CreateWall(model.ToArray(), "Model", Matrix3d.Identity, flags, db);
        if (!blockId.IsNull)
          count++;
      }

      return count;
    }


  }
}
