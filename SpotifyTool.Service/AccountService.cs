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
        private static EmbedIOAuthServer _server;
        private static readonly ManualResetEvent mre = new ManualResetEvent(false);
        private static readonly Uri callbackUri = new Uri("http://localhost:5000/callback");
        private static SpotifyClient _client;
        private static string tokenPath;

        public PrivateUser LoginSpotify()
        {
            tokenPath = GetPath();

            GetAuth();

            var token = ReadToken();

            var config = SpotifyClientConfig.CreateDefault(token.AccessToken).WithHTTPLogger(new SimpleConsoleHTTPLogger());
            _client = new SpotifyClient(config);

            return FetchUser();
        }
        private static void GetAuth()
        {
            _server = new EmbedIOAuthServer(callbackUri, 5000);
            _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID", EnvironmentVariableTarget.Machine), LoginRequest.ResponseType.Code)
            {
                Scope = GetScopes()
            };

            BrowserUtil.Open(request.ToUri());

            mre.WaitOne();
            mre.Reset();
        }

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

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID", EnvironmentVariableTarget.Machine),
                Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET", EnvironmentVariableTarget.Machine), 
                response.Code,
                callbackUri
              )
            );

            WriteToken(tokenResponse);

            mre.Set();
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            await _server.Stop();

            mre.Set();
        }

        private static void WriteToken(AuthorizationCodeTokenResponse token)
        {
            using (StreamWriter file = File.CreateText(tokenPath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, token);
            }
        }
        private AuthorizationCodeTokenResponse ReadToken()
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(tokenPath))
                {
                    string json = streamReader.ReadToEnd();
                    return JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(json);
                }
            }
            catch
            {
                return null;
            }
        }
        public void ClearToken()
        {
            if(File.Exists(tokenPath))
                File.Delete(tokenPath);
        }
        public string GetToken()
        {
            var token = ReadToken();
            return token.AccessToken;
        }
        private string GetPath()
        {
            var execPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(execPath, @".\spotify_token.json");
        }
        public UserProfile GetUserProfile(string userId)
        {
            using (var ctx = new ApplicationDbContext())
            {
                var user = ctx.Users.SingleOrDefault(x => x.Id == userId);

                if (user == null)
                    return null;

                return new UserProfile() { Id = user.Id, Username = user.UserName, Email = user.Email, SpotifyId = user.SpotifyId };
            }
        }
        public PrivateUser FetchUser()
        {
            try
            {
                var user = _client.UserProfile.Current().Result;

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Read();
            }
            return null;
        }
        public bool CheckNeedToRefreshToken()
        {
            var tokenToRefresh = ReadToken();

            if (tokenToRefresh == null)
                return true;

            return tokenToRefresh.ExpiresIn < 600;
        }
    }
}