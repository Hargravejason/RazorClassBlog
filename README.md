---

# RazorClassBlog

A lightweight, embeddable **blog system for ASP.NET Core Razor Pages**, delivered as a **Razor Class Library (RCL)**.

---

## About this project

**RazorClassBlog** exists to solve a very specific problem:

> "I need a simple, SEO-friendly blog inside my existing ASP.NET Core site -
> not a full CMS, not a headless platform, and not a WordPress clone."

This project provides:

* Public blog pages that search engines can crawl and index
* An optional admin UI for managing posts
* Clean architecture that lets the *host application* control authentication, identity, and data storage

It is designed to be:

* **Embedded**, not hosted
* **Configurable**, not opinionated
* **Small**, not over-engineered

If you want a full CMS, this probably isn't it.
If you want a blog that "just works" inside your app, it might be.

This was also a practical test to try an use AI (GPT5) to develop as much out of the box without tinkering as possible. So far its about 80% AI written, and 20% cleanup/re-structure.

---

## Features

### Public blog

* Blog index with pagination
* Individual post pages
* SEO-friendly URLs (`/blog/yyyy/mm/slug`)
* Breadcrumb navigation (HTML + JSON-LD)
* Canonical URLs and meta descriptions
* Structured data (BlogPosting / Article)
* Open Graph & Twitter Card metadata

### Admin UI (optional for a public facing side, can be turned off)

* Create, edit, and delete posts
* Draft, published, and scheduled posts
* Publish scheduling (future dates)
* Autosave drafts
* Rich text editor (TinyMCE) (must include separately)
* Role-based access using ASP.NET Core Identity

### Architecture

* Razor Class Library (RCL)
* Razor Pages UI
* Repository + service abstractions
* Entity Framework Core-based persistence
* Host-controlled database provider (SQL, Cosmos DB, etc.)

---

## Technology

* **Target framework:** .NET 9
* **UI:** Razor Pages + Bootstrap
* **Data:** Entity Framework Core
* **Auth:** ASP.NET Core Identity
* **Editor:** TinyMCE via Host site package (must include separately)

---

## Installation

```bash
dotnet add package RazorClassBlog
```

Or reference the project directly during development.

---

## Basic setup

### Database configuration

The library depends on an `IBlogDbContext` abstraction.
The host application supplies the concrete EF Core implementation.

Example (SQL DB):

```csharp
builder.Services.AddDbContext<SQLBlogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloggingDatabase")));
```

Example (Cosmos DB):

```csharp
builder.Services.AddDbContext<CosmosBlogDbContext>(options =>
    options.UseCosmos(
        builder.Configuration["Cosmos:ConnectionString"],
        builder.Configuration["Cosmos:DatabaseName"]));
```

The sample Cosmos context partitions `BlogPostDeliveries` by
`DeliveryPartitionKey`, set to `{BlogKey}-{PostId}` by the subscription service.
Deliveries for the same blog and post share a partition; different posts can
be distributed across partitions. A highly popular post can still create a hot
partition.
Delivery checks and unsubscribe token lookups continue to filter by `BlogKey`
and may query across partitions.

Existing Cosmos containers cannot change partition keys in place. Stop
notification sending and other delivery writes during migration and cutover.
Copy all delivery documents to a new container partitioned on
`/DeliveryPartitionKey`, setting that field to `{BlogKey}-{PostId}` for each
document and preserving their IDs, blog keys, subscriber IDs, post IDs, and
unsubscribe token hashes.
Verify the copy and point the host's context at the new container before
resuming writes. `EnsureCreated` does not migrate an existing container.
Direct writers must also populate the key. SQL hosts need no schema change for
this partitioning update; custom Cosmos contexts must explicitly map the
otherwise unmapped property and configure it as the partition key.

### Register services

Most of the configuration happens during service registration. 
Ensure you pass the `IBlogDbContext` abstraction object context in that you used for DbContext:

