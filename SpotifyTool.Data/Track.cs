using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Data
{
    public class Track
    {
        [Key]
        public string TrackId { get; set; }
        public string Title { get; set; }
        public int Popularity { get; set; } = 0;
        public bool IsExplicit { get; set; }

        public Track()
        {
            this.Artists = new HashSet<Artist>();
            this.Libraries = new HashSet<Library>();
        }
        public virtual ICollection<Artist> Artists { get; set; }
        public virtual ICollection<Library> Libraries { get; set; }

        public string AlbumId { get; set; }
        [ForeignKey(nameof(AlbumId))]
        public virtual Album Album { get; set; }
    }
}
