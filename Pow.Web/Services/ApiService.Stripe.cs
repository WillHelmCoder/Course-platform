using System.Net.Http.Json;
using Pow.Domain.DTOs;

namespace Pow.Web.Services;

// Stripe API Methods - Extension for ApiService
public partial class ApiService
{
    // ===== STRIPE CONFIG =====

    public async Task<string?> GetStripePublishableKeyAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<StripeConfigResponse>("api/stripe/config");
            return response?.PublishableKey;
        }
        catch
        {
            return null;
        }
    }

    // ===== CHECKOUT =====

    public async Task<CheckoutSessionResponse?> CreateCheckoutSessionAsync(Guid channelPlanId, string? successUrl = null, string? cancelUrl = null)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/stripe/checkout", new CreateCheckoutSessionRequest(channelPlanId, successUrl, cancelUrl));
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CheckoutSessionResponse>();

        var error = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Stripe checkout error: {response.StatusCode} - {error}");
        throw new Exception($"Stripe error: {error}");
    }

    public async Task<CheckoutSessionResponse?> CreatePlatformCheckoutSessionAsync(Guid planId, string? successUrl = null, string? cancelUrl = null)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/stripe/platform-checkout", new CreatePlatformCheckoutRequest(planId, successUrl, cancelUrl));
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CheckoutSessionResponse>();

        var error = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Stripe checkout error: {response.StatusCode} - {error}");
        throw new Exception($"Stripe error: {error}");
    }

    public async Task<CheckoutSessionResponse?> CreateCourseCheckoutSessionAsync(Guid courseId, string? successUrl = null, string? cancelUrl = null)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/stripe/course-checkout", new CreateCourseCheckoutRequest(courseId, successUrl, cancelUrl));
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CheckoutSessionResponse>();

        var error = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Stripe course checkout error: {response.StatusCode} - {error}");
        throw new Exception($"Stripe error: {error}");
    }

    // ===== CUSTOMER PORTAL =====

    public async Task<PortalSessionResponse?> CreatePortalSessionAsync(string? returnUrl = null)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PostAsJsonAsync("api/stripe/portal", new CreatePortalSessionRequest(returnUrl));
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<PortalSessionResponse>();
            return null;
        }
        catch
        {
            return null;
        }
    }

    // ===== ADMIN - SYNC PLAN =====

    public async Task<SyncPlanResponse?> SyncPlanToStripeAsync(Guid planId)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PostAsync($"api/stripe/sync-plan/{planId}", null);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<SyncPlanResponse>();
            return null;
        }
        catch
        {
            return null;
        }
    }
}
