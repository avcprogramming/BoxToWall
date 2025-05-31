// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

// Ignore Spelling: Json Deserialize

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using static System.String;
using static System.Math;
using System.Diagnostics;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Bricscad.Internal;
using Bricscad.EditorInput;
using Teigha.Geometry;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Internal;
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
    PlanPostfix = "_2d";

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
        ObjectId[] objectIds = EditorExt.Select(SolidL.SelectSolids);
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
          EntityFilterStyle.SolidTableFilter(null), null, ReadEnum.ForceMeter, 1, PointOfView.WCS);
        List<AvcDTObj> rawdata = dt.ExtractRawData(objectIds.ToSelectedObjects());
        if (rawdata is null || rawdata.Count == 0)
        {
          Cns.Warning(CmdLineL.NoSelected);
          return;
        }
        if (!dt.CreateTable(rawdata)) return;

        BoxFromTableStyle createBoxStyle = BoxFromTableStyle.GetCurrent();

        // Перебираем все одинаковые боксы
        int count = 0;
        using (LongOperationManager lom = new(BoxFromTableL.BoxToWallProgress, dt.Groups.Count * 2 + dt.TotalRowCount))
          foreach (DTGroup group in dt.Groups)
            foreach (AvcDTPart part in group.Data)
            {
              // для первого попавшегося бокса среди одинаковых создаем деталировку стены
              lom.TickOrEsc();
              AvcSolid avcSolid = part.Objects[0] as AvcSolid;
              if (avcSolid is null) continue;
              Cns.Info(BoxFromTableL.RequestForBox, avcSolid);
              Wall wall = new(WallTarget.WallToBox, avcSolid, part.Count);
              BoxData[] boxes = null;
              try
              {
                string json = Request(createBoxStyle.ServerAddress, wall);
                boxes = WebServices.DeserializeFromJson<BoxData[]>(json);
              }
              catch (Exception ex) { Cns.Warning(BoxFromTableL.RequestError, ex.Message); return; }
              if (boxes is null || boxes.Length == 0)
              { Cns.Info(BoxFromTableL.ZeroBoxDataError, wall); continue; }
              Cns.Info(BoxFromTableL.BoxDataReceived, boxes.Length, wall);
              lom.TickOrEsc();

              foreach (BoxData box in boxes)
                if (IsNullOrWhiteSpace(box.Shape)) box.Shape = "Box";

              // Группировка по блокам/группам
              Dictionary<string, List<BoxData>> groups = BoxData.Grouping(boxes);
              if (groups.Count == 0) { Cns.NothingInfo(); return; }

              count += BoxData.CreateWalls(groups, createBoxStyle.Flags, wall.StandUp, db, out List< ObjectId> btrIds);
              if (btrIds is null || btrIds.Count == 0) continue;

              using Transaction tr = db.TransactionManager.StartTransaction();
              foreach (ObjectId btrId in btrIds)
              {
                if (createBoxStyle.Flags.HasFlag(CreateBoxEnum.MakeBlock) && btrId.ObjectClass == BlockExt.dbBTR)
                {
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
                    AvcBlock block = AvcManager.Read(btrId, tr) as AvcBlock;
                    if (block is not null)
                    {
                      block.AddConstAttributes(att, PointOfView.WCS);
                      block.Save(tr);
                    }
                  }

                  // вставляем блок вместо всех боксов
                  InsertBlocks(part.Objects, btrId, wall, createBoxStyle.BlockLayer, tr);
                }
                tr.Commit();
              }
            }

        ed.ClearSelection(); // Принудительная очистка выделения 
        if (count > 0)
        {
#if !BRICS
          doc.Database.EvaluateFields(); // метод отсутствует в BricsCAD
#endif
          doc.TransactionManager.QueueForGraphicsFlush();
          doc.TransactionManager.FlushGraphics();
          ed.Regen();
          ed.UpdateScreen();
          Utils.FlushGraphics();
          Cns.Info(BoxFromTableL.BoxToWallResult, count);
          //_doc.SendStringToExecute("_REGENALL ", true, false, false);
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
        DocExt.ThirdTest = db.BlockTableId; // проверка в AssemblyStyle.NewName и в BlockCreate.CreateNewBlock
        if (!LicenseCheck.HasLicenseFor(DbCommand.BoxToVectorCmdName))
          throw new CancelException("");
#endif

        // Запрос списка солидов-боксов или блоков
        ObjectId[] objectIds = EditorExt.Select(BoxFromTableL.SelectSolidOrBlock);
        if (objectIds is null) return;

        // Группируем одинаковые боксы в таблицу деталей
        DataTable dt = new(GetColumns(), DTStyleEnum.Default, AvcSettings.LenStyle, SolidOrBlockFilter()
          , null, ReadEnum.ForceMeter, 1, PointOfView.WCS);
        List<AvcDTObj> rawdata = dt.FindViewportsAndExtractRawData(objectIds.ToSelectedObjects());
        if (rawdata is null || rawdata.Count == 0)
        {
          Cns.Warning(CmdLineL.NoSelected);
          return;
        }
        if (!dt.CreateTable(rawdata)) return;

        // подправим стиль создания блоков-сборок
        AssemblyStyle style = AssemblyStyle.GetCurrent();
        BlockCreateEnum createOpt = style.CreateOptions &
          ~(BlockCreateEnum.LayerZero | BlockCreateEnum.MeshToSolid | BlockCreateEnum.Unite | BlockCreateEnum.Insert
          | BlockCreateEnum.LabelOnTop | BlockCreateEnum.LableOnFront);
        BoxFromTableStyle createBoxStyle = BoxFromTableStyle.GetCurrent();

        ObjectId spaceId;
        Point3d insert;
        Vector3d step;
        Matrix3d scale;
        if (SysVar.PaperSpace)
        {
          insert = new Point3d(0, 0, 0);
          step = new(SysVar.Inch ? 4 : 100, 0,0);
          spaceId = SymbolUtilityServices.GetBlockPaperSpaceId(db);
          scale = Matrix3d.Scaling(0.04, new Point3d(0, 0, 0));
        }
        else // вставки чертежей в модель
        {
          insert = db.Extmin;
          if (insert.X == 1e-20) insert = new Point3d(0, -4000, 0);
          double gap = 4000;
          insert += new Vector3d(0, -gap, 0);
          step = new(gap, 0, 0);
          spaceId = db.GetModelId();
          scale = Matrix3d.Identity;
        }

        // Перебираем все одинаковые боксы
        int count = 0;
        using (LongOperationManager lom = new(BoxFromTableL.BoxtToVectorProgress, dt.TotalRowCount * 2))
          foreach (DTGroup group in dt.Groups)
            foreach (AvcDTPart part in group.Data)
            {
              // для первого попавшегося бокса среди одинаковых создаем деталировку стены
              lom.TickOrEsc();
              AvcEntity avcEnt = part.Objects[0] as AvcEntity;
              if (avcEnt is null) continue;
              Cns.Info(BoxFromTableL.RequestForVector, avcEnt);
              Wall wall = new(WallTarget.WallToVector, avcEnt, part.Count);
              PlanData plan2d = null;
              try
              {
                string json = Request(createBoxStyle.ServerAddress, wall);

                //..тест
                //PlanData plan = new()
                //{
                //  Name = avcEnt.Name,
                //  PLines = new PLineData[2]
                //  {
                //new ( new VertexData(0,0),
                //  new VertexData(0,wall.Thickness),
                //  new VertexData(wall.Width,wall.Thickness),
                //  new VertexData(wall.Width,0), true)
                //{ LineStyle = "HIDDEN2" },
                //new ( new VertexData(0,0), new VertexData(wall.Width,wall.Thickness))
                //  },
                //  Texts = new TextData[1]
                //  {
                //new (200,105, $"тут стена {avcEnt.Name} - {wall.Count}")
                //  },
                //  Dimensions = new DimensionData[2]
                //  {
                //new (0,0,wall.Width,0,0,-20,0) { Text ="Ширина=<>", DimScale = 5 },
                //new (0,0,0,wall.Thickness,-20,0,90)
                //  }
                //};
                //string json = WebServices.SerializeToJson(plan);
                //Debug.Print(json);

                plan2d = WebServices.DeserializeFromJson<PlanData>(json);
                Debug.Print(plan2d.ToString());
              }
              catch (System.Exception ex) { Cns.Warning(BoxFromTableL.RequestError, ex.Message); return; }
              if (plan2d is null || plan2d.IsNull)
              { Cns.Info(BoxFromTableL.NullPlanError, wall); continue; }
              Cns.Info(BoxFromTableL.PlanDataReceived, plan2d);
              lom.TickOrEsc();

              // Создаем чертежи
              List<ObjectId> ids = CreatePlan2d(plan2d, db, spaceId);
              if (ids is null || ids.Count == 0)
              { Cns.Info(BoxFromTableL.ZeroPlanError, plan2d.Name); continue; }

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

              //// создание слоя чертежа
              //ObjectId layerId = ObjectId.Null;
              //if (!IsNullOrEmpty(layer))
              //{
              //  LayerManager lm = new(_db, _tr);
              //  layerId = lm.GetOrCreate(layer, LayerEnum.Visible);
              //}

              // вставка чертежа
              BlockReference blRef = BlockExt.Insert(ret.BtrId, spaceId, insert, scale, tr);
              //if (blRef is not null && !layerId.IsNull)
              //  blRef.LayerId = layerId;
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

        doc.PermitClearSelection(); // Очистка выделения 
        if (count > 0)
        {
#if !BRICS
          doc.Database.EvaluateFields(); // метод отсутствует в BricsCAD
#endif
          doc.TransactionManager.QueueForGraphicsFlush();
          doc.TransactionManager.FlushGraphics();
          ed.Regen();
          ed.UpdateScreen();
          Utils.FlushGraphics();
          Cns.Info(BoxFromTableL.BoxToVectorResult, count);
        }
        else Cns.NothingInfo();
      }
      catch (CancelException ex) { Cns.CancelInfo(ex.Message); }
      catch (WarningException ex) { Cns.Warning(ex.Message); }
      catch (System.Exception ex) { Cns.Err(ex); }
    }

    private static List<DTColumn>
    GetColumns() => new(){
      new DTColumn("", "%name%", DTColumnEnum.Asc, AvcSettings.LenStyle),
      new DTColumn("", "%len%", DTColumnEnum.Desc, AvcSettings.LenStyle),
      new DTColumn("", "%width%", DTColumnEnum.Desc, AvcSettings.LenStyle),
      new DTColumn("", "%thickness%", DTColumnEnum.Desc, AvcSettings.LenStyle),
      new DTColumn("", "%mat%", DTColumnEnum.Asc, AvcSettings.LenStyle),
      new DTColumn("", "%kind%", DTColumnEnum.Asc, AvcSettings.LenStyle),
      new DTColumn("", "%info%", DTColumnEnum.Asc, AvcSettings.LenStyle),
    };

    private static EntityFilterStyle
    SolidOrBlockFilter() => new(null,
        ClassFilterEnum.Solid | ClassFilterEnum.Block,
        EntFilterEnum.Unfrozen,
        EntityFilterStyle.DefExtractionIgnoredLayers)
    { PermissibleClasses = ClassFilterEnum.Solid | ClassFilterEnum.Block };

    private static string
    Request(string server, Wall wall)
    {
      DateTime start = DateTime.Now;
      using HttpClient httpClient = new();
      string jsonSerialized = WebServices.SerializeToJson(wall);
      using StringContent content = new(jsonSerialized, Encoding.UTF8, "application/json");
      using HttpResponseMessage response = httpClient.PostAsync(server, content).Result;
      string respStr = response.Content.ReadAsStringAsync().Result;
      if (response.StatusCode != System.Net.HttpStatusCode.OK)
        throw new WarningException($"Web server error {response.StatusCode}:\r\n  {respStr}");
      Cns.Info(BoxFromTableL.RequestTime, (DateTime.Now - start).TotalSeconds);
      return respStr;
    }

    private static List<ObjectId>
    CreatePlan2d(PlanData plan2d, Database db, ObjectId spaceId)
    {
      List<ObjectId> ids = new();
      using Transaction tr = db.TransactionManager.StartTransaction();
      BlockTableRecord space = tr.GetObject(spaceId, OpenMode.ForWrite) as BlockTableRecord;
      foreach (Entity entity in plan2d.CreateEntities(db, tr)) using (entity)
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
    InsertBlocks(List<AvcObj> solids, ObjectId btrId, Wall wall, string layer, Transaction tr)
    {
      ObjectId layerId = ObjectId.Null;
      if (!IsNullOrEmpty(layer) && solids.Count > 0)
      {
        LayerManager lm = new(solids[0].Id.Database, tr);
        layerId = lm.GetOrCreate(layer, LayerEnum.Visible);
      }

      foreach (AvcObj obj in solids)
      {
        LongOperationManager.TryTickOrEsc();
        AvcSolid box = obj as AvcSolid;
        if (box is null) continue;
        Matrix3d standup = box.Metric.Lay.Inverse() * wall.LayDown;
        BlockReference blRef = BlockExt.Insert(btrId, box.Space.Id, Point3dExt.Null, standup, tr);
        if (blRef is not null && !layerId.IsNull)
          blRef.LayerId = layerId;
        // Удаление исходного солида-стены
        if (!box.ActualLayer?.IsLocked ?? false && !box.Id.IsErased)
        {
          Solid3d source = tr.GetObject(box.Id, OpenMode.ForWrite, false, true) as Solid3d;
          source?.Erase();
        }
      }
    }


  }
}
