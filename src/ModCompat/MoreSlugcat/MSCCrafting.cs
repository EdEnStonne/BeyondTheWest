using UnityEngine;
using BeyondTheWest;
using MoreSlugcats;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BeyondTheWest.Items;
using ObjectType = AbstractPhysicalObject.AbstractObjectType;
using MSCObjectType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using DLCObjectType = DLCSharedEnums.AbstractObjectType;
using CreatureType = CreatureTemplate.Type;
using RWCustom;

namespace BeyondTheWest.MSCCompat;

public static class CraftHooks
{
    public static bool StaticManager_CanCraftSpear(StaticChargeManager SCM, Spear spear)
    {
        return spear != null && spear.abstractSpear.electric 
            && (SCM.IsOvercharged || spear.abstractSpear.electricCharge > 0);
    }
    
    // Hooks
    public static void ApplyHooks()
    {
        GourmandCombos_InitCustomCrafts();
        On.MoreSlugcats.GourmandCombos.CraftingResults += GourmandCombos_SpitOutCustomCrafts;

        On.MoreSlugcats.ElectricSpear.CheckElectricCreature += ElectricSpear_SparkIsElectric;
        On.Player.CraftingResults += Player_BTW_CraftingResult;
        On.Player.GraspsCanBeCrafted += Player_StaticChargeManager_GraspsCanBeCrafted;
        On.Player.SpitUpCraftedObject += Player_BTW_SpitUpCraftedObject;
        IL.Player.GrabUpdate += Player_CanCraftObject;
        BTWPlugin.Log("CraftHooks ApplyHooks Done !");
    }

    private static AbstractPhysicalObject GourmandCombos_SpitOutCustomCrafts(On.MoreSlugcats.GourmandCombos.orig_CraftingResults orig, PhysicalObject crafter, Creature.Grasp graspA, Creature.Grasp graspB)
    {
        ObjectType abstractObjectType = GourmandCombos.CraftingResults_ObjectData(graspA, graspB, true);
        AbstractPhysicalObject customItem = null;

        if (abstractObjectType == AbstractTristor.TristorType)
        {
            customItem = new AbstractTristor(crafter.room.world, null, crafter.abstractPhysicalObject.pos, crafter.room.game.GetNewID());
        }
        else if (abstractObjectType == AbstractVoidCrystal.VoidCrystalType)
        {
            customItem = new AbstractVoidCrystal(crafter.room.world, null, crafter.abstractPhysicalObject.pos, crafter.room.game.GetNewID());
        }

        if (customItem is not null)
        {
            Custom.Log(new string[]
			{
				"(BTW) CRAFTING INPUT",
				graspA.grabbed.abstractPhysicalObject.type.ToString(),
				"+",
				graspB.grabbed.abstractPhysicalObject.type.ToString()
			});
            return customItem;
        }
        return orig(crafter, graspA, graspB);
    }

    //----------- Hooks
    private static void GourmandCombos_InitCustomCrafts()
    {
        BTWPlugin.Log($"Trying to extend GourmandCombos librairy.");
        int oldObjectLenght = GourmandCombos.craftingGrid_ObjectsOnly.GetLength(0);
        int oldCreatureLenght = GourmandCombos.craftingGrid_CrittersOnly.GetLength(0);
        int ObjectLenght = oldObjectLenght;
        int CreatureLenght = oldCreatureLenght;
        
        // Add new object to lib
        GourmandCombos.objectsLibrary[AbstractTristor.TristorType] = ObjectLenght++;
        GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType] = ObjectLenght++;
        
        // Create new tables
        var newGridObjectsOnly = new GourmandCombos.CraftDat[ObjectLenght, ObjectLenght];
        var newGridCritterObjects = new GourmandCombos.CraftDat[CreatureLenght, ObjectLenght];
        var newGridCrittersOnly = new GourmandCombos.CraftDat[CreatureLenght, CreatureLenght];

        BTWPlugin.Log($"GourmandCombos librairy old size was <{oldCreatureLenght}/{oldObjectLenght}>, aiming to extend to <{CreatureLenght}/{ObjectLenght}>.");

