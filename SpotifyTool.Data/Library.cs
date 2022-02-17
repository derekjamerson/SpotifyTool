using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Data
{
    public class Library
    {
        public int Id { get; set; }

        public int CountTracks { get; set; }
        public int CountArtists { get; set; }
        public int AveragePop { get; set; }
        public HashSet<string> DuplicateTrackIds { get; set; }
        public Dictionary<string, int> TracksPerArtist { get; set; }


        public Library()
        {
            this.Tracks = new List<Track>();
        }
        public virtual ICollection<Track> Tracks { get; set; }
    }
}
