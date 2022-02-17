using SpotifyTool.Models.Artist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Models.Library
{
    public class LibraryStats
    {
        public int TrackCount { get; set; }
        public int ArtistCount { get; set; }
        public Queue<KeyValuePair<ArtistSimple, int>> ArtistsWithMostTracks { get; set; }
        public double AveragePopularity { get; set; }
    }
}
