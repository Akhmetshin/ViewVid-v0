// здесь не ответы, здесь вопросы

using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace MPElementTest
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFile file = null;
        private uint NaturalHeight = 0;
        private uint NaturalWidth = 0;
        private TimeSpan NaturalDuration;

        public MainPage()
        {
            InitializeComponent();

            notify.Text = "";
            test.IsEnabled = false;
        }

        private async void Open_ClickAsync(object sender, RoutedEventArgs e)
        {
            // Create and open the file picker
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            openPicker.FileTypeFilter.Add(".mp4");
            openPicker.FileTypeFilter.Add(".mkv");
            openPicker.FileTypeFilter.Add(".avi");

            file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                mediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
                //mediaPlayerElement.MediaPlayer.Play();

                NaturalHeight = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoHeight; // что здесь происходит?
                NaturalWidth = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoWidth;
                NaturalDuration = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalDuration;

                notify.Text = file.DisplayName;
                test.IsEnabled = true;
            }
        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.Exit();
        }

        private void Test1_Click(object sender, RoutedEventArgs e)
        {
            string str = "dur=" + NaturalDuration.ToString() + ";  " + NaturalWidth.ToString() + "x" + NaturalHeight.ToString();
            if (NaturalWidth == 0 && NaturalHeight == 0) str += " <- what?"; // <- what?
            notify.Text = str;
        }

        private async void FrequencyAsync()
        {
            TimeSpan[] ts = new TimeSpan[100];
            TimeSpan[] delta = new TimeSpan[100];
            int N = 100;
            for (int i = 0; i < N; i++)
            {
                ts[i] = mediaPlayerElement.MediaPlayer.PlaybackSession.Position; // Position не успевает за кадрами?
                mediaPlayerElement.MediaPlayer.StepForwardOneFrame();
            }

            delta[0] = new TimeSpan();
            for (int i = 1; i < N; i++)
            {
                delta[i] = ts[i] - ts[i - 1];
            }

            //using (StreamWriter sw = new StreamWriter(new FileStream(@"C:\Temp\freq.txt", FileMode.CreateNew)))
            //{
            //    sw.WriteLine("test");
            //}
            // в консоле это работает

            StorageFile txtOut = await ApplicationData.Current.LocalFolder.CreateFileAsync("freq.txt", CreationCollisionOption.ReplaceExisting);
            IRandomAccessStream streamTxtOut = await txtOut.OpenAsync(FileAccessMode.ReadWrite);
            using (IOutputStream outputStream = streamTxtOut.GetOutputStreamAt(0))
            {
                using (DataWriter dataWriter = new DataWriter(outputStream))
                {
                    for (int i = 0; i < N; i++)
                    {
                        dataWriter.WriteString(ts[i].ToString() + "  " + delta[i].ToString() + "\n");
                    }

                    await dataWriter.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }
        }

        private void frequency_Click(object sender, RoutedEventArgs e)
        {
            FrequencyAsync();
        }

        // проверка на скорость при использовании CopyFrameToVideoSurface, CreateCopyFromSurfaceAsync, StepForwardOneFrame
        // здесь не задаётся скорость проигрывания, а перебираются все кадры подряд, но может быть, обрабатываются не все.
        private async void StepForwardOneFrame_Test_Click(object sender, RoutedEventArgs e)
        {
            // может быть лучше использовать локальную версию MediaPlayer?
            double rate = mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackRate;
            TimeSpan duration = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalDuration;
            TimeSpan pos;

            //notify.Text = "StepForwardOneFrameTest_Click";
            notify.Text = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoWidth.ToString() + "w - " +
                          mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoHeight.ToString() + "h";
            DateTime tStart = DateTime.Now;

            CanvasDevice canvasDevice = CanvasDevice.GetSharedDevice();
            SoftwareBitmap softwareBitmapImg;
            SoftwareBitmap frameServerDest = new SoftwareBitmap(BitmapPixelFormat.Rgba8, (int)mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoWidth, (int)mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoHeight, BitmapAlphaMode.Premultiplied);

            int i;
            using (CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(canvasDevice, frameServerDest))
            {
                for (i = 0; pos < duration; i++)
                {
                    mediaPlayerElement.MediaPlayer.CopyFrameToVideoSurface(canvasBitmap);
                    softwareBitmapImg = await SoftwareBitmap.CreateCopyFromSurfaceAsync(canvasBitmap);
                    
                    // обработка кадра

                    mediaPlayerElement.MediaPlayer.StepForwardOneFrame();
                    mediaPlayerElement.MediaPlayer.StepForwardOneFrame(); i++; // допустим, обрабатываются кадр - через два.
                    mediaPlayerElement.MediaPlayer.StepForwardOneFrame(); i++;

                    pos = mediaPlayerElement.MediaPlayer.PlaybackSession.Position;
                }
            }
            DateTime tStop = DateTime.Now;

            notify.Text = duration.ToString() + "  " + (tStop - tStart).ToString() + "  (" + i + ")"; // лучше профилировщик?
        }
    }
}
