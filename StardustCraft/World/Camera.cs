using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using StardustCraft.World.Blocks;
using StardustCraft.World.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.World
{
    public class Camera
    {
        public Vector3 Position = new Vector3(8, 10, 8);
        public float yaw = -90f;
        public float pitch = 0f;
        public Ray Ray;
        float speed = 10f;
        float sensitivity = 0.2f;
        Vector2 lastMousePos;
        public bool IsCursorLocked = true;
        bool firstMove = true;
        public void OnMouseMove(MouseMoveEventArgs e)
        {
            if (!IsCursorLocked)
            {
                Game.Instance.CursorState = CursorState.Normal;
                return;
            }
            Game.Instance.CursorState = CursorState.Grabbed;
            if (firstMove)
            {
                lastMousePos = e.Position;
                firstMove = false;
                return;
            }

            var deltaX = e.Position.X - lastMousePos.X;
            var deltaY = e.Position.Y - lastMousePos.Y;
            lastMousePos = e.Position;

            yaw += deltaX * sensitivity;
            pitch -= deltaY * sensitivity;

            pitch = Math.Clamp(pitch, -89f, 89f);
        }
        public Matrix4 GetView()
        {
            Vector3 front;
            front.X = MathF.Cos(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Sin(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
            front = Vector3.Normalize(front);

            Matrix4 view = Matrix4.LookAt(
                Position,
                Position + front,
                Vector3.UnitY
            );
            return view;
        }
        public void RayCast()
        {
            float maxDistance = 12f;
            Vector3 dir = new(
                    MathF.Cos(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch)),
                    MathF.Sin(MathHelper.DegreesToRadians(pitch)),
                    MathF.Sin(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch))
                );
            dir = Vector3.Normalize(dir);

            int x = (int)MathF.Floor(Position.X);
            int y = (int)MathF.Floor(Position.Y);
            int z = (int)MathF.Floor(Position.Z);

            int stepX = dir.X >= 0 ? 1 : -1;
            int stepY = dir.Y >= 0 ? 1 : -1;
            int stepZ = dir.Z >= 0 ? 1 : -1;

            float tDeltaX = MathF.Abs(1f / dir.X);
            float tDeltaY = MathF.Abs(1f / dir.Y);
            float tDeltaZ = MathF.Abs(1f / dir.Z);

            // Gestione precisa dei confini iniziali
            float tMaxX = (dir.X > 0) ? (MathF.Floor(Position.X) + 1 - Position.X) * tDeltaX
                                     : (Position.X - MathF.Floor(Position.X)) * tDeltaX;
            float tMaxY = (dir.Y > 0) ? (MathF.Floor(Position.Y) + 1 - Position.Y) * tDeltaY
                                     : (Position.Y - MathF.Floor(Position.Y)) * tDeltaY;
            float tMaxZ = (dir.Z > 0) ? (MathF.Floor(Position.Z) + 1 - Position.Z) * tDeltaZ
                                     : (Position.Z - MathF.Floor(Position.Z)) * tDeltaZ;

            // Se la posizione è esattamente sul bordo, tMaxX sarà 0. 
            // Per evitare loop infiniti o salti, forziamo i valori infiniti se dir è 0.
            if (dir.X == 0) tMaxX = float.PositiveInfinity;
            if (dir.Y == 0) tMaxY = float.PositiveInfinity;
            if (dir.Z == 0) tMaxZ = float.PositiveInfinity;

            Vector3i lastNormal = Vector3i.Zero;
            float t = 0;

            while (t <= maxDistance)
            {
                var block = Game.world.GetBlockAt(x, y, z);
                if (block.Type != BlockType.Air)
                {
                    Vector3 hitPos = Position + dir * t;
                    // La normale è semplicemente l'opposto dello step sull'ultimo asse che si è mosso
                    Ray = new Ray(block, (x, y, z), lastNormal, hitPos);
                    return;
                }

                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        t = tMaxX;
                        tMaxX += tDeltaX;
                        x += stepX;
                        lastNormal = new Vector3i(-stepX, 0, 0);
                    }
                    else
                    {
                        t = tMaxZ;
                        tMaxZ += tDeltaZ;
                        z += stepZ;
                        lastNormal = new Vector3i(0, 0, -stepZ);
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        t = tMaxY;
                        tMaxY += tDeltaY;
                        y += stepY;
                        lastNormal = new Vector3i(0, -stepY, 0);
                    }
                    else
                    {
                        t = tMaxZ;
                        tMaxZ += tDeltaZ;
                        z += stepZ;
                        lastNormal = new Vector3i(0, 0, -stepZ);
                    }
                }
            }
            Ray = new Ray();
        }
    }
}
