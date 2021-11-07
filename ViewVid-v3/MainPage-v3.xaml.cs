using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace ViewVid_v3
{
    // copy/paste (источник не помню)
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFolder folder = null;
        StorageFile file;
        private IReadOnlyList<StorageFile> fileList = null;
        int ind = 0;
        int z = 0;
        DateTime dateTime1 = DateTime.Now;

        private int lx = -1;
        private int ly = -1;
        private uint NaturalHeight = 1080; // сейчас так. пробовал разное.
        private uint NaturalWidth = 1920;

        //SoftwareBitmap softwareBitmap;
        PointCollection ps = new PointCollection();

        StorageFile txtOut;
        IRandomAccessStream streamTxtOut;
        IOutputStream outputStream;
        DataWriter dataWriter;

        bool flag = true; // предохранитель

        public MainPage()
        {
            InitializeComponent();

            mediaPlayerElement.MediaPlayer.MediaOpened += MediaPlayer_VideoFrameMediaOpened;
            mediaPlayerElement.MediaPlayer.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
            mediaPlayerElement.MediaPlayer.MediaEnded += MediaPlayer_VideoFrameEndedAsync;
        }

        private async void MediaPlayer_VideoFrameMediaOpened(object sender, object args)
        {
            if (flag) return;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                dateTime1 = DateTime.Now;

                z = 0;
                ps.Clear();

                notify2.Text = fileList.Count.ToString() + "\\" + (ind + 1).ToString();
            });
        }

        private async void MediaPlayer_VideoFrameEndedAsync(object sender, object args)
        {
            if (flag) return;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                string[] s = fileList[ind].Path.Split('\\', StringSplitOptions.RemoveEmptyEntries);

                string name = ApplicationData.Current.LocalFolder.Path + "\\" + s[s.Length - 2] + "\\" + fileList[ind].Name + ".pc"; // pc - PointCollection
                using (BinaryWriter WritePC = new BinaryWriter(File.Open(name, FileMode.Create)))
                {
                    WritePC.Write(ps.Count);
                    WritePC.Write(lx);
                    WritePC.Write(ly);

                    for (int i = 0; i < ps.Count; i++)
                    {
                        WritePC.Write(ps[i].X);
                        WritePC.Write(ps[i].Y);
                    }
                }

                DateTime dateTime2 = DateTime.Now;
                TimeSpan tm = dateTime2 - dateTime1; // оценочное суждение

                TimeSpan ts = mediaPlayerElement.MediaPlayer.PlaybackSession.Position;

                TimeSpan tsND = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalDuration;
                _ = dataWriter.WriteString(ps.Count.ToString() + "  " + name + "  " + ts.ToString() + "  " + tm.ToString() + "  " + tsND.ToString() + "\n");

                ind++;
                if (ind < fileList.Count)
                {
                    mediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStorageFile(fileList[ind]);
                    mediaPlayerElement.MediaPlayer.IsVideoFrameServerEnabled = true;
                    mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackRate = 3; // возможно, этот параметр зависит от производительности процессора. у меня больше 3 нельзя.
                    mediaPlayerElement.MediaPlayer.Play();

                    notify.Text = fileList[ind].DisplayName;
                }
                else
                {
                    _ = await dataWriter.StoreAsync();
                    _ = await outputStream.FlushAsync();

                    streamTxtOut.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.

                    notify.Text = "ind == fileList.Count";

                    mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackRate = 1;
                    mediaPlayerElement.MediaPlayer.IsVideoFrameServerEnabled = false;

                    file = fileList[ind - 1];

                    flag = true;
                }
            });
        }

        private async void MediaPlayer_VideoFrameAvailable(object sender, object args)
        {
            if (lx == -1 || ly == -1) return;

            CanvasDevice canvasDevice = CanvasDevice.GetSharedDevice();
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                SoftwareBitmap softwareBitmapImg;
                SoftwareBitmap frameServerDest = new SoftwareBitmap(BitmapPixelFormat.Rgba8, (int)NaturalWidth, (int)NaturalHeight, BitmapAlphaMode.Premultiplied);

                Point p;
                using (CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(canvasDevice, frameServerDest))
                {
                    mediaPlayerElement.MediaPlayer.CopyFrameToVideoSurface(canvasBitmap);

                    softwareBitmapImg = await SoftwareBitmap.CreateCopyFromSurfaceAsync(canvasBitmap);

                    unsafe
                    {
                        using (BitmapBuffer buffer = softwareBitmapImg.LockBuffer(BitmapBufferAccessMode.Write))
                        {
                            using (IMemoryBufferReference reference = buffer.CreateReference())
                            {
                                byte* dataInBytes;
                                uint capacity;
                                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                                p.X = z;
                                p.Y = dataInBytes[4 * lx + NaturalWidth * 4 * ly + 0]; // red
                                z++;
                                ps.Add(p);
                            }
                        }
                    }
                }
            });
        }

        private void PrintNotif()
        {
            notify.Text = z.ToString();
        }

        private async Task OpenAsync()
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
                mediaPlayerElement.MediaPlayer.Play();
                mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackRate = 3;

                notify.Text = file.DisplayName;
            }
            else
            {
            }
        }

        private void test_Click(object sender, RoutedEventArgs e)
        {
            notify.Text = z.ToString();
        }

        private void play_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayerElement.MediaPlayer.Play();
        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void MediaPlayerElement_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;

            // рассчёт x и y в координатах mediaPlayerElement
            PointerPoint pp = e.GetCurrentPoint(mediaPlayerElement);

            uint frameHeight = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoHeight;
            uint frameWidth = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoWidth;

            float imagePressedX = (float)(frameWidth / mediaPlayerElement.ActualWidth * pp.Position.X);
            float imagePressedY = (float)(frameHeight / mediaPlayerElement.ActualHeight * pp.Position.Y);

            lx = (int)Math.Round(imagePressedX);
            ly = (int)Math.Round(imagePressedY);

            notify.Text = lx.ToString() + "x  " + ly.ToString() + "y";

            // x и y в координатах layoutRoot
            PointerPoint ppLR = e.GetCurrentPoint(layoutRoot);

            Line linX = new Line
            {
                X1 = ppLR.Position.X - 15,
                X2 = ppLR.Position.X + 15,
                Y1 = ppLR.Position.Y,
                Y2 = ppLR.Position.Y,
                Stroke = new SolidColorBrush(Colors.Red)
            };
            layoutRoot.Children.Add(linX);
            Line LinY = new Line
            {
                X1 = ppLR.Position.X,
                X2 = ppLR.Position.X,
                Y1 = ppLR.Position.Y - 15,
                Y2 = ppLR.Position.Y + 15,
                Stroke = new SolidColorBrush(Colors.Red)
            };
            layoutRoot.Children.Add(LinY);

            mediaPlayerElement.MediaPlayer.IsVideoFrameServerEnabled = true;
            //mediaPlayerElement.MediaPlayer.StepForwardOneFrame();
            CaptureAsync();
            mediaPlayerElement.MediaPlayer.IsVideoFrameServerEnabled = false;
        }

        private async void CaptureAsync(int count = 1)
        {
            if (file == null)
            {
                return;
            }

            SoftwareBitmap softwareBitmapImg = null;
            CanvasDevice canvasDevice = CanvasDevice.GetSharedDevice();
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => // вот эту строку я не понял, но хорошая, полезная строка. я её в нескольких местах использовал. да и не разбирался, честно говоря.
            {
                SoftwareBitmap frameServerDest = new SoftwareBitmap(BitmapPixelFormat.Rgba8, (int)NaturalWidth, (int)NaturalHeight, BitmapAlphaMode.Premultiplied);

                using (CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(canvasDevice, frameServerDest))
                {
                    mediaPlayerElement.MediaPlayer.CopyFrameToVideoSurface(canvasBitmap);

                    softwareBitmapImg = await SoftwareBitmap.CreateCopyFromSurfaceAsync(canvasBitmap);
                }
            });

            unsafe
            {
                using (BitmapBuffer buffer = softwareBitmapImg.LockBuffer(BitmapBufferAccessMode.Write))
                {
                    using (IMemoryBufferReference reference = buffer.CreateReference())
                    {
                        byte* dataInBytes;
                        uint capacity;
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                        if (lx > -1 && ly > -1)
                        {
                            // тонкая красная линия
                            for (int tx = 0; tx < NaturalWidth; tx++)
                            {
                                dataInBytes[4 * tx + NaturalWidth * 4 * ly + 0] = 255; // red
                                dataInBytes[4 * tx + NaturalWidth * 4 * ly + 1] = 0;
                                dataInBytes[4 * tx + NaturalWidth * 4 * ly + 2] = 0;
                                dataInBytes[4 * tx + NaturalWidth * 4 * ly + 3] = 255; // альфа
                            }
                            for (int j = 0; j < 30; j++)
                            {
                                int ty = ly - 15 + j;
                                if (ty < 0) ty = 0;
                                if (ty > NaturalHeight - 1) ty = (int)NaturalHeight - 1; // на всякий случай проверил и вычел единицу
                                dataInBytes[4 * lx + NaturalWidth * 4 * ty + 0] = 0;
                                dataInBytes[4 * lx + NaturalWidth * 4 * ty + 1] = 255; // green
                                dataInBytes[4 * lx + NaturalWidth * 4 * ty + 2] = 0;
                                dataInBytes[4 * lx + NaturalWidth * 4 * ty + 3] = 255; // альфа
                            }
                        }
                    }
                }
            }

            StorageFolder _mediaFolder = KnownFolders.PicturesLibrary; // в манифесте нужно поставить галочку для Библиотеки изображений
            StorageFile saveAs = await _mediaFolder.CreateFileAsync("ViewVid-v0.jpg", CreationCollisionOption.GenerateUniqueName);

            using (IRandomAccessStream wStream = await saveAs.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, wStream);
                encoder.SetSoftwareBitmap(softwareBitmapImg);
                await encoder.FlushAsync();
                using (IOutputStream outStream = wStream.GetOutputStreamAt(0))
                {
                    await outStream.FlushAsync();
                }
            }
        }

        private async void file_ClickAsync(object sender, RoutedEventArgs e)
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
                mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackRate = 3;
                //mediaPlayerElement.MediaPlayer.Play();

                notify.Text = file.DisplayName;

                if (fileList == null && file != null)
                {
                    List<StorageFile> sf = new List<StorageFile>
                    {
                        file
                    };
                    fileList = sf;
                }
            }
            else
            {
            }
        }

        private async void folder_ClickAsync(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");
            folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                fileList = await folder.GetFilesAsync();
                if (fileList.Count > 0)
                {
                    mediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStorageFile(fileList[0]);
                }
            }
            else
            {
                notify.Text = "Operation cancelled.";
            }
        }

        private async void process_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (lx == -1 || ly == -1)
            {
                notify.Text = "if (lx == -1 || ly == -1)";
                return;
            }

            if (fileList == null && file != null)
            {
                List<StorageFile> sf = new List<StorageFile>
                {
                    file
                };
                fileList = sf;
            }
            if (fileList.Count > 0)
            {
                ind = 0;
                flag = false;

                string[] s = fileList[ind].Path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                await ApplicationData.Current.LocalFolder.CreateFolderAsync(s[s.Length - 2], CreationCollisionOption.OpenIfExists);

                //mediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStorageFile(fileList[ind]);
                mediaPlayerElement.MediaPlayer.IsVideoFrameServerEnabled = true;
                mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackRate = 3;
                mediaPlayerElement.MediaPlayer.Play();
                notify.Text = fileList[ind].DisplayName;

                txtOut = await ApplicationData.Current.LocalFolder.CreateFileAsync("list.txt", CreationCollisionOption.ReplaceExisting);
                streamTxtOut = await txtOut.OpenAsync(FileAccessMode.ReadWrite);
                outputStream = streamTxtOut.GetOutputStreamAt(0);
                dataWriter = new DataWriter(outputStream);
            }
        }

        private async void diagrPNG_ClickAsync(object sender, RoutedEventArgs e)
        {
            using (StreamReader reader = new StreamReader(await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync("list.txt")))
            {
                int max = -1;
                int head = 25;
                int h = 0 + head;
                string str;
                TimeSpan ts = new TimeSpan();

                while (null != (str = reader.ReadLine()))
                {
                    h += 255;
                    string[] subs = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    int m = int.Parse(subs[0]);
                    if (m > max)
                    {
                        max = m;
                        ts = TimeSpan.Parse(subs[4]);
                    }
                }
                reader.BaseStream.Position = 0;

                float five = max / ts.Minutes * 5;
                float fiveX = five;

                int j = 0;
                int w = max;
                if (w < 250) w = 250;
                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget offscreen = new CanvasRenderTarget(device, w, h, 96);

                CanvasTextFormat format = new CanvasTextFormat()
                {
                    FontSize = 18,
                    HorizontalAlignment = CanvasHorizontalAlignment.Left,
                    VerticalAlignment = CanvasVerticalAlignment.Top,
                    WordWrapping = CanvasWordWrapping.Wrap,
                    FontFamily = "Decor",
                };

                PointCollection ps = new PointCollection();
                Point p;

                using (CanvasDrawingSession ds = offscreen.CreateDrawingSession())
                {
                    // это шкала по 5 минут. пока так.
                    for (int i = 1; fiveX < max; i++)
                    {
                        fiveX = five * i;
                        string scale = (i * 5).ToString();
                        using (CanvasTextLayout tl = new CanvasTextLayout(ds, scale, format, w, 25))
                        {
                            ds.DrawTextLayout(tl, fiveX - 10, 0, Colors.Gray);
                        }
                        ds.DrawLine(fiveX, head, fiveX, offscreen.SizeInPixels.Height, Colors.Black);
                    }

                    while (null != (str = reader.ReadLine()))
                    {
                        string[] subs = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        int n = int.Parse(subs[0]);

                        using (BinaryReader sr = new BinaryReader(File.Open(subs[1], FileMode.Open)))
                        {
                            n = sr.ReadInt32(); // для сравнения
                            int x = sr.ReadInt32();
                            int y = sr.ReadInt32();
                            for (int i = 0; i < n; i++)
                            {
                                p.X = sr.ReadDouble();
                                p.Y = sr.ReadDouble();
                                ps.Add(p);
                            }
                        }

                        //----------------------------------------------------------------------------------------------------------------------------------------
                        string vidName = string.Format("{0}", System.IO.Path.GetFileName(subs[1]));
                        using (CanvasTextLayout tl = new CanvasTextLayout(ds, vidName, format, w, 25))
                        {
                            ds.DrawTextLayout(tl, 10, 10 + 255 * j + head, Colors.Gray);
                        }

                        for (int i = 0; i < ps.Count - 1; i++)
                        {
                            ds.DrawLine(i, (float)ps[i].Y + 255 * j + head, i + 1, (float)ps[i + 1].Y + 255 * j + head, Colors.Red);
                        }
                        //----------------------------------------------------------------------------------------------------------------------------------------

                        ps.Clear();
                        j++;
                    }
                }

                format.Dispose();

                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    stream.Seek(0);
                    await offscreen.SaveAsync(stream, CanvasBitmapFileFormat.Png);

                    StorageFolder folder = KnownFolders.PicturesLibrary; // в манифесте нужно поставить галочку для Библиотеки изображений
                    StorageFile file1 = await folder.CreateFileAsync("VV-Diagr-.png", CreationCollisionOption.GenerateUniqueName);
                    using (IRandomAccessStream fileStream1 = await file1.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await RandomAccessStream.CopyAndCloseAsync(stream.GetInputStreamAt(0), fileStream1.GetOutputStreamAt(0));
                    }
                    notify.Text = file1.DisplayName;
                }
            }

        }

        // вывести на экран диаграмму, если этот файл уже обработан (есть в файле liust.txt)
        private void diagr_Click(object sender, RoutedEventArgs e)
        {
            if (file == null) return;

            _ = NameFromListAsync();

        }

        // ищем для открытытого видеофайла в list.txt его файл с данными для кривой
        private async Task NameFromListAsync()
        {
            string name = file.Name + ".pc";

            int n = -1; // количество точек. записывается в двух местах. это неправильно. надо сохранять параметр только в одном месте, но переделывать не стал.
            bool flagYes = false;

            using (StreamReader reader = new StreamReader(await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync("list.txt")))
            {
                string str;

                while (null != (str = reader.ReadLine()))
                {
                    string[] subs = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (subs[1].Contains(name)) // не лучшее решение...
                    {
                        name = subs[1];
                        n = int.Parse(subs[0]); // из файла list.txt
                        flagYes = true;
                        break;
                    }
                }
            }

            if (!flagYes) return;

            DrawDiagr(name); // если есть, то на экран. как-то кривовато получилось

            return;
        }

        private void DrawDiagr(string name)
        {
            int n = -1;

            PointCollection ps = new PointCollection();
            Point p;
            int x;
            int y;
            using (BinaryReader sr = new BinaryReader(File.Open(name, FileMode.Open)))
            {
                n = sr.ReadInt32(); // из файла .pc
                x = sr.ReadInt32();
                y = sr.ReadInt32();
                double k = canvas.Width / n;
                for (int i = 0; i < n; i++)
                {
                    p.X = sr.ReadDouble() * k;
                    p.Y = sr.ReadDouble() / 2;
                    ps.Add(p);
                }
            }

            // жёлтый крест.
            // оказалось, просматривая видео после обработки, удобно видеть контрольную точку.
            // это я понял уже просматривая видео после обработки. добавил жёлтый крест.
            uint frameHeight = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoHeight;
            uint frameWidth = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoWidth;

            // для поправки. контрольную точку я устанавливаю по mediaPlayerElement, а здесь
            // рисую в layoutRoot. поэтому, получается сдвиг по x. причину сдвига я понял
            // почти сразу, не первый день за компьютером. решил проблемму не самым красивым
            // способом. красивое решение отложил на следующую версию)
            double w = (Window.Current.Bounds.Width - mediaPlayerElement.ActualWidth) / 2;

            double imagePressedX = mediaPlayerElement.ActualWidth / frameWidth * x + w; // поправка
            double imagePressedY = mediaPlayerElement.ActualHeight / frameHeight * y;

            Line linX = new Line
            {
                X1 = imagePressedX - 15,
                X2 = imagePressedX + 15,
                Y1 = imagePressedY,
                Y2 = imagePressedY,
                Stroke = new SolidColorBrush(Colors.Yellow)
            };
            layoutRoot.Children.Add(linX);

            Line LinY = new Line
            {
                X1 = imagePressedX,
                X2 = imagePressedX,
                Y1 = imagePressedY - 15,
                Y2 = imagePressedY + 15,
                Stroke = new SolidColorBrush(Colors.Yellow)
            };
            layoutRoot.Children.Add(LinY);

            Polyline polyline1 = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1,
                Points = ps
            };

            canvas.Children.Add(polyline1);
        }

        /* Для некоторых видеофайлов эти функции работают странно.
         * StepForwardOneFrame и StepBackwardOneFrame срабатывают на 3-5 раз.
         */
        private void forward_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayerElement.MediaPlayer.StepForwardOneFrame();
            /* Causes the MediaPlayer to move forward in the current media by one frame.
             */
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayerElement.MediaPlayer.StepBackwardOneFrame();
            /* Causes the playback position of the MediaPlayer to move backward by .042 seconds, which corresponds to one frame at 24 fps,
             * regardless of the actual frame rate of the content being played.
             */
        }
    }
}
