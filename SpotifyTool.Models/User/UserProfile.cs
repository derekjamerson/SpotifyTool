using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.Models.User
{
    public class UserProfile
    {
        [DisplayName("Account ID")]
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        [DisplayName("Spotify ID")]
        public string SpotifyId { get; set; }
        public string ImageUrl { get; set; }
    }
}
