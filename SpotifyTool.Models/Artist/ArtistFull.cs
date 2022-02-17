using SpotifyTool.Models.Album;
using SpotifyTool.Models.Track;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Models.Artist
{
    public class ArtistFull
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ICollection<AlbumSimple> Albums { get; set; }
        public ICollection<TrackSimple> Tracks { get; set; }
    }
}
