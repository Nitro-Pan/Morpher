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

namespace Morpher
{
    /// <summary>
    /// Interaction logic for MorphResult.xaml
    /// </summary>
    public partial class MorphResult : Window
    {
        private int imageIndex = 0;
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

            bitmapSources = images;
            GoToNextImage();
        }

        private void GoToNextImage() {
            Image imageToAdd = new();
            imageToAdd.Source = bitmapSources[imageIndex++];
            imageIndex %= bitmapSources.Count;
            Images.Children.Add(imageToAdd);
            currentImage = imageToAdd;
        }

        private void NextImage_MouseDown(object sender, MouseButtonEventArgs e) {
            GoToNextImage();
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
    }
}
