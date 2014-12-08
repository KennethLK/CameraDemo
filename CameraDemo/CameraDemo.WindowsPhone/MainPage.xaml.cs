using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CameraDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaCapture _mediaCapture = null;
        LowLagPhotoCapture _capture = null;
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            Loaded += MainPage_Loaded;

        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _mediaCapture = new MediaCapture();
            DeviceInformation device = (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).FirstOrDefault(d => d.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);

            _mediaCapture.Failed += MediaCapture_Failed;
            _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;

            await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
            {
                StreamingCaptureMode = StreamingCaptureMode.Video, //设备
                PhotoCaptureSource = PhotoCaptureSource.Photo, //拍摄类型
                AudioDeviceId = string.Empty,
                VideoDeviceId = device.Id
            });


            _mediaCapture.VideoDeviceController.LowLagPhoto.ThumbnailEnabled = true;
            _mediaCapture.VideoDeviceController.LowLagPhoto.ThumbnailFormat = MediaThumbnailFormat.Bmp;
            _mediaCapture.VideoDeviceController.LowLagPhoto.DesiredThumbnailSize = 1600;


            captureElement.Source = _mediaCapture;
            
            Size resolution = this.FindHighestSupportedPhotoResolution();

            ImageEncodingProperties encoding = ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8);
            encoding.Width  = unchecked((uint)resolution.Width);
            encoding.Height = unchecked((uint)resolution.Height);
            _capture = _mediaCapture.PrepareLowLagPhotoCaptureAsync(encoding).GetResults();
        }

        private Size FindHighestSupportedPhotoResolution()
        {
            List<Size> supportedResolutions = _mediaCapture
                .VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo)
                .OfType<VideoEncodingProperties>().ToList()
                .Select(encoding => new Size(encoding.Width, encoding.Height))
                .Where(r => r.Height * r.Width <= 10000)
                .ToList();

            return supportedResolutions.Aggregate(supportedResolutions.First(), (max, current) => (max.Height * max.Width) < (current.Height * current.Width) ? current : max);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            _mediaCapture.Dispose();
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private async void captureButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MediaEncodingProfile data = new MediaEncodingProfile();
            data.Video.Height = 800;
            data.Video.Width = 480;
            InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream();
            await _mediaCapture.PrepareLowLagRecordToStreamAsync(data, ms);

            BitmapImage bmp = new BitmapImage();
            ms.Seek(0);
            bmp.SetSource(ms);
            ms = null;
            img.Source = bmp;
            img.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn.Content.ToString() == "开启")
            {

                await _mediaCapture.StartPreviewAsync();
                btn.Content = "关闭";
                img.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                await _mediaCapture.StopPreviewAsync();
                btn.Content = "开启";
            }
        }

        private void MediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
        }

        MediaCaptureInitializationSettings _settings = null;
    }
}