        for (int x = 0; x < oldObjectLenght; x++)
        {
            for (int y = 0; y < oldObjectLenght; y++)
            {
                newGridObjectsOnly[x,y] = GourmandCombos.craftingGrid_ObjectsOnly[x,y];
            }
        }
        for (int x = 0; x < oldCreatureLenght; x++)
        {
            for (int y = 0; y < oldObjectLenght; y++)
            {
                newGridCritterObjects[x,y] = GourmandCombos.craftingGrid_CritterObjects[x,y];
            }
        }
        for (int x = 0; x < oldCreatureLenght; x++)
        {
            for (int y = 0; y < oldCreatureLenght; y++)
            {
                newGridCrittersOnly[x,y] = GourmandCombos.craftingGrid_CrittersOnly[x,y];
            }
        }

        GourmandCombos.craftingGrid_ObjectsOnly = newGridObjectsOnly;
        GourmandCombos.craftingGrid_CritterObjects = newGridCritterObjects;
        GourmandCombos.craftingGrid_CrittersOnly = newGridCrittersOnly;

        // Add new crafts
        BTWPlugin.Log($"GourmandCombos librairy extended to <{GourmandCombos.craftingGrid_CritterObjects.GetLength(0)}/{GourmandCombos.craftingGrid_CritterObjects.GetLength(1)}> ! Adding new craft...");
        bool oldShowdebug = GourmandCombos.showDebug;
        GourmandCombos.showDebug = true;

