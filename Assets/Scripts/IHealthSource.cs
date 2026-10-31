using System;

public interface IHealthSource
{
    float Health { get; }
    float Armor { get; }
    event Action Changed;
}
