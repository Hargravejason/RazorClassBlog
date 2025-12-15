---

# RazorClassBlog

A lightweight, embeddable **blog system for ASP.NET Core Razor Pages**, delivered as a **Razor Class Library (RCL)**.

---

## About this project

**RazorClassBlog** exists to solve a very specific problem:

> "I need a simple, SEO-friendly blog inside my existing ASP.NET Core site —
> not a full CMS, not a headless platform, and not a WordPress clone."

This project provides:

* Public blog pages that search engines can crawl and index
* An optional admin UI for managing posts
* Clean architecture that lets the *host application* control authentication, identity, and data storage

It is designed to be:

* **Embedded**, not hosted
* **Configurable**, not opinionated
* **Small**, not over-engineered

If you want a full CMS, this probably isn’t it.
If you want a blog that “just works” inside your app, it might be.

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

### Admin UI (optional)

* Create, edit, and delete posts
* Draft, published, and scheduled posts
* Publish scheduling (future dates)
* Autosave drafts
* Rich text editor (TinyMCE)
* Role-based access using ASP.NET Core Identity

### Architecture

* Razor Class Library (RCL)
* Razor Pages UI
* Repository + service abstractions
* Entity Framework Core–based persistence
* Host-controlled database provider (SQL, Cosmos DB, etc.)

---

## Technology

* **Target framework:** .NET 9
* **UI:** Razor Pages + Bootstrap
* **Data:** Entity Framework Core
* **Auth:** ASP.NET Core Identity
* **Editor:** TinyMCE (via CDN)

---

## Installation

```bash
dotnet add package RazorClassBlog
```

Or reference the project directly during development.

---

## Basic setup

## Database configuration

The library depends on an `IBlogDbContext` abstraction.
The host application supplies the concrete EF Core implementation.

Example (Cosmos DB):

```csharp
builder.Services.AddDbContext<CosmosBlogDbContext>(options =>
    options.UseCosmos(
        builder.Configuration["Cosmos:ConnectionString"],
        builder.Configuration["Cosmos:DatabaseName"]));
```


### Register services

```csharp
builder.Services.AddBlogging<CosmosBlogDbContext>(options =>
{
    options.BlogKey = "main";
    options.PublicPageSize = 5;
    options.AdminPageSize = 25;

    options.AdminRoles = new[] { "Admin", "BlogAdmin" };
    options.ReaderRoles = new[] { "User", "Customer" };

    // Disable admin UI on public-only sites
    // options.EnableAdminUi = false;
});
```

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
