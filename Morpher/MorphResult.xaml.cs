#define DEBUG

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
using System.Windows.Shapes;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Morpher
{
    /// <summary>
    /// Interaction logic for MorphResult.xaml
    /// </summary>
    public partial class MorphResult : Window
    {
        private int imageIndex = -1;
        private List<BitmapSource> bitmapSources;
        private Image currentImage;
        private bool playingThreadIsPlaying;


        public MorphResult() {
            InitializeComponent();
            Closing += CloseWindow;
        }

        public void LoadImages(List<BitmapSource> images) {
            if (images?.Count == 0) {
                Close();
                return;
            }

            ImageSelectionSlider.Maximum = images.Count - 1;
            ImageSelectionSlider.Minimum = 0;
            bitmapSources = images;
            GoToNextImage();
        }

        private void GoToNextImage() {
            Image imageToAdd = new();
            imageIndex++;
            imageToAdd.Source = bitmapSources[imageIndex %= bitmapSources.Count];
            Images.Children.Add(imageToAdd);
            Images.Children.Remove(currentImage);
            currentImage = imageToAdd;
        }

        private void GoToPrevImage() {
            Image imageToAdd = new();
            if (--imageIndex < 0) imageIndex = bitmapSources.Count - 1;
            imageToAdd.Source = bitmapSources[imageIndex];
            Images.Children.Add(imageToAdd);
            Images.Children.Remove(currentImage);
            currentImage = imageToAdd;
        }

        private void SelectImage(int nFrame) { 
            Image imageToAdd = new();
            imageToAdd.Source = bitmapSources[nFrame];
            Images.Children.Add(imageToAdd);
            Images.Children.Remove(currentImage);
            currentImage = imageToAdd;
        }

        private void SaveToGif() {
            SaveFileDialog saveFileDialog = new();

            saveFileDialog.Filter = "gif files (*.gif)|*.gif";
            saveFileDialog.InitialDirectory = /*Directory.GetCurrentDirectory();*/"c:\\";
            if (saveFileDialog.ShowDialog() ?? false) {
                GifBitmapEncoder encoder = new();
                foreach (BitmapSource src in bitmapSources) {
                    encoder.Frames.Add(BitmapFrame.Create(src));
                }

                // After adding all frames to gifEncoder (the GifBitmapEncoder)...
                using (var ms = new MemoryStream()) {
                    encoder.Save(ms);
                    var fileBytes = ms.ToArray();
                    // This is the NETSCAPE2.0 Application Extension.
                    var applicationExtension = new byte[] { 33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 };
                    var newBytes = new List<byte>();
                    newBytes.AddRange(fileBytes.Take(13));
                    newBytes.AddRange(applicationExtension);
                    newBytes.AddRange(fileBytes.Skip(13));
                    File.WriteAllBytes(saveFileDialog.FileName, newBytes.ToArray());
                }
            }
        }

        private void NextImage_MouseDown(object sender, MouseButtonEventArgs e) {
            GoToNextImage();
        }

        private void PrevImage_MouseDown(object sender, MouseButtonEventArgs e) {
            GoToPrevImage();
        }

        private void PlayMorph_MouseDown(object sender, MouseButtonEventArgs e) {
            PlayFrames();
            PlayStopButton.PreviewMouseDown -= PlayMorph_MouseDown;
            PlayStopButton.PreviewMouseDown += StopMorph_MouseDown;
        }

        private void StopMorph_MouseDown(object sender, MouseButtonEventArgs e) {
            playingThreadIsPlaying = false;
            PlayStopButton.PreviewMouseDown += PlayMorph_MouseDown;
            PlayStopButton.PreviewMouseDown -= StopMorph_MouseDown;
        }

        private void SaveImage_MouseDown(object sender, MouseButtonEventArgs e) {
            SaveToGif();
        }

        private async void PlayFrames() {
            Trace.WriteLine("Playing animation");
            playingThreadIsPlaying = true;
            while (playingThreadIsPlaying) {
                Trace.WriteLine("Next Frame");
                Application.Current.Dispatcher.Invoke(GoToNextImage);
                await Task.Delay(100);
            }
        }

        private void CloseWindow(object sender, CancelEventArgs e) {
            playingThreadIsPlaying = false;
        }

        private void ImageSelectionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            SelectImage((int) e.NewValue);
        }
    }
}
