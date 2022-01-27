#define DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Numerics;
using System.Windows;

namespace Morpher {
    class MorphContainer {
        private List<LinePair> lines = new List<LinePair>();
        private Canvas c0;
        private Canvas c1;
        private BitmapSource c0Image;
        private Image c0ImageVisual;
        private BitmapSource c1Image;
        private Image c1ImageVisual;

        //might be better to do this by passing the Line through to everything
        //but it exposes it more than I would like so maybe not.
        private LinePair selectedPair;
        private int whichPair;
        private int whichPoint;

        private const double LINE_SELECTION_TOLERANCE = 2;
        private const double POINT_SELECTION_TOLERANCE = 5;
        private const double MIN_LINE_SIZE = 5;

        public lineState state { get; private set; }

        public enum lineState { 
            CREATING,
            MOVING,
            RESIZING,
            NONE
        }

        public enum lineCollisionType {
            POINT,
            LINE,
            NONE
        }

        public enum morphDirection { 
            LEFT_TO_RIGHT,
            RIGHT_TO_LEFT
        }

        public MorphContainer(Canvas c0, Canvas c1) {
            this.c0 = c0;
            this.c1 = c1;
            state = lineState.NONE;
        }

        public void DrawNewLine(double x, double y) {
            lines.Add(new LinePair(x, y, x, y));
            c0.Children.Add(lines[lines.Count - 1].l0);
            c1.Children.Add(lines[lines.Count - 1].l1);
            selectedPair = lines[lines.Count - 1];
            state = lineState.CREATING;
        }

        public void ResizeLine(double x, double y) {
            if (selectedPair == null) {
                return;
            }

            if (state == lineState.CREATING) {
                selectedPair.l0.X2 = x;
                selectedPair.l0.Y2 = y;
                selectedPair.l1.X2 = x;
                selectedPair.l1.Y2 = y;
                return;
            }

            double pointTolerance = state == lineState.RESIZING ? double.MaxValue : POINT_SELECTION_TOLERANCE;

            if (IsCollidingWithPoint(selectedPair[whichPair].X1, selectedPair[whichPair].Y1, x, y, pointTolerance) && whichPoint == 0) {
                state = lineState.RESIZING;
                selectedPair[whichPair].X1 = x;
                selectedPair[whichPair].Y1 = y;
            } else if (IsCollidingWithPoint(selectedPair[whichPair].X2, selectedPair[whichPair].Y2, x, y, pointTolerance) && whichPoint == 1) {
                state = lineState.RESIZING;
                selectedPair[whichPair].X2 = x;
                selectedPair[whichPair].Y2 = y;
            } else {
                MoveLine(x, y);
            }
        }

        //possible to remove?
        double xPrev, yPrev;

        public void MoveLine(double x, double y) {
            if (selectedPair == null) {
                return;
            }

            state = lineState.MOVING;
            selectedPair[whichPair].X1 += x - xPrev;
            selectedPair[whichPair].Y1 += y - yPrev;
            selectedPair[whichPair].X2 += x - xPrev;
            selectedPair[whichPair].Y2 += y - yPrev;
            xPrev = x;
            yPrev = y;
        }

        public void ReleaseLine() {
            if (selectedPair == null) {
                state = lineState.NONE;
                return;
            }

            //remove line if it's way too short to really be a line
            if (DistToPoint(selectedPair[whichPair].X1, selectedPair[whichPair].Y1, selectedPair[whichPair].X2, selectedPair[whichPair].Y2) < MIN_LINE_SIZE) {
                c0.Children.Remove(selectedPair.l0);
                c1.Children.Remove(selectedPair.l1);
                lines.Remove(selectedPair);
            }

            selectedPair = null;
            state = lineState.NONE;
        }

