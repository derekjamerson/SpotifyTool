using SpotifyTool.Models.Album;
using SpotifyTool.Models.Artist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Models.Track
{
    public class TrackFull
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int Popularity { get; set; }
        public ICollection<ArtistSimple> Artists { get; set; }
        public AlbumSimple Album { get; set; }

    }
}
