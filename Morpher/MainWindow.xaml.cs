using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace Morpher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private bool isDragging = false;
        private MorphContainer mc;
        private int canvasToLoadNextImage = 0;

        private const string LEFT_BUTTON_NAME = "ImageButton1";
        private const string RIGHT_BUTTON_NAME = "ImageButton2";

        public MainWindow() {
            InitializeComponent();
            mc = new MorphContainer(LeftCanvas, RightCanvas);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e) {
            isDragging = true;
            if (mc.CheckLineCollision(sender as Canvas, (float)e.GetPosition(sender as Canvas).X, (float)e.GetPosition(sender as Canvas).Y)) {
                mc.ResizeLine((float)e.GetPosition(sender as Canvas).X, (float)e.GetPosition(sender as Canvas).Y);
            } else {
                mc.DrawNewLine((float)e.GetPosition(sender as Canvas).X, (float)e.GetPosition(sender as Canvas).Y);
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e) {
            isDragging = false;
            mc.ReleaseLine();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e) {
            if (isDragging) {
                mc.ResizeLine((float)e.GetPosition(sender as Canvas).X, (float)e.GetPosition(sender as Canvas).Y);
            } else {
                mc.CheckLineCollision(sender as Canvas, (float)e.GetPosition(sender as Canvas).X, (float)e.GetPosition(sender as Canvas).Y);
            }
        }

        private void OpenMI_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFile = new();

            openFile.Filter = "png files (*.png)|*.png|jpg files (*.jpg)|*.jpg";
            openFile.InitialDirectory = /*Directory.GetCurrentDirectory();*/"c:\\";
            if (openFile.ShowDialog() ?? false) {
                BitmapImage image = new BitmapImage(new Uri(openFile.FileName));
                mc.LoadImage(image, canvasToLoadNextImage);
                canvasToLoadNextImage++;
                canvasToLoadNextImage %= 2;
            }
        }

        private void MorphMI_Click(object sender, RoutedEventArgs e) {
            List<BitmapSource> bmp = mc.InitiateMorph();
            MorphResult morphResult = new MorphResult();
            morphResult.LoadImages(bmp);
            morphResult.Show();
        }

        private void OpenImage_Click(object sender, MouseButtonEventArgs e) {

            if (!(sender is Button b)) {
                return;
            }

            int canvasIndex;

            canvasIndex = b.Name == LEFT_BUTTON_NAME ? 0 : 1;

            OpenFileDialog openFile = new();

            openFile.Filter = "png files (*.png)|*.png|jpg files (*.jpg)|*.jpg";
            openFile.InitialDirectory = /*Directory.GetCurrentDirectory();*/"c:\\";
            if (openFile.ShowDialog() ?? false) {
                b.IsEnabled = false;
                b.Opacity = 0;
                BitmapImage image = new(new(openFile.FileName));
                mc.LoadImage(image, canvasIndex);
            }
        }

        private void ClearMI_Click(object sender, RoutedEventArgs e) {
            mc.Clear();
            ImageButton1.IsEnabled = true;
            ImageButton1.Opacity = 1;
            ImageButton2.IsEnabled = true;
            ImageButton2.Opacity = 1;
        }
    }
}