        // Tristors
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.Rock], 0, ObjectType.JellyFish, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.FlareBomb], 0, null, CreatureType.VultureGrub);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.VultureMask], 0, ObjectType.DataPearl, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.PuffBall], 0, ObjectType.SporePlant, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.DangleFruit], 0, ObjectType.FlareBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.SSOracleSwarmer], 0, ObjectType.OverseerCarcass, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.DataPearl], 0, null, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.WaterNut], 0, ObjectType.JellyFish, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.JellyFish], 0, null, CreatureType.SmallCentipede);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.Lantern], 0, null, CreatureType.VultureGrub);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.KarmaFlower], 0, DLCObjectType.SingularityBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.Mushroom], 0, ObjectType.PuffBall, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.FirecrackerPlant], 0, ObjectType.ScavengerBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.SlimeMold], 0, ObjectType.Lantern, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.FlyLure], 0, ObjectType.FirecrackerPlant, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.ScavengerBomb], 0, MSCObjectType.FireEgg, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.SporePlant], 0, ObjectType.PuffBall, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.EggBugEgg], 0, null, CreatureType.VultureGrub);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.NeedleEgg], 0, null, CreatureType.SmallCentipede);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.BubbleGrass], 0, ObjectType.FirecrackerPlant, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[ObjectType.OverseerCarcass], 0, ObjectType.DataPearl, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[DLCObjectType.SingularityBomb], 0, AbstractVoidCrystal.VoidCrystalType, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[MSCObjectType.FireEgg], 0, DLCObjectType.SingularityBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[DLCObjectType.Seed], 0, null, CreatureType.SmallCentipede);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[DLCObjectType.GooieDuck], 0, ObjectType.SporePlant, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[DLCObjectType.LillyPuck], 0, ObjectType.Lantern, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[DLCObjectType.GlowWeed], 0, ObjectType.Lantern, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[DLCObjectType.DandelionPeach], 0, ObjectType.PuffBall, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[AbstractTristor.TristorType], 0, null, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractTristor.TristorType], GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], 0, MSCObjectType.FireEgg, null);

        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.Fly], GourmandCombos.objectsLibrary[AbstractTristor.TristorType], 1, ObjectType.FlareBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.VultureGrub], GourmandCombos.objectsLibrary[AbstractTristor.TristorType], 1, ObjectType.Lantern, null);
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.SmallCentipede], GourmandCombos.objectsLibrary[AbstractTristor.TristorType], 1, ObjectType.JellyFish, null);
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.SmallNeedleWorm], GourmandCombos.objectsLibrary[AbstractTristor.TristorType], 1, ObjectType.ScavengerBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.Hazer], GourmandCombos.objectsLibrary[AbstractTristor.TristorType], 1, ObjectType.PuffBall, null);
        
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.SmallCentipede], GourmandCombos.objectsLibrary[ObjectType.Rock], 1, AbstractTristor.TristorType, null);
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.SmallCentipede], GourmandCombos.objectsLibrary[ObjectType.ScavengerBomb], 1, AbstractTristor.TristorType, null);

        // VoidCrystal
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.Rock], 0, MSCObjectType.FireEgg, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.FlareBomb], 0, null, CreatureType.VultureGrub);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.VultureMask], 0, ObjectType.DataPearl, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.PuffBall], 0, ObjectType.SporePlant, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.DangleFruit], 0, ObjectType.FlareBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.SSOracleSwarmer], 0, ObjectType.OverseerCarcass, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.DataPearl], 0, null, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.WaterNut], 0, ObjectType.JellyFish, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.JellyFish], 0, AbstractTristor.TristorType, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.Lantern], 0, null, CreatureType.VultureGrub);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.KarmaFlower], 0, DLCObjectType.SingularityBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.Mushroom], 0, ObjectType.PuffBall, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.FirecrackerPlant], 0, ObjectType.ScavengerBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.SlimeMold], 0, ObjectType.Lantern, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.FlyLure], 0, null, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.ScavengerBomb], 0, MSCObjectType.FireEgg, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.SporePlant], 0, ObjectType.PuffBall, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.EggBugEgg], 0, MSCObjectType.FireEgg, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.NeedleEgg], 0, MSCObjectType.FireEgg, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.BubbleGrass], 0, ObjectType.FirecrackerPlant, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[ObjectType.OverseerCarcass], 0, ObjectType.DataPearl, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[DLCObjectType.SingularityBomb], 0, MSCObjectType.FireEgg, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[MSCObjectType.FireEgg], 0, AbstractTristor.TristorType, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[DLCObjectType.Seed], 0, null, CreatureType.SmallCentipede);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[DLCObjectType.GooieDuck], 0, ObjectType.SporePlant, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[DLCObjectType.LillyPuck], 0, ObjectType.Lantern, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[DLCObjectType.GlowWeed], 0, ObjectType.Lantern, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[DLCObjectType.DandelionPeach], 0, ObjectType.PuffBall, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[AbstractTristor.TristorType], 0, MSCObjectType.FireEgg, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], 0, null, null);

        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.Fly], GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], 1, ObjectType.FlareBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.VultureGrub], GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], 1, ObjectType.Lantern, null);
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.SmallCentipede], GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], 1, ObjectType.ScavengerBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.SmallNeedleWorm], GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], 1, ObjectType.ScavengerBomb, null);
        GourmandCombos.SetLibraryData(GourmandCombos.critsLibrary[CreatureType.Hazer], GourmandCombos.objectsLibrary[AbstractVoidCrystal.VoidCrystalType], 1, ObjectType.PuffBall, null);

        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[MSCObjectType.FireEgg], GourmandCombos.objectsLibrary[ObjectType.JellyFish], 0, AbstractVoidCrystal.VoidCrystalType, null);
        GourmandCombos.SetLibraryData(GourmandCombos.objectsLibrary[MSCObjectType.FireEgg], GourmandCombos.objectsLibrary[DLCObjectType.SingularityBomb], 0, AbstractVoidCrystal.VoidCrystalType, null);

        GourmandCombos.showDebug = oldShowdebug;
        BTWPlugin.Log($"GourmandCombos librairy extended from <{oldCreatureLenght}/{oldObjectLenght}> to <{GourmandCombos.craftingGrid_CritterObjects.GetLength(0)}/{GourmandCombos.craftingGrid_CritterObjects.GetLength(1)}> ! Testing result of latest combo... [{GourmandCombos.GetLibraryData(AbstractTristor.TristorType, AbstractVoidCrystal.VoidCrystalType).type}]");
    }

    private static void Player_CanCraftObject(ILContext il)
    {
        BTWPlugin.Log("MSC IL 1 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(0),
                x => x.MatchCall<Player>(nameof(Player.FreeHand)),
                x => x.MatchLdcI4(out _),
                x => x.MatchBeq(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.SlugCatClass)),
                x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)),
                x => x.MatchCall(out _)
            ))
            {
                static bool CanCraft(bool orig, Player player)
                {
                    // Plugin.Log("MAKE IT CRAFT !!!");
                    if (StaticChargeManager.TryGetManager(player.abstractCreature, out var SCM))
                    {
                        return true;
                    }
                    if (player.GetAEC()?.realizedObject is not null)
                    {
                        return true;
                    }
                    return orig;
                }
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(CanCraft);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("MSC IL 1 ends");
    }
    private static bool ElectricSpear_SparkIsElectric(On.MoreSlugcats.ElectricSpear.orig_CheckElectricCreature orig, ElectricSpear self, Creature otherObject)
    {
        if (StaticChargeManager.TryGetManager(otherObject.abstractCreature, out var SCM) && SCM.Charge > 50f)
        {
            SCM.Charge -= 50f;
            return true;
        }
        return orig(self, otherObject);
    }
    private static void Player_BTW_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
    {
        if (StaticChargeManager.TryGetManager(self.abstractCreature, out var SCM))
        {
            self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);
            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i] != null)
                {
                    AbstractPhysicalObject abstractPhysicalObject = self.grasps[i].grabbed.abstractPhysicalObject;
                    if (abstractPhysicalObject.type == ObjectType.Spear && (abstractPhysicalObject as AbstractSpear).electric)
                    {
                        AbstractSpear abstractSpear = abstractPhysicalObject as AbstractSpear;
                        if (abstractSpear.realizedObject is ElectricSpear electricSpear)
                        {
                            if (SCM.IsOvercharged)
                            {
                                SCM.Charge -= SCM.FullECharge > 0 ? SCM.FullECharge : SCM.MaxECharge;

                                self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.firstChunk);
                                self.room.AddObject(new Explosion.ExplosionLight(electricSpear.firstChunk.pos, 50f, 1f, 4, new Color(0.7f, 1f, 1f)));
                                electricSpear.Spark();
                                electricSpear.Zap();
                                abstractSpear.electricCharge++;

                                if (abstractSpear.electricCharge > 3 && UnityEngine.Random.value < 0.1f * abstractSpear.electricCharge)
                                {
                                    BTWFunc.CustomKnockback(self, electricSpear.firstChunk.pos - self.mainBodyChunk.pos, 3f + UnityEngine.Random.value * 3f);
                                    electricSpear.ExplosiveShortCircuit();
                                    self.Stun(BTWFunc.FrameRate * 5);
                                }
                                SCM.Discharge(50f, 0.25f, 0, self.mainBodyChunk.pos, 1f);
                            }
                            else if (abstractSpear.electricCharge > 0)
                            {
                                SCM.overchargeImmunity = Mathf.Max(SCM.overchargeImmunity, BTWFunc.FrameRate * 20);
                                SCM.Charge += SCM.MaxECharge > SCM.FullECharge ? (SCM.MaxECharge - SCM.FullECharge) : SCM.FullECharge;

                                SCM.Discharge(80f, 0.85f, 0, self.mainBodyChunk.pos, 1f);

                                abstractSpear.electricCharge--;
                            }
                        }
                    }
                    else if (abstractPhysicalObject.type == AbstractTristor.TristorType && (abstractPhysicalObject as AbstractTristor).Charged)
                    {
                        AbstractTristor abstractTristor = abstractPhysicalObject as AbstractTristor;

                        SCM.endlessCharge = Mathf.Max(SCM.endlessCharge, BTWFunc.FrameRate * 20);
                        SCM.overchargeImmunity = Mathf.Max(SCM.overchargeImmunity, BTWFunc.FrameRate * 60);
                        SCM.Discharge(120f, 0.85f, 0, self.mainBodyChunk.pos, 0.75f);

                        abstractTristor.charge--;
                    }
			        return;
                }
            }
			return;
        }
        if (self.GetAEC()?.realizedObject is EnergyCore energyCore)
        {
            if (self.grasps[0] != null && self.grasps[0].grabbed is VoidCrystal voidCrystal)
            {
                self.ReleaseGrasp(0);
                voidCrystal.RemoveFromRoom();
                self.room.abstractRoom.RemoveEntity(voidCrystal.abstractPhysicalObject);

                energyCore.AEC.energy += voidCrystal.abstractVoidCrystal.containedVoidEnergy * 500f;

                self.room.PlaySound(SoundID.Spear_Bounce_Off_Wall, self.mainBodyChunk, false, 0.35f, BTWFunc.Random(2.6f, 2.85f));
                self.room.PlaySound(SoundID.Rock_Hit_Wall, self.mainBodyChunk, false, 0.45f, BTWFunc.Random(3f, 3.1f));
                self.room.PlaySound(SoundID.Weapon_Skid, self.mainBodyChunk, false, 0.75f, BTWFunc.Random(1.7f, 1.75f));
		        self.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Core_On, energyCore.firstChunk, false, 1f, BTWFunc.Random(2.1f, 2.5f));
		        self.room.AddObject(new ReverseShockwave(energyCore.firstChunk.pos, 30f, 0.045f, 7, false));
                VoidSpark.MakeDraggedSparks(self.room, 20f, energyCore.firstChunk.pos, (byte)BTWFunc.RandInt(15, 25), voidCrystal.baseColor, 0.2f);
			    return;
            }
			return;
        }
        orig(self);
    }
    private static ObjectType Player_BTW_CraftingResult(On.Player.orig_CraftingResults orig, Player self)
    {
        if (StaticChargeManager.TryGetManager(self.abstractCreature, out var SCM))
        {
            Creature.Grasp[] grasps = self.grasps;
            // bool holdingUp = SCM.IntDirectionalInput.y == 1;
            for (int i = 0; i < grasps.Length; i++)
            {
                if (grasps[i] != null && grasps[i].grabbed is IPlayerEdible && (grasps[i].grabbed as IPlayerEdible).Edible)
                {
                    return null;
                }
            }
            if (grasps[0] != null && grasps[0].grabbed is Spear spear1 && StaticManager_CanCraftSpear(SCM, spear1))
            {
                return ObjectType.Spear;
            }
            if (grasps[0] != null && grasps[0].grabbed is Tristor tristor1 && tristor1.AT.Charged)
            {
                return AbstractTristor.TristorType;
            }
            if (self.objectInStomach == null && grasps[0] == null 
                && grasps[1] != null && grasps[1].grabbed is Spear spear2 && StaticManager_CanCraftSpear(SCM, spear2))
            {
                return ObjectType.Spear;
            }
            if (self.objectInStomach == null && grasps[0] == null 
                && grasps[1] != null && grasps[1].grabbed is Tristor tristor2 && tristor2.AT.Charged)
            {
                return AbstractTristor.TristorType;
            }
			return null;
        }
        if (self.GetAEC()?.realizedObject is not null)
        {
            Creature.Grasp[] grasps = self.grasps;
        
            for (int i = 0; i < grasps.Length; i++)
            {
                if (grasps[i] != null && grasps[i].grabbed is IPlayerEdible && (grasps[i].grabbed as IPlayerEdible).Edible)
                {
                    return null;
                }
            }

            if (grasps[0] != null && grasps[0].grabbed is VoidCrystal)
            {
                return AbstractVoidCrystal.VoidCrystalType;
            }
			return null;
        }
        return orig(self);
    }
    private static bool Player_StaticChargeManager_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
    {
        if (StaticChargeManager.TryGetManager(self.abstractCreature, out var _))
        {
            return self.CraftingResults() != null;
        }
        if (self.GetAEC()?.realizedObject is not null)
        {
            return self.CraftingResults() != null;
        }
        return orig(self);
    }
}