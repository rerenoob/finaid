using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace finaid.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private bool IsAuthenticated { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

        // Subscribe to authentication state changes
        AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        var authState = await task;
        IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}