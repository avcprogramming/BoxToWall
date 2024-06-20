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
using System.Windows;
using System.Xml.Linq;



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
          EntityFilterStyle.SolidTableFilter(null), null, ReadEnum.ForceMeter, 1, db, PointOfView.WCS);
        List<AvcDTObj> rawdata = dt.ExtractRawData(objectIds.ToSelectedObjects());
        if (rawdata is null || rawdata.Count == 0)
        {
          Cns.Warning(Cns.Local(CnsL.NothingSucceeded));
          return;
        }
        if (!dt.CreateTable(rawdata)) return;

        BoxFromTableStyle createBoxStyle = BoxFromTableStyle.GetCurrent();

        // Перебираем все одинаковые боксы
        int count = 0;
        using (LongOperationManager lom = new(BoxFromTableL.BoxToWallProgress, dt.TotalRowCount * 2))
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

              ObjectId btrId = CreateWall(boxes, wall.Name, wall.StandUp, createBoxStyle.Flags, db);
              if (btrId.IsNull) continue;

              if (createBoxStyle.Flags.HasFlag(CreateBoxEnum.MakeBlock) && btrId.ObjectClass == BlockExt.dbBTR)
              {
                // Заполнение атрибутов
                using Transaction tr = db.TransactionManager.StartTransaction();
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
                InsertBlocks(part.Objects, btrId, wall, tr);
                tr.Commit();
              }
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
          Cns.Info(BoxFromTableL.BoxToWallResult, count);
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
        if (!LicenseCheck.HasLicenseFor(DbCommand.BoxToVectorCmdName))
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
        BoxFromTableStyle createBoxStyle = BoxFromTableStyle.GetCurrent();

        // временная система вставки чертежей в модель
        Point3d insert = db.Extmin;
        if (insert.X == 1e-20) insert = new Point3d(0, -4000, 0);
        double gap = 4000;
        insert += new Vector3d(0, -gap, 0);
        Vector3d step = new(gap, 0, 0);

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
              catch (System.Exception ex) { Cns.Warning(BoxFromTableL.RequestError, ex.Message); return; }
              if (plan2d is null || plan2d.IsNull)
              { Cns.Info(BoxFromTableL.NullPlanError, wall); continue; }
              Cns.Info(BoxFromTableL.PlanDataReceived, plan2d);
              lom.TickOrEsc();

              // Создаем чертежи
              List<ObjectId> ids = CreatePlan2d(plan2d, db, db.GetModelId());
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
          Cns.Info(BoxFromTableL.BoxToVectorResult, count);
          doc.SendStringToExecute("_REGENALL ", true, false, false);
        }
        else Cns.NothingInfo();
      }
      catch (CancelException ex) { Cns.CancelInfo(ex.Message); }
      catch (WarningException ex) { Cns.Warning(ex.Message); }
      catch (System.Exception ex) { Cns.Err(ex); }
    }

    internal static ObjectId
    CreateWall(BoxData[] boxes, string blockName, Matrix3d transform, CreateBoxEnum flags, Database db)
    {
      string up = blockName.ToUpper();
      bool toModel = up == "MODEL" || up == "*MODEL_SPACE";

      // Создаем солиды-детали и ставим их вертикально
      List<ObjectId> boxIds = CreateBoxes(boxes, db, db.GetModelId(), transform);
      if (boxIds is null || boxIds.Count == 0)
      { 
        if (!toModel) Cns.Info(BoxFromTableL.ZeroSolidListError, blockName); 
        return ObjectId.Null; 
      }
      ObjectId[] boxIdsArray = boxIds.ToArray();

      if (flags.HasFlag(CreateBoxEnum.Drill))
      {
        string hl = DrillOptions.HolesLayerName.ToUpper();
        if (IsNullOrWhiteSpace(hl)) Cns.Info(DrillL.LayerNotSpecified);
        else
        {
          // Поищем, есть ли среди боксов дырки для сверления
          bool hasHoles = false;
          foreach (BoxData boxData in boxes) 
            if (hl.Equals(boxData.Layer, StringComparison.OrdinalIgnoreCase) || boxData.IsBlock)
            { hasHoles = true; break; }
          if (hasHoles)
          {
#if RENT || VARS
            DocExt.ThirdTest = db.BlockTableId; // проверка в Subtraction
            if (!LicenseCheck.HasLicenseFor(DbCommand.DrillCmdName))
              throw new CancelException("");
#endif

            try
            {
              Cns.Info(DrillL.Drilling);
              int count = Drill.MakeDrill(boxIdsArray, hl, db);
              if (count > 0)
              {
                // Удалим ID дырок
                boxIds = new();
                foreach (ObjectId oldId in boxIdsArray)
                  if (!oldId.IsNull && !oldId.IsErased) boxIds.Add(oldId);
                boxIdsArray = boxIds.ToArray();
              }
            }
            catch(WarningException ex) { Cns.Info(ex.Message); }
          }
        }
      }

      ObjectId retId = boxIds[0];
      if (!toModel && (flags.HasFlag(CreateBoxEnum.MakeBlock) || flags.HasFlag(CreateBoxEnum.MakeGroup)))
      {
        AssemblyStyle style = AssemblyStyle.GetCurrent();
        using Transaction tr = db.TransactionManager.StartTransaction();
        string newName = IsNullOrWhiteSpace(blockName) ?
          style.NewName(boxIdsArray, db, tr) : DatabaseExt.ValidName(blockName);
        if (flags.HasFlag(CreateBoxEnum.MakeBlock)) // создание блока
        {
          // Подправим стиль создания блоков
          BlockCreateEnum createOpt = style.CreateOptions &
            ~(BlockCreateEnum.LayerZero | BlockCreateEnum.MeshToSolid | BlockCreateEnum.Unite | BlockCreateEnum.Insert)
            | BlockCreateEnum.BlockMetric;

          // создание блока в начале координат без вставки
          CreateBlockResult ret = BlockCreate.CreateNewBlock(
            boxIdsArray, newName, createOpt, BlockBasePointEnum.WCS, BlockRotationEnum.WCS, Point3dExt.Null,
            Matrix3d.Identity, style.LabelHeight, tr);
          if (ret.IsNull) return ObjectId.Null;
          boxIds.EraseAll(tr);
          retId = ret.BtrId;
        }
        else if (flags.HasFlag(CreateBoxEnum.MakeGroup)) // создание группы
        {
          Cns.Info(BoxFromTableL.CreateGroup, newName);
          Group group;
          DBDictionary dic = tr.GetObject(db.GroupDictionaryId, OpenMode.ForWrite) as DBDictionary;
          if (dic is null) return ObjectId.Null;
          if (dic.Contains(newName)) // старая группа - заменяем все объекты
          {
            retId = dic.GetAt(newName);
            group = tr.GetObject(retId, OpenMode.ForWrite) as Group;
            if (group is null) return ObjectId.Null;
            ObjectId[] oldIds = group.GetAllEntityIds();
            foreach (ObjectId oldId in oldIds)
              group.Remove(oldId);
          }
          else // новая группа
          {
            group = new(AvcWeb.AvcADLink, true);
            retId = dic.SetAt(newName, group);
            tr.AddNewlyCreatedDBObject(group, true);
          }

          foreach (ObjectId entId in boxIds)
            group.Append(entId);
        }
        tr.Commit();
      }

      return retId;
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

    /// <summary>
    /// Создаем солиды-детали и ставим их вертикально
    /// </summary>
    private static List<ObjectId>
    CreateBoxes(BoxData[] boxes, Database db, ObjectId spaceId, Matrix3d transform)
    {
      List<ObjectId> boxIds = new(boxes.Length);
      using Transaction tr = db.TransactionManager.StartTransaction();
      BlockTableRecord space = tr.GetObject(spaceId, OpenMode.ForWrite) as BlockTableRecord;
      for (int i = 0; i < boxes.Length; i++)
      {
        LongOperationManager.TryTickOrEsc();
        if (boxes[i].IsZeroSize)
        {
          Cns.Info(BoxFromTableL.ZeroSizeSolidError);
          continue;
        }

        using Entity entity = boxes[i].IsBlock ? boxes[i].CreateBlock(db,tr) : boxes[i].CreateSolid(db, tr);
        if (entity is null) continue;
        entity.TransformBy(transform);
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
