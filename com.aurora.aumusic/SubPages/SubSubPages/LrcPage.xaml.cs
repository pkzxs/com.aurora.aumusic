﻿//Copyright(C) 2015 Aurora Studio

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
//to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.



/// <summary>
/// Usings
/// </summary>
using Windows.UI.Xaml.Controls;
using Kfstorm.LrcParser;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using com.aurora.aumusic.shared;
using com.aurora.aumusic.shared.Lrc;
using Windows.UI.Xaml.Navigation;
using com.aurora.aumusic.shared.Songs;
using System.Threading;
using Windows.System.Threading;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Media.Playback;

namespace com.aurora.aumusic
{
    public sealed partial class LrcPage : Page
    {
        Song CurrentSong;
        AutoResetEvent trigger = new AutoResetEvent(false);
        ILrcFile lyric = null;
        List<LrcModel> lyrics;
        CurrentTheme Theme = ((Window.Current.Content as Frame).Content as MainPage).Theme;
        public ThreadPoolTimer LyricTimer { get; private set; }
        public bool DoSearch = true;

        public LrcPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var str = (string)ApplicationSettingsHelper.ReadSettingsValue("AutoLyric");
            if (str == "false")
            {
                DoSearch = false;
            }
            if (e.Parameter != null)
                this.CurrentSong = new Song(e.Parameter as SongModel);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            trigger.Set();
            base.OnNavigatedFrom(e);
            if (LyricTimer != null)
            {
                LyricTimer.Cancel();
            }
            trigger = null;
            CurrentSong = null;
            lyric = null;
            lyrics = null;
        }

        public async Task genLrc()
        {
            string result = null;
            try
            {
                result = await LrcHelper.Fetch(await LrcHelper.isLrcExist(CurrentSong), CurrentSong);
            }
            catch (System.Net.WebException)
            {
                this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                {
                    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                    loader.GetString("WebExceptionText");
                    ErrText.Text = "没有网络连接";
                }));

            }

            if (result == null)
            {
                lyric = null;
                lyrics = null;
                return;
            }
            else
            {
                var stream = await FileHelper.ReadFileasString(result);
                try
                {
                    lyric = LrcFile.FromText(stream);
                    lyrics = new List<LrcModel>();
                    foreach (var item in lyric.Lyrics)
                    {
                        lyrics.Add(new LrcModel(item));
                    }
                }
                catch (FormatException e)
                {
                    try
                    {
                        var strings = e.Message.Split('\'');
                        string s = strings[1].Substring(strings[1].IndexOf(':') + 1);
                        s = s.TrimEnd('0');
                        int j = stream.IndexOf(s);
                        int start, end;
                        for (int i = j; ; i--)
                        {
                            if (i <= 0 || stream[i] == '\n')
                            {
                                start = i;
                                for (j = stream.IndexOf(s); ; j++)
                                {
                                    if (j >= stream.Length || stream[j] == '\n')
                                    {
                                        end = j;
                                        break;
                                    }
                                }
                                break;
                            }

                        }
                        StringBuilder sb = new StringBuilder(stream.Substring(0, start));
                        sb.Append(stream.Substring(end));
                        s = sb.ToString();
                        await FileHelper.SaveFile(s, result);
                        lyric = LrcFile.FromText(s);
                        lyrics = new List<LrcModel>();
                        foreach (var item in lyric.Lyrics)
                        {
                            lyrics.Add(new LrcModel(item));
                        }
                    }
                    catch (Exception)
                    {
                        lyric = null;
                        lyrics = null;
                    }
                }
                trigger.Set();
            }
        }

        private async void WaitingRing_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(700);
            IAsyncAction asyncAction;
            if (!DoSearch)
            {
                WaitingRing.IsActive = false;
                WaitingPanel.Visibility = Visibility.Collapsed;
                return;
            }
            asyncAction = ThreadPool.RunAsync(
                         async (workItem) =>
                         {
                             await FetchLrc();
                             if (trigger != null)
                             {
                                 trigger.Set();
                                 await this.Dispatcher.RunAsync(
                                                              CoreDispatcherPriority.High,
                                                              new DispatchedHandler(() =>
                                                              {
                                                                  if (lyrics == null)
                                                                      return;
                                                                  if (WaitingRing != null)
                                                                  {
                                                                      LyricSource.Source = lyrics;
                                                                      LyricView.ItemsSource = LyricSource.View;
                                                                      WaitingRing.IsActive = false;
                                                                      WaitingPanel.Visibility = Visibility.Collapsed;
                                                                  }

                                                              }));
                                 if (lyric != null)
                                 {
                                     LyricTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
                                     {
                                         var now = BackgroundMediaPlayer.Current.Position;
                                         await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(async () =>
                                         {
                                             if (WaitingRing != null && lyrics != null)
                                             {
                                                 foreach (var item in lyrics)
                                                 {
                                                     item.MainColor = Resources["FixedTranslucentBrush"] as SolidColorBrush;
                                                 }
                                                 try
                                                 {
                                                     lyrics[lyric.Lyrics.IndexOf(lyric.BeforeOrAt(now))].MainColor = Resources["FixedWhiteBrush"] as SolidColorBrush;
                                                     await LyricView.ScrollToIndex(lyric.Lyrics.IndexOf(lyric.BeforeOrAt(now)));
                                                 }
                                                 catch (Exception)
                                                 {
                                                     TimerOver();
                                                 }
                                             }
                                         }));
                                     }, TimeSpan.FromSeconds(0.16));

                                 }

                                 else
                                     return;

                             }
                         });

            await Task.Run(async () =>
            {
                try
                {
                    bool result = trigger.WaitOne(15000);
                    {
                        if (lyric == null)
                        {
                            trigger.Set();
                            asyncAction.Cancel();
                            asyncAction.Close();
                            await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(() =>
                            {
                                if (WaitingRing != null)
                                {
                                    ErrPanel.Visibility = Visibility.Visible;
                                    WaitingRing.IsActive = false;
                                    WaitingPanel.Visibility = Visibility.Collapsed;
                                    LyricView.IsEnabled = false;
                                    LyricView.Visibility = Visibility.Collapsed;
                                }
                            }));
                        }

                    }
                }
                catch (Exception)
                {
                    return;
                }

            });

        }


        private void TimerOver()
        {
        }

        private async Task FetchLrc()
        {
            try
            {
                var uri = CurrentSong.Title + "-" + CurrentSong.Artists[0] + "-" + CurrentSong.Album + ".lrc";
                lyric = LrcFile.FromText(await FileHelper.ReadFileasString(uri));
                lyrics = new List<LrcModel>();
                foreach (var item in lyric.Lyrics)
                {
                    lyrics.Add(new LrcModel(item));
                }
                trigger.Set();
            }
            catch (Exception)
            {
                try
                {
                    await genLrc();
                    trigger.Set();
                }
                catch (Exception)
                {
                    if (trigger != null)
                    {
                        trigger.Set();
                    }
                }

            }
        }

        private void LyricView_LayoutUpdated(object sender, object e)
        {
            DynamicFooter.Height = LyricView.ActualHeight;
        }

    }
}
