﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Morpher {
    class LinePair {
        public Line l0 { get; private set; }
        public Line l1 { get; private set; }

        public Line this[int i] {
            get {
                if (i == 0) return l0;
                if (i == 1) return l1;
                throw new IndexOutOfRangeException("Index must be either 1 or 0 for a pair.");
            }
            private set {
                if (i == 0) l0 = value;
                if (i == 1) l1 = value;
                throw new IndexOutOfRangeException("Index must be either 1 or 0 for a pair.");
            }
        }

        public LinePair(double x1, double y1, double x2, double y2) {
            l0 = new Line {
                X1 = x1,
                X2 = x2,
                Y1 = y1,
                Y2 = y2,
                StrokeThickness = 2,
                Stroke = Brushes.Black
            };
            l1 = new Line {
                X1 = x1,
                X2 = x2,
                Y1 = y1,
                Y2 = y2,
                StrokeThickness = 2,
                Stroke = Brushes.Black
            };
        }

        /// <summary>
        /// Morphs a pixel from source to dest based on this LinePair (creates holes, not good)
        /// </summary>
        /// <param name="sourcePos">The source position of the pixel</param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public Vector2 ForwardMorph(Vector2 sourcePos, float a, float b, float p) {
            Vector2 vPQ = new Vector2((float)l0.X1, (float)l0.Y1) - new Vector2((float)l0.X2, (float)l0.Y2);
            Vector2 vPX = new Vector2((float)l0.X1, (float)l0.Y1) - sourcePos;
            Vector2 vnPQ = VectorMath.Normal(vPQ);

            float d = VectorMath.DistanceProject(vPX, vnPQ);
            float fl = VectorMath.DistanceProject(vPX, vPQ) / vPQ.Length();

            Vector2 vPQPrime = new Vector2((float)l1.X1, (float)l1.Y1) - new Vector2((float)l1.X2, (float)l1.Y2);

            Vector2 xPrime = new Vector2((float)l1.X1, (float)l1.Y1) + fl * vPQPrime + d * (vnPQ / vnPQ.Length());

            return xPrime;
        }

        /// <summary>
        /// Reverse morphs a pixel from dest to source, removing holes from the image in the process
        /// </summary>
        /// <param name="destPos">The destination position of the pixel</param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public Vector2 ReverseMorph(Vector2 destPos, float a, float b, float p) {
            Vector2 vPQ = new Vector2((float)l1.X2, (float)l1.Y2) - new Vector2((float)l1.X1, (float)l1.Y1);
            Vector2 vPX = destPos - new Vector2((float)l1.X1, (float)l1.Y1);
            Vector2 vnPQ = VectorMath.Normal(vPQ);

            float d = VectorMath.DistanceProject(vPX, vnPQ);
            float fl = VectorMath.DistanceProject(vPX, vPQ) / vPQ.Length();

            Vector2 vPQPrime = new Vector2((float)l0.X2, (float)l0.Y2) - new Vector2((float)l0.X1, (float)l0.Y1);

            Vector2 xPrime = new Vector2((float)l0.X1, (float)l0.Y1) + fl * vPQPrime + d * (VectorMath.Normal(vPQPrime) / VectorMath.Normal(vPQPrime).Length());

            return xPrime;
        }
    }
}