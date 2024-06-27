// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;
using System;

#if BRICS
using Teigha.Geometry;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.Geometry;
using Rt = Autodesk.AutoCAD.Runtime;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AVC
{

  /// <summary>
  /// назначение обращения к веб-серверу, имя вызвавшей команды. 
  /// </summary>
  internal enum WallTarget
  {
    WallToBox, 
    WallToVector
  }

  /// <summary>
  /// Вспомогательный класс для сериализации исходных данных команды BoxToWall или WallToVector
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  internal class
  Wall
  {

    /// <summary>
    /// назначение обращения к веб-серверу, имя вызвавшей команды. 
    /// Пока только WallToBox или WallToVector
    /// </summary>
    [DataMember]
    public string
    Target { get; set; } = "WallToBox";

    /// <summary>
    /// Текущий выбраный язык плагинов на момент обращения к веб-серверу. 
    /// En, Ru, It, Ge, Zh
    /// </summary>
    [DataMember]
    public string
    Local
    { get; set; } 

    /// <summary>
    /// размер стены (исходного бокса) по Z
    /// </summary>
    [DataMember]
    public double
    Height { get; set; }

    /// <summary>
    /// Наибольший из размеров основания стены
    /// </summary>
    [DataMember]
    public double
    Width { get; set; }

    /// <summary>
    /// Наименьший из размеров основания стены. 
    /// Если толщина путается с шириной, надо обозначить фасадную сторону цветом 30 (есть галочка в палитре свойств AVC)
    /// </summary>
    [DataMember]
    public double
    Thickness { get; set; }

    /// <summary>
    /// Толщина материала-каркаса стены. Берется у материала исходгого солида-бокса. 
    /// Можно назначить только через Палитру Свойств AVC в свойствах материала.
    /// Если не задан материал, то по умолчанию -1 (веб-сервер пусть сам решает какую толщину делать)
    /// </summary>
    [DataMember]
    public double
    Frame { get; set; }

    /// <summary>
    /// Толщина материала передней облицовки стены. 
    /// Берется у материала передней грани исходгого солида-бокса. 
    /// Можно назначить только через Палитру Свойств AVC в свойствах покрытия.
    /// Если 0 - надо оставить каркас без облицовки
    /// Если не задан материал, то по умолчанию -1 (веб-сервер пусть сам решает какую толщину делать)
    /// </summary>
    [DataMember]
    public double
    Front { get; set; }

    /// <summary>
    /// Толщина материала задней облицовки стены. 
    /// Берется у материала задней грани исходгого солида-бокса. 
    /// Можно назначить только через Палитру Свойств AVC в свойствах покрытия.
    /// Если 0 - надо оставить каркас без облицовки
    /// Если не задан материал, то по умолчанию -1 (веб-сервер пусть сам решает какую толщину делать)
    /// </summary>
    [DataMember]
    public double
    Back { get; set; }

    /// <summary>
    /// Имя материала каркаса
    /// </summary>
    [DataMember]
    public string
    FrameMat { get; set; }

    /// <summary>
    /// Имя материала передней обшивки
    /// </summary>
    [DataMember]
    public string
    FrontMat { get; set; }

    /// <summary>
    /// Имя материала задней обшивки
    /// </summary>
    [DataMember]
    public string
    BackMat { get; set; }

    /// <summary>
    /// Имя исходного солида-бокса (или блока в BoxToVector). 
    /// Если не пустое, то желательно его передать без изменений в PlanData.Name или в BoxData.Owner у всех деталей стены
    /// </summary>
    [DataMember]
    public string 
    Name { get; set; }

    /// <summary>
    /// Тип (Kind) исходного солида-бокса (задается в Палитре Свойств AVC)
    /// </summary>
    [DataMember]
    public string
    Kind  { get; set; }

    /// <summary>
    /// Количество одинаковых солидов-боксов или блоков в исходных данных (не зависимо от ориентации).
    /// Запрос на web-сервер идет только 1 раз для всех одинаковых исходных
    /// </summary>
    [DataMember]
    public int
    Count
    { get; set; }

    /// <summary>
    /// матрица разворота этой стены из положения выкладки (лежа) в вертикальное положение по осям WCS
    /// </summary>
    internal Matrix3d
    StandUp;

    /// <summary>
    /// матрица разворота этой стены из вертикальное положение по осям WCS 
    /// в положения выкладки лежа с разворотом наибольшей стороной вдоль X
    /// </summary>
    internal Matrix3d
    LayDown;

    public
    Wall()
    { }

    internal
    Wall(WallTarget target, AvcEntity ent, int count)
    {
      if (ent is null) return;

      if (target == WallTarget.WallToVector) Target = "WallToVector";
      else Target = "WallToBox";
      Local = Cns.LngCode;

      Frame = -1;
      Front = -1;
      Back = -1;
      FrameMat = "";
      FrontMat = "";
      BackMat = "";
      Count = count;
      Name = DatabaseExt.ValidName(ent.Name);

      if (ent is AvcSolid solid)
      {
        Height = solid.Metric.Length;
        Width = solid.Metric.Width;
        Thickness = solid.Metric.Thickness;
        Kind = solid.Kind;
        StandUp = EntityExt.AlignCSTo(Point3d.Origin, Vector3d.ZAxis, Vector3d.YAxis);
        LayDown = StandUp.Inverse();

        double dZ = solid.PvExtents(PointOfView.WCS).SizeZ();
        if (!double.IsNaN(dZ) && !Height.ApproxEqSize(dZ))
        {
          if (Width.ApproxEqSize(dZ))
          {
            Width = solid.Metric.Length;
            Height = solid.Metric.Width;
            LayDown = EntityExt.AlignCSTo(new Point3d(Width, 0, 0), -Vector3d.XAxis, Vector3d.YAxis);
          }
          else if (Thickness.ApproxEqSize(dZ))
          {
            Thickness = solid.Metric.Width;
            Width = solid.Metric.Length;
            Height = solid.Metric.Thickness;
            LayDown = Matrix3d.Identity;
          }
        }

        AvcMaterial mat = solid.ActualMaterial;
        if (mat is not null && !mat.IsBuiltIn)
        {
          if (mat.Thickness > 0) Frame = mat.Thickness;
          FrameMat = mat.Name;
        }

        if (solid.Metric.HasCovers)
        {
          foreach (AvcSolidFace face in solid.Metric.Covers)
            if (face.Dir == AvcSolidFace.Direction.Front && face.Material is not null && !face.Material.IsBuiltIn)
            {
              if (face.Material.Thickness > 0) Front = face.Material.Thickness;
              FrontMat = face.Material.Name;
            }
            else if (face.Dir == AvcSolidFace.Direction.Rear && face.Material is not null && !face.Material.IsBuiltIn)
            {
              if (face.Material.Thickness > 0) Back = face.Material.Thickness;
              BackMat = face.Material.Name;
            }
        }
      }
      else if (ent is AvcBlockRef block)
      {
        Height = block.BTR.Size.Z;
        Width = block.BTR.Size.X;
        Thickness = block.BTR.Size.Y;
        if (block.BTR.ConstAttributes.TryGetValue("Kind", out AvcAttribute att))
          Kind = att.TextString;
        if (block.BTR.ConstAttributes.TryGetValue("FrameMat", out att))
          FrameMat = att.TextString;
        if (block.BTR.ConstAttributes.TryGetValue("FrontMat", out att))
          FrontMat = att.TextString;
        if (block.BTR.ConstAttributes.TryGetValue("BackMat", out att))
          BackMat = att.TextString;
        if (block.BTR.ConstAttributes.TryGetValue("Frame", out att) &&
          AvcSettings.LenStyle.ParseDistance(att.TextString, out double x))
          Frame = x;
        if (block.BTR.ConstAttributes.TryGetValue("Front", out att) &&
          AvcSettings.LenStyle.ParseDistance(att.TextString, out x))
          Front = x;
        if (block.BTR.ConstAttributes.TryGetValue("Back", out att) &&
          AvcSettings.LenStyle.ParseDistance(att.TextString, out x))
          Back = x;
        StandUp = Matrix3d.Identity;
        LayDown = Matrix3d.Identity;
      }
    }

    public override string
    ToString() => $"{Name} {Height.ApproxSize()}x{Width.ApproxSize()}x{Thickness.ApproxSize()}";

  }
}
