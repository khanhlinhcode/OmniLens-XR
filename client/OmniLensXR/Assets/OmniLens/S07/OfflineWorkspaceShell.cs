using System;

namespace OmniLensXR.S07
{
    public enum WorkspaceNetworkState
    {
        Unknown = 0,
        Offline = 1,
        Online = 2,
    }

    public enum OfflineShellAction
    {
        Unknown = 0,
        OpenHome = 1,
        OpenScience3D = 2,
        OpenSettings = 3,
        OpenAgent = 4,
        CompleteOnboarding = 5,
        ResetWorkspace = 6,
    }

    public enum OfflineShellError
    {
        None = 0,
        InvalidAction,
        BackendUnavailable,
        PersistenceFailure,
    }

    public readonly struct OfflineShellResult
    {
        public OfflineShellResult(
            bool accepted,
            OfflineShellError error,
            WorkspaceApp selectedApp,
            bool onboardingComplete
        )
        {
            Accepted = accepted;
            Error = error;
            SelectedApp = selectedApp;
            OnboardingComplete = onboardingComplete;
        }

        public bool Accepted { get; }
        public OfflineShellError Error { get; }
        public WorkspaceApp SelectedApp { get; }
        public bool OnboardingComplete { get; }
    }

    public sealed class OfflineWorkspaceShell
    {
        private readonly IWorkspaceRepository _repository;
        private readonly Func<WorkspaceLayoutData> _defaultLayout;

        public OfflineWorkspaceShell(
            IWorkspaceRepository repository,
            Func<WorkspaceLayoutData> defaultLayout
        )
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _defaultLayout = defaultLayout ?? throw new ArgumentNullException(nameof(defaultLayout));
            SelectedApp = WorkspaceApp.Home;
        }

        public WorkspaceNetworkState NetworkState { get; private set; }
            = WorkspaceNetworkState.Unknown;

        public WorkspaceApp SelectedApp { get; private set; }

        public bool OnboardingComplete { get; private set; }

        public void SetNetworkState(WorkspaceNetworkState state)
        {
            if (!Enum.IsDefined(typeof(WorkspaceNetworkState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state));
            }

            NetworkState = state;
        }

        public OfflineShellResult Apply(OfflineShellAction action)
        {
            switch (action)
            {
                case OfflineShellAction.OpenHome:
                    SelectedApp = WorkspaceApp.Home;
                    break;
                case OfflineShellAction.OpenScience3D:
                    SelectedApp = WorkspaceApp.Science3D;
                    break;
                case OfflineShellAction.OpenSettings:
                    SelectedApp = WorkspaceApp.Settings;
                    break;
                case OfflineShellAction.OpenAgent:
                    if (NetworkState != WorkspaceNetworkState.Online)
                    {
                        return Reject(OfflineShellError.BackendUnavailable);
                    }

                    SelectedApp = WorkspaceApp.AgentActivity;
                    break;
                case OfflineShellAction.CompleteOnboarding:
                    OnboardingComplete = true;
                    break;
                case OfflineShellAction.ResetWorkspace:
                    WorkspaceLayoutData layout = _defaultLayout();
                    if (
                        layout == null
                        || !_repository.TrySave(
                            layout,
                            out WorkspaceRepositoryError saveError
                        )
                        || saveError != WorkspaceRepositoryError.None
                    )
                    {
                        return Reject(OfflineShellError.PersistenceFailure);
                    }

                    SelectedApp = WorkspaceApp.Home;
                    OnboardingComplete = false;
                    break;
                default:
                    return Reject(OfflineShellError.InvalidAction);
            }

            return new OfflineShellResult(
                true,
                OfflineShellError.None,
                SelectedApp,
                OnboardingComplete
            );
        }

        private OfflineShellResult Reject(OfflineShellError error)
        {
            return new OfflineShellResult(
                false,
                error,
                SelectedApp,
                OnboardingComplete
            );
        }
    }

    public enum SpatialInputFallback
    {
        None = 0,
        Touch = 1,
        GazeDwell = 2,
    }

    public static class S07InputFallback
    {
        public static SpatialInputFallback Resolve(bool touchPermissionAvailable)
        {
            return touchPermissionAvailable
                ? SpatialInputFallback.Touch
                : SpatialInputFallback.GazeDwell;
        }
    }
}
