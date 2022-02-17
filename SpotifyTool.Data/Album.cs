using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Data
{
    public class Album
    {
        [Key]
        public string AlbumId { get; set; }
        public string Title { get; set; }
        public string ReleaseDate { get; set; }
        //public List<Image> Images { get; set; }

        public Album()
        {
            this.Tracks = new List<Track>();
            this.Artists = new HashSet<Artist>();
        }

        public virtual ICollection<Track> Tracks { get; set; }

        public virtual ICollection<Artist> Artists { get; set; }
    }
}
