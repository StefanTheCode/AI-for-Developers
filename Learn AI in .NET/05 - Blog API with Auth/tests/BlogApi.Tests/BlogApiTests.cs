using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BlogApi.Tests;

/// <summary>
/// End-to-end tests for the auth + posts flow. Each test uses a unique username
/// so they stay independent even though they share the class's in-memory DB.
/// </summary>
public class BlogApiTests(BlogApiFactory factory) : IClassFixture<BlogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // --- helpers -------------------------------------------------------------

    private static string UniqueUser() => "user_" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>Registers a user and returns a JWT for them.</summary>
    private async Task<string> RegisterAndLoginAsync(string username, string password = "supersecret1")
    {
        var register = await _client.PostAsJsonAsync("/auth/register", new { username, password });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _client.PostAsJsonAsync("/auth/login", new { username, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await login.Content.ReadFromJsonAsync<TokenResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        return body.Token;
    }

    // --- tests ---------------------------------------------------------------

    [Fact]
    public async Task Register_then_login_returns_a_token()
    {
        var token = await RegisterAndLoginAsync(UniqueUser());
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_with_short_password_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/auth/register",
            new { username = UniqueUser(), password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var username = UniqueUser();
        await RegisterAndLoginAsync(username);

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { username, password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Listing_posts_is_public()
    {
        var response = await _client.GetAsync("/posts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Creating_a_post_without_a_token_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/posts",
            new { title = "No auth", content = "Should be rejected" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_a_post_with_a_token_then_reading_it_works()
    {
        var token = await RegisterAndLoginAsync(UniqueUser());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await _client.PostAsJsonAsync("/posts",
            new { title = "Hello", content = "World" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await create.Content.ReadFromJsonAsync<PostResponse>();
        created!.Id.Should().BeGreaterThan(0);

        var get = await _client.GetAsync($"/posts/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await get.Content.ReadFromJsonAsync<PostResponse>();
        fetched!.Title.Should().Be("Hello");
    }

    [Fact]
    public async Task A_user_cannot_edit_another_users_post()
    {
        // Author creates a post.
        var authorToken = await RegisterAndLoginAsync(UniqueUser());
        var authorClient = factory.CreateClient();
        authorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);

        var create = await authorClient.PostAsJsonAsync("/posts",
            new { title = "Mine", content = "Hands off" });
        var post = await create.Content.ReadFromJsonAsync<PostResponse>();

        // A different user tries to edit it -> 403 Forbidden.
        var otherToken = await RegisterAndLoginAsync(UniqueUser());
        var otherClient = factory.CreateClient();
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var edit = await otherClient.PutAsJsonAsync($"/posts/{post!.Id}",
            new { title = "Hijacked", content = "Not allowed" });

        edit.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // DTOs used only for deserializing responses in tests.
    private sealed record TokenResponse(string Token);
    private sealed record PostResponse(int Id, string Title, string Content);
}
