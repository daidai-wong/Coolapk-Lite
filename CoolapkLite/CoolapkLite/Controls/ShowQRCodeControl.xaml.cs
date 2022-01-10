﻿using QRCoder;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace CoolapkUWP.Controls
{
    public sealed partial class ShowQRCodeControl : UserControl
    {
        public static readonly DependencyProperty QRCodeTextProperty = DependencyProperty.Register(
            "QRCodeText",
            typeof(string),
            typeof(ShowQRCodeControl),
            new PropertyMetadata(string.Empty, new PropertyChangedCallback(OnQRCodeTextChanged))
        );

        public string QRCodeText
        {
            get => (string)GetValue(QRCodeTextProperty);
            set => SetValue(QRCodeTextProperty, value);
        }

        private static void OnQRCodeTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ShowQRCodeControl).RefreshQRCode();
        }

        private void FeedPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            Uri shareLinkString = ValidateAndGetUri(QRCodeText);
            if (shareLinkString != null)
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetWebLink(shareLinkString);
                dataPackage.Properties.Title = "动态分享";
                dataPackage.Properties.Description = QRCodeText;
                DataRequest request = args.Request;
                request.Data = dataPackage;
            }
            else
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(QRCodeText);
                dataPackage.Properties.Title = "内容分享";
                dataPackage.Properties.Description = "内含文本";
                DataRequest request = args.Request;
                request.Data = dataPackage;
            }
        }

        private static Uri ValidateAndGetUri(string uriString)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(uriString);
            }
            catch (FormatException)
            {
            }
            return uri;
        }

        private void ShowUIButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested -= FeedPage_DataRequested;
            dataTransferManager.DataRequested += FeedPage_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private async void RefreshQRCode()
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode("https://www.coolapk.com", QRCodeGenerator.ECCLevel.Q);
                if (QRCodeText != null) { qrCodeData = qrGenerator.CreateQrCode(QRCodeText, QRCodeGenerator.ECCLevel.Q); }
                using (PngByteQRCode qrCodeBmp = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImageBmp = qrCodeBmp.GetGraphic(
                        20,
                        new byte[] { 0, 0, 0, 0xFF },
                        new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
                    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                        {
                            writer.WriteBytes(qrCodeImageBmp);
                            await writer.StoreAsync();
                        }
                        BitmapImage image = new BitmapImage();
                        await image.SetSourceAsync(stream);

                        qrCodeImage.Source = image;
                    }
                }
            }
        }

        public ShowQRCodeControl()
        {
            InitializeComponent();
        }
    }
}