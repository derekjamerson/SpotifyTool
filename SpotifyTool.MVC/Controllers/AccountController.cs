using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using SpotifyAPI.Web;
using SpotifyTool.Data;
using SpotifyTool.Models.User;
using SpotifyTool.MVC.Models;
using SpotifyTool.Service;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;

namespace SpotifyTool.MVC.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private static AccountService _accountService;

        public AccountController()
        {
            _accountService = new AccountService();
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }


        #region AZURECODE
        //// AZURECODE
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public ActionResult SpotifyAuth()
        //{
        //    // Make sure "http://localhost:5000" is in your applications redirect URIs!
        //    var loginRequest = new LoginRequest(
        //      new Uri("https://spotifytool.azurewebsites.net:5000/callback"),
        //      _accountService.GetClientId(),
        //      LoginRequest.ResponseType.Code
        //    )
        //    {
        //        Scope = new[] { Scopes.UgcImageUpload, Scopes.UserReadPrivate, Scopes.UserReadEmail, Scopes.UserFollowModify, Scopes.UserFollowRead, Scopes.UserLibraryModify, Scopes.UserLibraryRead, Scopes.UserTopRead, Scopes.UserReadRecentlyPlayed, Scopes.PlaylistModifyPrivate, Scopes.PlaylistReadCollaborative, Scopes.PlaylistReadPrivate, Scopes.PlaylistModifyPublic }
        //    };
        //    var uri = loginRequest.ToUri();
        //    // Redirect user to uri via your favorite web-server
        //    return Redirect(uri.AbsoluteUri);
        //}
        //
        //[AllowAnonymous]
        //// This method should be called from your web-server when the user visits "http://localhost:5000"
        //public async Task GetCallback(string code)
        //{
        //    var response = await new OAuthClient().RequestToken(
        //      new AuthorizationCodeTokenRequest(_accountService.GetClientId(), _accountService.GetClientSecret(), code, new Uri("http://localhost:5000"))
        //    );
        //
        //    var spotify = new SpotifyClient(response.AccessToken);
        //    // Also important for later: response.RefreshToken
        //}
        #endregion


        #region LOCALCODE
        // LOCALCODE
        // POST: /Account/SpotifyLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SpotifyAuth()
        {
            var user = _accountService.LoginSpotify();

            if (user.Id == null)
                return RedirectToAction("Index", "Home");

            else
                return RedirectToAction("SpotifyLoginCallback", user);
        }

        // Get: /Account/SpotifyLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> SpotifyLoginCallback(PrivateUser spotifyUser)
        {
            var appUser = await UserManager.FindByEmailAsync(spotifyUser.Email);

            if (appUser == null)
            {
                var user = new ApplicationUser { UserName = spotifyUser.DisplayName ?? spotifyUser.Id, Email = spotifyUser.Email, SpotifyId = spotifyUser.Id };

                var result = await UserManager.CreateAsync(user);

                appUser = await UserManager.FindByEmailAsync(user.Email);
            }

            await SignInManager.SignInAsync(appUser, isPersistent: false, rememberBrowser: false);

            return RedirectToAction("Index", "Library");
        }
        #endregion


        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            try
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            }
            catch (NullReferenceException) { }

            return RedirectToAction("Index", "Home");
        }

        //Get: Account/ViewProfile
        public ActionResult ViewProfile()
        {
            var model = _accountService.GetUserProfile(User.Identity.GetUserId());

            return View(model);
        }

        public ActionResult UpdateProfile()
        {
            _accountService.FetchUser();

            return RedirectToAction("ViewProfile");
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}