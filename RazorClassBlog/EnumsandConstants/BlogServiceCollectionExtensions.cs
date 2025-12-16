using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;
using RazorClassBlog.Services;

namespace RazorClassBlog;

public static class BlogServiceCollectionExtensions
{
  public static IServiceCollection AddBlogging <TContext> (this IServiceCollection services, Action<BlogOptions>? configure = null) where TContext : IBlogDbContext
  {
    if (configure is not null)
      services.Configure(configure);
    else
      services.Configure<BlogOptions>(_ => { });

    services.TryAddScoped<IBlogRepository, Repositories.BlogRepository>();
    services.TryAddScoped<IBlogService, BlogService>();

    // Database configuration items
    services.AddScoped<IBlogDbContext>(sp => sp.GetRequiredService<TContext>());

    var razorPages = services.AddRazorPages();

    // Add role checks based on options
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

      razorPages.AddRazorPagesOptions(o =>
      {
        // Map Blog index to /property-insights
        o.Conventions.AddAreaPageRoute(
            "Blog",  
            "/Index",
            blogOptions.PublicRoutePrefix);

        // Map Blog post page to /property-insights/{year}/{month}/{slug}
        o.Conventions.AddAreaPageRoute(
            "Blog",
            "/Post",
            $"{blogOptions.PublicRoutePrefix}/{{year:int}}/{{month:int}}/{{slug}}");
      });
    }

    return services;
  }
}
