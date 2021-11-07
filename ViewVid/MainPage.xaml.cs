using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace ViewVid
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFolder folder = null;
        private StorageFile file = null;
        private IReadOnlyList<StorageFile> fileList = null;
        private int lx = -1;
        private int ly = -1;
        private uint NaturalHeight = 1080; // сейчас так. пробовал разное.
        private uint NaturalWidth = 1920;

        bool flagESC = false;

        public MainPage()
        {
            InitializeComponent();
            Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived; // Для удаления метки
            notify1.Text = "";
            notify2.Text = "";

            /* Когда я начинал писать эту программу то, предполагалось, и сначала так и было,
             * что контрольную точку я буду указывать мышкой. Навожу мышку на точку, которую хочу
             * отследить по всему видеофайлу, кликаю, запускаю. Нуда, пока писал программу вот так сидел,
             * нацеливал, кликал, научился вообще точно попадать, а потом забил нужные значения сразу в 
             * переменные. И оказалось всё не так просто.
             */
            // координаты контрольной точки для:
            lx = 255; // задний поворотник
            ly = 640;
            lx = 418; // передний
            ly = 607;
            // Просто менял их местами, а надо писать одновременно. Это сэкономит кучу времени.
            // Я искал момент срабатывания сигнализации - включаются сигналы поворота.

            /* Всего пришлось просмотреть 35 часов 20 минут. За это время другие машины
             * поочерёдно загораживали то передний повортник, то задний. Я сразу об этом не
             * подумал, начал писать программу, а когда заметил - доделывать не стал. Просто
             * рассортировал файлы по разным папкам и прогнал поочерёдно - передний и задний
             * поворотники.
             */

        }

        private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (args.KeyCode == 27) //ESC
            {
                if(flagESC)
                {
                    layoutRoot.Children.RemoveAt(layoutRoot.Children.Count - 1);
                    layoutRoot.Children.RemoveAt(layoutRoot.Children.Count - 1);
                    // Я пока не умею удалять нужные элементы коллекции.
                    // Поэтому, удаляю два последних. Выкрутился.

                    flagESC = false;
                }
            }
        }

        private void MediaPlayerElement_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (file == null)
            {
                return;
            }

            e.Handled = true;

            // рассчёт x и y в координатах mediaPlayerElement
            PointerPoint pp = e.GetCurrentPoint(mediaPlayerElement);

            uint frameHeight = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoHeight;
            uint frameWidth = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoWidth;

            double imagePressedX = frameWidth / mediaPlayerElement.ActualWidth * pp.Position.X;
            double imagePressedY = frameHeight / mediaPlayerElement.ActualHeight * pp.Position.Y;

            // координаты контрольной точки видео
            lx = (int)Math.Round(imagePressedX);
            ly = (int)Math.Round(imagePressedY);

            notify2.Text = lx.ToString() + "x  " + ly.ToString() + "y";

            // x и y в координатах layoutRoot
            pp = e.GetCurrentPoint(layoutRoot);

            Line linX = new Line
            {
                X1 = pp.Position.X - 15,
                X2 = pp.Position.X + 15,
                Y1 = pp.Position.Y,
                Y2 = pp.Position.Y,
                Stroke = new SolidColorBrush(Colors.Red)
            };
            layoutRoot.Children.Add(linX);
            Line LinY = new Line
            {
                X1 = pp.Position.X,
                X2 = pp.Position.X,
                Y1 = pp.Position.Y - 15,
                Y2 = pp.Position.Y + 15,
                Stroke = new SolidColorBrush(Colors.Red)
            };
            layoutRoot.Children.Add(LinY);
            
            Process.IsEnabled = true;
            
            flagESC = true;

            //Capture(); // Для проверки. Контрольная точка видео и метка на экране должны совпадать.
        }

        private async void PickFileButton_ClickAsync(object sender, RoutedEventArgs e)
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
                notify1.Text = file.DisplayName;

                string[] s = file.Path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                string name = ApplicationData.Current.LocalFolder.Path + "\\" + s[s.Length - 2] + "\\" + file.Name + ".pc";
                if (File.Exists(name))
                {
                    diagr.IsEnabled = true;
                }
                canvas.Children.Clear();
                
                Process.IsEnabled = true;
            }
            else
            {
                notify1.Text = "Operation cancelled.";
            }
        }

        private async void Folder_ClickAsync(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");
            folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageFile sampleFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("sample.txt", CreationCollisionOption.GenerateUniqueName);
                IRandomAccessStream stream = await sampleFile.OpenAsync(FileAccessMode.ReadWrite);
                using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                {
                    using (DataWriter dataWriter = new DataWriter(outputStream))
                    {
                        _ = dataWriter.WriteString("Список файлов для: " + folder.Path + "\n");

                        Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                        fileList = await folder.GetFilesAsync();

                        foreach (StorageFile file in fileList)
                        {
                            _ = dataWriter.WriteString(file.Path + "\n");
                        }

                        file = fileList[0]; // первый файл в проигрыватель
                        mediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);

                        await dataWriter.StoreAsync();
                        await outputStream.FlushAsync();
                    }
                }
                stream.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.
                notify1.Text = folder.Path;
                diagr.IsEnabled = true;
                Process.IsEnabled = true;
            }
            else
            {
                notify1.Text = "Operation cancelled.";
            }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            ProcessingAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            Capture();
        }

        // В этой функции началось моё понимание того как получить из видео пиксел с координатами x, y
        // В основном это copy/paste из интернета.
        private async void Capture(int count = 1)
        {
            if (file == null)
            {
                return;
            }

            /*
             * Я думал, что если создать локальный MediaPlayerElement и не выводить его на экран,
             * то программа будет работать быстрее. Похоже что это не так. Всё равно работает долго.
             */
            MediaPlayerElement mp = new MediaPlayerElement
            {
                Source = MediaSource.CreateFromStorageFile(file)
            };
            mp.MediaPlayer.PlaybackSession.Position = mediaPlayerElement.MediaPlayer.PlaybackSession.Position;

            //Get image stream
            MediaClip clip = await MediaClip.CreateFromFileAsync(file);
            MediaComposition composition = new MediaComposition();
            composition.Clips.Add(clip);

            //Create BMP
            WriteableBitmap wBitmap = new WriteableBitmap((int)NaturalWidth, (int)NaturalHeight);
            Stream streamForLen = wBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[(uint)streamForLen.Length];

            for (int i = 0; i < count; i++) // для серии снимков
            {
                TimeSpan ts = mp.MediaPlayer.PlaybackSession.Position;
                ImageStream imageStream = await composition.GetThumbnailAsync(ts, (int)NaturalWidth, (int)NaturalHeight, VideoFramePrecision.NearestFrame);
                wBitmap.SetSource(imageStream);

                //Get stream from BMP
                Stream stream = wBitmap.PixelBuffer.AsStream();
                await stream.ReadAsync(pixels, 0, pixels.Length);

                // похоже на прицел. так и есть, здесь я учился попадать в нужную точку
                if (lx > -1 && ly > -1)
                {
                    // тонкая красная линия
                    for (int tx = 0; tx < NaturalWidth; tx++)
                    {
                        pixels[4 * tx + NaturalWidth * 4 * ly + 0] = 0;
                        pixels[4 * tx + NaturalWidth * 4 * ly + 1] = 0;
                        pixels[4 * tx + NaturalWidth * 4 * ly + 2] = 255; // red
                        pixels[4 * tx + NaturalWidth * 4 * ly + 3] = 255; // альфа
                    }
                    for (int j = 0; j < 30; j++)
                    {
                        int ty = ly - 15 + j;
                        if (ty < 0) ty = 0;
                        if (ty > NaturalHeight - 1) ty = (int)NaturalHeight - 1; // на всякий случай проверил и вычел единицу
                        pixels[4 * lx + NaturalWidth * 4 * ty + 0] = 0;
                        pixels[4 * lx + NaturalWidth * 4 * ty + 1] = 255; // green
                        pixels[4 * lx + NaturalWidth * 4 * ty + 2] = 0;
                        pixels[4 * lx + NaturalWidth * 4 * ty + 3] = 255; // альфа
                    }

                    pixels[4 * lx + NaturalWidth * 4 * ly + 0] = 0;
                    pixels[4 * lx + NaturalWidth * 4 * ly + 1] = 0;
                    pixels[4 * lx + NaturalWidth * 4 * ly + 2] = 0;
                    pixels[4 * lx + NaturalWidth * 4 * ly + 3] = 255; // альфа
                }

                StorageFolder _mediaFolder = KnownFolders.PicturesLibrary; // в манифесте нужно поставить галочку для Библиотеки изображений
                StorageFile saveAs = await _mediaFolder.CreateFileAsync("ViewVidCapture.jpg", CreationCollisionOption.GenerateUniqueName);

                using (IRandomAccessStream wStream = await saveAs.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, wStream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)wBitmap.PixelWidth, (uint)wBitmap.PixelHeight, 96, 96, pixels);
                    await encoder.FlushAsync();
                    using (IOutputStream outStream = wStream.GetOutputStreamAt(0))
                    {
                        await outStream.FlushAsync();
                    }
                }

                mp.MediaPlayer.StepForwardOneFrame(); // вот здесь не понял, как будто начинает работать только со второго раза, но не всегда. иногда срабатывает сразу.
                                                      // видимо это зависит от позиции в момент захвата. (для серии фотографий)
            }
            return;
        }

        private async void ProcessingAsync()
        {
            if (lx == -1 || ly == -1)
            {
                notify1.Text = "if (lx == -1 || ly == -1) return;";
                return;
            }

            //---------------------------------------------------------------------------------------------------
            // Здесь надо пояснить.
            // Сначала, я предполагал написать программу, которая обрабатывает сразу все файлы в некоторой папке.
            // Это казалось удобно и логично. Обработать все файлы, вывести диаграммы в файл PNG и увидеть всю картину.
            // потом стало понятно, что один файл тоже стоит обработать и вывести диаграмму сразу на экран.
            // Это даже ещё удобнее и логичнее. Поэтому, появился следующий код в этой функции:
            if (fileList == null && file != null)
            {
                List<StorageFile> sf = new List<StorageFile>
                {
                    file
                };
                fileList = sf;
            }
            //---------------------------------------------------------------------------------------------------

            string pcName = null;

            //Create BMP
            WriteableBitmap wBitmap = new WriteableBitmap((int)NaturalWidth, (int)NaturalHeight);
            Stream streamForLen = wBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[(uint)streamForLen.Length];

            // чем больше шаг - тем меньше время обработки
            //TimeSpan tStep = new TimeSpan(0, 0, 0, 0, 700);
            TimeSpan tStep = new TimeSpan(0, 0, 0, 1, 0); // но, можно пропустить событие.

            // для каждой обрабатываемой папки свой list.txt и файлы .pc
            // Это надо думать дальше. В Windows 10 новая файловая парадигма. Я начинал программировать в DOS
            string[] s = file.Path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            StorageFile txtOut = await ApplicationData.Current.LocalFolder.CreateFileAsync(string.Format("{0}\\list.txt", s[s.Length - 2]), CreationCollisionOption.ReplaceExisting);
            IRandomAccessStream streamTxtOut = await txtOut.OpenAsync(FileAccessMode.ReadWrite);
            using (IOutputStream outputStream = streamTxtOut.GetOutputStreamAt(0))
            {
                // We'll add more code here in the next step.
                using (DataWriter dataWriter = new DataWriter(outputStream))
                {
                    int i = 1;
                    foreach (StorageFile f in fileList)
                    {
                        DateTime dateTime1 = DateTime.Now;

                        PointCollection pc = new PointCollection();
                        Point p;
                        int n;
                        Stream stream;
                        ImageStream imageStream;
                        /*
                         * MediaPlayerElement оказался не нужен. Не уверен что я добрался до пикселов видео по самому короткому пути.
                         */
                        MediaClip clip = await MediaClip.CreateFromFileAsync(f);
                        MediaComposition composition = new MediaComposition();
                        composition.Clips.Add(clip);

                        TimeSpan tsND = clip.OriginalDuration - new TimeSpan(0, 0, 0, 0, 100); // это чтобы не вылететь за конец файла. было такое.

                        TimeSpan ts = new TimeSpan(); // текущая позиция

                        double len = Math.Ceiling((tsND - ts) / tStep);
                        notify2.Text = fileList.Count.ToString() + "/" + len.ToString();

                        for (n = 0; ts < tsND; n++)
                        {
                            imageStream = await composition.GetThumbnailAsync(ts, (int)NaturalWidth, (int)NaturalHeight, VideoFramePrecision.NearestFrame);

                            wBitmap.SetSource(imageStream);

                            stream = wBitmap.PixelBuffer.AsStream();
                            _ = stream.Read(pixels, 0, pixels.Length);
                            p.X = n;
                            p.Y = pixels[4 * lx + NaturalWidth * 4 * ly + 2]; // +2 - красный байт. может быть стоило взять синий или зелёный. не знаю который более чувствителен.
                            pc.Add(p);

                            imageStream.Dispose();

                            ts += tStep;
                            notify1.Text = i.ToString() + "/" + n.ToString();
                        }
                        i++;

                        pcName = System.IO.Path.GetDirectoryName(txtOut.Path) + "\\" + f.Name + ".pc"; // pc - PointCollection

                        DateTime dateTime2 = DateTime.Now;
                        TimeSpan tm = dateTime2 - dateTime1; // оценочное суждение

                        _ = dataWriter.WriteString(pc.Count.ToString() + "  " + pcName + "  " + ts.ToString() + "  " + tm.ToString() + "  " + tsND.ToString() + "\n");

                        WritePointCollection(pcName, pc);

                        composition.Clips.Clear();
                    }
                    await dataWriter.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }
            streamTxtOut.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.

            if (file == null) return;

            pcName = ApplicationData.Current.LocalFolder.Path + "\\" + s[s.Length - 2] + "\\" + file.Name + ".pc";
            DrawDiagr(pcName);

            MakeDiagrAsync();
        }

        private void WritePointCollection(string name, PointCollection pc)
        {
            using (BinaryWriter dataWriter = new BinaryWriter(File.Open(name, FileMode.Create)))
            {
                dataWriter.Write(pc.Count);
                dataWriter.Write(lx);
                dataWriter.Write(ly);
                // не хватает параметра tStep. полезно сохранить в файле шаг обработки по времени.

                for (int i = 0; i < pc.Count; i++)
                {
                    dataWriter.Write(pc[i].X);
                    dataWriter.Write(pc[i].Y);
                }
            }
        }

        // Эта функция появилась позже.
        // Сначала, я хотел вывести диаграммы по всем файлам на одну картинку и увидеть всю ситуацию сразу.
        // Потом оказалось, что обрабатывать много файлов это долго.
        // И удобно по каждому видео иметь диаграмму сразу на экране. Если файл уже обработан.
        private void DrawDiagr(string name)
        {
            int n = -1;

            PointCollection pc = new PointCollection();
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
                    pc.Add(p);
                }
            }

            // Оказалось, просматривая видео после обработки, удобно видеть контрольную точку.
            // Это я понял уже просматривая видео после обработки. добавил метку.
            uint frameHeight = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoHeight;
            uint frameWidth = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalVideoWidth;

            // Для поправки. контрольную точку я устанавливаю по mediaPlayerElement, а здесь
            // рисую в layoutRoot. Поэтому, получается сдвиг по x. Причину сдвига я понял
            // почти сразу, не первый день за компьютером. Решил проблемму не самым красивым
            // способом. Красивое решение отложил на следующую версию)
            double w = (Window.Current.Bounds.Width - mediaPlayerElement.ActualWidth) / 2; // размер окна не менять!

            double imagePressedX = mediaPlayerElement.ActualWidth / frameWidth * x + w; // поправка
            double imagePressedY = mediaPlayerElement.ActualHeight / frameHeight * y;

            Line linX = new Line
            {
                X1 = imagePressedX - 15,
                X2 = imagePressedX - 1,
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
                Y2 = imagePressedY - 1,
                Stroke = new SolidColorBrush(Colors.Yellow)
            };
            layoutRoot.Children.Add(LinY);

            // диаграмма. надо помнить, что она перевёрнутая.
            // наверное стоило так: p.Y = 128 - sr.ReadDouble() / 2;
            // 128 - высота canvas. для моей задачи это не важно.
            Polyline polyline1 = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1,
                Points = pc
            };

            canvas.Children.Clear();
            canvas.Children.Add(polyline1);
            // Оказалось, что диаграмма и slider плохо совпадают.
            // Сначала диаграмма опережает событие, потом отстаёт.
            // Отложил этот вопрос на потом.
        }

        private async void MakeDiagrAsync()
        {
            string[] s = file.Path.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            using (StreamReader reader = new StreamReader(await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(string.Format("{0}\\list.txt", s[s.Length - 2]))))
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

                float five = 0;
                float fiveX = 0;
                if (ts.Minutes > 5)
                {
                    five = max / ts.Minutes * 5;
                    fiveX = five;
                }

                int j = 0;
                int w = max;
                if (w < 250) w = 250;

                // Рисование в картинке я нашёл конечно в интернете.
                // Навтыкал вместе с using, методом copy/paste.
                // Наверное, если разобраться, тут можно оптимизировать.
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

                PointCollection pc = new PointCollection();
                Point p;

                using (CanvasDrawingSession ds = offscreen.CreateDrawingSession())
                {
                    // это шкала по 5 минут. пока так.
                    if (ts.Minutes > 5)
                    {
                        for (int i = 1; fiveX < max; i++)
                        {
                            fiveX = five * i;
                            string scale = (i * 5).ToString();
                            using (CanvasTextLayout tl = new CanvasTextLayout(ds, scale, format, 30, 25))
                            {
                                ds.DrawTextLayout(tl, fiveX - 10, 0, Colors.Gray);
                            }
                            ds.DrawLine(fiveX, head, fiveX, offscreen.SizeInPixels.Height, Colors.Black);
                        }
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
                                pc.Add(p);
                            }
                        }

                        //----------------------------------------------------------------------------------------------------------------------------------------
                        string vidName = string.Format("{0}", System.IO.Path.GetFileName(subs[1]));
                        using (CanvasTextLayout tl = new CanvasTextLayout(ds, vidName, format, w, 25))
                        {
                            ds.DrawTextLayout(tl, 10, 10 + 255 * j + head, Colors.Gray);
                        }

                        for (int i = 0; i < pc.Count - 1; i++)
                        {
                            ds.DrawLine(i, (float)pc[i].Y + 255 * j + head, i + 1, (float)pc[i + 1].Y + 255 * j + head, Colors.Red);
                        }
                        //----------------------------------------------------------------------------------------------------------------------------------------

                        pc.Clear();
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
                    notify2.Text = file1.DisplayName;
                }
            }
            // в картинку с диаграммами надо для информации добавить шаг времени с которым файлы обрабатывались.
        }

        // эта кнопка для случая, когда была обработана сразу вся папка и
        // к файлу уже есть .pc
        private void Diagr_Click(object sender, RoutedEventArgs e)
        {
            string[] s = file.Path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            string name = ApplicationData.Current.LocalFolder.Path + "\\" + s[s.Length - 2] + "\\" + file.Name + ".pc";
            if (!File.Exists(name))
            {
                notify1.Text = "Нет такого файла";
                return;
            }
            DrawDiagr(name);
            MakeDiagrAsync();
        }
    }
}
