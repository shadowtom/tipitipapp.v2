
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using tipitipapp.domain.Interfaces;
using tipitipapp.domain.Interfaces.Repository;
using tipitipapp.domain.Interfaces.Services;
using tipitipapp.domain.Services;
using tipitipapp.domain.Utilities.Security;
using tipitipapp.infrastructure.Data;
using tipitipapp.infrastructure.Repository;
using tipitipapp.ViewModels;
using tipitipapp.Views;

namespace tipitipapp.Extention
{
    public static class DependencyExtention
    {
        public static IServiceCollection AddDepedencyExtention(this IServiceCollection services, IConfiguration configuration)
        {
            var cstring = configuration.GetSection("ConnectionString:DefaultConnection").Value;
            services.AddDbContext<AppDbContext>(options =>
               options.UseSqlServer(cstring,
               sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                                    maxRetryCount: 5,
                                    maxRetryDelay: TimeSpan.FromSeconds(30),
                                    errorNumbersToAdd: null
                                    ))
               );
            // Register ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();

            // Register Views
            services.AddTransient<LoginPage>();
            services.AddTransient<RegisterPage>();
            services.AddTransient<MainPage>();

            // Register Shell
            services.AddSingleton<AppShell>();

            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IPasswordHasher, PasswordHasher>();
            return services;
        }
    }
}
