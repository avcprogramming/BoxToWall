﻿// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

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
  /// Вспомогательный объект для получения с web-сервера данных о текстах, десериализации из JSON 
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  TextData
  {

    /// <summary>
    /// Текстовый стиль. Если не существет - программа попытается вытащить такой стиль из шаблона.
    /// Если не задан - бует текущий стиль чертежа.
    /// </summary>
    [DataMember]
    public string 
    Content { get; set; }

    [DataMember]
    public string 
    Style { get; set; }

    /// <summary>
    /// Точка вставки текста (левый нижний угол)
    /// </summary>
    [DataMember]
    public double 
    X { get; set; }

    [DataMember]
    public double 
    Y { get; set; }

    /// <summary>
    /// Размер символов
    /// </summary>
    [DataMember]
    public double 
    Height { get; set; }

    /// <summary>
    /// Поворот текста. Градусы
    /// </summary>
    [DataMember]
    public double 
    Rotation { get; set; }

    /// <summary>
    /// Создавать рамку вогруг текста
    /// </summary>
    [DataMember]
    public bool 
    Frame { get; set; }

    /// <summary>
    /// Имя цвета (как он отображается в Палитре Свойств AVC). 
    /// По умолчанию - текущий цвет чертежа.
    /// </summary>
    [DataMember]
    public string 
    Color { get; set; }

    /// <summary>
    /// Слой по умолчанию - Аннотации. 
    /// Если слоя нет в чертеже - программа попытается вытащить его из шаблона или создаст новый.
    /// </summary>
    [DataMember]
    public string 
    Layer { get; set; }

    internal bool 
    IsNull => IsNullOrWhiteSpace(Content);

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
      text.Location = new Point3d(X, Y, 0);
      text.Rotation = Rotation/180*PI;

      if (!IsNullOrWhiteSpace(Layer))
      {
        LayerManager lm = new(db, tr);
        ObjectId layerId = lm.GetOrCreate(Layer, LayerEnum.Visible);
        if (!layerId.IsNull) text.LayerId = layerId;
      }

      if (!IsNullOrWhiteSpace(Color))
        if (ColorExt.TryParseColor(Color, out Color color)) text.Color = color;
        else Cns.Info(BoxFromTableL.ColorErr, Color);

      return text;
    }

    public override string 
    ToString() => IsNull ? "Null" : $"{Content} {X.ApproxSize()};{Y.ApproxSize()}";

  }
}
