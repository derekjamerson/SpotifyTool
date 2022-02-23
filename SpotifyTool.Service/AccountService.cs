using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Http;
using SpotifyTool.Data;
using SpotifyTool.Models.User;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SpotifyTool.Service
{
    public class AccountService
    {
        private static SpotifyClient _client;


        #region LOCALCODE
        // LOCALCODE
        private static EmbedIOAuthServer _server;
        private static readonly Uri callbackUri = new Uri("http://localhost:5000/callback");
        private static ManualResetEvent mre;
        private static readonly string filePath = @"C:\Users\Derek\source\repos\SpotifyTool/token.txt";


        public PrivateUser LoginSpotify()
        {
            GetAuth();

            var token = GetToken();
            var config = SpotifyClientConfig.CreateDefault(token.AccessToken).WithAuthenticator(new AuthorizationCodeAuthenticator(GetClientId(), GetClientSecret(), token));
            _client = new SpotifyClient(config);

            return FetchUser();
        }
        private void GetAuth()
        {
            _server = new EmbedIOAuthServer(callbackUri, 5000);
            _server.Start();
            mre = new ManualResetEvent(false);

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, GetClientId(), LoginRequest.ResponseType.Code)
            {
                Scope = GetScopes()
            };

            BrowserUtil.Open(request.ToUri());

            mre.WaitOne();
            mre.Reset();
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                GetClientId(),
                GetClientSecret(),
                response.Code,
                callbackUri
              )
            );

            File.WriteAllText(filePath, JsonConvert.SerializeObject(tokenResponse));

            mre.Set();
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            await _server.Stop();

            mre.Set();
        }
        #endregion


        private static List<string> GetScopes()
        {
            return
                new List<string>()
                {
                    Scopes.UgcImageUpload,
                    Scopes.UserReadPlaybackState,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadCurrentlyPlaying,
                    Scopes.UserReadPrivate,
                    Scopes.UserReadEmail,
                    Scopes.UserFollowModify,
                    Scopes.UserFollowRead,
                    Scopes.UserLibraryModify,
                    Scopes.UserLibraryRead,
                    Scopes.Streaming,
                    Scopes.AppRemoteControl,
                    Scopes.UserReadPlaybackPosition,
                    Scopes.UserTopRead,
                    Scopes.UserReadRecentlyPlayed,
                    Scopes.PlaylistModifyPrivate,
                    Scopes.PlaylistReadCollaborative,
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistModifyPublic
                };
        }

        public AuthorizationCodeTokenResponse GetToken()
        {
            return JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(File.ReadAllText(filePath));
        }
        public UserProfile GetUserProfile(string userId)
        {
            using (var ctx = new ApplicationDbContext())
            {
                var user = ctx.Users.SingleOrDefault(x => x.Id == userId);

                if (user == null)
                    return null;

                return new UserProfile() { Id = user.Id, Username = user.UserName, Email = user.Email, SpotifyId = user.SpotifyId, ImageUrl = user.ImageUrl };
            }
        }
        private PrivateUser FetchUser()
        {
            return _client.UserProfile.Current().Result;
        }
        public void UpdateUser()
        {
            using(var ctx = new ApplicationDbContext())
            {
                var user = FetchUser();
                var inDb = ctx.Users.FirstOrDefault(x => x.SpotifyId == user.Id);
                inDb.UserName = user.DisplayName ?? user.Id;
                inDb.Email = user.Email;
                inDb.SpotifyId = user.Id;

                var image = user.Images.FirstOrDefault();
                if (image != null)
                    inDb.ImageUrl = image.Url;

                ctx.SaveChanges();
            }
        }
        public string GetClientId()
        {
            using (var ctx = new ApplicationDbContext())
            {
                return ctx.Clients.FirstOrDefault().ClientId;
            }
        }public string GetClientSecret()
        {
            using (var ctx = new ApplicationDbContext())
            {
                return ctx.Clients.FirstOrDefault().ClientSecret;
            }
        }
    }
}