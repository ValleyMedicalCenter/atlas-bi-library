# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Atlas BI Library is a unified report library built with ASP.NET Core 8.0, Entity Framework, and modern frontend technologies. It aggregates metadata from multiple BI platforms and provides a searchable, unified interface for reports and documentation.

## Key Technologies

- **Backend**: ASP.NET Core 8.0, Entity Framework Core, C#
- **Frontend**: JavaScript ES6+, Gulp build system, SCSS
- **Database**: SQL Server with Entity Framework migrations  
- **Search**: Apache Solr
- **Testing**: xUnit, Coverlet
- **Authentication**: SAML2, Windows Authentication support

## Development Commands

### Initial Setup
```bash
# Install dependencies
npm install
npm run dotnet:restore

# Initialize database
npm run db:update

# Trust development certificate (first-time setup)
dotnet dev-certs https --trust
```

### Development Workflow
```bash
# Start development server (builds assets, starts IIS Express, opens browser)
npm start

# Development with file watching and parallel processes
npm run dev

# Watch for changes (builds assets on change)
npm run watch
```

### Building & Publishing
```bash
# Build frontend assets
gulp build
npm run build

# Build .NET project
npm run dotnet:build
dotnet build

# Publish production build
npm run dotnet:publish
```

### Testing
```bash
# Run development tests with coverage
npm run test:dev

# Run specific test categories
npm run test:integrationTests
npm run test:browserTests  
npm run test:functionTests

# Generate HTML coverage report (outputs to /coverage)
npm run test:report_html
```

### Code Quality
```bash
# Run linting
npm run lint           # Both JS and SCSS
npm run lint:js        # XO linter for JavaScript
npm run lint:scss      # StyleLint for SCSS

# Format code
npm run format         # Both static files and C#
npm run format:static  # Prettier for JS/CSS/JSON/MD/etc
npm run format:cs      # CSharpier for C# files

# Commit with conventional commits
npm run commit
```

### Database Operations
```bash
# Update database with latest migrations
npm run db:update

# Create new migration (use EF CLI directly)
dotnet ef migrations add MigrationName --project web/web.csproj
```

### Development Services
```bash
# Start SAML development IdP
npm run dev:saml

# Start Solr instance
npm run dev:solr

# Generate SAML certificates
npm run saml:cert
npm run saml:setup
```

## Architecture Overview

### Project Structure
- **`web/`** - Main ASP.NET Core application
  - `Controllers/` - MVC controllers
  - `Pages/` - Razor pages and components
  - `Models/` - Entity models and view models
  - `Services/` - Business logic services
  - `Authorization/` - Custom authorization handlers
  - `Middleware/` - Custom ASP.NET Core middleware
  - `Migrations/` - Entity Framework database migrations
  - `wwwroot/` - Static web assets (built by Gulp)
  - `solr/` - Apache Solr configuration and binaries

- **`web.Tests/`** - Unit and integration tests
- **`gulpfile.js/`** - Gulp build system (modular)
  - Handles SCSS compilation, JavaScript bundling, font processing
  - Builds optimized assets for production

### Key Configuration Files
- `appsettings.json` - Application configuration template
- `appsettings.cust.json` - Custom configuration (create manually)
- Connection strings, SMTP, Solr, feature flags configured here

### Frontend Build Process
The Gulp build system processes:
- **SCSS** → CSS with autoprefixing, minification
- **JavaScript** → Bundled and minified with Rollup
- **Fonts** → Font Awesome subset generation
- **Packages** → Third-party library processing

### Database Architecture
- Uses Entity Framework Core with SQL Server
- Code-first approach with migrations
- Complex relationships between Reports, Collections, Users, Groups
- Includes audit trails and soft deletion patterns

### Search Integration
- Apache Solr provides full-text search capabilities
- Two cores: `atlas` (main content) and `atlas_lookups` (metadata)
- Search indexing happens via background jobs (Hangfire)

## Development Guidelines

### Testing Approach
- Integration tests verify end-to-end workflows
- Browser tests use Selenium for UI testing  
- Function tests focus on business logic
- Coverage reports required - maintain high coverage
- All tests run in isolated demo mode (`Demo=True`)

### Code Style
- C# follows CSharpier formatting
- JavaScript uses XO linter with Prettier
- SCSS follows StandardSCSS configuration
- Pre-commit hooks enforce formatting

### Entity Framework Patterns
- Models follow domain-driven design principles
- Use explicit loading for complex relationships
- Migrations include both schema and data changes
- Soft delete patterns implemented via `IsDeleted` flags

### Frontend Architecture
- Progressive enhancement approach
- Vanilla JavaScript with modern ES6+ features
- Modular SCSS with Bulma CSS framework
- Chart.js for data visualization
- Responsive design with mobile-first approach

### Security Considerations
- SAML2 and Windows Authentication supported
- XSS protection with DOMPurify
- CSRF tokens on all forms
- SQL injection prevention through EF parameterization
- Content Security Policy implementation

## Configuration Requirements

### Required Custom Files
Create these files manually (not in source control):
- `web/appsettings.cust.json` - Production configuration
- `web/appsettings.cust.Development.json` - Development overrides

### Key Settings
- Database connection strings
- SMTP configuration for notifications  
- Solr endpoints for search
- SAML authentication settings
- Feature toggles for optional functionality

## Common Issues

### Database Connection
- Ensure SQL Server is running and connection string is correct
- Run `npm run db:update` after pulling schema changes
- Check user permissions for database operations

### Solr Search
- Solr must be running on configured ports (default 8983)
- Index population happens via ETL processes
- Clear Solr cache when making search schema changes

### Build Issues
- Clear `node_modules` and reinstall if npm packages are corrupted
- Rebuild .NET solution after pulling significant changes
- Check that all required global dotnet tools are installed

### Authentication
- SAML configuration requires proper certificate setup
- Windows Authentication needs proper IIS/domain configuration
- Development mode allows bypassing authentication for testing