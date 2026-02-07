using AppwriteHelper;
using AppwriteHelper.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebApiJwtBearer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.Configure<AppwriteSettingsOptions>(
                builder.Configuration.GetSection(AppwriteSettingsOptions.SectionName));

            builder.Services.AddAuthentication(AppwriteAuthenticationDefaults.JwtAuthenticationScheme).
                AddAppwriteJwtBearerAuthentication(options =>
                {
                    var appwriteSettings = builder.Configuration
                        .GetSection(AppwriteSettingsOptions.SectionName)
                        .Get<AppwriteSettingsOptions>() ?? new AppwriteSettingsOptions();

                    options.AppwriteProject = appwriteSettings.Project;
                    options.AppwriteEndpoint = appwriteSettings.Endpoint;
                });

            builder.Services.AddAppwriteServerClient();
            builder.Services.AddAppwriteUserClient();


            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseAppwriteUserClientAuthentication();

            app.MapControllers();

            app.Run();
        }
    }
}
