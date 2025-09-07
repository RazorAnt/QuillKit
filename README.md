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

## ✏️ Roadmap
- Complete Azure Blog Storage work
- Admin pages
- Put together better readme and documentation

## 📁 Project Structure

```
QuillKit/
├── Content/                    # Your content and configuration
│   ├── config/
│   │   └── site-config.yml    # Site configuration
│   ├── *.md                   # Your posts and pages
│   └── media/                 # Images and media files
├── Controllers/               # MVC controllers
├── Models/                    # Data models
├── Services/                  # Business logic services
├── Views/                     # Razor view templates
├── wwwroot/                   # Static web assets
└── Extensions/                # Custom extensions and helpers
```

## 🎨 Theming

QuillKit features a flexible theming system:

- **Dynamic Configuration**: Theme properties are stored in `site-config.yml`
- **View Location Expansion**: Themes can override any view
- **Bootstrap Integration**: Built on Bootstrap 5.3 with custom styling

### Theme Structure
```
Content/themes/your-theme/
├── Views/                     # Override any view
│   ├── Shared/
│   │   └── _Layout.cshtml
│   └── Site/
└── assets/                    # Theme-specific assets
    ├── css/
    └── js/
```


