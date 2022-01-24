using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Morpher {
    class VectorMath {

        /// <summary>
        /// Projects Vector v onto Vector u
        /// </summary>
        /// <param name="v">The projector</param>
        /// <param name="u">The projectee</param>
        /// <returns>V projected onto U</returns>
        public static Vector2 Project(Vector2 v, Vector2 u) {
            return Vector2.Dot(v, u) / u.LengthSquared() * u;
        }

        public static float DistanceProject(Vector2 v, Vector2 u) {
            return Vector2.Dot(v, u) / u.Length();
        }

        public static Vector2 Normal(Vector2 v) {
            return new Vector2(-v.Y, v.X);
        }

        public static float DistanceVector(Vector2 v, Vector2 u) {
            return (float)Math.Sqrt(Math.Pow(u.X - v.X, 2) + Math.Pow(u.Y - v.Y, 2));
        }
    }
}
