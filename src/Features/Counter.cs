using UnityEngine;

namespace BeyondTheWest;

public class Counter
{
    public Counter(int max)
    {
        this.max = max;
        this._count = 0;
    }

    public void Reset()
    {
        this._count = 0;
    }
    public void Reset(int max)
    {
        this.max = max;
        Reset();
    }
    public void ResetUp()
    {
        this._count = this._max;
    }
    public void ResetUp(int max)
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
    public void Add(int num)
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
    public void Substract(int num)
    {
        if (this._count > 0)
        {
            this._count -= (int)Mathf.Min(this._count, num); // overflow my beloved
        }
    }
    public void Up()
    {
        Add();
    }
    public void Up(int num)
    {
        Add(num);
    }
    public void Down()
    {
        Substract();
    }

    public void Down(int num)
    {
        Substract(num);
    }

    public override string ToString()
    {
        return $"{_count}/{_max}";
    }

    private int _max = BTWFunc.FrameRate;
    private int _count = 0;

    public float fractInv
    {
        get
        {
            return 1 - this.fract;
        }
    }
    public float fract
    {
        get
        {
            return Mathf.Clamp01((float)this._count/this._max);
        }
    }
    public int max
    {
        get
        {
            return this._max;
        }
        set
        {
            this._max = value;
            if (this._max <= 0)
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
    public int value
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
    public int valueUp
    {
        get
        {
            return this._count;
        }
        set
        {
            this._count = (int)Mathf.Clamp(value, 0, this._max);
        }
    }
    public int valueDown
    {
        get
        {
            return this._max - this._count;
        }
        set
        {
            this._count = (int)Mathf.Clamp(this._max - value, 0, this._max);
        }
    }
}