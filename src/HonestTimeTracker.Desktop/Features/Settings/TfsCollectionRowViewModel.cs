using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Desktop.Common;

namespace HonestTimeTracker.Desktop.Features.Settings;

public enum ConnectionTestState
{
    None,
    Testing,
    Succeeded,
    Failed
}

public class TfsCollectionRowViewModel(TfsCollectionDto dto) : ViewModelBase
{
    private ConnectionTestState _testState;
    private string _status = string.Empty;

    public TfsCollectionDto Dto { get; } = dto;
    public string Name => Dto.Name;
    public string Url => Dto.Url;
    public bool HasPat => Dto.HasPat;
    public bool Enabled => Dto.Enabled;

    /// Result of the last connection test; kept in memory only.
    public ConnectionTestState TestState
    {
        get => _testState;
        private set => Set(ref _testState, value);
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public void SetTesting()
    {
        TestState = ConnectionTestState.Testing;
        Status = "Testing…";
    }

    public void SetSucceeded(string userName)
    {
        TestState = ConnectionTestState.Succeeded;
        Status = $"Connected as {userName}";
    }

    public void SetFailed(string message)
    {
        TestState = ConnectionTestState.Failed;
        Status = message;
    }
}
