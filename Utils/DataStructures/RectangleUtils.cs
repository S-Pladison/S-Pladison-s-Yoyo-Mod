using System.Drawing;

namespace SPYoyoMod.Utils.DataStructures
{
    public static partial class DataStructureExtensions
    {
        public static bool Intersects(this Rectangle @this, Rectangle other)
        {
            if (other.Left < @this.Right && @this.Left < other.Right && other.Top < @this.Bottom) return @this.Top < other.Bottom;
            return false;
        }
    }
}