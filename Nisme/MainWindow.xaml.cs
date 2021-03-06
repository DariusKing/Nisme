﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Threading;
using System.Net;
using Un4seen.Bass;
using System.IO;
using System.Runtime.InteropServices;
using Lala.API;

namespace Nisme
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer progressTimer;
        //Player player;
        Vimae.Player player;
        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += new EventHandler(MainWindow_SourceInitialized);
        }

        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            //GlassHelper.ExtendGlassFrame(this, new Thickness(0, 30, 0, 0));
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();
            BassNet.Registration("miles@vimae.com", "2X22292815172922");
            nowPlaying.Artist.Text = String.Empty;
            nowPlaying.Song.Text = String.Empty;
            nowPlaying.parent = this;
            menuBar.parent = this;
            playList.parent = this;
            player = new Vimae.Player();
            player.Played += new EventHandler(player_Played);
            player.Stopped += new EventHandler(player_Stopped);
            player.QueueModified += new EventHandler(player_QueueModified);
            Loading win = new Loading();
            win.ShowModeless(new ThreadStart(LoadLibrary));
            progressTimer = new DispatcherTimer();
            progressTimer.Interval = new TimeSpan(1000); // This equals to 1 second, some tweaking may be necessary. //- WedTM
            progressTimer.Tick += new EventHandler(progressTimer_Tick);
            progressTimer.Start();
            nowPlaying.Visibility = Visibility.Visible;
            nowPlaying.MetaData.Visibility = Visibility.Hidden;
            this.Show();
        }

        void LoadLibrary()
        {
                LoadLibrary(false);
                if (Lala.API.Functions.LibraryNeedsUpdate(Lala.API.Instance.CurrentUser.Library.TrackCount))
                {
                    LoadLibrary(true);
                }
        }

        void player_QueueModified(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    nowPlaying.QueueLength.Text = Lala.API.Instance.CurrentUser.Queue.Count.ToString();
                }));
        }

        void player_Stopped(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                nowPlaying.MetaData.Visibility = Visibility.Hidden;
            }));
        }

        void player_Played(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                nowPlaying.MetaData.Visibility = Visibility.Visible;
            }));
            progressTimer.Start();
        }


        void Events_Play(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                nowPlaying.MetaData.Visibility = Visibility.Visible;
                progressTimer.Start();
            }));
        }


        void progressTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (Bass.BASS_ChannelIsActive(player.Channel) != BASSActive.BASS_ACTIVE_STOPPED)
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        int cpu = Convert.ToInt32(Bass.BASS_GetCPU() * 100);
                        menuBar.CPU.Text = "CPU Usage: " + cpu.ToString() + "%";
                        player.Update();
                        double PercentOfTrack = player.Song.CurrentPositionInSeconds / player.Song.TotalLengthInSeconds;
                        double trackBarLength = nowPlaying.trackBarBg.ActualWidth;
                        nowPlaying.playedAmount.Width = PercentOfTrack * trackBarLength;
                        nowPlaying.TotalTime.Text = Lala.API.Functions.SecondsToTime(player.Song.TotalLengthInSeconds, false);
                        nowPlaying.CurrentTime.Text = Lala.API.Functions.SecondsToTime(player.Song.CurrentPositionInSeconds, false);
                        double percentOfDownload = player.Song.CurrentPosition / player.Song.TotalLength;
                        nowPlaying.loadedAmount.Width = percentOfDownload * trackBarLength;
                    }));
                }
                else if (Lala.API.Instance.CurrentUser.Queue.Count > 0)
                {
                    PlayNextInQueue();
                }
                else
                    progressTimer.Stop();
            }

            catch (DllNotFoundException)
            {
                Functions.DownloadDlls();
            }
        }


        private void LoadLibrary(bool force)
        {
            
                    Lala.API.Functions.LoadUser(force); // Loads the current user and populates Lala.API.Instance.CurrentUser with their data.
                    Lala.API.Instance.CurrentUser.Library.Playing = Lala.API.Instance.CurrentUser.Library.Playlists[0];
                    //dataGrid1.DataContext = UserLibrary.Playing.Songs;
                    LoadDataGrid();
                    LoadPlaylists();
        }

        private delegate void DataGridLoader();
        private void LoadDataGrid()
        {
            if (this.dataGrid1.Dispatcher.CheckAccess())
            {
                dataGrid1.ItemsSource = Lala.API.Instance.CurrentUser.Library.Playing.Songs;
            }
            else
            {
                this.dataGrid1.Dispatcher.Invoke(DispatcherPriority.Normal, new DataGridLoader(LoadDataGrid));
            }
        }

        private delegate void PlaylistLoader();
        private void LoadPlaylists()
        {
            if (this.dataGrid1.Dispatcher.CheckAccess())
            {
                playList.PlaylistsContainer.ItemsSource = Lala.API.Instance.CurrentUser.Library.Playlists;
            }
            else
            {
                this.dataGrid1.Dispatcher.Invoke(DispatcherPriority.Normal, new DataGridLoader(LoadPlaylists));
            }
        }
        private void UnloadLibrary()
        {
            Lala.API.Instance.CurrentUser = null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }



        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window_Closing(sender, null);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void dataGrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            progressTimer.IsEnabled = true;
            Song selected = (Song)dataGrid1.SelectedItem;
            UpdateMetaData();
            player.Play(selected);
        }

        private void UpdateMetaData()
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    nowPlaying.Artist.Text = Lala.API.Instance.CurrentUser.Library.Playing.CurrentSong.Artist;
                    nowPlaying.Song.Text = Lala.API.Instance.CurrentUser.Library.Playing.CurrentSong.Title;
                    nowPlaying.AlbumImage.Source = new BitmapImage(new Uri(Lala.API.Instance.CurrentUser.Library.Playing.CurrentSong.AlbumImage));
                }));
        }


        public void PlayNextInQueue()
        {
            Song selected = (Song)Lala.API.Instance.CurrentUser.Queue[0];
            player.RemoveSongFromQueue(0);
            Lala.API.Instance.CurrentUser.Library.Playing.CurrentSong = selected;
            UpdateMetaData();
            player.Play(selected);
        }

        public void PlayNewQueue()
        {
           Lala.API.Instance.CurrentUser.Queue.Clear();
            Song selected = (Song)dataGrid1.SelectedItem;
            if (selected == null)
                return;
            int StartIndex = dataGrid1.Items.IndexOf(selected);
            Lala.API.Instance.CurrentUser.Queue.Add(selected);
            for (int i = 1; i < 99; i++)
            {
                if ((i + StartIndex) <= (dataGrid1.Items.Count - StartIndex) - 1)
                {
                    player.AddSongToQueue((Song)dataGrid1.Items[i + StartIndex]);
                }
            }
            PlayNextInQueue();
        }


        private void dataGrid1_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //Add the currently selected track to the Q and update the nowPlaying bar to reflect the number of items in the Q.
            if (e.Key == Key.Q)
            {
                Song sng = (Song)dataGrid1.SelectedItem;
                if (sng == null)
                    return;
                player.AddSongToQueue(sng);
            }
        }

        private void dataGrid1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlayNewQueue();
        }
    }
}
