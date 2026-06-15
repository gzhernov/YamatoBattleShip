using System;
using UnityEngine;

namespace World
{
[Serializable]
public struct WorldVector3D
{
    [SerializeField] private double x;
    [SerializeField] private double y;
    [SerializeField] private double z;

    public double X
    {
        get => x;
        set => x = value;
    }

    public double Y
    {
        get => y;
        set => y = value;
    }

    public double Z
    {
        get => z;
        set => z = value;
    }

    public WorldVector3D(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static WorldVector3D operator +(WorldVector3D left, Vector3 right)
    {
        return new WorldVector3D(
            left.x + right.x,
            left.y + right.y,
            left.z + right.z
        );
    }

    public Vector3 ToUnityVector3()
    {
        return new Vector3((float)x, (float)y, (float)z);
    }

    public override string ToString()
    {
        return $"({x:F2}, {y:F2}, {z:F2})";
    }
}
}
