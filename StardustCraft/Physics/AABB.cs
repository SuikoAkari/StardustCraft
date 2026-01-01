using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardustCraft.Physics
{
    public enum CollisionType
    {
        Horizontal,
        Ground,
        Ceiling
    }
    public struct AABB
    {
        public Vector3 Min;
        public Vector3 Max;

        public Vector3 Center => (Min + Max) * 0.5f;

        public bool Intersects(ref AABB other)
        {
            return Min.X < other.Max.X && Max.X > other.Min.X &&
                   Min.Y < other.Max.Y && Max.Y > other.Min.Y &&
                   Min.Z < other.Max.Z && Max.Z > other.Min.Z;
        }
        // Crea AABB da un range di blocchi
        public static AABB FromBlockRange(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            Vector3 min = new Vector3(x1, y1, z1);
            Vector3 max = new Vector3(x2 + 1, y2 + 1, z2 + 1); // +1 perché blocchi sono inclusivi
            return new AABB() { Min=min,Max=max};
        }
        public static AABB FromBlockPosition(int x, int y, int z)
        {
            return new AABB
            {
                Min = new Vector3(x, y, z),
                Max = new Vector3(x + 1, y + 1, z + 1)
            };
        }
    }
}
