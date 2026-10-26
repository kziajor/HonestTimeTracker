namespace HonestTimeTracker.Application.Tfs;

public enum TfsErrorKind
{
    Unauthorized,
    Unreachable,
    InvalidResponse,
    Other
}

public class TfsException(TfsErrorKind kind, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public TfsErrorKind Kind { get; } = kind;
}
