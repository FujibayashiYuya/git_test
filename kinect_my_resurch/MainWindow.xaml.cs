using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Alea;
using Alea.Parallel;

namespace kinect_my_resurch
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /*Sensor→Source→Reader→FrameReference→Frame*/
        //本体関係
        KinectSensor kinect;
        CoordinateMapper mapper;
        MultiSourceFrameReader multiReader;

        // Color関係
        FrameDescription colorFrameDescription;
        //取得するカラー画像のフォーマット
        ColorImageFormat colorImageFormat = ColorImageFormat.Bgra;
        private byte[] colorFrameData;

        // Depth関係
        FrameDescription depthFrameDescription;
        //センサーからフレームデータを受け取る中間ストレージ
        private ushort[] depthFrameData = null;

        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                //Kinectへの参照を確保
                this.kinect = KinectSensor.GetDefault();
                this.mapper = kinect.CoordinateMapper;

                //Color情報取得
                this.colorFrameDescription
                    = kinect.ColorFrameSource.CreateFrameDescription(this.colorImageFormat);
                this.colorFrameData = new byte[colorFrameDescription.LengthInPixels *
                                            colorFrameDescription.BytesPerPixel];

                //深度について
                this.depthFrameDescription = this.kinect.DepthFrameSource.FrameDescription;
                //受信して変換するピクセルを配置するためのスペースを割り当てます
                this.depthFrameData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];

                this.multiReader = this.kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);
                this.multiReader.MultiSourceFrameArrived += multiReader_MultiSourceFrameArrived;
                kinect.Open();
            }
            catch
            {
                MessageBox.Show("Kinectの検出できやせん");
            }
        }

        void multiReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var multiFrame = e.FrameReference.AcquireFrame();
            //例外処理
            if (multiFrame == null)
            {
                MessageBox.Show("マルチフレームがありません");
                return;
            }

            //データの取得
            var colorFrame = multiFrame.ColorFrameReference.AcquireFrame();
            var depthFrame = multiFrame.DepthFrameReference.AcquireFrame();
            if (colorFrame == null || depthFrame == null)
            {
                return;
            }
            colorFrame.CopyConvertedFrameDataToArray(colorFrameData, this.colorImageFormat);
            depthFrame.CopyFrameDataToArray(this.depthFrameData);

            //描写
            //Depthのサイズで作成
            var colorImageBuffer = new byte[depthFrameDescription.LengthInPixels * colorFrameDescription.BytesPerPixel];
            //Depth座標系に対応するカラー座標系の取得
            var colorSpace = new ColorSpacePoint[depthFrameDescription.LengthInPixels];
            mapper.MapDepthFrameToColorSpace(depthFrameData, colorSpace);
            for (int i = 0; i < this.depthFrameData.Length; ++i)
            {
                int colorX = (int)colorSpace[i].X;
                int colorY = (int)colorSpace[i].Y;
                if ((colorX < 0) || (colorFrameDescription.Width <= colorX) ||
                             (colorY < 0) || (colorFrameDescription.Height <= colorY))
                {
                    continue;
                }

                //カラー画像のインデックス
                int colorIndex = colorY * colorFrameDescription.Width + colorX;
                int colorImageIndex = (int)(i * colorFrameDescription.BytesPerPixel);
                int colorBufferIndex = (int)(colorIndex * colorFrameDescription.BytesPerPixel);

                colorImageBuffer[colorImageIndex + 0] = colorFrameData[colorBufferIndex + 0];
                colorImageBuffer[colorImageIndex + 1] = colorFrameData[colorBufferIndex + 1];
                colorImageBuffer[colorImageIndex + 2] = colorFrameData[colorBufferIndex + 2];
            }
            Images.Source = BitmapSource.Create(this.depthFrameDescription.Width,
                this.depthFrameDescription.Height,
                96, 96, PixelFormats.Bgr32, null, colorImageBuffer, this.depthFrameDescription.Width * (int)this.colorFrameDescription.BytesPerPixel);
            colorFrame.Dispose();
            depthFrame.Dispose();
        }
        /*
                private void DrawDepthCoodinate()
                {
                    //Depthのサイズで作成
                    var colorImageBuffer = new byte[depthFrameDescription.LengthInPixels * colorFrameDescription.BytesPerPixel];
                    //Depth座標系に対応するカラー座標系の取得
                    var colorSpace = new ColorSpacePoint[depthFrameDescription.LengthInPixels];
                    mapper.MapDepthFrameToColorSpace(depthFrameData, colorSpace);

                    for (int i = 0; i < this.depthFrameData.Length; ++i)
                    {
                        int colorX = (int)colorSpace[i].X;
                        int colorY = (int)colorSpace[i].Y;
                        if ((colorX < 0) || (colorFrameDescription.Width <= colorX) ||
                                     (colorY < 0) || (colorFrameDescription.Height <= colorY))
                        {
                            return;
                        }

                        //カラー画像のインデックス
                        int colorIndex = colorY * colorFrameDescription.Width + colorX;
                        int colorImageIndex = (int)(i * colorFrameDescription.BytesPerPixel);
                        int colorBufferIndex = (int)(colorIndex * colorFrameDescription.BytesPerPixel);

                        colorImageBuffer[colorImageIndex + 0] = colorFrameData[colorBufferIndex + 0];
                        colorImageBuffer[colorImageIndex + 1] = colorFrameData[colorBufferIndex + 1];
                        colorImageBuffer[colorImageIndex + 2] = colorFrameData[colorBufferIndex + 2];
                    }
                    Images.Source = BitmapSource.Create(this.depthFrameDescription.Width,
                        this.depthFrameDescription.Height,
                        96, 96, PixelFormats.Bgr32, null, colorImageBuffer, this.depthFrameDescription.Width * (int)this.colorFrameDescription.BytesPerPixel);
                }*/
        /*
                ///<summary>
                ///kinectがカラー画像を取得したときに実行されるメソッド
                ///</summary>
                ///<param name="sender">
                ///イベントを通知したオブジェクト（kinect）
                ///</param>
                ///<param name="e">
                ///イベント時に渡されるデータ（カラー画像）
                ///</param>
                void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
                {
                    //通知されたフレームの取得
                    ColorFrame colorFrame = e.FrameReference.AcquireFrame();

                    //例外処理
                    if (colorFrame == null)
                    {
                        return;
                    }

                    //画像情報を確保するバッファ（領域）を確保
                    //高さ*幅*画素あたりのデータ量
                    byte[] colors = new byte[this.colorFrameDescription.Width
                        * this.colorFrameDescription.Height * this.colorFrameDescription.BytesPerPixel];

                    //用意した領域に画素情報を複製
                    colorFrame.CopyConvertedFrameDataToArray(colors, this.colorImageFormat);

                    //画素情報をビットマップとして扱う
                    BitmapSource bitmapSource = BitmapSource.Create(this.colorFrameDescription.Width,
                        this.colorFrameDescription.Height,
                        96, 96, PixelFormats.Bgra32, null, colors, this.colorFrameDescription.Width * (int)this.colorFrameDescription.BytesPerPixel);

                    //this.canvas.Background = new ImageBrush(bitmapSource);

                    colorFrame.Dispose();
                }

                private void Reader_DepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
                {
                    ushort minDepth = 0;
                    ushort maxDepth = 0;

                    //フレームの取得
                    DepthFrame depthFrame = e.FrameReference.AcquireFrame();
                    //例外処理
                    if (depthFrame == null)
                    {
                        return;
                    }

                    depthFrame.CopyFrameDataToArray(this.depthFrameData);
                    minDepth = depthFrame.DepthMinReliableDistance;
                    maxDepth = depthFrame.DepthMaxReliableDistance;
                    int colorPixelIndex = 0;
                    for (int i = 0; i < this.depthFrameData.Length; ++i)
                    {
                        // Get the depth for this pixel
                        ushort depth = this.depthFrameData[i];
                        int image_x = i % 512;
                        int image_y = i / 512;
                        //懸念ポイント intにしていいか
                        int world_z = depth;
                        int world_x = (int)(image_x * depth / 365.6);
                        int world_y = (int)(image_y * depth / 365.6);

                        // To convert to a byte, we're mapping the depth value to the byte range.
                        // Values outside the reliable depth range are mapped to 0 (black).
                        byte intensity = (byte)(depth * 256 / 8000);

                        // Write out blue byte B
                        this.depthPixels[colorPixelIndex++] = intensity;

                        // Write out green byte G
                        this.depthPixels[colorPixelIndex++] = intensity;

                        // Write out red byte    R                    
                        this.depthPixels[colorPixelIndex++] = intensity;

                        // Write out alpha byte                        
                        this.depthPixels[colorPixelIndex++] = 255;
                    }

                    Images.Source = BitmapSource.Create(this.depthFrameDescription.Width,
                        this.depthFrameDescription.Height,
                        96, 96, PixelFormats.Bgra32, null, depthPixels, this.depthFrameDescription.Width * (int)this.colorFrameDescription.BytesPerPixel);
                    //Debug.WriteLine(this.depthPixels[1]);
                }*/


        /// <summary>
        /// この WPF アプリケーションが終了するときに実行されるメソッド。
        /// </summary>
        /// <param name="e">
        /// イベントの発生時に渡されるデータ。
        /// </param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (multiReader != null)
            {
                multiReader.MultiSourceFrameArrived -= multiReader_MultiSourceFrameArrived;
                multiReader.Dispose();
                multiReader = null;
            }

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }
    }
}
