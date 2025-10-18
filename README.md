# <img src="./wwwroot/images/quill-icon.png" alt="Icon" width="35" style="vertical-align: text-bottom" /> QuillKit

> *Your thoughts, published beautifully*

A modern, fast, markdown-based site engine built with ASP.NET Core 9.0. QuillKit combines the simplicity of markdown with the power of a dynamic web application, featuring flexible theming and hybrid content storage.

## ✨ Features

- **📝 Markdown-First**: Write your content in markdown with front matter support
- **🎨 Dynamic Theming**: Flexible theme system with view location expansion
- **🔄 Hybrid Storage**: Local files or Azure Blob Storage
- **📱 Responsive Design**: Beautiful, mobile-first Bootstrap 5.3 interface
- **🔗 Social Integration**: Built-in social media links with brand-accurate styling
- **🖼️ Media Support**: Lightbox galleries and video embed support
- **🌐 Clean URLs**: SEO-friendly routing and navigation

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 9.0 (MVC)
- **UI**: Bootstrap 5.3.8 + Bootstrap Icons 1.13.1
- **Configuration**: YAML with YamlDotNet
- **Markdown**: Markdig parser with extensions
- **Storage**: Hybrid (Local files or Azure Blob Storage)
- **Caching**: In-memory with file watching

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- A text editor or IDE (VS Code, Visual Studio, etc.)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/RazorAnt/QuillKit.git
   cd QuillKit
   ```

2. **Configure admin authentication**
   
   Edit `appsettings.Development.json` and set your admin credentials:
   
   ```json
   "AdminAuth": {
     "Username": "admin",
     "PasswordHash": "YOUR_PASSWORD_HASH_HERE"
   }
   ```
   
   **To generate a password hash:**
   1. Go to [SHA256 Hash Generator](https://codebeautify.org/sha256-hash-generator)
   2. Enter your desired password (e.g., `mypassword123`)
   3. Copy the SHA256 hash
   4. Paste it into `PasswordHash`
   
   **Default credentials (for testing):**
   - Username: `admin`
   - Password: `admin123`
   - Hash: `240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9`

3. **Run the application**
   ```bash
   dotnet run
   ```
   
   Visit `http://localhost:5000` to see your site and `http://localhost:5000/admin` to access the admin panel.

## ⚙️ Configuration

### Site Configuration (`Content/config/site-config.yml`)

Edit this YAML file to customize your site:

```yaml
site:
  title: "Your Site Title"
  subtitle: "Your tagline"
  description: "Site description for SEO"
  
author:
  name: "Your Name"
  bio: "Your bio"
  
social:
  github: "your-username"
  twitter: "your-handle"
```

### Admin Area

Access the admin dashboard at `/admin` with your configured credentials:

- **Dashboard**: View site statistics and recent posts
- **Backup Data**: Download a zip file with all your data
- **Editor**: Create and edit posts and pages
- **Media**: Manage images and media files
- **Settings**: Configure site-wide settings

## 📝 Writing Content

QuillKit uses markdown files with YAML front matter for all posts and pages. Front matter defines metadata about your content.

You can write content in two ways:

- **Web Editor**: Use the admin dashboard to create and edit posts directly in your browser
- **Local Editor**: Write in your favorite markdown editor (Obsidian for me) and upload `.md` files through the admin panel

### Front Matter Reference

Every post or page starts with a YAML front matter block:

```yaml
---
title: "Your Post Title"
author: "Your Name"
type: "Post"
date: 2025-10-20T14:30:00.0000000
slug: "your-post-slug"
status: "Published"
categories:
  - "Category One"
  - "Category Two"
tags:
  - "tag1"
  - "tag2"
description: "Brief description for SEO and previews"
excerpt: "Custom excerpt shown on the homepage"
image: "/media/featured-image.jpg"
link: "https://example.com"
---

Your markdown content goes here...
```

