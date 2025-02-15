﻿using CoolapkLite.BackgroundTasks;
using CoolapkLite.Common;
using CoolapkLite.Helpers;
using CoolapkLite.Models;
using CoolapkLite.Pages.BrowserPages;
using CoolapkLite.Pages.FeedPages;
using CoolapkLite.Pages.SettingsPages;
using CoolapkLite.ViewModels.BrowserPages;
using CoolapkLite.ViewModels.DataSource;
using CoolapkLite.ViewModels.FeedPages;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace CoolapkLite.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class PivotPage : Page, IHaveTitleBar
    {
        private Action Refresh;
        public Frame MainFrame => PivotContentFrame;

        public PivotPage()
        {
            InitializeComponent();
            UIHelper.AppTitle = this;
            UIHelper.ShellDispatcher = Dispatcher;
            PivotContentFrame.Navigate(typeof(Page));
            if (SystemInformation.Instance.OperatingSystemVersion.Build >= 22000)
            { CommandBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right; }
            AppTitle.Text = ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? "酷安 Lite";
            if (!(AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop"))
            { UpdateTitleBarLayout(false); }
            NotificationsModel.Instance?.Update();
            LiveTileTask.Instance?.UpdateTile();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            { HardwareButtons.BackPressed += System_BackPressed; }
            // Add handler for ContentFrame navigation.
            PivotContentFrame.Navigated += On_Navigated;
            Pivot.ItemsSource = GetMainItems();
            if (e.Parameter is IActivatedEventArgs ActivatedEventArgs)
            { OpenActivatedEventArgs(ActivatedEventArgs); }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (this.IsAppWindow())
            {
                this.GetWindowForElement().Changed -= AppWindow_Changed;
            }
            else
            {
                Window.Current.SetTitleBar(null);
                SystemNavigationManager.GetForCurrentView().BackRequested -= System_BackRequested;
                CoreApplicationViewTitleBar TitleBar = CoreApplication.GetCurrentView().TitleBar;
                TitleBar.LayoutMetricsChanged -= TitleBar_LayoutMetricsChanged;
                TitleBar.IsVisibleChanged -= TitleBar_IsVisibleChanged;
            }
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            { HardwareButtons.BackPressed -= System_BackPressed; }
            PivotContentFrame.Navigated -= On_Navigated;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.IsAppWindow())
            {
                this.GetWindowForElement().Changed += AppWindow_Changed;
            }
            else
            {
                Window.Current.SetTitleBar(CustomTitleBar);
                SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
                CoreApplicationViewTitleBar TitleBar = CoreApplication.GetCurrentView().TitleBar;
                TitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
                TitleBar.IsVisibleChanged += TitleBar_IsVisibleChanged;
                UpdateTitleBarLayout(TitleBar);
            }
        }

        private void OpenActivatedEventArgs(IActivatedEventArgs args)
        {
            _ = PivotContentFrame.OpenActivatedEventArgs(args);
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            HideProgressBar();
            if (PivotContentFrame.Visibility == Visibility.Collapsed)
            {
                Pivot.Visibility = Visibility.Collapsed;
                PivotContentFrame.Visibility = Visibility.Visible;
            }
            if (!this.IsAppWindow())
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = PivotContentFrame.BackStackDepth == 0 ? AppViewBackButtonVisibility.Collapsed : AppViewBackButtonVisibility.Visible;
            }
        }

        private void System_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = TryGoBack();
            }
        }

        private void System_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = TryGoBack();
            }
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PivotItem MenuItem = Pivot.SelectedItem as PivotItem;
            if ((Pivot.SelectedItem as PivotItem).Content is Frame Frame && Frame.Content is null)
            {
                _ = Frame.Navigate(typeof(AdaptivePage), new AdaptiveViewModel(
                    MenuItem.Tag.ToString() == "indexV8"
                        ? "/main/indexV8"
                        : MenuItem.Tag.ToString().Contains("V")
                            ? $"/page?url={MenuItem.Tag}"
                            : $"/page?url=V9_HOME_TAB_FOLLOW&type={MenuItem.Tag}"));
                Refresh = () => _ = (Frame.Content as AdaptivePage).Refresh(true);
            }
            else if ((Pivot.SelectedItem as PivotItem).Content is Frame __ && __.Content is AdaptivePage AdaptivePage)
            {
                Refresh = () => _ = AdaptivePage.Refresh(true);
            }
        }

        private bool TryGoBack()
        {
            if (!Dispatcher.HasThreadAccess || !PivotContentFrame.CanGoBack)
            { return false; }

            if (PivotContentFrame.BackStackDepth > 1)
            {
                PivotContentFrame.GoBack();
            }
            else
            {
                PivotContentFrame.GoBack();
                Pivot.Visibility = Visibility.Visible;
                PivotContentFrame.Visibility = Visibility.Collapsed;
            }

            return true;
        }

        private void UpdateTitleBarLayout(CoreApplicationViewTitleBar TitleBar)
        {
            CustomTitleBar.Height = TitleBar.Height;
            LeftPaddingColumn.Width = new GridLength(TitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(TitleBar.SystemOverlayRightInset);
        }

        private void UpdateTitleBarLayout(bool IsVisible)
        {
            TopPaddingRow.Height = IsVisible && !UIHelper.HasStatusBar && !UIHelper.HasTitleBar ? new GridLength(32) : new GridLength(0);
            CustomTitleBar.Visibility = IsVisible && !UIHelper.HasStatusBar && !UIHelper.HasTitleBar ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (Refresh != null)
            {
                Refresh();
            }
            else if ((Pivot.SelectedItem as PivotItem).Content is ListView ListView && ListView.ItemsSource is EntityItemSourse ItemsSource)
            {
                _ = ItemsSource.Refresh(true);
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag.ToString())
            {
                case "User":
                    _ = await SettingsHelper.CheckLoginAsync()
                        ? PivotContentFrame.Navigate(typeof(ProfilePage), new ProfileViewModel())
                        : PivotContentFrame.Navigate(typeof(BrowserPage), new BrowserViewModel(UriHelper.LoginUri));
                    break;
                case "Bookmark":
                    _ = PivotContentFrame.Navigate(typeof(BookmarkPage), new BookmarkViewModel());
                    break;
                case "Setting":
                    _ = PivotContentFrame.Navigate(typeof(SettingsPage));
                    break;
                case "SearchButton":
                    _ = PivotContentFrame.Navigate(typeof(SearchingPage), new SearchingViewModel(string.Empty));
                    break;
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args) => UpdateTitleBarLayout(sender.TitleBar.IsVisible);

        private void TitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args) => UpdateTitleBarLayout(sender.IsVisible);

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args) => UpdateTitleBarLayout(sender);

        #region 搜索框

        private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ObservableCollection<object> observableCollection = new ObservableCollection<object>();
                sender.ItemsSource = observableCollection;
                string keyWord = sender.Text;
                await ThreadSwitcher.ResumeBackgroundAsync();
                (bool isSucceed, JToken result) = await RequestHelper.GetDataAsync(UriHelper.GetUri(UriType.SearchWords, keyWord), true);
                if (isSucceed && result != null && result is JArray array && array.Count > 0)
                {
                    foreach (JToken token in array)
                    {
                        switch (token.Value<string>("entityType"))
                        {
                            case "apk":
                                await Dispatcher.AwaitableRunAsync(() => observableCollection.Add(new SearchWord(token as JObject)));
                                break;
                            case "searchWord":
                            default:
                                await Dispatcher.AwaitableRunAsync(() => observableCollection.Add(new SearchWord(token as JObject)));
                                break;
                        }
                    }
                }
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            //if (args.ChosenSuggestion is AppModel app)
            //{
            //    UIHelper.NavigateInSplitPane(typeof(AppPages.AppPage), "https://www.coolapk.com" + app.Url);
            //}
            //else
            if (args.ChosenSuggestion is SearchWord word)
            {
                _ = PivotContentFrame.Navigate(typeof(SearchingPage), new SearchingViewModel(word.ToString()));
            }
            else if (args.ChosenSuggestion is null && !string.IsNullOrEmpty(sender.Text))
            {
                _ = PivotContentFrame.Navigate(typeof(SearchingPage), new SearchingViewModel(sender.Text));
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is SearchWord searchWord)
            {
                sender.Text = searchWord.ToString();
            }
        }

        #endregion

        #region 进度条

        public async void ShowProgressBar()
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
        }

        public async void ShowProgressBar(double value)
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
            ProgressBar.Value = value;
        }

        public async void PausedProgressBar()
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = true;
        }

        public async void ErrorProgressBar()
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowPaused = false;
            ProgressBar.ShowError = true;
        }

        public async void HideProgressBar()
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
            ProgressBar.Value = 0;
        }

        public async void ShowMessage(string message = null)
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.ResumeForegroundAsync();
            }

            AppTitle.Text = message ?? ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? "酷安 Lite";

            if (this.IsAppWindow())
            {
                this.GetWindowForElement().Title = message ?? string.Empty;
            }
            else
            {
                ApplicationView.GetForCurrentView().Title = message ?? string.Empty;
            }
        }

        #endregion

        public static ObservableCollection<PivotItem> GetMainItems()
        {
            ResourceLoader loader = ResourceLoader.GetForCurrentView("CirclePage");
            ObservableCollection<PivotItem> items = new ObservableCollection<PivotItem>
            {
                new PivotItem() { Tag = "indexV8", Header = loader.GetString("indexV8"), Content = new Frame() },
                new PivotItem() { Tag = "V9_HOME_TAB_FOLLOW", Header = loader.GetString("follow"), Content = new Frame() },
                new PivotItem() { Tag = "circle", Header = loader.GetString("circle"), Content = new Frame() },
                new PivotItem() { Tag = "apk", Header = loader.GetString("apk"), Content = new Frame() },
                new PivotItem() { Tag = "topic", Header = loader.GetString("topic"), Content = new Frame() },
                new PivotItem() { Tag = "question", Header = loader.GetString("question"), Content = new Frame() },
                new PivotItem() { Tag = "product", Header = loader.GetString("product"), Content = new Frame() }
            };
            return items;
        }
    }
}
