namespace HonestTimeTracker.Application.Tfs;

public interface ISecretProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}
