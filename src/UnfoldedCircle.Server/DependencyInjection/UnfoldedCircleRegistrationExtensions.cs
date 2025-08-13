using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.Server.Configuration;
using UnfoldedCircle.Server.DependencyInjection;
using UnfoldedCircle.Server.Dns;
using UnfoldedCircle.Server.WebSocket;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for registering the Unfolded Circle server in an ASP.NET Core application.
/// </summary>
public static class UnfoldedCircleRegistrationExtensions
{
    /// <summary>
    /// Adds the Unfolded Circle server to the application builder.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="configureOptions">Optional configuration options for the server.</param>
    /// <typeparam name="TUnfoldedCircleWebSocketHandler">The type of socket handler to use.</typeparam>
    /// <typeparam name="TConfigurationService">The type of configuration service to use.</typeparam>
    /// <typeparam name="TConfigurationItem">The type of configuration item to use.</typeparam>
    /// <returns>A <see cref="WebApplicationBuilder"/> with the Unfolded Circle server added to it.</returns>
    public static WebApplicationBuilder AddUnfoldedCircleServer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TUnfoldedCircleWebSocketHandler,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TConfigurationService,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TConfigurationItem>(
        this WebApplicationBuilder builder,
        Action<UnfoldedCircleOptions>? configureOptions = null)
        where TConfigurationItem : UnfoldedCircleConfigurationItem
        where TConfigurationService : class, IConfigurationService<TConfigurationItem>
        where TUnfoldedCircleWebSocketHandler : UnfoldedCircleWebSocketHandler<MediaPlayerCommandId, TConfigurationItem> =>
        AddUnfoldedCircleServer<
            TUnfoldedCircleWebSocketHandler,
            MediaPlayerCommandId,
            TConfigurationService,
            TConfigurationItem>(builder, configureOptions);

    /// <summary>
    /// Adds the Unfolded Circle server to the application builder.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="configureOptions">Optional configuration options for the server.</param>
    /// <typeparam name="TUnfoldedCircleWebSocketHandler">The type of socket handler to use.</typeparam>
    /// <typeparam name="TMediaPlayerCommandId">The type of media player command id to use.</typeparam>
    /// <typeparam name="TConfigurationService">The type of configuration service to use.</typeparam>
    /// <typeparam name="TConfigurationItem">The type of configuration item to use.</typeparam>
    /// <returns>A <see cref="WebApplicationBuilder"/> with the Unfolded Circle server added to it.</returns>
    public static WebApplicationBuilder AddUnfoldedCircleServer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TUnfoldedCircleWebSocketHandler,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TMediaPlayerCommandId,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TConfigurationService,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TConfigurationItem>(
        this WebApplicationBuilder builder,
        Action<UnfoldedCircleOptions>? configureOptions = null)
        where TConfigurationItem : UnfoldedCircleConfigurationItem
        where TConfigurationService : class, IConfigurationService<TConfigurationItem>
        where TMediaPlayerCommandId : struct, Enum
        where TUnfoldedCircleWebSocketHandler : UnfoldedCircleWebSocketHandler<TMediaPlayerCommandId, TConfigurationItem>
    {
        var unfoldedCircleOptions = new UnfoldedCircleOptions();
        configureOptions?.Invoke(unfoldedCircleOptions);

        builder.Services.AddOptions<UnfoldedCircleOptions>();
        if (configureOptions is not null)
            builder.Services.PostConfigure(configureOptions);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(builder.Configuration.GetOrDefault("UC_INTEGRATION_HTTP_PORT", unfoldedCircleOptions.ListeningPort));
            options.AddServerHeader = false;
        });

        builder.Services.AddSingleton<IConfigurationService<TConfigurationItem>, TConfigurationService>();
        builder.Services.AddHostedService<MDnsBackgroundService<TConfigurationItem>>();

        builder.Services.AddSingleton<UnfoldedCircleWebSocketHandler<TMediaPlayerCommandId, TConfigurationItem>, TUnfoldedCircleWebSocketHandler>();
        builder.Services.AddSingleton<UnfoldedCircleMiddleware<TUnfoldedCircleWebSocketHandler, TMediaPlayerCommandId, TConfigurationItem>>();

        return builder;
    }

    /// <summary>
    /// Uses the Unfolded Circle server middleware in the application pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="webSocketOptions">Optional options to customize the websocket behaviour.</param>
    /// <typeparam name="TUnfoldedCircleWebSocketHandler">The type of socket handler to use.</typeparam>
    /// <typeparam name="TConfigurationItem">The type of configuration item to use.</typeparam>
    public static IApplicationBuilder UseUnfoldedCircleServer<TUnfoldedCircleWebSocketHandler, TConfigurationItem>(this IApplicationBuilder builder,
        WebSocketOptions? webSocketOptions = null)
        where TUnfoldedCircleWebSocketHandler : UnfoldedCircleWebSocketHandler<MediaPlayerCommandId, TConfigurationItem>
        where TConfigurationItem : UnfoldedCircleConfigurationItem =>
        UseUnfoldedCircleServer<TUnfoldedCircleWebSocketHandler, MediaPlayerCommandId, TConfigurationItem>(builder, webSocketOptions);

    /// <summary>
    /// Uses the Unfolded Circle server middleware in the application pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="webSocketOptions">Optional options to customize the websocket behaviour.</param>
    /// <typeparam name="TUnfoldedCircleWebSocketHandler">The type of socket handler to use.</typeparam>
    /// <typeparam name="TMediaPlayerCommandId">The type of media player command id to use.</typeparam>
    /// <typeparam name="TConfigurationItem">The type of configuration item to use.</typeparam>
    public static IApplicationBuilder UseUnfoldedCircleServer<TUnfoldedCircleWebSocketHandler, TMediaPlayerCommandId, TConfigurationItem>(this IApplicationBuilder builder,
        WebSocketOptions? webSocketOptions = null)
        where TUnfoldedCircleWebSocketHandler : UnfoldedCircleWebSocketHandler<TMediaPlayerCommandId, TConfigurationItem>
        where TConfigurationItem : UnfoldedCircleConfigurationItem
        where TMediaPlayerCommandId : struct, Enum
    {
        webSocketOptions ??= new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2)
        };

        builder.UseWebSockets(webSocketOptions);
        builder.UseMiddleware<UnfoldedCircleMiddleware<TUnfoldedCircleWebSocketHandler, TMediaPlayerCommandId, TConfigurationItem>>();

        return builder;
    }
}