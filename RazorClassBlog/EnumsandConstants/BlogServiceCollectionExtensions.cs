using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;
using RazorClassBlog.Services;

namespace RazorClassBlog;

public static class BlogServiceCollectionExtensions
{
  public static IServiceCollection AddBlogging(this IServiceCollection services, Action<BlogOptions>? configure = null)
  {
    if (configure is not null)
    {
      services.Configure(configure);
    }
    else
    {
      services.Configure<BlogOptions>(_ => { });
    }

    // NOTE: Host still has to register IBlogDbContext -> concrete DbContext

    services.TryAddScoped<IBlogRepository, Repositories.BlogRepository>();
    services.TryAddScoped<IBlogService, BlogService>();


    using (var sp = services.BuildServiceProvider())
    {
      var blogOptions = sp.GetRequiredService<IOptions<BlogOptions>>().Value;

      var adminRoles = (blogOptions.AdminRoles?.Length ?? 0) > 0
          ? blogOptions.AdminRoles
          : new[] { "Administrator", "Admin" };

      var readerRoles = blogOptions.ReaderRoles ?? Array.Empty<string>();

      services.AddAuthorization(options =>
      {
        options.AddPolicy("BlogAdmin", policy =>
        {
          policy.RequireAuthenticatedUser();
          if (readerRoles.Length > 0)
          {
            policy.RequireRole(adminRoles!);
          }
          else
          {
            // if no reader roles set, any authenticated user is a "admin"
            // nothing else to add
          }
        });

        options.AddPolicy("BlogReader", policy =>
        {
          policy.RequireAuthenticatedUser();

          if (readerRoles.Length > 0)
          {
            policy.RequireRole(readerRoles);
          }
          else
          {
            // if no reader roles set, any authenticated user is a "reader"
            // nothing else to add
          }
        });
      });

      // Conditionally remove admin area routes
      if (!blogOptions.EnableAdminUi)
      {
        var razorPages = services.AddRazorPages();

        razorPages.AddRazorPagesOptions(options =>
        {
            options.Conventions.AddAreaFolderRouteModelConvention(
            "BlogAdmin",
            "/",
            model =>
            {
              // This wipes out all selectors => no endpoints generated
              model.Selectors.Clear();
            });
        });
      }
    }

    return services;
  }
}
