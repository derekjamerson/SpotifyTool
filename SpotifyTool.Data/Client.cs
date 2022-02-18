using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Data
{
    public class Client
    {
        [Key]
        public int ClientKey { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
