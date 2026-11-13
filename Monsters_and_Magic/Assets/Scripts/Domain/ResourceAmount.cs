using System;

[Serializable]
public struct ResourceAmount
{
    public ResourceConfig resource;
    public int amount;

    public ResourceAmount(ResourceConfig resource, int amount)
    {
        this.resource = resource;
        this.amount = amount;
    }
}
