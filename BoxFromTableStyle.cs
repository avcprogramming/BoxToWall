// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Collections.Generic;
using static System.String;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Bricscad.EditorInput;
using Teigha.Geometry;
using Teigha.Runtime;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Rt = Autodesk.AutoCAD.Runtime;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AVC
{
  [Flags]
  internal enum
  CreateBoxEnum
  {
    MakeBlock = 1 << 0,
    MakeGroup = 1 << 1,
    RequestFile = 1 << 2,
    Drill = 1 << 3,
    Expose = 1 << 4,

    Default = MakeBlock | RequestFile | Drill | Expose,
    NotLoaded = 1 << 31
  }

  internal class 
  BoxFromTableStyle : StyleParent
  {
    public const string
    BoxFromTableRegKey = "AVC_BoxFromTable";

    public const string
    DefServerAddress = "http://acadapi.vars.ru";

    //===========================================================================================================
    #region Поля

    private CreateBoxEnum 
    _flags = CreateBoxEnum.NotLoaded;

    private string
    _file;

    private string
    _page;

    private string
    _separator;

    private static string
    _serverAddress = null;

    #endregion

    //===========================================================================================================
    #region свойства

    public CreateBoxEnum
    Flags
    { 
      get => _flags = RegRead("Flags", CreateBoxEnum.Default, CreateBoxEnum.NotLoaded, _flags);
      set => _flags = RegWrite("Flags", CreateBoxEnum.Default, CreateBoxEnum.NotLoaded, value); 
    }

    public string 
    File
    {
      get => _file = RegRead("File", "", _file);
      set => _file = RegWrite("File", "", value, true);
    }

    public string
    Page
    {
      get => _page = RegRead("Page", "Parts", _page);
      set => _page = RegWrite("Page", "Parts", value, true);
    }

    public string
    Separator
    {
      get => _separator = RegRead("Separator", ",", _separator);
      set => _separator = RegWrite("Separator", ",", value, false);  
    }

    public string
    ServerAddress
    {
      get => _serverAddress = RegRead("ServerAddress", DefServerAddress, _serverAddress);
      set => _serverAddress = RegWrite("ServerAddress", DefServerAddress, value, false);
    }

    #endregion

    //===========================================================================================================

    public
    BoxFromTableStyle(string Key) : base(BoxFromTableRegKey, Key)
    {
      StyleNames = BoxFromTableL.BoxFromTableNames;
    }

    //===================================================================================================================
    #region статические методы
    /// <summary>
    /// Текущий набор настроек
    /// </summary>
    /// <returns></returns>
    public static BoxFromTableStyle
    GetCurrent()
    {
      StyleManager sm = new(BoxFromTableRegKey);
      if (sm.FirstRun)
      {
        BoxFromTableStyle m = new("1"); m.SetDefault();
        //m = new BoxFromTableStyle("2"); m.SetDefault();
        //m = new BoxFromTableStyle("3"); m.SetDefault();
        //m = new BoxFromTableStyle("4"); m.SetDefault();
        sm.CurrentKey = "1";
      }
      return new BoxFromTableStyle(sm.CurrentKey);
    }
    #endregion

    //===================================================================================================================
    #region Методы

    /// <summary>
    /// Сбрасывает все поля на дефолты
    /// </summary>
    public override void
    SetDefault()
    {
      Name = DefName;
      Flags = CreateBoxEnum.Default;
      File = "";
      Separator = ",";
      Page = "Parts";
    }


    #endregion

  }
}
