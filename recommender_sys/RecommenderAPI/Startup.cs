using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BooxBox.DataAccess;
using BooxBox.DataAccess.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecommenderAPI.Scheme;

namespace RecommenderAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = "MainAuthScheme";
                options.DefaultForbidScheme = "MainAuthScheme";
                options.AddScheme<AuthSchemeHandler>("MainAuthScheme", "Auth scheme");
            });

            var envVariables = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            // API key
            var apiKey = envVariables.GetValue(typeof(string), "API_KEY") as string;
            services.AddSingleton<IKey>(new ApiKey(apiKey));

            // Database auth info
            var dbUsr = envVariables.GetValue(typeof(string), "DB_USR") as string;
            var dbPsw = envVariables.GetValue(typeof(string), "DB_PSW") as string;
            services.AddSingleton<IDatabaseAuth>(new DatabaseAuth(dbUsr, dbPsw));

            // Database DI
            services.AddSingleton<IDatabase, Database>();

            // Repository DI
            services.AddScoped<IBoxRecordMapper, BoxRecordMapper>();
            services.AddScoped<IBaseRepository, BaseRepository>();
            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ILikeRepository, LikeRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<IBoxRepository, BoxRepository>();
            services.AddScoped<IPreferencesRepository, PreferencesRepository>();
            services.AddScoped<IRecommenderRepository, RecommenderRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
