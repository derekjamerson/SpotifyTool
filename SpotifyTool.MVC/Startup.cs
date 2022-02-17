using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SpotifyTool.MVC.Startup))]
namespace SpotifyTool.MVC
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
