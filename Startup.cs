using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(EDS.Startup))]
namespace EDS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
