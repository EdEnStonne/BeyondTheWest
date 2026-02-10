using UnityEngine;
using BeyondTheWest;
using System.Collections.Generic;
using BeyondTheWest.MeadowCompat;
using System;
using System.Linq;

namespace BeyondTheWest.Items;

public class AbstractVoidCrystal : AbstractPhysicalObject
{
    public static List<IconLayerCount> VoidCrystalLayersCount = new()
    {
        new() {name = "Base", count = 3, fragment = 4},
        new() {name = "DShard", count = 2, fragment = 4},
        new() {name = "Shard", count = 3, fragment = 2},
        new() {name = "Frag", count = 6, fragment = 2}
    };
    public static AbstractObjectType VoidCrystalType;
    public static MultiplayerUnlocks.SandboxUnlockID VoidCrystalUnlock;
    public static IconSymbol.IconSymbolData VoidCrystalIconData;
    public static string VoidCrystalIconName = "VoidCrystalIcon";
    public static Color VoidCrystalIconColor = new Color(1f, 0.2f, 0.5f);
    public AbstractVoidCrystal(World world, VoidCrystal realizedObject, WorldCoordinate pos, EntityID ID) : base(world, VoidCrystalType, null, pos, ID)
    {
        this.realizedObject = realizedObject;
        this.appearance = new(this);
        this.containedVoidEnergy = this.appearance.shard * 0.75f;
        this.blue = BTWFunc.Random(0.3f, 0.8f);
        this.red = Mathf.Clamp01(BTWFunc.Random(0.5f, 2f));
    }

    public bool isMeadowInit = false;
    public float containedVoidEnergy = 0.75f;
    public float blue = 0.5f;
    public float red = 1f;
    public VoidCrystalAppearance appearance;
    public override void Update(int time)
    {
        base.Update(time);
        
        if (BTWPlugin.meadowEnabled && !this.isMeadowInit)
        {
            MeadowCalls.BTWItems_AbstractVoidCrystalInit(this);
        }
    }
    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new VoidCrystal(this)
        {
            baseColor = new(red, 0.2f, blue)
        };
    }


    public override string ToString()
    {
        return SaveHelper.ToSaveString(this, 
            appearance.ToString(), (int)(containedVoidEnergy * 100), (int)(blue * 100), (int)(red * 100));
    }

    public class VoidCrystalAppearance
    {
        public static string GetTextureName(string layer, int variant, int fragment)
        {
            return $"Crystal{layer}{variant}_{fragment}";
        }
        public static string GetRandomTextureName()
        {
            int[] dices = new int[3];
            dices[0] = BTWFunc.RandInt(0, VoidCrystalLayersCount.Count - 1);
            dices[1] = BTWFunc.RandInt(1, VoidCrystalLayersCount[dices[0]].count);
            dices[2] = BTWFunc.RandInt(1, VoidCrystalLayersCount[dices[0]].fragment);
            return GetTextureName(VoidCrystalLayersCount[dices[0]].name, dices[1], dices[2]);
        }
        public static int GetFragmentAmoutOfLayer(string layer)
        {
            int layerIndex = VoidCrystalLayersCount.FindIndex(x => x.name == layer);
            if (layerIndex > 0)
            {
                return VoidCrystalLayersCount[layerIndex].fragment;
            }
            return 0;
        }
        public static string[] GetTexturesOfLayer(string layer, int textureIndex)
        {
            int layerIndex = VoidCrystalLayersCount.FindIndex(x => x.name == layer);
            string[] textures = new string[VoidCrystalLayersCount[layerIndex].fragment];
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = GetTextureName(layer, textureIndex, i + 1);
            }
            return textures;
        }
        public static string[] GetRandomTexturesOfLayer(string layer, out int randomTextureIndex)
        {
            int layerIndex = VoidCrystalLayersCount.FindIndex(x => x.name == layer);
            randomTextureIndex = BTWFunc.RandInt(1, VoidCrystalLayersCount[layerIndex].count);
            return GetTexturesOfLayer(layer, randomTextureIndex);
        }
        public static VoidCrystalAppearance FromString(string saveString)
        {
            int layers = saveString.Length / 3;
            VoidCrystalAppearance appearance = new()
            {
                texture = new string[layers][],
                layerIndex = new int[layers],
                layerRotation = new int[layers],
                layerName = new string[layers],
                textureCount = 0,
                shard = 1
            };

            for (int i = 0; i < layers; i++)
            {
                string baseName = saveString[0 + i * 3] == 'D' ? "DShard" : saveString[0 + i * 3] == 'S' ? "Shard" : "Frag";
                int baseIndex = int.Parse($"{saveString[1 + i * 3]}");
                int rot = int.Parse($"{saveString[2 + i * 3]}");

                if (i == 0)
                {
                    appearance.texture[i] = GetTexturesOfLayer("Base", baseIndex);
                    appearance.layerName[i] = "Base";
                }
                else 
                {
                    if (baseName == "DShard")
                    {
                        appearance.shard += 2;
                    }
                    else if (baseName == "Shard")
                    {
                        appearance.shard += 1;
                    }
                    appearance.layerName[i] = baseName;
                    appearance.texture[i] = GetTexturesOfLayer(appearance.layerName[i], baseIndex);
                }
                
                appearance.layerIndex[i] = baseIndex;
                appearance.layerRotation[i] = rot;
                appearance.textureCount += appearance.texture[i].Length;
            }

            return appearance;
        }


        public VoidCrystalAppearance() {}
        public VoidCrystalAppearance(Settings settings) : this(null, settings) {}
        public VoidCrystalAppearance(AbstractVoidCrystal abstractVoidCrystal) : this(abstractVoidCrystal, new Settings()) {}
        public VoidCrystalAppearance(AbstractVoidCrystal abstractVoidCrystal, Settings settings)
        {
            this.abstractVoidCrystal = abstractVoidCrystal;
            
            int layers = settings.layers;
            this.texture = new string[layers][];
            this.layerIndex = new int[layers];
            this.layerRotation = new int[layers];
            this.layerName = new string[layers];
            this.textureCount = 0;
            this.shard = 1;

            for (int i = 0; i < layers; i++)
            {
                if (i == 0)
                {
                    this.texture[i] = GetRandomTexturesOfLayer("Base", out int baseIndex);
                    this.layerName[i] = "Base";
                    this.layerIndex[i] = baseIndex;
                    this.layerRotation[i] = 0;
                }
                else 
                {
                    if (BTWFunc.Chance(settings.doubleShardChance) && this.shard <= settings.maxShard - 2)
                    {
                        this.layerName[i] = "DShard";
                        this.shard += 2;
                    }
                    else if (BTWFunc.Chance(settings.shardChance) && this.shard <= settings.maxShard - 1)
                    {
                        this.layerName[i] = "Shard";
                        this.shard += 1;
                    }
                    else
                    {
                        this.layerName[i] = "Frag";
                    }
                    this.texture[i] = GetRandomTexturesOfLayer(this.layerName[i], out int fragIndex);
                    this.layerIndex[i] = fragIndex;
                    this.layerRotation[i] = BTWFunc.RandInt(0, 3);
                }
                this.textureCount += this.texture[i].Length;
            }
        }

        public Vector2 GetTextureCoordFromLinearCoord(int coord)
        {
            int count = 0;
            for (int i = 0; i < texture.Length; i++)
            {
                for (int j = 0; j < texture[i].Length; j++)
                {
                    if (count == coord)
                    {
                        return new Vector2(i, j);
                    }
                    count++;
                }
            }
            return Vector2.zero;
        }
        public int GetLinearCoordFromTextureCoord(Vector2 coord)
        {
            int count = 0;
            for (int i = 0; i < texture.Length; i++)
            {
                for (int j = 0; j < texture[i].Length; j++)
                {
                    if (new Vector2(i, j) == coord)
                    {
                        return count;
                    }
                    count++;
                }
            }
            return 0;
        }

        // Format (i = index, r = rot, L = layer) :"Bi0LirLirLir"
        public override string ToString()
        {
            string saveString = "";
            for (int i = 0; i < this.texture.Length; i++)
            {
                saveString += this.layerName[i][0];
                saveString += this.layerIndex[i].ToString();
                saveString += this.layerRotation[i].ToString();
            }
            return saveString;
        }

        public AbstractVoidCrystal abstractVoidCrystal;
        public int textureCount;
        public string[][] texture;
        public int[] layerRotation;
        public int[] layerIndex;
        public string[] layerName;
        public int shard;

        public const int layers = 5;
        public const float doubleShardChance = 0.1f;
        public const float shardChance = 0.4f;
        public const int maxShard = 5;
        public string[] AllTextures
        {
            get
            {
                string[] t = new string[this.textureCount];
                int count = 0;
                for (int i = 0; i < texture.Length; i++)
                {
                    for (int j = 0; j < texture[i].Length; j++)
                    {
                        t[count++] = texture[i][j];
                    }
                }
                return t;
            }
        }

        public struct Settings
        {
            public Settings(){}
            public Settings(int layers, float doubleShardChance, float shardChance, int maxShard)
            {
                this.layers = layers;
                this.doubleShardChance = doubleShardChance;
                this.shardChance = shardChance;
                this.maxShard = maxShard;
            }
            public int layers = VoidCrystalAppearance.layers;
            public float doubleShardChance = VoidCrystalAppearance.doubleShardChance;
            public float shardChance = VoidCrystalAppearance.shardChance;
            public int maxShard = VoidCrystalAppearance.maxShard;
        }
    }
}

