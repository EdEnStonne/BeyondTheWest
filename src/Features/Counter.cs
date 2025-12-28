using UnityEngine;

namespace BeyondTheWest;

public class Counter
{
    public Counter() {}
    public Counter(uint max)
    {
        this.max = max;
    }

    public void Reset()
    {
        this._count = 0;
    }
    public void Reset(uint max)
    {
        this.max = max;
        Reset();
    }
    public void ResetUp()
    {
        this._count = this._max;
    }
    public void ResetUp(uint max)
    {
        this.max = max;
        ResetUp();
    }
    public void Tick()
    {
        if (this._count < this._max)
        {
            this._count++;
        }
    }
    public void Add()
    {
        Tick();
    }
    public void Add(uint num)
    {
        this.value += num;
    }
    public void Substract()
    {
        if (this._count > 0)
        {
            this._count--;
        }
    }
    public void Substract(uint num)
    {
        this.value -= num;
    }
    public void Up()
    {
        Add();
    }
    public void Up(uint num)
    {
        Add(num);
    }
    public void Down()
    {
        Substract();
    }

    public void Down(uint num)
    {
        Substract(num);
    }

    private uint _max = BTWFunc.FrameRate;
    private uint _count = 0;

    public float fractInv
    {
        get
        {
            return 1 - this.fract;
        }
        set
        {
            this.fract = 1 - value;
        }
    }
    public float fract
    {
        get
        {
            return Mathf.Clamp01((float)this._count/this._max);
        }
        set
        {
            this._count = (uint)Mathf.Clamp01((float)value/this._max);
        }
    }
    public uint max
    {
        get
        {
            return this._max;
        }
        set
        {
            this._max = value;
            if (this._max == 0)
            {
                this._max = 1;
            }
            if (this._count > this._max)
            {
                this._count = this._max;
            }
        }
    }
    public bool ended
    {
        get
        {
            return this._count >= this._max;
        }
    }
    public bool reachedMax
    {
        get
        {
            return this.ended;
        }
    }
    public bool atZero
    {
        get
        {
            return this._count <= 0;
        }
    }
    public uint value
    {
        get
        {
            return this.valueUp;
        }
        set
        {
            this.valueUp = value;
        }
    }
    public uint valueUp
    {
        get
        {
            return this._count;
        }
        set
        {
            this._count = (uint)Mathf.Clamp(value, 0, this._max);
        }
    }
    public uint valueDown
    {
        get
        {
            return this._max - this._count;
        }
        set
        {
            this._count = (uint)Mathf.Clamp(this._max - value, 0, this._max);
        }
    }
}