### Field Guide

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| `title` | ✅ Yes | String | The title of your post or page |
| `author` | ❌ No | String | Author name |
| `type` | ❌ No | String | `Post` or `Page` (default: `Post`) |
| `date` | ✅ Yes | DateTime | Publication date (`YYYY-MM-DDTHH:MM:SS.0000000`) |
| `slug` | ✅ Yes | String | URL-friendly identifier (e.g., `my-awesome-post`) |
| `status` | ❌ No | String | `Published` or `Draft` (default: `Draft`) |
| `categories` | ❌ No | List | Content categories (YAML list format) |
| `tags` | ❌ No | List | Content tags (YAML list format) |
| `description` | ❌ No | String | Meta description for SEO |
| `excerpt` | ❌ No | String | Custom excerpt (overrides auto-generated) |
| `image` | ❌ No | String | Featured image URL (absolute or relative path) |
| `link` | ❌ No | String | External link URL (for link posts) |

### Examples

**Minimal Post:**
```yaml
---
title: "Quick Thought"
author: "Al Nyveldt"
slug: "quick-thought"
status: "Published"
---

Just a simple post with the essentials.
```

**Full-Featured Post:**
```yaml
---
title: "Getting Started with QuillKit"
author: "Al Nyveldt"
type: "Post"
date: 2025-10-20T09:00:00.0000000
slug: "getting-started-quillkit"
status: "Published"
categories:
  - "Blogging"
  - "Tutorials"
tags:
  - "quillkit"
  - "markdown"
  - "getting-started"
description: "Learn how to set up and use QuillKit for your blog"
excerpt: "A comprehensive guide to getting started with QuillKit"
image: "/media/quillkit-intro.jpg"
---

Your detailed markdown content...
```

### Tips

- **Field Order**: Doesn't matter—organize however you prefer
- **Optional Fields**: Can be omitted entirely if not needed
- **Empty Fields**: Can be left blank (`image: `)
- **Relative Paths**: Use `/media/image.jpg` for files in your Content/media folder
- **Auto-Excerpt**: If you don't provide an excerpt, QuillKit will generate one from your content



QuillKit features a powerful and flexible theming system that lets you customize the look and feel of your site without touching the core code.

### How Theming Works

The theming system uses a **cascading view override approach**:

1. **Check `Content/Theme/Views`** for custom views (highest priority)
2. **Fall back to default views** in `/Views` (default implementation)
3. **Include theme assets** from `Content/Theme/assets`

This means you can override just the files you want to customize, and everything else will use sensible defaults.

### Theme Structure

```
Content/Theme/
├── Views/                     # Override any view
│   ├── Shared/
│   │   ├── _Layout.cshtml     # Custom site layout
│   │   └── _Sidebar.cshtml    # Custom sidebar
│   └── Site/
│       ├── Index.cshtml       # Homepage
│       ├── Post.cshtml        # Post detail
│       └── Page.cshtml        # Page detail
└── assets/                    # Theme-specific assets
    ├── css/
    │   └── theme.css          # Custom stylesheets
    └── js/
        └── theme.js           # Custom scripts
```

### Getting Started with Themes

1. **Create your theme folder** in `Content/Theme`
2. **Copy default views** from `/Views` that you want to customize
3. **Modify the HTML/CSS** to match your design
4. **Add custom assets** (CSS, JavaScript, images) to `assets/`
5. **Reference your assets** in views with paths like `/theme-assets/css/theme.css`

The default Bootstrap 5.3 styling will still apply to any views you don't override, giving you a solid foundation to build upon.

## ☁️ Azure Deployment

QuillKit is designed for easy deployment to Azure with full support for cloud storage.

### Quick Start

1. **Deploy your fork** as an Azure Web App
2. **Create an Azure Storage Account** for your posts and media files
3. **Configure Azure App Settings** to override local defaults:
   - `ContentProvider`: `AzureBlob`
   - `ConnectionStrings__AzureStorage`: Your storage connection string
   - `AdminAuth__Username` and `AdminAuth__PasswordHash`: Your admin credentials

### Configuration Priority

QuillKit respects this configuration hierarchy (highest to lowest):

1. **Azure App Settings** (environment variables)
2. **appsettings.Production.json** (if included in deployment)
3. **appsettings.json** (defaults)

This ensures your secrets stay safe in Azure and never touch version control.
