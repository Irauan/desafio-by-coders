namespace DesafioByCoders.Api.Middleware;

/// <summary>
/// Extension methods for registering exception handling middleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception handling middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This middleware should be added early in the pipeline to catch exceptions
    /// from all subsequent middleware and endpoint handlers.
    /// </para>
    /// </remarks>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
