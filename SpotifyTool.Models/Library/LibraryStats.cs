using SpotifyTool.Models.Artist;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Models.Library
{
    public class LibraryStats
    {
        [DisplayName("Number of Tracks")]
        public int TrackCount { get; set; }
        [DisplayName("Number of Artists")]
        public int ArtistCount { get; set; }
        [DisplayName("Most Frequent Artists")]
        public Queue<KeyValuePair<ArtistSimple, int>> ArtistsWithMostTracks { get; set; }
        [DisplayName("Average Popularity")]
        public double AveragePopularity { get; set; }
    }
}
