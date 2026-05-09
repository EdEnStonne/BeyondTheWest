using UnityEngine;
using BeyondTheWest;
using static Pom.Pom;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BeyondTheWest;
public class NumberDisplay : UpdatableAndDeletable, IDrawable
{
    public static ConditionalWeakTable<Room, Dictionary<int, NumberDisplay>> numberDisplayTable = new();
    public NumberDisplay(PlacedObject placedObject, Room room) : base()
    {
        this.room = room;
        this.pos = placedObject.pos;
        this.id = ((ManagedData)placedObject.data).GetValue<int>(POMFIELD_ID);
        this.value = ((ManagedData)placedObject.data).GetValue<int>(POMFIELD_VALUE);
        this.alignement = ((ManagedData)placedObject.data).GetValue<Alignement>(POMFIELD_ALIGN);
        this.scale = ((ManagedData)placedObject.data).GetValue<float>(POMFIELD_SCALE);
        this.alpha = ((ManagedData)placedObject.data).GetValue<float>(POMFIELD_ALPHA);
        this.visible = ((ManagedData)placedObject.data).GetValue<bool>(POMFIELD_VISIBLE);
        this.small = ((ManagedData)placedObject.data).GetValue<bool>(POMFIELD_SMALL);

        numberDisplayTable.GetOrCreateValue(this.room).Add(this.id, this);
    }

    public override void Destroy()
    {
        base.Destroy();
        if (numberDisplayTable.TryGetValue(this.room, out var dict))
        {
            dict.Remove(this.id);
        }
    }

    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
		    rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[i]);
        }
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[12];
        sLeaser.sprites[0] = new FSprite($"BTW-icon{(this.small ? "S" : "")}", true)
        {
            scale = this.scale / scaleDiviser,
            alpha = 0f,
            color = this.color
        };
        if (shader != "")
        {
            sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders[shader];
        }
        for (int i = 1; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i] = new FSprite($"BTW0icon{(this.small ? "S" : "")}", true)
            {
                scale = sLeaser.sprites[0].scale,
                alpha = sLeaser.sprites[0].alpha,
                color = sLeaser.sprites[0].color,
                shader = sLeaser.sprites[0].shader,
            };
        }
        this.AddToContainer(sLeaser, rCam, null);
    }

    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {}

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        int numberAmount = 
            value == 0 ? 1 :
            value < 0 ? 1 + (int)Mathf.Log10(-value) :
            1 + (int)Mathf.Log10(value);
        int characterAmount = value < 0 ? numberAmount + 1 : numberAmount;

        this.DisplaySize = new Vector2(iconSizePixel.x * characterAmount, iconSizePixel.y) * scale / scaleDiviser;
        
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            int num = i == 0 ? 0 : (Mathf.Abs(value) / (int)Mathf.Pow(10, i - 1)) % 10;
            int pos = i == 0 ? 0 : characterAmount - i;

            sLeaser.sprites[i].scale = this.scale / scaleDiviser;
            sLeaser.sprites[i].alpha = 
                !visible ? 0 :
                i == 0 ? (value < 0 ? 1 : 0) :
                i <= numberAmount ? this.alpha : 0;
            sLeaser.sprites[i].color = this.color;
            sLeaser.sprites[i].x = this.pos.x 
                - iconSizePixel.x * (characterAmount - pos) * scale / scaleDiviser 
                + (alignement == Alignement.Left ? this.DisplaySize.x : alignement == Alignement.Right ? iconSizePixel.x * scale / scaleDiviser : this.DisplaySize.x/2f + iconSizePixel.x * scale / (scaleDiviser * 2))
                - camPos.x;
            sLeaser.sprites[i].y = this.pos.y - camPos.y;
            sLeaser.sprites[i].shader = sLeaser.sprites[i].alpha == 0 ? FShader.defaultShader : rCam.room.game.rainWorld.Shaders[shader];
            if (i != 0)
            {
                sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName($"BTW{num}icon{(this.small ? "S" : "")}");
            }
        }
    }
    
    public Vector2 pos;
    public readonly int id;
    public int value;
    public Color color = Color.white;
    public string shader = "GateHologram";
    public float scale = 0.5f;
    private const float scaleDiviser = 10f;
    public float alpha = 1f;
    public bool small = true;
    public Alignement alignement;
    public bool visible = true;
    public Vector2 DisplaySize {get; private set;}
    public static Vector2Int iconSizePixel = new(400, 400);

    public class Alignement : ExtEnum<Alignement>
	{
		public Alignement(string value, bool register = false) : base(value, register) { }
		public static Alignement Center;
		public static Alignement Left;
		public static Alignement Right;
	}
    public const string POMFIELD_ID = "ID";
    public const string POMFIELD_VALUE = "value";
    public const string POMFIELD_SCALE = "scale";
    public const string POMFIELD_ALPHA = "alpha";
    public const string POMFIELD_SMALL = "small";
    public const string POMFIELD_VISIBLE = "visible";
    public const string POMFIELD_ALIGN = "alignement";
    internal static void POMRegister()
    {
        Alignement.Center = new("Center", true);
        Alignement.Left = new("Left", true);
        Alignement.Right = new("Right", true);
        
        ManagedField[] fields = new ManagedField[7]
		{
			new IntegerField(POMFIELD_ID, 0, int.MaxValue, 0, ManagedFieldWithPanel.ControlType.arrows, "ID"),
			new IntegerField(POMFIELD_VALUE, int.MinValue + 1, int.MaxValue - 1, 0, ManagedFieldWithPanel.ControlType.text, "Value"),
			new FloatField(POMFIELD_SCALE, 0f, 10f, 1f, 0.1f, ManagedFieldWithPanel.ControlType.text, "Scale"),
			new FloatField(POMFIELD_ALPHA, 0f, 1f, 1f, 0.01f, ManagedFieldWithPanel.ControlType.slider, "Alpha"),
			new BooleanField(POMFIELD_SMALL, true, ManagedFieldWithPanel.ControlType.button, "Small Text"),
			new BooleanField(POMFIELD_VISIBLE, true, ManagedFieldWithPanel.ControlType.button, "Visible"),
			new ExtEnumField<Alignement>(POMFIELD_ALIGN, Alignement.Right, null, ManagedFieldWithPanel.ControlType.arrows, "Alignement")
		};
        
        RegisterFullyManagedObjectType(fields, typeof(NumberDisplay), "Number Display", "TutorialDisplay");
    }
}