// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using static System.String;
using System.Collections.Generic;
using System.Linq;
#if BRICS
using Teigha.DatabaseServices;
#else
using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace AVC
{
  internal class
  CreateBoxResult
  {
    public HashSet<ObjectId>
    All = new();

    public Dictionary<string, HashSet<ObjectId>>
    Commands = new();

    public bool
    IsNull => All.Count == 0;

    public void
    Add(ObjectId id, string commands)
    {
      if (id.IsNull) return;
      All.Add(id);
      if (IsNullOrEmpty(commands)) return;
      foreach (string cmd in commands.Split(new char[] { ',', ';' }))
      {
        if (IsNullOrWhiteSpace(cmd)) continue;
        string command = cmd.Trim().ToLower();
        if (Commands.TryGetValue(command, out HashSet<ObjectId> list))
          list.Add(id);
        else Commands.Add(command, new HashSet<ObjectId>() { id });
      }
    }

    public void
    Remove(ObjectId id)
    {
      if (id.IsNull) return;
      All.Remove(id);
      foreach (var pair in Commands)
        pair.Value.Remove(id);
    }

    public void
    Replace(ObjectId oldId, ObjectId newId)
    {
      if (oldId.IsNull) return;
      if (newId.IsNull) { Remove(oldId); return; }
      if (All.Remove(oldId))
        All.Add(newId);
      foreach (var pair in Commands)
        if (pair.Value.Remove(oldId))
          pair.Value.Add(newId);
    }

    public void
    Replace(HashSet<ObjectId> oldIds, HashSet<ObjectId> newIds)
    {
      if (oldIds is null || oldIds.Count == 0) return;
      bool removed = false;
      foreach (ObjectId id in oldIds)
        removed |= All.Remove(id);
      if (!removed) return;
      if (newIds is not null)
        foreach (ObjectId id in newIds)
          if (!id.IsNull && !id.IsErased)
            All.Add(id);
      foreach (var pair in Commands)
      {
        removed = false;
        foreach (ObjectId id in oldIds)
          removed |= pair.Value.Remove(id);
        if (removed && newIds is not null)
          foreach (ObjectId id in newIds)
            if (!id.IsNull && !id.IsErased)
              pair.Value.Add(id);
      }
    }

    public void
    ClearErased()
    {
      if (IsNull) return;
      All.RemoveWhere(id => id.IsNull || id.IsErased);
      foreach (var pair in Commands)
        pair.Value.RemoveWhere(id => id.IsNull || id.IsErased);
    }

    public ObjectId[]
    ToArray()
    {
      return All.ToArray();
    }
  }
}
