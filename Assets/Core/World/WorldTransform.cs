using System;
using UnityEngine;

namespace World
{
[Serializable]
public sealed class WorldTransform
{
    [SerializeField] private WorldVector3D position;

    [SerializeField] private Quaternion rotation = Quaternion.identity;
    [SerializeField] private Vector3 scale = Vector3.one;

    public WorldVector3D Position
    {
        get => position;
        set => position = value;
    }

    public Quaternion Rotation
    {
        get => rotation;
        set => rotation = value;
    }

    public Vector3 Scale
    {
        get => scale;
        set => scale = value;
    }

    public void SetPosition(WorldVector3D newPosition)
    {
        position = newPosition;
    }

    public void SetFromUnityTransform(Transform sourceTransform)
    {
        if (sourceTransform == null)
            return;

        position = new WorldVector3D(
            sourceTransform.position.x,
            sourceTransform.position.y,
            sourceTransform.position.z
        );
        rotation = sourceTransform.rotation;
        scale = sourceTransform.lossyScale;
    }

    public Vector3 GetLocalUnityPosition(double originX, double originY, double originZ)
    {
        return new Vector3(
            (float)(position.X - originX),
            (float)(position.Y - originY),
            (float)(position.Z - originZ)
        );
    }

    public Vector3 GetLocalUnityPosition(WorldVector3D worldOrigin)
    {
        return GetLocalUnityPosition(worldOrigin.X, worldOrigin.Y, worldOrigin.Z);
    }
}
}
