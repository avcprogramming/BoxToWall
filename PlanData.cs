// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Bricscad.EditorInput;
using Teigha.Geometry;
using Teigha.Runtime;
using CadApp = Bricscad.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AVC
{
  /// <summary>
  /// Вспомогательный объект для получения с web-сервера данных о плоском чертеже: его имя и набор примитивов
  /// Для десериализации из JSON 
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  PlanData
  {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public PLineData[] PLines { get; set; }

    [DataMember]
    public TextData[] Texts { get; set; }

    [DataMember]
    public DimensionData[] Dimensions { get; set; }

    internal bool IsNull => (PLines is null || PLines.Length == 0) && 
      (Texts is null || Texts.Length == 0) && 
      (Dimensions is null || Dimensions.Length == 0);

    public PlanData() { }

    public List<Entity>
    CreateEntities(Database db, Transaction tr)
    {
      List<Entity> ret = new();
      if (IsNull || db is null || tr is null) return ret;
      foreach (PLineData pline in PLines)
      {
        Curve curve = pline.CreateCurve(db, tr);
        if (curve is not null) ret.Add(curve);
      }
      foreach (TextData text in Texts)
      {
        MText mt = text.CreateText(db, tr);
        if (mt is not null) ret.Add(mt);
      }
      foreach (DimensionData dim in Dimensions)
      {
        Dimension d = dim.CreateDimension(db, tr);
        if (d is not null) ret.Add(d);
      }
      return ret;
    }

    public override string
    ToString() => IsNull ? "Null" : $"{Name} {PLines?.Length}|{Texts?.Length}|{Dimensions?.Length}";

  }
}
