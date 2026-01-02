using StardustCraft.Physics;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardustCraft.World.Blocks;

namespace StardustCraft.World.Entities
{
    public class PlayerEntity : Entity
    {
        public float MoveSpeed = 6f;
        public float JumpForce = 6f;

        private bool isGrounded=true;
        public int selectedInventorySlot = 0;
        public BlockType[] inventory=new BlockType[9] { BlockType.Dirt, BlockType.Grass, BlockType.Stone, BlockType.OakPlanks, BlockType.OakLog, BlockType.Sand, BlockType.Gravel,BlockType.SnowGrass,BlockType.Gravel};
        
        public PlayerEntity(Vector3 startPosition)
        {
            Position = startPosition;
            Size = new Vector3(0.6f, 1.8f, 0.6f);
            HasCollision = true;
            IsAffectedByGravity = true;
        }
        public override AABB GetAABB()
        {
            Vector3 halfSize = Size * 0.5f;
            return new AABB
            {
                Min = Position - halfSize,
                Max = Position + halfSize
            };
        }
        public override void OnCollision(Vector3 normal, float penetration, CollisionType type)
        {
            // Gestisci collisioni specifiche del player
            switch (type)
            {
                case CollisionType.Ground:
                    // Suono atterraggio, effetti particellari...
                    break;

                case CollisionType.Ceiling:
                    // Colpito il soffitto
                    break;

                case CollisionType.Horizontal:
                    // Colpito un muro
                    break;
            }
        }
        public override void Update(float deltaTime)
        {
            
        }
        public void Move(Vector3 input, float yaw, float deltaTime)
        {
            // direzione camera → direzione movimento
            Vector3 forward = new(
                MathF.Cos(MathHelper.DegreesToRadians(yaw)),
                0,
                MathF.Sin(MathHelper.DegreesToRadians(yaw))
            );
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

            Vector3 wishDir = forward * input.Z + right * input.X;
            if (wishDir.LengthSquared > 0)
                wishDir = Vector3.Normalize(wishDir);

            var vel = Velocity;
            

            vel.X = wishDir.X * MoveSpeed;
            vel.Z = wishDir.Z * MoveSpeed;
            Velocity = vel;
        }

        public void Jump()
        {
            if (!isGrounded) return;

            var vel = Velocity;
            vel.Y = JumpForce;
            Velocity = vel;
        }
    }
}
