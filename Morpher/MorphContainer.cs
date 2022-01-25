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
        private BitmapSource c1Image;

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

            Image imageToAdd = new Image();
            imageToAdd.Source = image;

            if (canvasIndex == 0) {
                //c0Image = FitToCanvas(image, canvasIndex);
                c0Image = image;
                //imageToAdd.Width = c0.ActualWidth;
                //imageToAdd.Height = c0.ActualHeight;
                c0.Children.Add(imageToAdd);
            } else {
                //c1Image = FitToCanvas(image, canvasIndex);
                c1Image = image;
                //imageToAdd.Width = c1.Width;
                //imageToAdd.Height = c1.Height;
                c1.Children.Add(imageToAdd);
            }
        }

        public BitmapSource Morph() {
            WriteableBitmap bmp = new WriteableBitmap(c0Image.PixelWidth, c0Image.PixelHeight, c0Image.DpiX, c0Image.DpiY, c0Image.Format, c0Image.Palette);

            int bytesPerPixel = c0Image.Format.BitsPerPixel / 8;
            int stride = c0Image.PixelWidth * bytesPerPixel;
            int totalPixels = c0Image.PixelWidth * stride;

            byte[] pixels = new byte[totalPixels];
            byte[] morphedPixels = new byte[totalPixels];
            c0Image.CopyPixels(pixels, stride, 0);

            for (int y = 0; y < c0Image.PixelHeight; y++) {
                for (int x = 0; x < c0Image.PixelWidth; x++) {
                    Vector2 sourcePixels = WeightedMorph(new(x, y), 8, 2, 0.4f);
                    int xPos = Math.Clamp((int)Math.Round(sourcePixels.X), 0, bmp.PixelWidth - 1);
                    int yPos = Math.Clamp((int)Math.Round(sourcePixels.Y), 0, bmp.PixelHeight - 1);
                    for (int i = 0; i < bytesPerPixel; i++) {
                        morphedPixels[(y * c0Image.PixelWidth + x) * bytesPerPixel + i] = pixels[(yPos * c0Image.PixelWidth + xPos) * bytesPerPixel + i];
                    }
                }
            }

            Int32Rect destRect = new(0, 0, bmp.PixelWidth, bmp.PixelHeight);
            bmp.WritePixels(destRect, morphedPixels, stride, 0);
            return bmp;
        }

        private Vector2 WeightedMorph(Vector2 destPos, float a, float b, float p) {
            Vector2 totalDelta = Vector2.Zero;
            float weightTotal = 0.0f;
            for (int i = 0; i < lines.Count; i++) {
                MorphDataPackage sourceData = lines[i].ReverseMorph(destPos);
                Vector2 delta = sourceData.xPrime - destPos; //might be other way around, verify
                Line sourceLine = lines[i].l0;
                float distanceToLine = sourceData.d;
                float sourceLineVector = VectorMath.DistanceVector(new((float)sourceLine.X1, (float)sourceLine.Y1), new((float)sourceLine.X2, (float)sourceLine.Y2));
                float weight = (float) Math.Pow(Math.Abs(Math.Pow(sourceLineVector, p) / a + distanceToLine), b);
                weightTotal += weight;
                totalDelta += weight * delta;
            }
            totalDelta /= weightTotal;
            return destPos - totalDelta;
        }

        //incorrect, fix image to canvwidth*canvheight
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
                lp.l0.Stroke = Brushes.Black;
                lp.l1.Stroke = Brushes.Black;
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
