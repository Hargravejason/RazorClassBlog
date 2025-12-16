using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RazorClassBlog;
using RazorClassBlog.EntityFramework;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add database contexts
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<ApplicationDbContext>();

// Register RCL services
builder.Services.AddDbContext<CosmosBlogDbContext>(options =>
  options.UseCosmos(
    connectionString: builder.Configuration["Cosmos:ConnectionString"] ?? throw new InvalidOperationException("Connection string 'Cosmos:ConnectionString' not found."),
    databaseName: builder.Configuration["Cosmos:DatabaseName"] ?? throw new InvalidOperationException("Connection string 'Cosmos:DatabaseName' not found.")));
builder.Services.AddBlogging<CosmosBlogDbContext>(options =>
{
  options.PublicRoutePrefix = "/articles";              // default; change if you want a different public blog route
  options.PublicDisplayName = "Articles";               // default; change if you want a different public blog name
  options.BlogKey = "main";                             // default; change if you want multiple blogs later
  options.PublicPageSize = 5;                           // show 5 posts per page publicly
  options.AdminPageSize = 25;                           // show 25 in admin list
  options.DefaultOrganizationName = "Acme Aces";        // default author name if no user info available
  options.DefaultOrganizationImageURL = "https://cdn.AcmeAces.com/images/acme-aces-logo.png"; // default author image if no user info available
  //options.EnableAdminUi = false;                      // option to completely disable the admin UI - useful if you have a separate admin app
  //options.AdminRoles = new[] { "User", "BlogAdmin" }; // permssions to manage blog
  //options.ReaderRoles = new[] { "User", "Customer" }; // permission to be considered a "reader" (e.g. can comment)
});

var app = builder.Build();

#region Setup Idenity roles in the system
using (var scope = app.Services.CreateScope())
{
  using (var dbContext = scope.ServiceProvider.GetRequiredService<IBlogDbContext>())
  {
    if(dbContext is CosmosBlogDbContext dbc)
      await dbc.Database.EnsureCreatedAsync();
  }
}

#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();  
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
