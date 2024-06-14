// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

// Ignore Spelling: Json Deserialize

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Runtime.Serialization;
using static System.String;
using static System.Math;
using System.Diagnostics;

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
[assembly: Rt.CommandClass(typeof(AVC.BoxToWallCmd))]
#endif
namespace AVC
{
  /// <summary>
  /// Команда  Подмена солида-бокса на блок - секцию стены
  /// </summary>
  public static class
  BoxToWallCmd
  {
    internal const string
    RegKey = "AVC_BoxToWall";

    internal const string
    PlanPostfix = "_2d";

    #region Дефолтные настройки

    public const string
    DefServerAddress = "http://acadapi.vars.ru";

    public const double
    DefFrame = -1;

    public const double
    DefFront = -1;

    public const double
    DefBack = -1;

    #endregion

    //=============================================================================================================
    #region Поля

    private static string
    _serverAddress = null;

    private static double
    _frame = double.NaN;

    private static double
    _front = double.NaN;

    private static double
    _back = double.NaN;

    #endregion

    //=============================================================================================================
    #region Настройки в реестре


    public static string
    ServerAddress
    {
      get
      {
        if (_serverAddress == null)
          _serverAddress = Reg.GetCUStr(RegKey, "ServerAddress", DefServerAddress);
        return _serverAddress;
      }
      set
      {
        if (IsNullOrEmpty(value) || value == DefServerAddress)
        {
          _serverAddress = DefServerAddress;
          Reg.DelCUVal(RegKey, "ServerAddress");
        }
        else
        {
          _serverAddress = value;
          Reg.SetCUStr(RegKey, "ServerAddress", value);
        }
      }
    }

    public static double
    Frame
    {
      get
      {
        if (double.IsNaN(_frame))
        {
          _frame = Reg.GetCUDbl(RegKey, "Frame", DefFrame);
        }
        return _frame;
      }
      set
      {
        if (value < STol.EqPoint) _frame = DefFrame;
        else _frame = value;
        if (_frame == DefFrame)
          Reg.DelCUVal(RegKey, "Frame");
        else
          Reg.SetCUDbl(RegKey, "Frame", value);
      }
    }

    public static double
    Front
    {
      get
      {
        if (double.IsNaN(_front))
        {
          _front = Reg.GetCUDbl(RegKey, "Front", DefFront);
        }
        return _front;
      }
      set
      {
        if (value < STol.EqPoint) _front = DefFront;
        else _front = value;
        if (_front == DefFront)
          Reg.DelCUVal(RegKey, "Front");
        else
          Reg.SetCUDbl(RegKey, "Front", value);
      }
    }

    public static double
    Back
    {
      get
      {
        if (double.IsNaN(_back))
        {
          _back = Reg.GetCUDbl(RegKey, "Back", DefBack);
        }
        return _back;
      }
      set
      {
        if (value < STol.EqPoint) _back = DefBack;
        else _back = value;
        if (_back == DefBack)
          Reg.DelCUVal(RegKey, "Back");
        else
          Reg.SetCUDbl(RegKey, "Back", value);
      }
    }

    #endregion
    //=============================================================================================================

#if !RENT
    [Rt.CommandMethod(InitializationPlugin.commandGroup, DbCommand.BoxToWallCmdName, Rt.CommandFlags.UsePickSet | Rt.CommandFlags.Redraw)]
#endif
    public static void
    BoxToWall()
    {
      try
      {
        Document doc = CadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        Database db = doc.Database;
        Editor ed = doc.Editor;
        Transient.Clear();
        AvcManager.StartCash();

        // Запрос списка солидов-боксов
        ObjectId[] objectIds = CnsAcad.Select(SolidL.SelectSolids);
        if (objectIds is null) return;

#if DEMO
        CnsAcad.licChecker.ActivationIfTime();
#endif
#if FREEWAR
        Donation.AskForDonation(true);
#endif
#if RENT || VARS
        DocExt.ThirdTest = db.BlockTableId; // проверка  в AssemblyStyle.NewName
        if (!LicenseCheck.HasLicenseFor(DbCommand.BoxToWallCmdName))
          throw new CancelException("");
#endif

        // Группируем одинаковые боксы в таблицу деталей
        DataTable dt = new(GetColumns(), DTStyleEnum.Default, AvcSettings.LenStyle,
          EntityFilterStyle.SolidTableFilter(RegKey + "\\Filter"), null, ReadEnum.ForceMeter, 1, db, PointOfView.WCS);
        List<AvcDTObj> rawdata = dt.ExtractRawData(objectIds.ToSelectedObjects());
        if (rawdata is null || rawdata.Count == 0)
        {
          Cns.Warning(Cns.Local(CnsL.NothingSucceeded));
          return;
        }
        if (!dt.CreateTable(rawdata)) return;

        // Подправим стиль создания блоков
        AssemblyStyle style = AssemblyStyle.GetCurrent();
        BlockCreateEnum createOpt = style.CreateOptions &
          ~(BlockCreateEnum.LayerZero | BlockCreateEnum.MeshToSolid | BlockCreateEnum.Unite | BlockCreateEnum.Insert)
          | BlockCreateEnum.BlockMetric;

        // Перебираем все одинаковые боксы
        int count = 0;
        using (LongOperationManager lom = new("Деталировка", dt.TotalRowCount * 2))
          foreach (DTGroup group in dt.Groups)
            foreach (AvcDTPart part in group.Data)
            {
              // для первого попавшегося бокса среди одинаковых создаем деталировку стены
              lom.TickOrEsc();
              AvcSolid avcSolid = part.Objects[0] as AvcSolid;
              if (avcSolid is null) continue;
              Cns.Info($"Запрашиваем деталировку для стены {avcSolid}...");
              Wall wall = new( WallTarget.WallToBox, avcSolid, Frame, Front, Back, part.Count);
              BoxData[] boxes = null;
              try
              {
                string json = Request(wall);
                boxes = WebServices.DeserializeFromJson<BoxData[]>(json);
              }
              catch (System.Exception ex) { Cns.Warning("Ошибка при запросе на сервер. \r\n " + ex.Message); return; }
              if (boxes is null || boxes.Length == 0)
              { Cns.Info("Не удалось создать одну стену"); continue; }
              Cns.Info($"Получены {boxes.Length} деталей для стены.");
              lom.TickOrEsc();

              // Создаем солиды-детали и ставим их вертикально
              List<ObjectId> boxIds = CreateBoxes(boxes, db, avcSolid.Space.Id, wall);
              if (boxIds is null || boxIds.Count == 0)
              { Cns.Info("Не удалось создать одну стену - не получилось создать ни одной детали"); continue; }

              ObjectId[] boxIdsArray = boxIds.ToArray();

              using Transaction tr = db.TransactionManager.StartTransaction();

              // создание блока в начале координат без вставки
              string newName = style.NewName(boxIdsArray, db, tr);
              CreateBlockResult ret = BlockCreate.CreateNewBlock(
                boxIdsArray, newName, createOpt, BlockBasePointEnum.WCS, BlockRotationEnum.WCS, Point3dExt.Null,
                Matrix3d.Identity, style.LabelHeight, tr);
              if (ret.IsNull) continue;
              boxIds.EraseAll(tr);

              // Заполнение атрибутов
              Dictionary<string, string> att = new();
              if (!IsNullOrWhiteSpace(wall.Kind))
                att.Add("Kind", wall.Kind);
              if (!IsNullOrWhiteSpace(wall.FrameMat))
                att.Add("FrameMat", wall.FrameMat);
              if (!IsNullOrWhiteSpace(wall.FrontMat))
                att.Add("FrontMat", wall.FrontMat);
              if (!IsNullOrWhiteSpace(wall.BackMat))
                att.Add("BackMat", wall.BackMat);
              if (wall.Frame > 0)
                att.Add("Frame", wall.Frame.ToFStr());
              if (wall.Front > 0) 
                att.Add("Front", wall.Front.ToFStr());
              if (wall.Back > 0)
                att.Add("Back", wall.Back.ToFStr());
               if (att.Count > 0)
              {
                AvcBlock block = AvcManager.Read(ret.BtrId, tr) as AvcBlock;
                if (block is not null)
                {
                  block.AddConstAttributes(att, PointOfView.WCS);
                  block.Save(tr);
                }
              }

              // вставляем блок вместо всех боксов
              InsertBlocks(part.Objects, ret.BtrId, wall, tr);
              tr.Commit();

              count++;
            }

        doc.ClearSelection(); // Очистка выделения 
        if (count > 0)
        {
#if !BRICS
          doc.Database.EvaluateFields(); // метод отсутствует в BricsCAD
#endif
          ed.Regen();
          ed.UpdateScreen();
          Cns.Info("Удалось создать стен {0}", count);
          //doc.SendStringToExecute("_REGENALL ", true, false, false);
        }
        else Cns.NothingInfo(); 
      }
      catch (CancelException ex) { Cns.CancelInfo(ex.Message); }
      catch (WarningException ex) { Cns.Warning(ex.Message); }
      catch (System.Exception ex) { Cns.Err(ex); }
    }

#if !RENT
    [Rt.CommandMethod(InitializationPlugin.commandGroup, DbCommand.BoxToVectorCmdName, Rt.CommandFlags.UsePickSet | Rt.CommandFlags.Redraw)]
#endif
    public static void
    BoxToVector()
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
        if (!LicenseCheck.HasLicenseFor(DbCommand.BoxToWallCmdName))
          throw new CancelException("");
#endif
        // Запрос списка солидов-боксов или блоков
        ObjectId[] objectIds = CnsAcad.Select(BoxFromTableL.SelectSolidOrBlock);
        if (objectIds is null) return;

        // Группируем одинаковые боксы в таблицу деталей
        DataTable dt = new(GetColumns(), DTStyleEnum.Default, AvcSettings.LenStyle, SolidOrBlockFilter()
          , null, ReadEnum.ForceMeter, 1, db, PointOfView.WCS);
        List<AvcDTObj> rawdata = dt.ExtractRawData(objectIds.ToSelectedObjects());
        if (rawdata is null || rawdata.Count == 0)
        {
          Cns.Warning(Cns.Local(CnsL.NothingSucceeded));
          return;
        }
        if (!dt.CreateTable(rawdata)) return;

        // подправим стиль создания блоков-сборок
        AssemblyStyle style = AssemblyStyle.GetCurrent();
        BlockCreateEnum createOpt = style.CreateOptions &
          ~(BlockCreateEnum.LayerZero | BlockCreateEnum.MeshToSolid | BlockCreateEnum.Unite | BlockCreateEnum.Insert
          | BlockCreateEnum.LabelOnTop | BlockCreateEnum.LableOnFront);

        // временная система вставки чертежей в модель
        Point3d insert = db.Extmin;
        if (insert.X == 1e-20) insert = new Point3d(0, -4000, 0);
        double gap = 4000;
        insert += new Vector3d(0, -gap, 0);
        Vector3d step = new (gap, 0, 0);

        // Перебираем все одинаковые боксы
        int count = 0;
        using (LongOperationManager lom = new("Создание чертежей", dt.TotalRowCount * 2))
          foreach (DTGroup group in dt.Groups)
            foreach (AvcDTPart part in group.Data)
            {
              // для первого попавшегося бокса среди одинаковых создаем деталировку стены
              lom.TickOrEsc();
              AvcEntity avcEnt = part.Objects[0] as AvcEntity;
              if (avcEnt is null) continue;
              Cns.Info($"Запрашиваем чертежи для стены {avcEnt}...");
              Wall wall = new(WallTarget.WallToVector, avcEnt, Frame, Front, Back, part.Count);
              PlanData plan2d = null;
              try
              {
                string json = Request(wall);

                //..тест
                //    PlanData plan = new()
                //    {
                //      Name = avcEnt.Name,
                //      PLines = new PLineData[2]
                //      {
                //new ( new VertexData(0,0), new VertexData(0,wall.Thickness), new VertexData(wall.Width,wall.Thickness), new VertexData(wall.Width,0), true),
                //new ( new VertexData(0,0), new VertexData(wall.Width,wall.Thickness))
                //      },
                //      Texts = new TextData[1]
                //      {
                //new (200,105, $"тут стена {avcEnt.Name} - {wall.Count}")
                //      },
                //      Dimensions = new DimensionData[2]
                //      {
                //new (0,0,wall.Width,0,0,-20,0) { Text ="Ширина=<>" },
                //new (0,0,0,wall.Thickness,-20,0,90)
                //      }
                //    };
                //string json = WebServices.SerializeToJson(plan);
                //Debug.Print(json);

                plan2d = WebServices.DeserializeFromJson<PlanData>(json);
                Debug.Print(plan2d.ToString());
              }
              catch (System.Exception ex) { Cns.Warning("Ошибка при запросе на сервер. \r\n " + ex.Message); return; }
              if (plan2d is null || plan2d.IsNull)
              { Cns.Info($"Не удалось создать чертеж стены {wall}"); continue; }
              Cns.Info($"Получены 2d чертежи: {plan2d}");
              lom.TickOrEsc();

              // Создаем чертежи
              List<ObjectId> ids = CreatePlan2d(plan2d, db, db.GetModelId());
              if (ids is null || ids.Count == 0)
              { Cns.Info("Не удалось создать плоский чертеж - не получилось создать ни одного объекта"); continue; }

              ObjectId[] idsArray = ids.ToArray();

              using Transaction tr = db.TransactionManager.StartTransaction();

              // Создание имени блока
              string newName = IsNullOrWhiteSpace(plan2d.Name) ?
                IsNullOrWhiteSpace(avcEnt.Name) ? style.NewName(idsArray, db, tr) : avcEnt.Name : plan2d.Name;
              newName = DatabaseExt.ValidName(newName) + PlanPostfix;

              // создание блока в начале координат без вставки
              CreateBlockResult ret = BlockCreate.CreateNewBlock(
                idsArray, newName, createOpt, BlockBasePointEnum.WCS, BlockRotationEnum.WCS, Point3dExt.Null,
                Matrix3d.Identity, 0, tr);
              if (ret.IsNull) continue;
              ids.EraseAll(tr);

              // вставка чертежа

              BlockExt.Insert(ret.BtrId, db.GetModelId(), insert, Matrix3d.Identity, tr);
              insert += step;

              // удаляем боксы
              foreach (AvcObj obj in part.Objects)
                if (obj is AvcSolid nextBox)
              {
                if (nextBox is null) continue;
                if (!nextBox.ActualLayer?.IsLocked ?? false)
                {
                  Solid3d source = tr.GetObject(nextBox.Id, OpenMode.ForWrite, false, true) as Solid3d;
                  source?.Erase();
                }
              }
              tr.Commit();
              count++;
            }

        doc.ClearSelection(); // Очистка выделения 
        if (count > 0)
        {
#if !BRICS
          doc.Database.EvaluateFields(); // метод отсутствует в BricsCAD
#endif
          ed.Regen();
          ed.UpdateScreen();
          Cns.Info("Удалось создать чертежей {0}", count);
          //doc.SendStringToExecute("_REGENALL ", true, false, false);
        }
        else Cns.NothingInfo();
      }
      catch (CancelException ex) { Cns.CancelInfo(ex.Message); }
      catch (WarningException ex) { Cns.Warning(ex.Message); }
      catch (System.Exception ex) { Cns.Err(ex); }
    }

    internal static void
    SetDefault()
    {
      Frame = DefFrame;
      Front = DefFront;
      Back = DefBack;
      ServerAddress = DefServerAddress;
    }

    private static List<DTColumn>
    GetColumns() => new(){
      new DTColumn("", "%name%", DTColumnEnum.Asc, AvcSettings.LenStyle),
      new DTColumn("", "%len%", DTColumnEnum.Desc, AvcSettings.LenStyle),
      new DTColumn("", "%width%", DTColumnEnum.Desc, AvcSettings.LenStyle),
      new DTColumn("", "%thickness%", DTColumnEnum.Desc, AvcSettings.LenStyle),
      new DTColumn("", "%mat%", DTColumnEnum.Asc, AvcSettings.LenStyle),
    };

    private static EntityFilterStyle
    SolidOrBlockFilter() => new(RegKey + "\\Filter",
        ClassFilterEnum.Solid | ClassFilterEnum.Block,
        EntFilterEnum.Unfrozen,
        EntityFilterStyle.DefExtractionIgnoredLayers)
        { PermissibleClasses = ClassFilterEnum.Solid | ClassFilterEnum.Block };

    private static string
    Request(Wall wall)
    {
      DateTime start = DateTime.Now;
      using HttpClient httpClient = new();
      string jsonSerialized = WebServices.SerializeToJson(wall);
      using StringContent content = new(jsonSerialized, Encoding.UTF8, "application/json");
      using HttpResponseMessage response = httpClient.PostAsync(DefServerAddress, content).Result;
      string respStr = response.Content.ReadAsStringAsync().Result;
      if (response.StatusCode != System.Net.HttpStatusCode.OK)
        throw new WarningException($"Ошибка сервера {response.StatusCode}:\r\n  {respStr}");
      Cns.Info($"  Запрос к серверу обработан за {(DateTime.Now - start).TotalSeconds}c");
      return respStr;
    }

    /// <summary>
    /// Создаем солиды-детали и ставим их вертикально
    /// </summary>
    private static List<ObjectId>
    CreateBoxes(BoxData[] boxes, Database db, ObjectId spaceId, Wall wall)
    {
      List<ObjectId> boxIds = new (boxes.Length);
      using Transaction tr = db.TransactionManager.StartTransaction();
      BlockTableRecord space = tr.GetObject(spaceId, OpenMode.ForWrite) as BlockTableRecord;
      for (int i = 0; i < boxes.Length; i++)
      {
        if (boxes[i].IsZeroSize)
        {
          Cns.Info($"Попытка создать солид нулевого размера. Пропускаем.");
          continue;
        }

        using Solid3d entity = boxes[i].CreateSolid(db, tr);
        if (entity is null) continue;
        entity.TransformBy(wall.StandUp);
        //Transient.DebugDraw(entity, 2);
        ObjectId boxId = space.AppendEntity(entity);
        if (boxId.IsNull) continue;
        boxIds.Add(boxId);
        tr.AddNewlyCreatedDBObject(entity, true);
      }
      tr.Commit();
      return boxIds;
    }

    private static List<ObjectId> 
    CreatePlan2d(PlanData plan2d, Database db, ObjectId spaceId)
    {
      List<ObjectId> ids = new();
      using Transaction tr = db.TransactionManager.StartTransaction();
      BlockTableRecord space = tr.GetObject(spaceId, OpenMode.ForWrite) as BlockTableRecord;
      foreach(Entity entity in plan2d.CreateEntities(db, tr)) using (entity)
        {
          ObjectId id = space.AppendEntity(entity);
          if (id.IsNull) continue;
          ids.Add(id);
          tr.AddNewlyCreatedDBObject(entity, true);
        }
      tr.Commit();
      return ids;
    }

    private static void
    InsertBlocks(List<AvcObj> solids, ObjectId btrId, Wall wall, Transaction tr)
    {
      foreach (AvcObj obj in solids)
      {
        AvcSolid nextBox = obj as AvcSolid;
        if (nextBox is null) continue;
        Matrix3d nextTrans = nextBox.Metric.Lay.Inverse() * wall.LayDown;
        BlockExt.Insert(btrId, nextBox.Space.Id, Point3dExt.Null, nextTrans, tr);
        // Удаление исходного солида-стены
        if (!nextBox.ActualLayer?.IsLocked ?? false)
        {
          Solid3d source = tr.GetObject(nextBox.Id, OpenMode.ForWrite, false, true) as Solid3d;
          source?.Erase();
        }
      }
    }


  }
}
