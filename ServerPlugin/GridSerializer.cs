using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;

namespace ServerPlugin;

public static class GridSerializer
{
    public static bool Save(string path, string blueprintName, IEnumerable<MyCubeGrid> grids)
    {
        var definition = MyObjectBuilderSerializerKeen.CreateNewObject<MyObjectBuilder_ShipBlueprintDefinition>();
        definition.Id = new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_ShipBlueprintDefinition)), blueprintName);
        definition.CubeGrids = grids.Select(GetObjectBuilder).ToArray();

        var definitions = MyObjectBuilderSerializerKeen.CreateNewObject<MyObjectBuilder_Definitions>();
        definitions.ShipBlueprints = new[] { definition };

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        return MyObjectBuilderSerializerKeen.SerializeXML(path, false, definitions);
    }

    public static bool Load(string path, out List<MyObjectBuilder_CubeGrid> grids)
    {
        grids = new List<MyObjectBuilder_CubeGrid>();
        if (!File.Exists(path))
            return false;

        if (!MyObjectBuilderSerializerKeen.DeserializeXML(path, out MyObjectBuilder_Definitions definitions))
            return false;

        if (definitions.ShipBlueprints != null)
        {
            foreach (var blueprint in definitions.ShipBlueprints)
            {
                if (blueprint?.CubeGrids != null)
                    grids.AddRange(blueprint.CubeGrids);
            }
        }

        if (definitions.Prefabs != null)
        {
            foreach (var prefab in definitions.Prefabs)
            {
                if (prefab?.CubeGrids != null)
                    grids.AddRange(prefab.CubeGrids);
            }
        }

        foreach (var grid in grids)
            ResetLandingGear(grid);

        return grids.Count > 0;
    }

    public static void TransferOwnership(IEnumerable<MyObjectBuilder_CubeGrid> grids, long identityId)
    {
        foreach (var grid in grids)
        {
            foreach (var block in grid.CubeBlocks)
            {
                block.Owner = identityId;
                block.BuiltBy = identityId;
            }
        }
    }

    private static MyObjectBuilder_CubeGrid GetObjectBuilder(MyCubeGrid grid)
    {
        RemovePilots(grid);
        if (grid.GetObjectBuilder() is not MyObjectBuilder_CubeGrid objectBuilder)
            throw new InvalidOperationException($"{grid.DisplayName} did not produce a cube-grid object builder.");

        return objectBuilder;
    }

    private static void RemovePilots(MyCubeGrid grid)
    {
        foreach (var cockpit in grid.GetFatBlocks().OfType<MyCockpit>())
        {
            if (cockpit.Pilot == null)
                continue;

            cockpit.RequestRemovePilot();
            cockpit.RemovePilot();
        }
    }

    private static void ResetLandingGear(MyObjectBuilder_CubeGrid grid)
    {
        foreach (var block in grid.CubeBlocks.OfType<MyObjectBuilder_LandingGear>())
        {
            block.IsLocked = false;
            block.AutoLock = true;
            block.FirstLockAttempt = false;
            block.AttachedEntityId = null;
            block.MasterToSlave = null;
            block.GearPivotPosition = null;
            block.OtherPivot = null;
            block.LockMode = LandingGearMode.Unlocked;
        }
    }
}
