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
  /// Вспомогательный объект для получения с web-сервера данных о размерах, десериализации из JSON 
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  DimensionData
  {
    [DataMember]
    public string 
    Style { get; set; }

    [DataMember]
    public double 
    FromX { get; set; }

    [DataMember]
    public double 
    FromY { get; set; }

    [DataMember]
    public double 
    ToX { get; set; }

    [DataMember]
    public double 
    ToY { get; set; }

    [DataMember]
    public double 
    DimLineX { get; set; }

    [DataMember]
    public double 
    DimLineY { get; set; }

    [DataMember]
    public double 
    Rotation { get; set; }

    /// <summary>
    /// Масштаб размеров для вывода правильных цифр (если задан текст, то нет смысла указывать масштаб).
    /// 0 - это 1:1
    /// </summary>
    [DataMember]
    public double 
    Scale { get; set; }

    /// <summary>
    /// Текст размера. Если пустой - будет обмеренное число. Может содержать <> в позиции куда надо вставить размер.
    /// </summary>
    [DataMember]
    public string 
    Text { get; set; }

    /// <summary>
    /// Высота текста. Если 0, то берем как задано в стиле
    /// </summary>
    [DataMember]
    public double 
    Height { get; set; }

    [DataMember]
    public string 
    Color { get; set; }

    /// <summary>
    /// Слой по умолчанию - Аннотации
    /// </summary>
    [DataMember]
    public string 
    Layer { get; set; }

    internal double
    Measure => Sqrt((FromX - ToX) * (FromX - ToX) + (FromY - ToY) * (FromY - ToY));

    internal bool
    IsNull => Measure < STol.EqPoint;

    public 
    DimensionData() { }

    public
    DimensionData(double fromX, double fromY, double toX, double toY, double dimLineX, double dimLineY, double rotation)
    { 
      FromX = fromX;
      FromY = fromY;
      ToX = toX;
      ToY = toY;
      DimLineX = dimLineX;
      DimLineY = dimLineY;
      Rotation = rotation;
    }

    public Dimension 
    CreateDimension(Database db, Transaction tr)
    {
      DimStyleManager dm = new(db, tr, AvcSettings.TemplateFile);
      LayerManager lm = new(db, tr);
      RotatedDimension dim = new()
      {
        XLine1Point = new Point3d(FromX, FromY, 0),
        XLine2Point = new Point3d(ToX, ToY, 0),
        Rotation = Rotation/180.0*PI,
        DimLinePoint = new Point3d(DimLineX, DimLineY, 0)
      };
      dim.SetDatabaseDefaults();
      dm.SetDimStyle(dim, Style);
      if (Scale > 0.0) dim.Dimlfac /= Scale; // в стиле может быть свой масштаб
      if (!IsNullOrWhiteSpace(Text)) dim.DimensionText = Text;
      if (Height > STol.EqPoint) dim.Dimtxt = Height;
      dim.LayerId = IsNullOrWhiteSpace(Layer) ? lm.GetOrCreate(LayerEnum.Annotation) : lm.GetOrCreate(Layer, LayerEnum.Annotation);
      if (!IsNullOrWhiteSpace(Color))
        if (ColorExt.TryParseColor(Color, out Color color)) dim.Color = color;
        else Cns.Info(BoxFromTableL.ColorErr, Color);
      return dim;
    }

    public override string
    ToString() => IsNull ? "Null" : IsNullOrWhiteSpace(Text) ? Measure.ToFStr() : Text;

  }
}
