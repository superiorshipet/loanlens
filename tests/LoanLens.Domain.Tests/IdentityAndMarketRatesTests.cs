using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using LoanLens.Application.Common;
using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using LoanLens.Domain.Enums;
using LoanLens.Infrastructure.Configuration;
using LoanLens.Infrastructure.Entities;
using LoanLens.Infrastructure.Persistence;
using LoanLens.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LoanLens.Domain.Tests;

public class IdentityAndMarketRatesTests
{
    private readonly IOptions<JwtOptions> _jwtOptions;

    public IdentityAndMarketRatesTests()
    {
        _jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "LoanLens.Api",
            Audience = "LoanLens.Client",
            Secret = "SuperSecureSecretKeyForTestingPurposesMustBeAtLeast32BytesLong!",
            ExpiryHours = 24
        });
    }

    [Fact]
    public void JwtTokenGenerator_ShouldGenerateValidToken_WithRoleClaims()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_jwtOptions);
        var userId = Guid.NewGuid();
        var email = "admin@loanlens.com";
        var role = "Admin";
        var fullName = "Admin User";

        // Act
        var token = generator.GenerateToken(userId, email, role, fullName);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("LoanLens.Api", jwt.Issuer);
        Assert.Contains(jwt.Claims, c => (c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid") && c.Value == userId.ToString());
        Assert.Contains(jwt.Claims, c => (c.Type == ClaimTypes.Email || c.Type == "email") && c.Value == email);
        Assert.Contains(jwt.Claims, c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin");
    }

    [Fact]
    public async Task AuthService_RegisterAndLogin_ShouldSucceed()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var authService = new AuthService(dbContext, jwtGen);

        var registerDto = new RegisterRequestDto("test@loanlens.com", "Password@123", "Test User");

        // Act - Register
        var registerResult = await authService.RegisterAsync(registerDto);

        // Assert Register
        Assert.NotNull(registerResult);
        Assert.Equal("test@loanlens.com", registerResult.Email);
        Assert.Equal("Admin", registerResult.Role); // First user is Admin
        Assert.NotEmpty(registerResult.Token);

        // Act - Register second user
        var secondUser = await authService.RegisterAsync(new RegisterRequestDto("user2@loanlens.com", "Password@123", "User Two"));
        Assert.Equal("User", secondUser.Role);

        // Act - Login
        var loginResult = await authService.LoginAsync(new LoginRequestDto("test@loanlens.com", "Password@123"));

        // Assert Login
        Assert.NotNull(loginResult);
        Assert.Equal("test@loanlens.com", loginResult.Email);
        Assert.Equal("Test User", loginResult.FullName);
        Assert.NotEmpty(loginResult.Token);
    }

    [Fact]
    public async Task AuthService_DuplicateEmail_ShouldThrowException()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var authService = new AuthService(dbContext, jwtGen);

        var registerDto = new RegisterRequestDto("duplicate@loanlens.com", "Password@123", "First User");
        await authService.RegisterAsync(registerDto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authService.RegisterAsync(new RegisterRequestDto("duplicate@loanlens.com", "Password@456", "Second User")));
    }

    [Fact]
    public async Task AuthService_WrongPassword_ShouldThrowUnauthorized()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var authService = new AuthService(dbContext, jwtGen);

        await authService.RegisterAsync(new RegisterRequestDto("authfail@loanlens.com", "CorrectPassword123", "User"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            authService.LoginAsync(new LoginRequestDto("authfail@loanlens.com", "WrongPassword")));
    }

    [Fact]
    public async Task BankRatesService_ShouldReturnCbeCorridorAndCommercialBankRates()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new RedisCacheService(memoryCache, NullLogger<RedisCacheService>.Instance, null);
        var scraperService = new BankRatesScraperService(new HttpClient(), cacheService, Options.Create(new ScraperOptions()), NullLogger<BankRatesScraperService>.Instance);
        var ratesService = new BankRatesService(cacheService, scraperService, NullLogger<BankRatesService>.Instance);

        // Act
        var result = await ratesService.GetMarketRatesAsync("en");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(28.25m, result.CbeLendingRate);
        Assert.Equal(27.25m, result.CbeDepositRate);
        Assert.Equal(27.75m, result.CbeMainOperationRate);
        Assert.True(result.AveragePersonalLoanRate > 25m);
        Assert.NotEmpty(result.Rates);

        // Ensure key banks are present
        Assert.Contains(result.Rates, r => r.BankName.Contains("National Bank of Egypt"));
        Assert.Contains(result.Rates, r => r.BankName.Contains("Banque Misr"));
        Assert.Contains(result.Rates, r => r.BankName.Contains("Commercial International Bank"));

        // Subsequent call should hit cache seamlessly
        var cachedResult = await ratesService.GetMarketRatesAsync("en");
        Assert.NotNull(cachedResult);
        Assert.Equal(result.AsOfDate, cachedResult.AsOfDate);
    }

    [Fact]
    public async Task BankRatesScraperService_ShouldScrapeAndPopulateMarketData()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new RedisCacheService(memoryCache, NullLogger<RedisCacheService>.Instance, null);
        var scraperService = new BankRatesScraperService(new HttpClient(), cacheService, Options.Create(new ScraperOptions()), NullLogger<BankRatesScraperService>.Instance);

        // Act
        var scrapeResult = await scraperService.ScrapeLatestRatesAsync();

        // Assert
        Assert.NotNull(scrapeResult);
        Assert.True(scrapeResult.Success);
        Assert.True(scrapeResult.RatesUpdatedCount > 0);
        Assert.Equal(28.25m, scrapeResult.CbeLendingRate);
        Assert.Equal(27.25m, scrapeResult.CbeDepositRate);
        Assert.NotEmpty(scrapeResult.ExtractedRates);
        Assert.NotEmpty(scrapeResult.SourcesChecked);
        Assert.Contains("AngleSharp", scrapeResult.ScraperEngine);
    }

    [Fact]
    public async Task RedisCacheService_WithMemoryFallback_ShouldStoreAndRetrieveData()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new RedisCacheService(memoryCache, NullLogger<RedisCacheService>.Instance, null);

        var key = "test-key";
        var value = "sample-value";

        // Act
        await cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var retrieved = await cacheService.GetAsync<string>(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value, retrieved);

        // Remove
        await cacheService.RemoveAsync(key);
        var afterRemoval = await cacheService.GetAsync<string>(key);
        Assert.Null(afterRemoval);
    }

    [Fact]
    public async Task AuthService_UpdateUser_ShouldModifyFieldsAndAllowRolePromotion()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var authService = new AuthService(dbContext, jwtGen);

        var firstUser = await authService.RegisterAsync(new RegisterRequestDto("admin@loanlens.com", "Password@123", "First Admin"));
        var secondUser = await authService.RegisterAsync(new RegisterRequestDto("user@loanlens.com", "Password@123", "Normal User"));
        Assert.Equal("User", secondUser.Role);

        // Act - Promote second user to Admin and update name
        var updateRequest = new UpdateUserRequestDto("Promoted Admin", "newemail@loanlens.com", "Admin", false, "NewPassword@123");
        var updated = await authService.UpdateUserAsync(secondUser.UserId, updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Promoted Admin", updated.FullName);
        Assert.Equal("newemail@loanlens.com", updated.Email);
        Assert.Equal("Admin", updated.Role);
        Assert.False(updated.IsActive);

        // Act - Verify login with updated credentials
        // Should fail because IsActive is false
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            authService.LoginAsync(new LoginRequestDto("newemail@loanlens.com", "NewPassword@123")));
    }

    [Fact]
    public async Task AuthService_GetAdminDashboard_ShouldReturnAggregatedMetrics()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var authService = new AuthService(dbContext, jwtGen);

        await authService.RegisterAsync(new RegisterRequestDto("admin@loanlens.com", "Password@123", "Admin"));
        await authService.RegisterAsync(new RegisterRequestDto("borrower@loanlens.com", "Password@123", "Borrower"));

        // Act
        var dashboard = await authService.GetAdminDashboardAsync();

        // Assert
        Assert.NotNull(dashboard);
        Assert.Equal(2, dashboard.UserStats.TotalUsers);
        Assert.Equal(2, dashboard.UserStats.ActiveUsers);
        Assert.Equal(1, dashboard.UserStats.AdminUsers);
        Assert.Equal(1, dashboard.UserStats.StandardUsers);
        Assert.Equal(2, dashboard.RecentUsers.Count);
        Assert.Equal(28.25m, dashboard.MarketStats.CbeLendingRate);
    }

    [Fact]
    public async Task AuthService_RequestPasswordReset_ShouldSendEmailAndStoreToken()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new RedisCacheService(memoryCache, NullLogger<RedisCacheService>.Instance);
        var testEmailService = new TestEmailService();
        var authService = new AuthService(dbContext, jwtGen, null, cacheService, testEmailService);

        await authService.RegisterAsync(new RegisterRequestDto("resetuser@loanlens.com", "OldPassword@123", "Reset User"));

        // Act
        var result = await authService.RequestPasswordResetAsync(new ForgotPasswordRequestDto("resetuser@loanlens.com"));

        // Assert
        Assert.True(result.Success);
        Assert.Single(testEmailService.SentResetEmails);
        var sentEmail = testEmailService.SentResetEmails[0];
        Assert.Equal("resetuser@loanlens.com", sentEmail.To);
        Assert.NotEmpty(sentEmail.Token);

        // Verify token in cache
        var cachedEmail = await cacheService.GetAsync<string>($"auth:reset-token:{sentEmail.Token}");
        Assert.Equal("resetuser@loanlens.com", cachedEmail);
    }

    [Fact]
    public async Task AuthService_ResetPassword_WithValidToken_ShouldSucceedAndAllowNewLogin()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new RedisCacheService(memoryCache, NullLogger<RedisCacheService>.Instance);
        var testEmailService = new TestEmailService();
        var authService = new AuthService(dbContext, jwtGen, null, cacheService, testEmailService);

        await authService.RegisterAsync(new RegisterRequestDto("user@loanlens.com", "OldPassword@123", "User Name"));

        // Request reset
        await authService.RequestPasswordResetAsync(new ForgotPasswordRequestDto("user@loanlens.com"));
        var token = testEmailService.SentResetEmails[0].Token;

        // Act - Reset password
        var resetResult = await authService.ResetPasswordAsync(new ResetPasswordRequestDto("user@loanlens.com", token, "BrandNewPassword@456"));

        // Assert
        Assert.True(resetResult.Success);
        Assert.Single(testEmailService.SentSuccessEmails);

        // Token should be invalidated from cache
        var cachedEmailAfter = await cacheService.GetAsync<string>($"auth:reset-token:{token}");
        Assert.Null(cachedEmailAfter);

        // Login with old password should fail
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            authService.LoginAsync(new LoginRequestDto("user@loanlens.com", "OldPassword@123")));

        // Login with new password should succeed
        var loginResult = await authService.LoginAsync(new LoginRequestDto("user@loanlens.com", "BrandNewPassword@456"));
        Assert.NotNull(loginResult);
        Assert.Equal("user@loanlens.com", loginResult.Email);
    }

    [Fact]
    public async Task AuthService_ResetPassword_WithInvalidToken_ShouldFail()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new RedisCacheService(memoryCache, NullLogger<RedisCacheService>.Instance);
        var testEmailService = new TestEmailService();
        var authService = new AuthService(dbContext, jwtGen, null, cacheService, testEmailService);

        await authService.RegisterAsync(new RegisterRequestDto("user@loanlens.com", "Password@123", "User"));

        // Act - Try reset with bogus token
        var resetResult = await authService.ResetPasswordAsync(new ResetPasswordRequestDto("user@loanlens.com", "invalid-token", "NewPassword@123"));

        // Assert
        Assert.False(resetResult.Success);
        Assert.Empty(testEmailService.SentSuccessEmails);
    }

    [Fact]
    public async Task AuthService_ResetPassword_WithShortPassword_ShouldFail()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new RedisCacheService(memoryCache, NullLogger<RedisCacheService>.Instance);
        var testEmailService = new TestEmailService();
        var authService = new AuthService(dbContext, jwtGen, null, cacheService, testEmailService);

        await authService.RegisterAsync(new RegisterRequestDto("user@loanlens.com", "Password@123", "User"));

        // Act - Try reset with password shorter than 6 characters
        var resetResult = await authService.ResetPasswordAsync(new ResetPasswordRequestDto("user@loanlens.com", "any-token", "123"));

        // Assert
        Assert.False(resetResult.Success);
    }

    [Fact]
    public async Task AuthService_RegisterWithBorrowerProfile_ShouldPersistAllFinancialDetails()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var authService = new AuthService(dbContext, jwtGen);

        var request = new RegisterRequestDto(
            Email: "borrower.profile@loanlens.com",
            Password: "Password@123",
            FullName: "Profile User",
            Age: 32,
            Stability: IncomeStability.Freelance,
            MonthlyIncome: 25000m,
            FixedMonthlyExpenses: 7000m,
            CurrentMonthlyInstallments: 1500m,
            Savings: 60000m,
            DependentsCount: 2,
            HasHealthInsurance: true,
            SecondaryIncome: 3000m
        );

        // Act - Register
        var authResult = await authService.RegisterAsync(request);

        // Assert - Profile retrieval
        var profile = await authService.GetCurrentUserProfileAsync(authResult.UserId);
        Assert.NotNull(profile);
        Assert.Equal(32, profile.Age);
        Assert.Equal(IncomeStability.Freelance, profile.Stability);
        Assert.Equal(25000m, profile.MonthlyIncome);
        Assert.Equal(7000m, profile.FixedMonthlyExpenses);
        Assert.Equal(1500m, profile.CurrentMonthlyInstallments);
        Assert.Equal(60000m, profile.Savings);
        Assert.Equal(2, profile.DependentsCount);
        Assert.True(profile.HasHealthInsurance);
        Assert.Equal(3000m, profile.SecondaryIncome);
    }

    [Fact]
    public async Task AuthService_RegisterWithInvalidAgeOrNegativeIncome_ShouldThrowException()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LoanLensDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var jwtGen = new JwtTokenGenerator(_jwtOptions);
        var authService = new AuthService(dbContext, jwtGen);

        // Act & Assert - Age too young
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authService.RegisterAsync(new RegisterRequestDto("young@loanlens.com", "Password@123", "Young User", Age: 16)));

        // Act & Assert - Age too old
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authService.RegisterAsync(new RegisterRequestDto("old@loanlens.com", "Password@123", "Old User", Age: 95)));

        // Act & Assert - Negative income
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authService.RegisterAsync(new RegisterRequestDto("neg@loanlens.com", "Password@123", "Neg User", MonthlyIncome: -500m)));
    }

    [Fact]
    public void FlexibleEnumConverter_ShouldParseStringsIntegersAndQuotedNumbers()
    {
        // Arrange
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new FlexibleEnumConverterFactory());

        // Act & Assert - Strings by name (case-insensitive)
        var s1 = JsonSerializer.Deserialize<IncomeStability>("\"Freelance\"", jsonOptions);
        var s2 = JsonSerializer.Deserialize<IncomeStability>("\"stableemployed\"", jsonOptions);
        var s3 = JsonSerializer.Deserialize<IncomeStability>("\"BUSINESSOWNER\"", jsonOptions);

        Assert.Equal(IncomeStability.Freelance, s1);
        Assert.Equal(IncomeStability.StableEmployed, s2);
        Assert.Equal(IncomeStability.BusinessOwner, s3);

        // Act & Assert - Numeric strings & raw numbers
        var n1 = JsonSerializer.Deserialize<IncomeStability>("2", jsonOptions);
        var n2 = JsonSerializer.Deserialize<IncomeStability>("\"2\"", jsonOptions);

        Assert.Equal(IncomeStability.Freelance, n1);
        Assert.Equal(IncomeStability.Freelance, n2);

        // Act & Assert - CalculationMethod & RateType
        var calc = JsonSerializer.Deserialize<InterestCalculationMethod>("\"Reducing\"", jsonOptions);
        var rate = JsonSerializer.Deserialize<InterestRateType>("\"Variable\"", jsonOptions);

        Assert.Equal(InterestCalculationMethod.Reducing, calc);
        Assert.Equal(InterestRateType.Variable, rate);

        // Act & Assert - Serialization outputs clean word
        var serialized = JsonSerializer.Serialize(IncomeStability.Freelance, jsonOptions);
        Assert.Equal("\"Freelance\"", serialized);
    }

    private class TestEmailService : IEmailService
    {
        public List<(string To, string Subject, string Token)> SentResetEmails { get; } = new();
        public List<(string To, string Subject)> SentSuccessEmails { get; } = new();

        public Task SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetToken, string language = "en", CancellationToken ct = default)
        {
            SentResetEmails.Add((toEmail, language == "ar" ? "إعادة تعيين كلمة المرور" : "Reset Your Password", resetToken));
            return Task.CompletedTask;
        }

        public Task SendPasswordResetSuccessEmailAsync(string toEmail, string recipientName, string language = "en", CancellationToken ct = default)
        {
            SentSuccessEmails.Add((toEmail, language == "ar" ? "تم تحديث كلمة المرور بنجاح" : "Your Password Was Successfully Reset"));
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
