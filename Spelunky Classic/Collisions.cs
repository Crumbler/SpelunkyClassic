using SFML.System;
using System;
using VelcroPhysics.Collision;
using VelcroPhysics.Dynamics;
using VelcroPhysics.Extensions;
using VelcroPhysics.Factories;
using VelcroPhysics.Shared;
using VelcroPhysics.Tools;
using VelcroPhysics.Collision.RayCast;
using VelcroPhysics.Utilities;
using V = Microsoft.Xna.Framework;
using System.Collections.Generic;
using SFML.Window;
using SFML.Graphics;

namespace Spelunky_Classic
{
    public static class Collisions
    {
        private const float RATIO = 16f;
        private static World world;
        private static List<Fixture> templist;
        public static void Initialize()
        {
            world = new World(new V.Vector2(0, -10));
            templist = new List<Fixture>();
        }
        public static bool RayCast(ref Vector2f ppos, Vector2f endpos)
        {
            templist.Clear();
            templist = world.RayCast(ppos.ToXna(), endpos.ToXna());
            return templist.Count > 0;
        }
        public static void AddTile(ref Sprite sprite)
        {
            BodyFactory.CreateRectangle(world, 1f, 1f, 1f, sprite.Position.ToXna());
        }
        public static bool ApproxZero(float x) => x > -0.1f && x < 0.1f;
    }
}
