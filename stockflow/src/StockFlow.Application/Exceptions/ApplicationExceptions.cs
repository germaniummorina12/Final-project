namespace StockFlow.Application.Exceptions;

public class NotFoundException(string entity, object key) : Exception($"{entity} '{key}' was not found.");

public class ConflictException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class ConcurrencyConflictException(string message, Exception? inner = null) : ConflictException(message, inner);

public class UnauthorizedException(string message) : Exception(message);