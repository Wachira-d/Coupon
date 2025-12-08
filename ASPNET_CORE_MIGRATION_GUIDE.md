# ASP.NET Core Migration Guide
## Migrating Coupon System from ASP.NET Web Forms to ASP.NET Core 8.0

**Date:** 2025-12-08
**Current Framework:** ASP.NET Web Forms 4.7.2
**Target Framework:** ASP.NET Core 8.0
**Estimated Effort:** 4-8 weeks (depending on team size)

---

## Table of Contents
1. [Why Migrate?](#why-migrate)
2. [Pre-Migration Checklist](#pre-migration-checklist)
3. [Migration Strategy](#migration-strategy)
4. [Step-by-Step Migration Plan](#step-by-step-migration-plan)
5. [Architecture Changes](#architecture-changes)
6. [Code Migration Examples](#code-migration-examples)
7. [Testing Strategy](#testing-strategy)
8. [Deployment Plan](#deployment-plan)
9. [Post-Migration Tasks](#post-migration-tasks)

---

## Why Migrate?

### Critical Reasons
1. **.NET Framework 4.7.2 is End-of-Life**
   - No more security updates
   - No new features
   - Increasingly difficult to find developers

2. **ASP.NET Core Benefits**
   - Cross-platform (Windows, Linux, macOS)
   - Better performance (2-3x faster)
   - Modern architecture and tooling
   - Active development and long-term support
   - Built-in dependency injection
   - Better security features
   - Cloud-native support

3. **Business Impact**
   - Reduced hosting costs (can run on Linux)
   - Better scalability
   - Easier containerization (Docker)
   - Improved developer productivity

---

## Pre-Migration Checklist

### 1. Current System Audit
- [x] Document all pages and functionality
- [x] Identify external dependencies
- [x] List all third-party libraries
- [x] Document database schema
- [ ] Create comprehensive test suite
- [ ] Document API contracts (if any)

### 2. Team Preparation
- [ ] Train team on ASP.NET Core
- [ ] Set up development environments
- [ ] Establish coding standards for new architecture
- [ ] Choose UI framework (Razor Pages, MVC, Blazor)

### 3. Infrastructure
- [ ] Set up new repositories
- [ ] Configure CI/CD pipelines
- [ ] Provision testing environments
- [ ] Plan database migration strategy

---

## Migration Strategy

### Recommended Approach: Strangler Fig Pattern

Gradually migrate functionality while keeping the old system running:

```
Phase 1: New Shell (Weeks 1-2)
├── Create new ASP.NET Core project
├── Set up dependency injection
├── Configure database connections
└── Implement authentication

Phase 2: Core Services (Weeks 3-4)
├── Migrate helper classes
├── Migrate data access layer
├── Create domain models
└── Implement business logic

Phase 3: User-Facing Pages (Weeks 5-6)
├── Migrate Central page
├── Migrate Lotus page
├── Migrate Starbucks page
└── Migrate test page

Phase 4: Admin Portal (Week 7)
├── Migrate Report page
├── Implement enhanced authentication
└── Add new admin features

Phase 5: Testing & Deployment (Week 8)
├── Comprehensive testing
├── Performance testing
├── Security testing
└── Production deployment
```

---

## Step-by-Step Migration Plan

### Phase 1: Create New ASP.NET Core Project

#### 1.1 Create Project
```bash
dotnet new webapp -n CouponCore
cd CouponCore
```

#### 1.2 Project Structure
```
/CouponCore
├── Controllers/         # For MVC controllers
├── Pages/              # For Razor Pages
├── Models/             # Domain models
├── Services/           # Business logic
├── Data/               # Data access layer
├── Helpers/            # Utility classes
├── wwwroot/            # Static files
│   ├── css/
│   ├── js/
│   └── images/
├── appsettings.json    # Configuration
└── Program.cs          # Application entry point
```

#### 1.3 Install Required Packages
```bash
# Database
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# Authentication
dotnet add package Microsoft.AspNetCore.Authentication
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore

# Barcode
dotnet add package BarcodeLib.Standard

# Logging
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

#### 1.4 Configure Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Configure database
builder.Services.AddDbContext<CouponDbContext>(options =>
{
    var dbType = builder.Configuration["DatabaseType"];
    if (dbType == "PostgreSQL")
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer"));
    }
});

// Add dependency injection
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<ILogService, LogService>();

// Configure authentication
builder.Services.AddAuthentication()
    .AddLdap("LDAP", options =>
    {
        options.Server = builder.Configuration["LDAP:Server"];
    });

// Configure logging
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});

app.MapRazorPages();
app.MapControllers();

app.Run();
```

---

### Phase 2: Migrate Data Access Layer

#### 2.1 Create Entity Framework Core Models
```csharp
// Models/Coupon.cs
public class Coupon
{
    public string ID { get; set; }
    public string Code { get; set; }
    public int Value { get; set; } // 200 or 300
    public DateTime? Date_DataIn { get; set; }
    public DateTime? Date_Send { get; set; }
    public DateTime? Date_Use { get; set; }
    public DateTime Expire_DateTime { get; set; }
    public bool Used_Status { get; set; }
    public bool Send_Status { get; set; }
}

// Models/Log.cs
public class Log
{
    public int Id { get; set; }
    public DateTime Created_DateTime { get; set; }
    public string Machine_Name { get; set; }
    public string IP_Address { get; set; }
    public string Browser { get; set; }
    public string Access_URL { get; set; }
    public int Found_Coupon { get; set; }
}

// Models/AdminLog.cs
public class AdminLog
{
    public int Id { get; set; }
    public DateTime Created_DateTime { get; set; }
    public string Machine_Name { get; set; }
    public string IP_Address { get; set; }
    public string Browser { get; set; }
    public string Access_URL { get; set; }
    public string Password { get; set; }
    public bool CanAccess { get; set; }
}
```

#### 2.2 Create DbContext
```csharp
// Data/CouponDbContext.cs
using Microsoft.EntityFrameworkCore;

public class CouponDbContext : DbContext
{
    public CouponDbContext(DbContextOptions<CouponDbContext> options)
        : base(options)
    {
    }

    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<AdminLog> AdminLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("Coupon");
            entity.HasKey(e => e.ID);
            entity.Property(e => e.ID).HasMaxLength(50);
            entity.Property(e => e.Code).HasMaxLength(100);
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.ToTable("Logs");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<AdminLog>(entity =>
        {
            entity.ToTable("Admin_Logs");
            entity.HasKey(e => e.Id);
        });
    }
}
```

#### 2.3 Create Repository Pattern
```csharp
// Services/ICouponService.cs
public interface ICouponService
{
    Task<Coupon> GetCouponByIdAsync(string id);
    Task<bool> RedeemCouponAsync(string id);
    Task<List<Coupon>> SearchCouponsAsync(CouponSearchCriteria criteria);
    Task<CouponStatistics> GetStatisticsAsync();
}

// Services/CouponService.cs
public class CouponService : ICouponService
{
    private readonly CouponDbContext _context;
    private readonly ILogger<CouponService> _logger;

    public CouponService(CouponDbContext context, ILogger<CouponService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Coupon> GetCouponByIdAsync(string id)
    {
        return await _context.Coupons
            .FirstOrDefaultAsync(c => c.ID == id);
    }

    public async Task<bool> RedeemCouponAsync(string id)
    {
        var coupon = await GetCouponByIdAsync(id);
        if (coupon == null || coupon.Used_Status)
        {
            return false;
        }

        coupon.Date_Use = DateTime.Now;
        coupon.Used_Status = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Coupon {CouponId} redeemed successfully", id);
        return true;
    }

    public async Task<List<Coupon>> SearchCouponsAsync(CouponSearchCriteria criteria)
    {
        var query = _context.Coupons.AsQueryable();

        if (!string.IsNullOrEmpty(criteria.Id))
        {
            query = query.Where(c => c.ID.Contains(criteria.Id));
        }

        if (!string.IsNullOrEmpty(criteria.Code))
        {
            query = query.Where(c => c.Code.Contains(criteria.Code));
        }

        if (criteria.DateFrom.HasValue)
        {
            query = query.Where(c => c.Date_DataIn >= criteria.DateFrom);
        }

        // Add more filters as needed

        return await query.ToListAsync();
    }

    public async Task<CouponStatistics> GetStatisticsAsync()
    {
        var total = await _context.Coupons.CountAsync();
        var used = await _context.Coupons.CountAsync(c => c.Used_Status);
        var sent = await _context.Coupons.CountAsync(c => c.Send_Status);

        return new CouponStatistics
        {
            Total = total,
            Used = used,
            Sent = sent
        };
    }
}
```

---

### Phase 3: Migrate Pages

#### 3.1 Central Page (Razor Pages)
```csharp
// Pages/Central.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class CentralModel : PageModel
{
    private readonly ICouponService _couponService;
    private readonly ILogService _logService;
    private readonly ILogger<CentralModel> _logger;

    public CentralModel(
        ICouponService couponService,
        ILogService logService,
        ILogger<CentralModel> logger)
    {
        _couponService = couponService;
        _logService = logService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string Id { get; set; }

    public Coupon Coupon { get; set; }
    public string BarcodeUrl { get; set; }
    public bool IsRedeemed { get; set; }
    public string ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Validate input
        if (string.IsNullOrEmpty(Id) || !IsValidCouponId(Id))
        {
            ErrorMessage = "รหัสคูปองไม่ถูกต้อง";
            return Page();
        }

        // Get coupon
        Coupon = await _couponService.GetCouponByIdAsync(Id);

        // Log access
        await _logService.LogAccessAsync(new LogEntry
        {
            Created_DateTime = DateTime.Now,
            IP_Address = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Browser = HttpContext.Request.Headers["User-Agent"],
            Access_URL = HttpContext.Request.Path,
            Found_Coupon = Coupon != null ? 1 : 0
        });

        // Validate coupon
        if (Coupon == null)
        {
            ErrorMessage = "รหัสคูปองไม่ถูกต้อง";
            return Page();
        }

        if (DateTime.Now > Coupon.Expire_DateTime)
        {
            ErrorMessage = "คูปองหมดอายุแล้ว";
            return Page();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRedeemAsync()
    {
        // Validate CSRF token (built-in in Razor Pages)
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var success = await _couponService.RedeemCouponAsync(Id);

        if (success)
        {
            IsRedeemed = true;
            BarcodeUrl = await GenerateBarcodeAsync(Id);
            _logger.LogInformation("Coupon {CouponId} redeemed", Id);
        }
        else
        {
            ErrorMessage = "เกิดข้อผิดพลาดในการใช้คูปอง";
        }

        return Page();
    }

    private bool IsValidCouponId(string id)
    {
        // Add validation logic
        return !string.IsNullOrEmpty(id) &&
               id.Length == 4 &&
               id.All(char.IsLetterOrDigit);
    }

    private async Task<string> GenerateBarcodeAsync(string id)
    {
        // Barcode generation logic
        // Return URL to generated barcode image
        return $"/barcodes/{id}.jpg";
    }
}
```

#### 3.2 Central Page (Razor View)
```html
<!-- Pages/Central.cshtml -->
@page
@model CentralModel
@{
    ViewData["Title"] = "Central Coupon";
}

<div class="container">
    @if (!string.IsNullOrEmpty(Model.ErrorMessage))
    {
        <div class="alert alert-danger">
            @Model.ErrorMessage
        </div>
    }

    @if (Model.Coupon != null && !Model.IsRedeemed)
    {
        <div class="coupon-display">
            <img src="/images/Central/@(Model.Coupon.Value).png" alt="Coupon" />
            <p>มูลค่า: @Model.Coupon.Value บาท</p>

            <form method="post" asp-page-handler="Redeem">
                <input type="hidden" asp-for="Id" />
                <button type="submit" class="btn btn-primary">
                    กดใช้สิทธิ์
                </button>
            </form>
        </div>
    }

    @if (Model.IsRedeemed)
    {
        <div class="barcode-display">
            <h3>คูปองของคุณ</h3>
            <img src="@Model.BarcodeUrl" alt="Barcode" />
            <p>กรุณานำบาร์โค้ดนี้ไปแสดงที่แคชเชียร์</p>
        </div>
    }
</div>
```

---

### Phase 4: Migrate Admin Portal

#### 4.1 Admin Authentication
```csharp
// Pages/Admin/Login.cshtml.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

public class LoginModel : PageModel
{
    private readonly ILdapAuthService _ldapAuth;
    private readonly IConfiguration _config;
    private readonly ILogger<LoginModel> _logger;

    [BindProperty]
    public string Email { get; set; }

    [BindProperty]
    public string Password { get; set; }

    public string ErrorMessage { get; set; }

    public LoginModel(
        ILdapAuthService ldapAuth,
        IConfiguration config,
        ILogger<LoginModel> logger)
    {
        _ldapAuth = ldapAuth;
        _config = config;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validate input
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Email and password required";
            return Page();
        }

        // Check if email is authorized
        var authorizedEmails = _config["ReportPassword"]?.Split(';') ?? Array.Empty<string>();
        if (!authorizedEmails.Any(e => e.Trim().Equals(Email, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "Unauthorized email";
            _logger.LogWarning("Unauthorized login attempt: {Email}", Email);
            return Page();
        }

        // Validate LDAP credentials
        var isValid = await _ldapAuth.ValidateCredentialsAsync(Email, Password);

        if (isValid)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Email),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "Admin");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(principal);

            _logger.LogInformation("Admin login successful: {Email}", Email);
            return RedirectToPage("/Admin/Report");
        }

        ErrorMessage = "Invalid password";
        _logger.LogWarning("Failed login attempt: {Email}", Email);
        return Page();
    }
}
```

---

### Phase 5: Configuration

#### 5.1 appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "DatabaseType": "PostgreSQL",

  "ConnectionStrings": {
    "SQLServer": "Server=192.168.80.31;Database=Coupon_Honda;User Id=admin;Password=***;",
    "PostgreSQL": "Host=192.168.80.31;Port=5432;Database=Coupon_Honda;Username=postgres;Password=***;"
  },

  "LDAP": {
    "Server": "10.137.157.210"
  },

  "Coupon": {
    "NumberOfDigits": 4,
    "ExpirationYears": 5
  },

  "Security": {
    "RateLimit": {
      "MaxAttempts": 10,
      "WindowMinutes": 60
    }
  },

  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

---

## Architecture Changes

### Before (Web Forms)
```
┌─────────────┐
│  .aspx Page │ ← UI + Logic mixed
├─────────────┤
│   Code-Behind│ ← Direct database calls
├─────────────┤
│   Database  │
└─────────────┘
```

### After (ASP.NET Core)
```
┌─────────────┐
│ Razor Page  │ ← UI only
├─────────────┤
│  Page Model │ ← Presentation logic
├─────────────┤
│   Services  │ ← Business logic
├─────────────┤
│ Repositories│ ← Data access
├─────────────┤
│ DbContext   │ ← ORM layer
├─────────────┤
│   Database  │
└─────────────┘
```

---

## Testing Strategy

### 1. Unit Tests
```csharp
// Tests/Services/CouponServiceTests.cs
using Xunit;
using Moq;

public class CouponServiceTests
{
    [Fact]
    public async Task GetCouponById_ValidId_ReturnsCoupon()
    {
        // Arrange
        var mockContext = new Mock<CouponDbContext>();
        var service = new CouponService(mockContext.Object, Mock.Of<ILogger<CouponService>>());

        // Act
        var result = await service.GetCouponByIdAsync("1234");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1234", result.ID);
    }

    [Fact]
    public async Task RedeemCoupon_AlreadyUsed_ReturnsFalse()
    {
        // Test implementation
    }
}
```

### 2. Integration Tests
```csharp
// Tests/IntegrationTests/CentralPageTests.cs
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class CentralPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CentralPageTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Central_Get_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/Central?id=1234");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Central", content);
    }
}
```

---

## Deployment Plan

### 1. Blue-Green Deployment Strategy
```
1. Deploy new ASP.NET Core app to "green" environment
2. Test thoroughly on green environment
3. Switch traffic from "blue" (old) to "green" (new)
4. Monitor for issues
5. Keep blue environment running for 24-48 hours
6. If issues arise, switch back to blue
7. After successful verification, decommission blue
```

### 2. Docker Deployment
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CouponCore/CouponCore.csproj", "CouponCore/"]
RUN dotnet restore "CouponCore/CouponCore.csproj"
COPY . .
WORKDIR "/src/CouponCore"
RUN dotnet build "CouponCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CouponCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CouponCore.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'
services:
  coupon-app:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DatabaseType=PostgreSQL
    volumes:
      - ./logs:/app/Logs
    depends_on:
      - postgres

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: Coupon_Honda
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ***
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

---

## Post-Migration Tasks

### 1. Monitoring
- [ ] Set up Application Insights or similar APM
- [ ] Configure alerts for errors and performance issues
- [ ] Set up log aggregation (ELK stack or similar)
- [ ] Monitor resource usage (CPU, memory, disk)

### 2. Documentation
- [ ] Update architecture documentation
- [ ] Create API documentation (if applicable)
- [ ] Update deployment procedures
- [ ] Create troubleshooting guides

### 3. Performance Optimization
- [ ] Implement response caching
- [ ] Add database indexes
- [ ] Optimize database queries
- [ ] Implement CDN for static assets

### 4. Security Enhancements
- [ ] Implement rate limiting at application level
- [ ] Add CAPTCHA for admin login
- [ ] Set up Web Application Firewall (WAF)
- [ ] Conduct penetration testing

---

## Timeline Summary

| Phase | Duration | Key Activities |
|-------|----------|----------------|
| **Phase 1** | Week 1-2 | Project setup, infrastructure |
| **Phase 2** | Week 3-4 | Data layer migration |
| **Phase 3** | Week 5-6 | User pages migration |
| **Phase 4** | Week 7 | Admin portal migration |
| **Phase 5** | Week 8 | Testing & deployment |

**Total Estimated Time:** 8 weeks
**Buffer Time:** +2 weeks for unforeseen issues

---

## Success Criteria

- [ ] All functionality migrated successfully
- [ ] All tests passing (unit, integration, E2E)
- [ ] Performance meets or exceeds current system
- [ ] Zero data loss during migration
- [ ] Security audit completed and passed
- [ ] Documentation complete
- [ ] Team trained on new system
- [ ] Stakeholders satisfied

---

## Resources

### Learning Materials
- [Microsoft Learn: Migrate to ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/migration/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)

### Tools
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Postman](https://www.postman.com/) - For API testing

---

**Document Version:** 1.0
**Last Updated:** 2025-12-08
**Contact:** Development Team
