namespace ProductService.API.Models;

public sealed record ApiErrorResponse(string Title, string[]? Errors = null);