        public bool CheckLineCollision(Canvas c, double x, double y) {
            foreach (LinePair lp in lines) {
                int canvasIndex = c == c0 ? 0 : 1;
                if (IsCollidingWithPoint(lp[canvasIndex].X1, lp[canvasIndex].Y1, x, y, POINT_SELECTION_TOLERANCE) || IsCollidingWithPoint(lp[canvasIndex].X2, lp[canvasIndex].Y2, x, y, POINT_SELECTION_TOLERANCE)) {
                    lp.l0.Stroke = Brushes.Yellow;
                    lp.l1.Stroke = Brushes.Yellow;
                    selectedPair = lp;
                    whichPair = canvasIndex;
                    whichPoint = IsCollidingWithPoint(lp[canvasIndex].X1, lp[canvasIndex].Y1, x, y, POINT_SELECTION_TOLERANCE) ? 0 : 1;
                    BlackOutUnselectedLines();
                    return true;
                } else if (IsCollidingWithLine(lp[canvasIndex], x, y, LINE_SELECTION_TOLERANCE)) {
                    lp.l0.Stroke = Brushes.Blue;
                    lp.l1.Stroke = Brushes.Blue;
                    //kinda dirty, needs to be set here otherwise line offsets (unless other solution)
                    xPrev = x;
                    yPrev = y;
                    selectedPair = lp;
                    whichPair = canvasIndex;
                    BlackOutUnselectedLines();
                    return true;
                }
            }
            selectedPair = null;
            BlackOutUnselectedLines();
            return false;
        }

        public void LoadImage(BitmapSource image, int canvasIndex) {
            if (canvasIndex != 0 && canvasIndex != 1) {
                throw new IndexOutOfRangeException("Canvas index must be either 1 or 0.");
            }

            Image imageToAdd = new();
            imageToAdd.Source = FitToCanvas(image, canvasIndex);

            if (canvasIndex == 0) {
                c0.Children.Remove(c0ImageVisual);
                c0Image = FitToCanvas(image, canvasIndex);
                c0ImageVisual = imageToAdd;
                c0.Children.Insert(0, imageToAdd);
            } else {
                c1.Children.Remove(c1ImageVisual);
                c1Image = FitToCanvas(image, canvasIndex);
                c1ImageVisual = imageToAdd;
                c1.Children.Insert(0, imageToAdd);
            }
        }

        public List<BitmapSource> InitiateMorph() {
            return Morph(c0Image, morphDirection.LEFT_TO_RIGHT, 5);
        }

        public List<BitmapSource> Morph(BitmapSource src, morphDirection dir, int nFrames) {
            List<List<LinePair>> transitionLines;

            if (dir == morphDirection.RIGHT_TO_LEFT) {
                List<LinePair> reversedLines = new();
                foreach (LinePair lp in lines) {
                    reversedLines.Add(new LinePair(lp.l1, lp.l0));
                }
                transitionLines = CreateTweenLines(nFrames, reversedLines);
            } else {
                transitionLines = CreateTweenLines(nFrames, lines);
            }

            List<BitmapSource> morphedBitmaps = new();

            for (int i = 0; i < transitionLines.Count; i++) {
                morphedBitmaps.Add(createFrame(src, transitionLines[i]));
#if DEBUG
                Trace.WriteLine($"Created frame {i + 1}/{transitionLines.Count}");
#endif
            }

            return morphedBitmaps;
        }

        private List<List<LinePair>> CreateTweenLines(int nFrames, List<LinePair> linePairs) {
            List<List<LinePair>> morphLines = new();
            for (int i = 0; i < nFrames; i++) {
                List<LinePair> tweenLines = new();
                foreach (LinePair lp in linePairs) {
                    Vector2 a = new((float)lp[0].X1, (float)lp[0].Y1);
                    Vector2 b = new((float)lp[0].X2, (float)lp[0].Y2);
                    Vector2 aPrime = new((float)lp[1].X1, (float)lp[1].Y1);
                    Vector2 bPrime = new((float)lp[1].X2, (float)lp[1].Y2);
                    Vector2 tweenA = Vector2.Lerp(a, aPrime, (i) / ((float)nFrames));
                    Vector2 tweenB = Vector2.Lerp(b, bPrime, (i) / ((float)nFrames));
                    Line tweenedLine = new() {
                        X1 = tweenA.X,
                        Y1 = tweenA.Y,
                        X2 = tweenB.X,
                        Y2 = tweenB.Y,
                        StrokeThickness = lp[0].StrokeThickness,
                        Stroke = lp[0].Stroke,
                    };
                    tweenLines.Add(new(lp[0], tweenedLine));
                }
                morphLines.Add(tweenLines);
            }
            return morphLines;
        }

