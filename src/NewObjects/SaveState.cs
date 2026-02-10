using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BeyondTheWest.MSCCompat;
using RWCustom;
using UnityEngine;

namespace BeyondTheWest.Items;

public static class SaveHelper
{
    public static List<AbstractPhysicalObject.AbstractObjectType> customSupportedTypes = new()
    {
        AbstractTristor.TristorType,
        AbstractVoidCrystal.VoidCrystalType,
        AbstractPhysicalObject.AbstractObjectType.Spear
    };

    public static bool Supported(string objType) 
        => customSupportedTypes.Exists(x => x.ToString() == objType);
    public static bool Supported(AbstractPhysicalObject.AbstractObjectType objType) 
        => customSupportedTypes.Exists(x => x == objType);

    public static string ToSaveString(AbstractPhysicalObject apo, params object[] attributes) // Mostly taken from FisObs
    {
        string CustomData = "";
        foreach (var item in attributes)
        {
            if (CustomData == "")
            {
                CustomData = $"{item}";
            }
            else
            {
                CustomData = $"{CustomData}<oA>{item}";
            }
        }
        if (CustomData == "")
        {
            return $"{apo.ID}<oA>{apo.type}<oA>{apo.pos}";
        }
        
        return $"{apo.ID}<oA>{apo.type}<oA>{apo.pos}<oA>{CustomData}";
    }

    public static AbstractPhysicalObject GetCustomObject(World world, string objString)
    {
        AbstractPhysicalObject result = null;
        try
        {
            string[] data = objString.Split(new[] { "<oA>" }, StringSplitOptions.None);
            string type = data[1];
            if (Supported(type))
            {
                int rippleLayer = 0;
                EntityID id;
                if (data[0].Contains("<oB>"))
                {
                    string[] dataSplit = Regex.Split(data[0], "<oB>");
                    id = EntityID.FromString(dataSplit[0]);
                    rippleLayer = int.Parse(dataSplit[1]);
                }
                else
                {
                    id = EntityID.FromString(data[0]);
                }
                WorldCoordinate coord = WorldCoordinate.FromString(data[2]);
                string[] customData = data.Length > 3 ? data.Skip(3).ToArray() : new string[0]{};
                BTWPlugin.Log($"Loading custom item with : type [{type}], id [{id}], coords {coord}, data [{string.Join(", ", customData)}]");

                if (type == AbstractTristor.TristorType.ToString())
                {
                    result = new AbstractTristor(world, null, coord, id)
                    {
                        charge = int.TryParse(customData[0], out var c) ? c : 0,
                        mainHue = float.TryParse(customData[1], out var mh) ? mh : 0.5f,
                        secHue = float.TryParse(customData[2], out var sh) ? sh : 0.5f,
                        shard1Type = int.TryParse(customData[3], out var s1t) ? s1t : 1,
                        shard2Type = int.TryParse(customData[4], out var s2t) ? s2t : 1,
                        rockType = int.TryParse(customData[5], out var rt) ? rt : 1,
                        coreType = int.TryParse(customData[6], out var ct) ? ct : 1,
                        rotationOffset = new float[2]
                        {
                            float.TryParse(customData[7], out var r1) ? r1 : BTWFunc.Random(360),
                            float.TryParse(customData[8], out var r2) ? r2 : BTWFunc.Random(360)
                        },
                    };
                    BTWPlugin.Log($"Loaded custom AbstractTristor from [{objString}] !");
                }
                else if (type == AbstractVoidCrystal.VoidCrystalType.ToString())
                {
                    result = new AbstractVoidCrystal(world, null, coord, id)
                    {
                        appearance = AbstractVoidCrystal.VoidCrystalAppearance.FromString(customData[0]),
                        containedVoidEnergy = int.TryParse(customData[1], out var cve) ? cve / 100f : 0.75f,
                        blue = int.TryParse(customData[2], out var b) ? b / 100f : 0.5f,
                        red = int.TryParse(customData[3], out var r) ? r / 100f : 1f,
                    };
                    (result as AbstractVoidCrystal).appearance.abstractVoidCrystal = result as AbstractVoidCrystal;
                    BTWPlugin.Log($"Loaded custom AbstractVoidCrystal from [{objString}] !");
                }
                else if (type == AbstractPhysicalObject.AbstractObjectType.Spear.ToString())
                {
                    if (data[4] == "BTWC")
                    {
                        result = new AbstractCrystalSpear(world, null, coord, id)
                        {
                            stuckInWallCycles = int.TryParse(data[3], out var siwc) ? siwc : 0,
                            appearance = AbstractVoidCrystal.VoidCrystalAppearance.FromString(data[5]),
                            blue = int.TryParse(data[6], out var b) ? b / 100f : 0.5f,
                            red = int.TryParse(data[7], out var r) ? r / 100f : 1f,
                        };
                        BTWPlugin.Log($"Loaded custom AbstractCrystalSpear from [{objString}] !");
                    }
                    else
                    {
                        result = null;
                        return null;
                    }
                }
                else 
                {
                    BTWPlugin.logger.LogError($"Attempted to load custom object [{objString}], which wasn't detected properly..?");
                    result = null;
                }
            }
            else
            {
                BTWPlugin.logger.LogError($"Attempted to load custom object [{objString}], which is NOT SUPPORTED BY BTW !");
                result = null;
            }
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError($"[EXCEPTION] Couldn't load custom BTW object from string ! " +
                "\n" +
                objString +
                "\n" +
                ex.Message +
                "\n" +
                ex.StackTrace);
            result = null;
        }
        return result;
    }
}