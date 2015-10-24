﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Foundation;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
using Windows.UI;

namespace com.aurora.aumusic
{
    public class AlbumItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public string AlbumName;
        public string AlbumArtWork;
        private uint _year = 0;
        private Color _palette = new Color();
        public Color Palette
        {
            get
            {
                return _palette;
            }
            set
            {
                this._palette = value;
                this.OnPropertyChanged();
            }
        }
        private Color _textmaincolor = Color.FromArgb(255, 0, 0, 0);
        public Color TextMainColor
        {
            get
            {
                return _textmaincolor;
            }
            set
            {
                _textmaincolor = value;
                this.OnPropertyChanged();
            }
        }
        private Color _textsubcolor = Color.FromArgb(255, 63, 63, 63);
        public Color TextSubColor
        {
            get
            {
                return _textsubcolor;
            }
            set
            {
                _textsubcolor = value;
                this.OnPropertyChanged();
            }
        }
        public uint Year
        {
            get
            {
                return _year;
            }
            set
            {
                if (value > _year)
                    _year = value;
            }
        }
        private string[] _albumartists = null;
        public string[] AlbumArtists
        {
            get
            {
                return _albumartists;
            }
            set
            {
                if (value[0] == "Unknown Artists")
                {
                    return;
                }
                _albumartists = value;
            }
        }
        private string[] _artists = null;
        public string[] Artists
        {
            get
            {
                return _artists;
            }
            set
            {
                if (value[0] == "Unknown AlbumArtists")
                {
                    return;
                }
                _artists = value;
            }
        }

        public int Position { get; internal set; }

        public string[] Genres = null;
        public uint Rating = 0;
        public List<Song> Songs = new List<Song>();

        public AlbumItem(List<Song> tempSongs)
        {
            this.Songs = tempSongs;
            this.AlbumName = tempSongs[0].Album;
        }

        public AlbumItem()
        {
        }

        public void getArtwork()
        {
            AlbumArtWork = Songs[0].ArtWork;
        }

        public void refreshArtists()
        {
            if (Songs.Count != 0)
            {
                int i = 0, j = 0, k = 0, m = 0, o = 0;
                foreach (var item in Songs)
                {

                    if (item.AlbumArtists.Length > i)
                    {
                        i = item.AlbumArtists.Length;
                        k = j + 1;

                    }

                    if (item.Artists.Length > m)
                    {
                        m = item.Artists.Length;
                        o = j + 1;

                    }


                    j++;
                }
                if (k != 0)
                {
                    AlbumArtists = Songs[k - 1].AlbumArtists;
                }

                if (o != 0)
                {
                    Artists = Songs[o - 1].Artists;
                }

                if (AlbumArtists[0] == "Unknown AlbumArtists")
                {
                    AlbumArtists = Artists;
                }
                if (Artists[0] == "Unknown Artists")
                {
                    Artists = AlbumArtists;
                }
            }
        }

        internal async Task Refresh()
        {
            if (Songs.Count != 0)
            {
                this.refreshArtists();
                this.refreshGenres();
                this.refreshYear();
                if (this.refreshArtwork())
                {
                    await this.refreshPalette();
                }
                this.Sort();
            }
        }

        public void GenerateTextColor()
        {
            if ((Palette.R * 0.299 + Palette.G * 0.587 + Palette.B * 0.114) < 85)
            {
                TextMainColor = Color.FromArgb(255, 255, 255, 255);
                TextSubColor = Color.FromArgb(255, 190, 190, 190);
            }
        }

        public void getYear()
        {
            if (Songs.Count != 0)
            {
                foreach (var item in Songs)
                {
                    Year = item.Year;
                }
            }
        }


        private async Task refreshPalette()
        {
            await this.GetPalette();
        }

        private void refreshYear()
        {
            foreach (var item in Songs)
            {
                if (item.Year > this.Year)
                {
                    this.Year = item.Year;
                }

            }

        }

        public void Restore()
        {
            this.getArtwork();
            this.getYear();
            this.getGenres();
            this.refreshArtists();
            foreach (var item in Songs)
            {
                item.Position = this.Position;
            }
        }

        private bool refreshArtwork()
        {
            bool b = false;
            foreach (var item in Songs)
            {
                if (this.AlbumArtWork != item.ArtWork && item.ArtWork != "ms-appx:///Assets/unknown.png")
                {
                    this.AlbumArtWork = item.ArtWork;
                    b = true;
                }
            }
            return b;
        }

        private void refreshGenres()
        {
            foreach (var item in Songs)
            {
                if (item.Genres.Length > this.Genres.Length)
                {
                    this.Genres = item.Genres;
                }
            }
        }


        public async Task Initial()
        {
            if (Songs.Count != 0)
            {
                this.AlbumArtists = Songs[0].AlbumArtists;
                this.Artists = Songs[0].Artists;
                this.Genres = Songs[0].Genres;
                this.AlbumArtWork = Songs[0].ArtWork;
                this.Year = Songs[0].Year;
                await this.GetPalette();
            }
        }

        public async Task GetPalette()
        {
            Uri urisource = new Uri(AlbumArtWork);
            BitmapHelper p = new BitmapHelper();
            Palette = await p.New(urisource);
            this.GenerateTextColor();
        }

        public void Sort()
        {
            if (Songs.Count != 0)
            {
                uint track = 0;
                foreach (var item in Songs)
                {
                    track += item.Track;
                }
                if (track == 0)
                {
                    Songs.Sort((first, second) =>
                    {
                        return first.Title.CompareTo(second.Title);
                    });
                    for (int i = 1; i <= Songs.Count; i++)
                    {
                        Songs[i - 1].Track = (uint)i;
                        Songs[i - 1].TrackCount = (uint)Songs.Count;
                    }
                    return;
                }
                Songs.Sort((first, second) =>
                {
                    return first.Disc.CompareTo(second.Disc);
                });
                Songs.Sort((first, second) =>
                {
                    return first.Track.CompareTo(second.Track);
                });
            }
        }

        private void getGenres()
        {
            if (Songs.Count != 0)
            {
                int i = 0, j = 0, k = 0;
                foreach (var item in Songs)
                {
                    if (item.Genres != null)
                    {
                        if (item.Genres.Length != 0)
                        {
                            if (item.Genres.Length > i)
                            {
                                i = item.AlbumArtists.Length;
                                k = j;
                            }
                        }
                    }
                    j++;
                }
                if (k != 0)
                {
                    if (Songs[k].Genres[0] != null)
                    {
                        List<string> l = new List<string>();
                        foreach (var item in Songs[k].Genres)
                        {
                            l.Add(item);
                        }
                        Genres = l.ToArray();
                    }
                }
                else
                {
                    Genres = new[] { "Unknown Genres" };
                }
            }

        }
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
