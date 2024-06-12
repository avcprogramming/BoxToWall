// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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

#if !RENT
[assembly: Rt.CommandClass(typeof(AVC.BoxFromTableCmd))]
#endif
namespace AVC
{
  public static class 
  BoxFromTableCmd
  {
#if !RENT
    [Rt.CommandMethod(InitializationPlugin.commandGroup, DbCommand.BoxFromTableCmdName, CommandFlags.Redraw)]
#endif
    public static void
    BoxFromTable()
    {
      try
      {
        Document doc = CadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        Database db = doc.Database;
        Editor ed = doc.Editor;
        Transient.Clear();
        AvcManager.StartCash();
        int count = 0;





        doc.ClearSelection(); // Очистка выделения 
        if (count > 0)
        {
#if !BRICS
          doc.Database.EvaluateFields(); // метод отсутствует в BricsCAD
#endif
          ed.Regen();
          ed.UpdateScreen();
          Cns.Info(BUpdateL.Done, count);
          //doc.SendStringToExecute("_REGENALL ", true, false, false);
        }
        else Cns.Info(BUpdateL.NoOne);
      }
      catch (CancelException ex) { Cns.CancelInfo(ex.Message); }
      catch (WarningException ex) { Cns.Warning(ex.Message); }
      catch (System.Exception ex) { Cns.Err(ex); }
    }


  }
}