public static class VoidCrystalHooks
{
    public static void Register()
    {
        Futile.atlasManager.ActuallyLoadAtlasOrImage(AbstractVoidCrystal.VoidCrystalIconName, "icons/icon_Crystal", "");
        foreach (IconLayerCount iconLayer in AbstractVoidCrystal.VoidCrystalLayersCount)
        {
            for (int i = 1; i <= iconLayer.count; i++)
            {
                for (int f = 1; f <= iconLayer.fragment; f++)
                {
                    Futile.atlasManager.ActuallyLoadAtlasOrImage(
                        AbstractVoidCrystal.VoidCrystalAppearance.GetTextureName(iconLayer.name, i, f), 
                        $"assets/Crystal/{iconLayer.name}_{i}_Crystal_{f}", "");
                }
            }
        }

        AbstractVoidCrystal.VoidCrystalType = new("VoidCrystal", true);
        AbstractVoidCrystal.VoidCrystalUnlock = new("VoidCrystal", true);
        AbstractVoidCrystal.VoidCrystalIconData = new(CreatureTemplate.Type.StandardGroundCreature, AbstractVoidCrystal.VoidCrystalType, 0);
        MultiplayerUnlocks.ItemUnlockList.Add(AbstractVoidCrystal.VoidCrystalUnlock);
        
        BTWPlugin.Log($"Registered AbstractVoidCrystal ! Type : [{AbstractVoidCrystal.VoidCrystalType}], Unlock [{AbstractVoidCrystal.VoidCrystalUnlock}]");   
    }
    public static void ApplyHooks()
    {
        BTWPlugin.Log("VoidCrystalHooks ApplyHooks Done !");    
    }
}