﻿using CoolapkLite.Helpers;
using CoolapkLite.Models;
using CoolapkLite.Models.Users;
using CoolapkLite.ViewModels.DataSource;
using CoolapkLite.ViewModels.Providers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace CoolapkLite.ViewModels.FeedPages
{
    public class CreateFeedViewModel : IViewModel
    {
        public static string[] ImageTypes = new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".heif", ".heic" };

        private string title = string.Empty;
        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        public readonly CreateUserItemSourse CreateUserItemSourse = new CreateUserItemSourse();
        public readonly CreateTopicItemSourse CreateTopicItemSourse = new CreateTopicItemSourse();

        public readonly ObservableCollection<WriteableBitmap> Pictures = new ObservableCollection<WriteableBitmap>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        public async Task Refresh(bool reset)
        {
            await CreateUserItemSourse.Refresh(reset);
            await CreateTopicItemSourse.Refresh(reset);
        }

        bool IViewModel.IsEqual(IViewModel other) => other is CreateFeedViewModel model && Equals(model);

        public async Task ReadFile(IStorageFile file)
        {
            using (IRandomAccessStreamWithContentType stream = await file.OpenReadAsync())
            {
                await ReadStream(stream);
            }
        }

        public async Task ReadStream(IRandomAccessStream stream)
        {
            BitmapDecoder ImageDecoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap SoftwareImage = await ImageDecoder.GetSoftwareBitmapAsync();
            try
            {
                WriteableBitmap WriteableImage = new WriteableBitmap((int)ImageDecoder.PixelWidth, (int)ImageDecoder.PixelHeight);
                await WriteableImage.SetSourceAsync(stream);
                Pictures.Add(WriteableImage);
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(CreateFeedViewModel)).Warn(ex.ExceptionToMessage(), ex);
                try
                {
                    using (InMemoryRandomAccessStream random = new InMemoryRandomAccessStream())
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, random);
                        encoder.SetSoftwareBitmap(SoftwareImage);
                        await encoder.FlushAsync();
                        WriteableBitmap WriteableImage = new WriteableBitmap((int)ImageDecoder.PixelWidth, (int)ImageDecoder.PixelHeight);
                        await WriteableImage.SetSourceAsync(random);
                        Pictures.Add(WriteableImage);
                    }
                }
                catch (Exception e)
                {
                    SettingsHelper.LogManager.GetLogger(nameof(CreateFeedViewModel)).Error(e.ExceptionToMessage(), e);
                }
            }
        }

        public async Task<bool> CheckData(DataPackageView data)
        {
            if (data.Contains(StandardDataFormats.Bitmap))
            {
                return true;
            }
            else if (data.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await data.GetStorageItemsAsync();
                IEnumerable<IStorageItem> images = items.Where(i => i is StorageFile).Where(i =>
                {
                    foreach (string type in ImageTypes)
                    {
                        if (i.Name.EndsWith(type, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
                });
                if (images.Any()) { return true; }
            }
            return false;
        }

        public async void PickImage()
        {
            FileOpenPicker FileOpen = new FileOpenPicker();
            FileOpen.FileTypeFilter.Add(".jpg");
            FileOpen.FileTypeFilter.Add(".jpeg");
            FileOpen.FileTypeFilter.Add(".png");
            FileOpen.FileTypeFilter.Add(".bmp");
            FileOpen.SuggestedStartLocation = PickerLocationId.ComputerFolder;

            foreach (StorageFile file in await FileOpen.PickMultipleFilesAsync())
            {
                if (file != null) { await ReadFile(file); }
            }
        }

        public async Task<List<string>> UploadPic()
        {
            int i = 0;
            List<string> results = new List<string>();
            if (!Pictures.Any()) { return results; }
            UIHelper.ShowMessage("上传图片");
            foreach (WriteableBitmap pic in Pictures)
            {
                i++;
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    Stream pixelStream = pic.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                        (uint)pic.PixelWidth,
                        (uint)pic.PixelHeight,
                        96.0,
                        96.0,
                        pixels);

                    await encoder.FlushAsync();

                    byte[] bytes = stream.GetBytes();
                    (bool isSucceed, string result) = await RequestHelper.UploadImage(bytes, "pic");
                    if (isSucceed) { results.Add(result); }
                }
                UIHelper.ShowMessage($"已上传 ({i}/{Pictures.Count})");
            }
            return results;
        }

        public async Task DropFile(DataPackageView data)
        {
            if (data.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference bitmap = await data.GetBitmapAsync();
                using (IRandomAccessStreamWithContentType random = await bitmap.OpenReadAsync())
                {
                    await ReadStream(random);
                }
            }
            else if (data.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await data.GetStorageItemsAsync();
                IEnumerable<IStorageItem> images = items.Where(i => i is StorageFile).Where(i =>
                {
                    foreach (string type in ImageTypes)
                    {
                        if (i.Name.EndsWith(type, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
                });
                if (images.Any()) { await ReadFile(images.FirstOrDefault() as StorageFile); }
            }
        }
    }

    public class CreateUserItemSourse : EntityItemSourse
    {
        private string keyword = string.Empty;
        public string Keyword
        {
            get => keyword;
            set
            {
                if (keyword != value)
                {
                    keyword = value;
                    UpdateProvider(value);
                }
            }
        }

        public CreateUserItemSourse(string keyword = " ")
        {
            Keyword = keyword;
        }

        private void UpdateProvider(string keyword)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                Provider = new CoolapkListProvider(
                    (p, firstItem, lastItem) =>
                    UriHelper.GetUri(
                        UriType.SearchCreateUsers,
                        keyword,
                        p,
                        p > 1 ? $"&firstItem={firstItem}&lastItem={lastItem}" : string.Empty),
                    GetEntities,
                    "uid");
            }
            else if (SettingsHelper.Get<string>(SettingsHelper.Uid) is string uid && !string.IsNullOrEmpty(uid))
            {
                Provider = new CoolapkListProvider(
                    (p, firstItem, lastItem) =>
                        UriHelper.GetUri(
                            UriType.GetUserList,
                            "followList",
                            uid,
                            p,
                            string.IsNullOrEmpty(firstItem) ? string.Empty : $"&firstItem={firstItem}",
                            string.IsNullOrEmpty(lastItem) ? string.Empty : $"&lastItem={lastItem}"),
                    (o) => new Entity[] { new UserModel((JObject)o["fUserInfo"]) },
                    "fuid");
            }
        }

        private IEnumerable<Entity> GetEntities(JObject jo)
        {
            yield return new UserModel(jo);
        }
    }

    public class CreateTopicItemSourse : EntityItemSourse
    {
        private string keyword = string.Empty;
        public string Keyword
        {
            get => keyword;
            set
            {
                if (keyword != value)
                {
                    keyword = value;
                    UpdateProvider(value);
                }
            }
        }

        public CreateTopicItemSourse(string keyword = " ")
        {
            Keyword = keyword;
        }

        private void UpdateProvider(string keyword)
        {
            Provider = new CoolapkListProvider(
                (p, firstItem, lastItem) =>
                UriHelper.GetUri(
                    UriType.SearchCreateTags,
                    keyword,
                    p,
                    p > 1 ? $"&firstItem={firstItem}&lastItem={lastItem}" : string.Empty),
                GetEntities,
                "id");
        }

        private IEnumerable<Entity> GetEntities(JObject jo)
        {
            yield return new TopicModel(jo);
        }
    }
}
