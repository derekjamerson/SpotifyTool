using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Data
{
    public class Artist
    {
        [Key]
        public string ArtistId { get; set; }
        public string Name { get; set; }

        public Artist()
        {
            this.Albums = new HashSet<Album>();
            this.Tracks = new List<Track>();
            this.Libraries = new HashSet<Library>();
        }
        public virtual ICollection<Album> Albums { get; set; }
        public virtual ICollection<Track> Tracks { get; set; }
        public virtual ICollection<Library> Libraries { get; set; }
    }
}
