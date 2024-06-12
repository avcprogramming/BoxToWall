// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;
using System;

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
  /// Вспомогательный обьект для получения с web-сервера данных о текстах, десериализации из JSON 
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  TextData
  {
    [DataMember]
    public string Content { get; set; }

    [DataMember]
    public string Style { get; set; }

    [DataMember]
    public double X { get; set; }

    [DataMember]
    public double Y { get; set; }

    [DataMember]
    public double Height { get; set; }

    [DataMember]
    public double Rotation { get; set; }

    [DataMember]
    public bool Frame { get; set; }

    [DataMember]
    public string Color { get; set; }

    /// <summary>
    /// Слой по умолчанию - Аннотации
    /// </summary>
    [DataMember]
    public string Layer { get; set; }

    internal bool IsNull => IsNullOrWhiteSpace(Content);

    public TextData() { }

    public TextData(double x, double y, string content) 
    { 
      X = x; Y = y; Content = content;
    }

    public MText
    CreateText(Database db, Transaction tr)
    {
      if (IsNullOrEmpty(Content)) return null;
      MText text = MTextExt.CreateText(Content, Style, Height, Frame, db, tr);
      if (text is null) return null;
      if (IsNullOrEmpty(Style)) text.Attachment = AttachmentPoint.BottomLeft;
      text.Rotation = Rotation;

      if (!IsNullOrWhiteSpace(Layer))
      {
        LayerManager lm = new(db, tr);
        ObjectId layerId = lm.GetOrCreate(Layer, LayerEnum.Visible);
        if (!layerId.IsNull) text.LayerId = layerId;
      }

      if (ColorExt.TryParseColor(Color, out Color color)) text.Color = color;
      return text;
    }

    public override string 
    ToString() => IsNull ? "Null" : $"{Content} {X.ApproxSize()};{Y.ApproxSize()}";

  }
}
