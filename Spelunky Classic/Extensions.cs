using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using SFML.System;

namespace Spelunky_Classic
{
    public static class Extensions
    {
        public static Vector2 ToXna(this Vector2f input) => new Vector2(input.X, input.Y);
    }
}
