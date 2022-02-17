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
        private static string spotifyClientId = "a0947f5c419d4f52b56dbce466137f20";
        private static string challenge = "";
        private static string verifier = "";
        private static PKCETokenResponse tokenResp;

        public PrivateUser LoginSpotify()
        {
            GetAuth();

            var config = SpotifyClientConfig.CreateDefault(tokenResp.AccessToken).WithAuthenticator(new PKCEAuthenticator(spotifyClientId, tokenResp));
            _client = new SpotifyClient(config);

            return FetchUser();
        }
        private static void GetAuth()
        {
            _server = new EmbedIOAuthServer(callbackUri, 5000);
            _server.Start();
            (verifier, challenge) = PKCEUtil.GenerateCodes();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID", EnvironmentVariableTarget.Machine), LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
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
              new PKCETokenRequest(
                spotifyClientId,
                response.Code, 
                callbackUri,
                verifier
              )
            );

            tokenResp = tokenResponse;

            mre.Set();
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            await _server.Stop();

            mre.Set();
        }

        public string GetToken()
        {
            return tokenResp.AccessToken;
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
    }
}