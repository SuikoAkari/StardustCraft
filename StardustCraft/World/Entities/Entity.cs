using StardustCraft.Physics;
using StardustCraft.World.Blocks;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.World.Entities;
public struct Ray
{
    public Block? Block { get; set; }
    public Vector3i Position { get; set; }
    public Vector3i Normal { get; set; }
    public Vector3 End { get; set; }

    public Ray()
    {
        Block = null;
        Position = Vector3i.Zero;
        Normal = Vector3i.Zero;
        End = Vector3.Zero;
    }

    public Ray(Block block, Vector3i position, Vector3i normal, Vector3 end)
    {
        Block = block;
        Position = position;
        Normal = normal;
        End = end;
    }
}
public struct TransformSnapshot
{
    public Vector3 Prev;
    public Vector3 Curr;
}
public abstract class Entity
{
    public Vector3 Position = Vector3.Zero;
    public Vector3 PreviousPosition = Vector3.Zero;
    public Vector3 FinalPosition = Vector3.Zero;
    public Vector3 RenderPos = Vector3.Zero;
    public Vector3 Velocity = Vector3.Zero;
    public Vector3 Acceleration = Vector3.Zero;
    public Vector3 Size = new Vector3(1, 1, 1);
    public bool IsActive = true;
    public bool HasCollision = true;
    public bool IsAffectedByGravity = true;
    public bool IsOnGround = false;
    public float GravityScale = 1.0f;
    public float Drag = 0.1f;
    public abstract AABB GetAABB();
    public abstract void Update(float deltaTime);
    public abstract void OnCollision(Vector3 normal, float penetration, CollisionType type);

    // Metodi di movimento
    public void AddForce(Vector3 force)
    {
        Acceleration += force;
    }

    public void AddImpulse(Vector3 impulse)
    {
        Velocity += impulse;
    }

    public void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = Vector3.Normalize(target - Position);
        Velocity = direction * speed;
    }
}