        private BitmapSource createFrame(BitmapSource src, List<LinePair> morphLines) {
            WriteableBitmap bmp = new(src.PixelWidth, src.PixelHeight, src.DpiX, src.DpiY, src.Format, src.Palette);

            int bytesPerPixel = (src.Format.BitsPerPixel + 7) / 8;
            int stride = src.PixelWidth * bytesPerPixel;
            int totalPixels = src.PixelHeight * stride;

            byte[] pixels = new byte[totalPixels];
            byte[] morphedPixels = new byte[totalPixels];
            src.CopyPixels(pixels, stride, 0);

            for (int y = 0; y < src.PixelHeight; y++) {
                for (int x = 0; x < src.PixelWidth; x++) {
                    Vector2 sourcePixels = WeightedMorph(new(x, y), morphLines, 0.01f, 2, 0);
                    int xPos = Math.Clamp((int)Math.Round(sourcePixels.X), 0, bmp.PixelWidth - 1);
                    int yPos = Math.Clamp((int)Math.Round(sourcePixels.Y), 0, bmp.PixelHeight - 1);
                    for (int i = 0; i < bytesPerPixel; i++) {
                        morphedPixels[(y * src.PixelWidth + x) * bytesPerPixel + i] = pixels[(yPos * src.PixelWidth + xPos) * bytesPerPixel + i];
                    }
                }
            }

            Int32Rect destRect = new(0, 0, bmp.PixelWidth, bmp.PixelHeight);
            bmp.WritePixels(destRect, morphedPixels, stride, 0);
            return bmp;
        }

        private Vector2 WeightedMorph(Vector2 destPos, List<LinePair> morphLines, float a, float b, float p) {
            Vector2 totalDelta = Vector2.Zero;
            float weightTotal = 0.0f;
            for (int i = 0; i < morphLines.Count; i++) {
                MorphDataPackage sourceData = morphLines[i].ReverseMorph(destPos);
                Vector2 delta = destPos - sourceData.xPrime; //might be other way around, verify
                Line fromLine = morphLines[i][0];
                float distanceToLine = ActualDistance(fromLine, sourceData);
                float sourceLineVector = VectorMath.DistanceVector(new((float)fromLine.X1, (float)fromLine.Y1), new((float)fromLine.X2, (float)fromLine.Y2));
                float weight = (float) Math.Pow(Math.Abs(Math.Pow(sourceLineVector, p) / (a + distanceToLine)), b);
                weightTotal += weight;
                totalDelta += weight * delta;
            }
            totalDelta /= weightTotal;
            return destPos - totalDelta;
        }

        private float ActualDistance(Line line, MorphDataPackage data) {
            if (data.fl > 1) {
                return Math.Abs(VectorMath.DistanceVector(new((float)line.X2, (float)line.Y2), data.xPrime));
            } else if (data.fl < 0) {
                return Math.Abs(VectorMath.DistanceVector(new((float)line.X1, (float)line.Y1), data.xPrime));
            }

            return Math.Abs(data.d);
        }

        private BitmapSource FitToCanvas(BitmapSource bitmapSource, int canvasIndex) {
            if (canvasIndex != 0 && canvasIndex != 1) {
                throw new IndexOutOfRangeException("Canvas index must be either 1 or 0.");
            }

            Canvas targetCanvas = canvasIndex == 0 ? c0 : c1;

            ScaleTransform t = new ScaleTransform();
            t.ScaleX = targetCanvas.ActualWidth / bitmapSource.PixelWidth;
            t.ScaleY = targetCanvas.ActualHeight / bitmapSource.PixelHeight;
            BitmapSource bitmap = new TransformedBitmap(bitmapSource, t);
            return bitmap;
        }

        private void BlackOutUnselectedLines() {
            foreach (LinePair lp in lines) {
                if (lp == selectedPair) continue;
                lp.l0.Stroke = Brushes.Green;
                lp.l1.Stroke = Brushes.Green;
            }
        }

        private bool IsCollidingWithPoint(double a, double b, double x, double y, double tolerance) {
            return DistToPoint(a, b, x, y) < tolerance;
        }

        private bool IsCollidingWithLine(Line l, double x, double y, double tolerance) {
            return DistToPoint(l.X1, l.Y1, x, y) + DistToPoint(l.X2, l.Y2, x, y) - DistToPoint(l.X1, l.Y1, l.X2, l.Y2) < tolerance;
        }

        private double DistToPoint(double a, double b, double x, double y) {
            Vector2 v = new Vector2((float)a, (float)b);
            Vector2 u = new Vector2((float)x, (float)y);
            return VectorMath.DistanceVector(v, u);
        }
    }
}
