using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        var ratesService = new BankRatesService(cacheService, NullLogger<BankRatesService>.Instance);

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
}
