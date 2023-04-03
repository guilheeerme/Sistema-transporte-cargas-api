using Application.Interfaces;
using Application.Services;

namespace Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        { 
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }
}
