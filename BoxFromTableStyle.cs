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
  internal class 
  BoxFromTableStyle : StyleParent
  {
    public const string
    BoxFromTableStyleRegKey = "AVC_BoxFromTable";

    //===========================================================================================================

    public
    BoxFromTableStyle(string Key) : base(BoxFromTableStyleRegKey, Key)
    {
      StyleNames = BoxFromTableL.BoxFromTableNames;
    }

  }
}
