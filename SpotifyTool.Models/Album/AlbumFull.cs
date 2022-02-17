using SpotifyTool.Models.Artist;
using SpotifyTool.Models.Track;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Models.Album
{
    public class AlbumFull
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string ReleaseDate { get; set; }

        public ICollection<TrackSimple> Tracks { get; set; }

        public ICollection<ArtistSimple> Artists { get; set; }
    }
}
