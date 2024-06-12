// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.Reflection;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;

namespace AVC
{
  /// <summary>
  /// Вспомогательный обьект для получения с web-сервера данных о текстах, десериализации из JSON 
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

    public override string
    ToString() => IsNull ? "Null" : $"{Name} {PLines?.Length}|{Texts?.Length}|{Dimensions?.Length}";

  }
}