```csharp
builder.Services.AddBlogging<CosmosBlogDbContext>(options =>
{
  options.BlogKey = "main";       // default; change if you plan on having multiple blogs in one database
  options.SetBlogTypes<BlogType>(); // using RazorClassBlog.EnumsandConstants; or use your own enum
  options.PublicPageSize = 10;    // default; change if you want to show more per page on the public facing side
  options.AdminPageSize = 20;     // default; change if you want to show more per page in the admin facing side

  options.DefaultOrganizationName = "Acme Aces";                                        //what is the name of the organization this blog exists for?
  options.DefaultOrganizationImageURL = "https://cdn.AcmeAces.com/acme-aces-logo.png";  //what is the organization home page image for seo reference?

  options.AdminRoles = new[] { "Admin", "BlogAdmin" };  // default "Administrator"; change to fit your identity setup, leave blank for no auth checks
  options.ReaderRoles = new[] { "User", "Customer" };   // default empty; change to fit your identity setup, leave blank for no auth checks

  //options.EnableAdminUi = false;                      // Disable admin UI on public-only sites. Useful if you have separate sites for admin and public

  //options.BlogDescription = "News, updates, and tips from Our Company.";                                                  // default; change if you want the description on the blog/index page
  //options.BlogReason = "We share product updates, how-tos, and best practices to help you get more out of our platform."; // default; change if you want the reason on the blog/index page (can be html)

  //options.PublicRoutePrefix = "/blog";     // default; change if you want a different public blog route
  //options.PublicDisplayName = "Blog";      // default; change if you want a different public blog name
});
```

### Post types and JSON-LD

The default `BlogType` enum contains `BlogPosting`, `Article`, and `NewsArticle`.
Use `options.SetBlogTypes<YourEnum>()` to supply a host-defined enum. Its member
names populate the admin dropdown and are emitted as the schema.org `@type` in
`_BlogPostingJSONLDPartial.cshtml`, so use valid schema.org type names. Enum
members are ordered by their underlying numeric values; the first is used for
new posts and existing posts with no type. The selected name is stored as a
string, so keep names stable when changing the enum. The host can override the
partial to customize the rest of the JSON-LD output.

Existing SQL databases need a nullable `BlogType` column on `BlogPosts` through
the host's normal migration process before deploying this change. Existing
Cosmos documents need no migration; missing or null values use the configured
first type.

Ensure Razor Pages, Identity, and Authorization are enabled:

```csharp
builder.Services.AddRazorPages();
builder.Services.AddAuthorization();
```

Middleware:

```csharp
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
```

## Admin UI & security

### Admin pages

Admin pages live under:

```
/BlogAdmin
```

Access is controlled via ASP.NET Core Identity roles configured in `BlogOptions`.

### Disable admin UI entirely

If the admin UI is hosted in a separate site, routes can be removed entirely:

```csharp
options.EnableAdminUi = false;
```

When disabled:

* Admin routes are not registered
* `/BlogAdmin` returns 404
* No admin surface exists to attack

---

### Customize the public routing entirely

You can configure the public facing side to have custom names for the routes now:

```csharp
options.PublicRoutePrefix = "/articles";
options.PublicDisplayName = "Articles";
```

When used the public blog will be accessible at `/articles` instead of `/blog`, and the display name will be "Articles" instead of "Blog". 
But all other functionality remains the same, items such as 

```csharp
<a asp-page="Index" asp-area="Blog">@Options.Value.PublicDisplayName</a>
```

Will now render as:

```html
<a href="/articles">Articles</a>
```

Useful if you want to have a name other than blog for your site.

---

## Publishing model

* **Draft**: not visible publicly
* **Published**: visible immediately
* **Scheduled**: visible when `PublishedUtc <= UtcNow`

---

## Author handling

Posts support an editable display author:

* **AuthorId**: internal identifier (audit)
* **AuthorName**: displayed author (person or organization)

Examples:

* "Jane Doe"
* "Acme Marketing Team"

---

## Non-goals

This project intentionally does **not** attempt to be:

* A full CMS
* A headless content platform
* A page builder
* A replacement for WordPress, Ghost, or similar systems

---

## License

MIT License

---