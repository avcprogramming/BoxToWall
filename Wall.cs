// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;
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

  internal enum WallTarget
  {
    WallToBox, 
    WallToVector
  }

  /// <summary>
  /// Вспомогательный класс для сериализации исходных данных команды BoxToWall
  /// </summary>
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  internal class
  Wall
  {
    [DataMember]
    public string
    Target { get; set; } = "WallToBox";

    [DataMember]
    public double
    Height { get; set; }

    [DataMember]
    public double
    Width { get; set; }

    [DataMember]
    public double
    Thickness { get; set; }

    [DataMember]
    public double
    Frame { get; set; }

    [DataMember]
    public double
    Front { get; set; }

    [DataMember]
    public double
    Back { get; set; }

    [DataMember]
    public string
    FrameMat { get; set; }

    [DataMember]
    public string
    FrontMat { get; set; }

    [DataMember]
    public string
    BackMat { get; set; }

    [DataMember]
    public string 
    Name { get; set; }

    [DataMember]
    public string
    Kind  { get; set; }

    /// <summary>
    /// матрица разворота этой стены из положения выкладки (лежа) в вертикальное положение по осям WCS
    /// </summary>
    internal Matrix3d
    StandUp;

    /// <summary>
    /// матрица разворота этой стены из вертикальное положение по осям WCS в положения выкладки лежа с разворотом наибольшей стороной вдоль X
    /// </summary>
    internal Matrix3d
    LayDown;

    public
    Wall()
    { }

    internal
    Wall(WallTarget target, AvcSolid solid, double frame, double front, double back)
    {
      if (solid == null) return;

      if (target == WallTarget.WallToVector) Target = "WallToVector";
      else Target = "WallToBox";
      Height = solid.Metric.Length;
      Width = solid.Metric.Width;
      Thickness = solid.Metric.Thickness;
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

      Frame = frame;
      Front = front;
      Back = back;
      FrameMat = "";
      FrontMat = "";
      BackMat = "";
      Name = solid.Name;
      Kind = solid.Kind;

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
            Front = face.Material.Thickness;
            FrontMat = face.Material.Name;
          }
          else if (face.Dir == AvcSolidFace.Direction.Rear && face.Material is not null && !face.Material.IsBuiltIn)
          {
            Back = face.Material.Thickness;
            BackMat = face.Material.Name;
          }
      }

    }
  }
}
