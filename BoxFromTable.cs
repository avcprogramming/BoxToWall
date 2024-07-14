// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static System.String;
using static System.Math;
using Ex = OfficeOpenXml;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Colors;
using Bricscad.EditorInput;
using Teigha.Geometry;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
using Ofd = Bricscad.Windows.OpenFileDialog;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Rt = Autodesk.AutoCAD.Runtime;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Ofd = Autodesk.AutoCAD.Windows.OpenFileDialog;
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
    [Rt.CommandMethod(InitializationPlugin.commandGroup, DbCommand.BoxFromTableCmdName, Rt.CommandFlags.Redraw)]
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
#if DEMO
        CnsAcad.licChecker.ActivationIfTime();
#endif
#if FREEWAR
        Donation.AskForDonation(true);
#endif
#if RENT || VARS
        DocExt.ThirdTest = db.BlockTableId; // проверка  в AssemblyStyle.NewName
        if (!LicenseCheck.HasLicenseFor(DbCommand.BoxFromTableCmdName))
          throw new CancelException("");
#endif
        // Чтение файла
        BoxFromTableStyle style = BoxFromTableStyle.GetCurrent();
        string file = style.Flags.HasFlag(CreateBoxEnum.RequestFile) || !File.Exists(style.File) ?
          RequestFile(style) : style.File;
        if (file is null) return;
        string ext = Path.GetExtension(file).ToUpper();
        IEnumerable<BoxData> boxes;
        if (ext == ".CSV" || ext == ".TSV" || ext == ".TXT")
          boxes = ReadCSV(file, style);
        else if (ext == ".XLS" || ext == ".XLSX" || ext == ".XLSM")
          boxes = ReadXLS(file, style);
        else if (ext == ".JSON")
          boxes = ReadJSON(file);
        else
        {
          Cns.Warning(FileL.ExtensionError, ext);
          return;
        }
        if (boxes is null) { Cns.NothingInfo(); return; }

        // Группировка по блокам/группам
        Dictionary<string, List<BoxData>> groups = BoxData.Grouping(boxes);
        if (groups.Count == 0) { Cns.NothingInfo(); return; }

        // Создание солидов и блоков
        int count;
        List<ObjectId> btrs;
        using (LongOperationManager lom = new(BoxFromTableL.CreateBoxes, boxes.Count() + groups.Count))
        {
          count = BoxData.CreateWalls(groups, style.Flags, Matrix3d.Identity, db, out btrs);
        }

        // Выставка блоков
        if (style.Flags.HasFlag(CreateBoxEnum.Expose) && btrs.Count > 0) 
          ExposeBlocks(btrs, db);

        doc.PermitClearSelection(); // Очистка выделения 
        if (count > 0)
        {
#if !BRICS
          doc.Database.EvaluateFields(); // метод отсутствует в BricsCAD
#endif
          ed.Regen();
          ed.UpdateScreen();
          Cns.Info(BoxFromTableL.Done, count);
          //doc.SendStringToExecute("_REGENALL ", true, false, false);
        }
        else Cns.Info(BUpdateL.NoOne);
      }
      catch (CancelException ex) { Cns.CancelInfo(ex.Message); }
      catch (WarningException ex) { Cns.Warning(ex.Message); }
      catch (System.IO.IOException ex) { Cns.Warning(ex.Message); }
      catch (System.Exception ex) { Cns.Err(ex); }
    }

    private static Ofd
    _dlg;

    private static string
    RequestFile(BoxFromTableStyle style)
    {
      if (_dlg is null)
        _dlg = new(Cns.Local(BoxFromTableL.SelectFile), style.File, "csv;tsv;xls;xlsx;xlsm;json",
          Cns.Local(BoxFromTableL.DialogTitle), Ofd.OpenFileDialogFlags.AllowAnyExtension);
      if (_dlg.ShowDialog() != DialogResult.OK) return null;

      if (!File.Exists(_dlg.Filename))
      { Cns.FileNotFoundWarning(_dlg.Filename); return null; }
      style.File = _dlg.Filename;
      return _dlg.Filename;
    }

    private static BoxData[]
    ReadJSON(string file)
    {
      if (IsNullOrWhiteSpace(file)) return null;
      Cns.Info(FileL.ReadingFile, file);
      string json = File.ReadAllText(file);
      return WebServices.DeserializeFromJson<BoxData[]>(json);
    }

    private static List<BoxData>
    ReadXLS(string file, BoxFromTableStyle style)
    {
      if (IsNullOrWhiteSpace(file)) return null;
      using LongOperationManager lom = new(Cns.Local(FileL.ReadingFile, file), 100, true);
      lom.TickOrEsc();
      using Ex.ExcelPackage pack = new(new FileInfo(file));
      lom.TickOrEsc();
      using Ex.ExcelWorkbook wb = pack.Workbook;
      lom.TickOrEsc();
      Ex.ExcelWorksheet sheet;
      if (IsNullOrWhiteSpace(style.Page)) sheet = wb.Worksheets[0];
      else
      {
        sheet = wb.Worksheets[style.Page];
        if (sheet is null)
        {
          Cns.Info(BoxFromTableL.PageNotExists, style.Page);
          sheet = wb.Worksheets[0];
        }
      }

      int rowCount = 0;
      List<BoxData> boxes = new();

      for (int row = 1; row <= sheet.Cells.Rows; row++) // Обычно sheet.Cells.Rows - огромное число. Надо искать пустую строку
      {
        lom.TickOrEsc();
        rowCount++;
        if (sheet.Row(row).Collapsed) continue;
        List<object> columns = new();
        for (int col = 1; col <= sheet.Cells.Columns && col <= BoxData.ColumnCount; col++)
          if (sheet.Cells[row, col].Merge) { columns = null; break; } // объединенные ячейки - заголовок
          else if (sheet.Cells[row, col].Value is null) columns.Add("");
          else columns.Add(sheet.Cells[row, col].Value);
        if (columns is null) continue;
        if (columns.Count == 0 || columns[0] is null || columns[0].ToString() == "") break; // обрываем на первой пустой строке
        BoxData box = new(columns.ToArray());
        if (!box.AllowedShape)
        {
          if (boxes.Count > 0) Cns.Info(BoxFromTableL.SkipsLine, box.Shape);
          continue;
        }
        if (!box.IsNullMessage()) boxes.Add(box);
      }

      Cns.Info(BoxFromTableL.TableRowCount, rowCount - 1, boxes.Count);
      return boxes;
    }

    private const string
    Protector = "ˌ"; // это не запятая, а экзотический символ маленькой нижней черты

    private static List<BoxData>
    ReadCSV(string file, BoxFromTableStyle createBoxStyle)
    {
      if (IsNullOrWhiteSpace(file)) return null;
      using LongOperationManager lom = new(Cns.Local(FileL.ReadingFile, file), 100, true);
      lom.TickOrEsc();
      string text = File.ReadAllText(file).Replace("/" + createBoxStyle.Separator, Protector); // предотвращение использования запятой после слеша как разделителя колонок
      string[] splitter = new string[] { createBoxStyle.Separator };
      string[] lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
      List<BoxData> boxes = new();

      foreach (string line in lines)
      {
        lom.TickOrEsc();
        string[] columns = line.Split(splitter, StringSplitOptions.None);
        for (int i = 0; i < columns.Length; i++)
          columns[i] = columns[i].Replace(Protector, createBoxStyle.Separator); // возврат запятых внутри колонок
        BoxData box = new(columns);
        if (!box.AllowedShape)
        {
          if (boxes.Count > 0) Cns.Info(BoxFromTableL.SkipsLine, box.Shape);
          continue;
        }
        if (!box.IsNullMessage()) boxes.Add(box);
      }

      Cns.Info(BoxFromTableL.TableRowCount, lines.Length, boxes.Count);
      return boxes;
    }

    private static void
    ExposeBlocks(List<ObjectId> btrs, Database db)
    {
#if RENT || VARS
      DocExt.ThirdTest = db.BlockTableId; // защита в Expose.ExposeAssembly(AvcDTPart asm
      if (!LicenseCheck.HasLicenseFor("Expose"))
        throw new CancelException("");
#endif

      ExposeStyle exposeStyle = ExposeStyle.GetCurrent();
      EntFilterEnum oldFilter = exposeStyle.Filter.Flags;
      try
      {
        // отключаем все фильтрации, чтоб нечаянно не отфильтровать сборки
        exposeStyle.Filter.Flags = EntFilterEnum.BlockScaled; // не считаем количество блоков в модели - количество всегда 1.
        int multiplier = DataTableStyle.MultiplyRequest(exposeStyle.DTFlags); // получить множитель
        Expose expose = new(exposeStyle, multiplier, db);
        PointOfView pv = PointOfView.WCS;

        db.UpdateExt(false);
        Extents3d ext = db.DrawingSize(db.Inch() ? 80 : 2000);
        double gap = exposeStyle.ActualFrameSpace * 2.0;
        if (gap < STol.EqPoint) gap = ext.SizeX() * 0.1;
        Point3d insert = new(ext.MaxPoint.X + gap, ext.MinPoint.Y - gap, 0);

        List<ObjectId> blockRefIds = new();
        using LongOperationManager lom = new(CommandL.Expose, btrs.Count);
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
          BlockManager bm = new(db, tr, AvcSettings.TemplateFile);
          foreach (ObjectId blockId in btrs)
          {
            lom.TickOrEsc();
            BlockReference br = bm.CreateBlockReference(blockId, insert);
            if (br != null)
            {
              br.Layer = "0"; // чтоб нечаянно не отфильтровать по текущему слою
              ObjectId brId = br.SaveEnt(db);
              if (!brId.IsNull) blockRefIds.Add(brId);
            }
          }
          tr.Commit();
        }

        if (blockRefIds.Count > 0)
        {
          expose.ExposeAssemblies(blockRefIds.ToSelectedObjects(), insert, pv);
          blockRefIds.EraseAll();
        }

      }
      finally { exposeStyle.Filter.Flags = oldFilter; }
    }



  }
}
