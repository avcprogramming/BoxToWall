// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

// Ignore Spelling: Vertices

using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;

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
    [DataMember]
    public bool Closed { get; set; }

    [DataMember]
    public string Color { get; set; }

    [DataMember]
    public string Layer { get; set; }

    [DataMember]
    public string LineType { get; set; }

    [DataMember]
    public double LineWeight { get; set; }

    [DataMember]
    public VertexData[] Vertices { get; set; }

    internal bool
    IsNull => Vertices is null || Vertices.Length < 2
      || (Vertices.Length == 2 && Vertices[0].DistanceTo(Vertices[1]) < STol.EqPoint);

    public
    PLineData() { }

    public
    PLineData(VertexData p1, VertexData p2)
    {
      Vertices = new VertexData[2] { p1, p2 };
      Closed = false;
    }

    public
    PLineData(VertexData p1, VertexData p2, VertexData p3, VertexData p4, bool closed)
    {
      Vertices = new VertexData[4] { p1, p2, p3, p4 };
      Closed = closed;
    }

    public override string
    ToString() => IsNull ? "Null" : $"{Vertices.Length} {Vertices[0]} - {Vertices[Vertices.Length-1]}";

  }

  //================================================== VertexData ====================================================================
  [DataContract]
  [Obfuscation(Exclude = true, Feature = "renaming")]
  public class
  VertexData
  {
    [DataMember]
    public double 
    X { get; set; }

    [DataMember]
    public double 
    Y { get; set; }

    [DataMember]
    public double 
    Bulge { get; set; }

    public
    VertexData()
    { }

    public
    VertexData(double x, double y, double bulge)
    { X = x; Y = y; Bulge = bulge; }

    internal double
    DistanceTo(VertexData other) => Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));

    public override string
    ToString() => $"{X.ApproxSize()};{Y.ApproxSize()} Bulge={Bulge.Approx(6)}";

  }




}
