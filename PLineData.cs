// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

// Ignore Spelling: Vertices

using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;
#if BRICS
using Teigha.DatabaseServices;
using Teigha.Colors;
using Teigha.Geometry;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Rt = Autodesk.AutoCAD.Runtime;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AVC
{
  /// <summary>
  /// Вспомогательный обьект для получения с web-сервера данных о полилиниях, десериализации из JSON 
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  PLineData
  {
    /// <summary>
    /// Замкнутая полилиния или окружность. Последний вертекс соединен с первым.
    /// </summary>
    [DataMember]
    public bool Closed { get; set; }

    [DataMember]
    public string Color { get; set; }

    [DataMember]
    public string Layer { get; set; }

    [DataMember]
    public string LineType { get; set; }

    [DataMember]
    public double LineWeight { get; set; } = -3;

    /// <summary>
    /// Список вершин и кривизны сегментов.
    /// Просто линия - два вертекса с Bulge=0 и Closed = false.
    /// Для задания окружности укажите 2 вертекса на пересечении окружности с горизонталью с Bulge=1.
    /// </summary>
    [DataMember]
    public VertexData[] Vertices { get; set; }

    internal bool
    IsNull => Vertices is null || Vertices.Length < 2
      || (Vertices.Length == 2 && Vertices[0].DistanceTo(Vertices[1]) < STol.EqPoint);

    public
    PLineData()
    { }

    /// <summary>
    /// Создать прямую линию
    /// </summary>
    public
    PLineData(VertexData p1, VertexData p2)
    {
      Vertices = new VertexData[2] { p1, p2 };
      Closed = false;
    }

    /// <summary>
    /// Создать 4х-угольник или полилинию из 3х сегментов
    /// </summary>
    /// <param name="closed">замкнутый четырехугольник</param>
    public
    PLineData(VertexData p1, VertexData p2, VertexData p3, VertexData p4, bool closed)
    {
      Vertices = new VertexData[4] { p1, p2, p3, p4 };
      Closed = closed;
    }

    public Curve
    CreateCurve(Database db, Transaction tr)
    {
      if (IsNull) return null;
      PSegmentColl pline = new();
      for (int i = 0; i < Vertices.Length - 1; i++)
        pline.Add(new PSegment(Vertices[i].ToPoint(), Vertices[i + 1].ToPoint(), Vertices[i].Bulge, ObjectId.Null));
      if (Closed) 
        pline.Add(new PSegment(Vertices[Vertices.Length - 1].ToPoint(), Vertices[0].ToPoint(), Vertices[Vertices.Length - 1].Bulge, ObjectId.Null));
      Curve curve = pline.ToCurve(STol.Liner);
      if (curve is null)
      {
        Cns.Info(BoxFromTableL.PLineConversionErr);
        return null;
      }
      curve.SetDatabaseDefaults(db);
      if (!IsNullOrWhiteSpace(Layer))
      {
        LayerManager lm = new(db, tr);
        ObjectId layerId = lm.GetOrCreate(Layer, LayerEnum.Visible);
        if (!layerId.IsNull) curve.LayerId = layerId;
      }
      if (!IsNullOrWhiteSpace(Color))
        if (ColorExt.TryParseColor(Color, out Color color)) curve.Color = color;
        else Cns.Info(BoxFromTableL.ColorErr, Color);
      if (!IsNullOrWhiteSpace(LineType))
      {
        ObjectId lineTypeId = EntityExt.TryParseLinetype(LineType, db, tr);
        if (lineTypeId.IsNull) Cns.Info(BoxFromTableL.LineTypeErr, LineType);
        else curve.LinetypeId = lineTypeId;
      }
      if (!double.IsNaN(LineWeight) && LineWeight >= 0)
        try
        {
          LineWeight w = (LineWeight)(int)(LineWeight * 100);
          curve.LineWeight = w;
        }
        catch { Cns.Info(BoxFromTableL.LineWeightErr, LineWeight); }
      return curve;
    }

    public override string
    ToString() => IsNull ? "Null" : $"{Vertices.Length} {Vertices[0]} - {Vertices[Vertices.Length - 1]}";

  }

  //================================================== VertexData ====================================================================
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  VertexData
  {
    [DataMember]
    public double
    X
    { get; set; }

    [DataMember]
    public double
    Y
    { get; set; }

    /// <summary>
    /// Выпуклость сегмента после этого вертекса. 
    /// У последнего вертекса незамкнутой полилинии - не имеет смысла.
    /// 0 - линейный сегмент.
    /// >0 - дуга против часовой стрелки, <0 - по часовой. 
    /// 1 - дуга в 180 градусов.
    /// Вычисляется как Bulge = Tan(arc / 4), где arc - угол дуги в радианах.
    /// Радиус кривизны дуги Radius = Length / (2.0 * Sin(Atan(Abs(Bulge)) * 2.0)), где Length - расстояние от начала до конца сегмента по прямой
    /// Автокад не понимает Bulge меньше чем 1e-6
    /// </summary>
    [DataMember]
    public double
    Bulge
    { get; set; }

    public
    VertexData()
    { }

    public
    VertexData(double x, double y, double bulge = 0.0)
    { X = x; Y = y; Bulge = bulge; }

    internal double
    DistanceTo(VertexData other) => Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));

    public override string
    ToString() => $"{X.ApproxSize()};{Y.ApproxSize()}" + (Bulge == 0 ? "" : $" ᴖ{Bulge.Approx(6)}");

    public Point2d
    ToPoint() => new(X, Y);

  }




}
