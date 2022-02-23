using SpotifyAPI.Web;
using SpotifyTool.Data;
using SpotifyTool.Models.Artist;
using SpotifyTool.Models.Library;
using SpotifyTool.Models.Track;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Service
{
    public class LibraryService
    {
        private static SpotifyClient _client;

        public LibraryService()
        {
            var _acctService = new AccountService();
            _client = new SpotifyClient(_acctService.GetToken().AccessToken);
        }

        public async Task FetchLibrary(string userId)
        {
            using (var ctx = new ApplicationDbContext())
            {
                // DEBUG
                TimeSpan totalTime = new TimeSpan(0, 0, 0);
                var startTime = DateTime.Now;
                // DEBUG
                var page = await _client.Library.GetTracks();
                var library = new Library();
                var _tracks = new List<Track>();
                var _artists = new HashSet<Artist>();
                var _albums = new HashSet<Album>();

                // DEBUG
                var totalSongs = page.Total;
                // DEBUG

                do
                {
                    // DEBUG
                    var beforeProcess = DateTime.Now;
                    // DEBUG

                    foreach (var track in page.Items.Select(x => x.Track).ToList())
                    {
                        // ARTIST
                        var _artistsForTrack = new HashSet<Artist>();
                        foreach (var artist in track.Artists)
                        {
                            var inList = _artists.FirstOrDefault(x => x.ArtistId == artist.Id);
                            if (inList == null)
                            {
                                var inDb = ctx.Artists.Include(x => x.Albums).FirstOrDefault(x => x.ArtistId == artist.Id);
                                if (inDb != null)
                                {
                                    inDb.Name = artist.Name;
                                    
                                    _artistsForTrack.Add(inDb);
                                    _artists.Add(inDb);
                                }
                                else
                                {
                                    var newArtist = new Artist { ArtistId = artist.Id, Name = artist.Name };
                                    _artistsForTrack.Add(newArtist);
                                    _artists.Add(newArtist);
                                }
                            }
                            else
                            {
                                _artistsForTrack.Add(inList);
                            }
                        }
                        foreach (var artist in track.Album.Artists)
                        {
                            if (_artistsForTrack.FirstOrDefault(x => x.ArtistId == artist.Id) == null)
                            {
                                var inList = _artists.FirstOrDefault(x => x.ArtistId == artist.Id);
                                if (inList == null)
                                {
                                    var inDb = ctx.Artists.Include(x => x.Albums).FirstOrDefault(x => x.ArtistId == artist.Id);
                                    if (inDb != null)
                                    {
                                        inDb.Name = artist.Name;
                                        _artists.Add(inDb);
                                    }
                                    else
                                    {
                                        var newArtist = new Artist { ArtistId = artist.Id, Name = artist.Name };
                                        _artists.Add(newArtist);
                                    }
                                }
                            }
                        }

                        // ALBUM
                        Album albumForThisTrack = null;
                        var albumInList = _albums.FirstOrDefault(x => x.AlbumId == track.Album.Id);
                        if (albumInList == null)
                        {
                            var inDb = ctx.Albums.Include(x => x.Artists).FirstOrDefault(x => x.AlbumId == track.Album.Id);

                            if (inDb != null)
                            {
                                inDb.Title = track.Album.Name;
                                inDb.ReleaseDate = track.Album.ReleaseDate;
                                inDb.Artists = track.Album.Artists.Select(x => _artistsForTrack.FirstOrDefault(y => y.ArtistId == x.Id)).ToList();
                                _albums.Add(inDb);
                                albumForThisTrack = inDb;
                            }
                            else
                            {
                                var newAlbum =
                                    new Album
                                    {
                                        AlbumId = track.Album.Id,
                                        Title = track.Album.Name,
                                        Artists = track.Album.Artists.Select(x => _artistsForTrack.FirstOrDefault(y => y.ArtistId == x.Id)).ToList(),
                                    };
                                albumForThisTrack = newAlbum;
                                _albums.Add(albumForThisTrack);
                            }
                        }
                        else
                        {
                            albumForThisTrack = albumInList;
                        }


                        // TRACK
                        if (_tracks.FirstOrDefault(x => x.TrackId == track.Id) == null)
                        {
                            var inDb = ctx.Tracks.Include(x => x.Artists).Include(x => x.Album).FirstOrDefault(x => x.TrackId == track.Id);

                            if (inDb != null)
                            {
                                inDb.Title = track.Name;
                                inDb.Popularity = track.Popularity;
                                inDb.Album = albumForThisTrack;
                                inDb.Artists = _artistsForTrack;
                                inDb.IsExplicit = track.Explicit;
                            }
                            else
                            {
                                inDb =
                                    new Track
                                    {
                                        TrackId = track.Id,
                                        Title = track.Name,
                                        Popularity = track.Popularity,
                                        Album = albumForThisTrack,
                                        Artists = _artistsForTrack,
                                        IsExplicit = track.Explicit,
                                    };
                                ctx.Tracks.Add(inDb);
                            }
                            _tracks.Add(inDb);
                        }
                        else
                        {
                            library.DuplicateTrackIds.Add(track.Id);
                        }
                    }

                    // DEBUG
                    var processTime = DateTime.Now - beforeProcess;
                    var beforeRequest = DateTime.Now;
                    // DEBUG

                    page =
                        page.Next == null ? null : await _client.NextPage(page);

                    // DEBUG
                    var requestTime = DateTime.Now - beforeRequest;
                    totalTime = DateTime.Now - startTime;
                    if(page == null)
                        Debug.WriteLine($"DONE --- {totalTime} --- {totalTime.TotalSeconds / totalSongs}/song");
                    else
                        Debug.WriteLine($"{page.Offset / 20} / {page.Total / 20 + 1}\n   {processTime} + {requestTime} = {requestTime + processTime} / {totalTime}");
                    // DEBUG

                } while (page != null);

                library.Tracks = _tracks;
                library.CountArtists = _artists.Count;
                library.CountTracks = library.Tracks.Count;
                library.TracksPerArtist = GetTracksPerArtist(library.Tracks, _artists);
                library.AveragePop = CalcAvgPop(library.Tracks);
                library.Artists = _artists;

                var user = ctx.Users.FirstOrDefault(x => x.Id == userId);
                user.Library = library;
                user.LastFetch = DateTime.Now;

                ctx.SaveChanges();
            }
        }
        public LibraryStats GetStats(string userId)
        {
            using(var ctx = new ApplicationDbContext())
            {
                var stats = new LibraryStats();
                var library = ctx.Users.Include(x => x.Library).FirstOrDefault(x => x.Id == userId).Library;
                stats.TrackCount = library.CountTracks;
                stats.ArtistCount = library.CountArtists;
                stats.AveragePopularity = library.AveragePop;
                stats.ArtistsWithMostTracks = GetArtistsWithMostTracks(library.TracksPerArtist, 10);
                return stats;
            }
        }
        private string GetTracksPerArtist(ICollection<Track> library, ICollection<Artist> _artists)
        {
            var _output = "";

            foreach(var artist in _artists)
            {
                int numSongs = 0;
                foreach(var track in library)
                {
                    if(track.Artists.FirstOrDefault(x => x.ArtistId == artist.ArtistId) != null)
                        numSongs++;
                }

                _output += (artist.ArtistId + "#" + numSongs + '#');
            }

            return _output;
        }
        private Queue<KeyValuePair<ArtistSimple, int>> GetArtistsWithMostTracks(string model, int numRequested)
        {

            using (var ctx = new ApplicationDbContext())
            {
                var result = new List<KeyValuePair<ArtistSimple, int>>();
                var forbidden = new List<string>() { "0LyfQWJT6nXafLPZqxe9Of" };
                var splitter = model.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                for(int i = 0; i < splitter.Length; i++)
                {
                    if(i % 2 == 0)
                    {
                        var aId = splitter[i];
                        if (!forbidden.Contains(aId))
                        {
                            var artist = ctx.Artists.FirstOrDefault(x => x.ArtistId == aId);
                            var convA = new ArtistSimple { Id = artist.ArtistId, Name = artist.Name };
                            result.Add(new KeyValuePair<ArtistSimple, int>(convA, Int32.Parse(splitter[i + 1])));
                        }
                    }
                }
                if(numRequested > 0)
                    return new Queue<KeyValuePair<ArtistSimple, int>>(result.OrderByDescending(x => x.Value).Take(numRequested));
                else
                    return new Queue<KeyValuePair<ArtistSimple, int>>(result.OrderByDescending(x => x.Value));
            }
        }
        public string GetLastFetch(string userId)
        {
            using(var ctx = new ApplicationDbContext())
            {
                var lastFetch = ctx.Users.FirstOrDefault(x => x.Id == userId).LastFetch;
                if (lastFetch == null)
                    return "Never";
                else
                    return lastFetch.ToString();
            }
        }
        public Queue<TrackListItem> GetTracksByArtistInLibrary(string artistId, string userId)
        {
            using (var ctx = new ApplicationDbContext())
            {
                var result = new List<TrackListItem>();
                var user = ctx.Users.Include(x => x.Library.Tracks).FirstOrDefault(x => x.Id == userId);
                var _listOfTracks = user.Library.Tracks.Where(x => x.Artists.FirstOrDefault(y => y.ArtistId == artistId) != null).ToList();

                foreach(var track in _listOfTracks)
                {
                    result.Add(new TrackListItem { Title = track.Title, Album = track.Album.Title});
                }
                return new Queue<TrackListItem>(result.OrderBy(x => x.Title));
            }
        }
        public string GetArtistNameById(string id)
        {
            using (var ctx = new ApplicationDbContext())
            {
                return ctx.Artists.FirstOrDefault(x => x.ArtistId == id).Name;
            }
        }
        private int CalcAvgPop(ICollection<Track> library)
        {
            return (int)Math.Round(library.Select(x => x.Popularity).Average(), MidpointRounding.AwayFromZero);
        }
        //public List<ArtistSimple> GetArtists(string userId)
        //{
        //    using(var ctx = new ApplicationDbContext())
        //    {
        //        var user = ctx.Users.FirstOrDefault(x => x.Id==userId);
        //        return
        //            ctx
        //                .Libraries
        //                .FirstOrDefault(x => x. == userId)
        //    }
        //}
    }
}
