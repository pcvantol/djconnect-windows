using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using DJConnect.Windows.Contracts;
using DJConnect.Windows.Models;
using DJConnect.Windows.Services;

namespace DJConnect.Windows.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SettingsStore _settingsStore = new();
    private readonly CredentialStore _credentialStore = new();
    private readonly DJConnectApiClient _apiClient = new(new HttpClient());
    private readonly MdnsAdvertiser _mdnsAdvertiser;
    private LocalClientApiService? _localClientApi;
    private AppSettings _settings = new();
    private ClientIdentity _identity = ClientIdentity.CreateOrLoad(null);
    private string _homeAssistantUrl = DJConnectContract.DefaultHomeAssistantUrl;
    private string _token = "";
    private string _pairingCode = "030610";
    private string _askDJText = "";
    private string _status = "Niet gekoppeld";
    private string _nowPlaying = "Midnight City - M83";
    private string _notice = "";
    private string _askDJNotice = "";
    private string _queueNotice = "";
    private string _playlistNotice = "";
    private string _playlistSearchText = "";
    private string _voiceStatus = "";
    private string _permissionNotice = "";
    private string _updateRequiredMessage = "";
    private string _homeAssistantVersionText = "";
    private string _whatsNewTitle = "";
    private string _whatsNewBody = "";
    private string _whatsNewOnlineUrl = "https://djconnect.dev";
    private string _selectedFeedbackType = "Bug";
    private string _feedbackText = "";
    private string _feedbackPreviewText = "";
    private string _feedbackNotice = "";
    private string _crashReportPreviewText = "";
    private string _crashReportNotice = "";
    private string _logSearchText = "";
    private string _logNotice = "";
    private string _askDJMood = "Groove";
    private string _wakePhrase = "Hey DJ";
    private string _wakewordNotice = "";
    private PermissionExplanationKind _activePermissionKind = PermissionExplanationKind.None;
    private PermissionExplanationMode _activePermissionMode = PermissionExplanationMode.Request;
    private string _trackTitle = "";
    private string _trackArtist = "";
    private string _trackAlbum = "";
    private string _artworkUrl = "";
    private double _playbackPositionMs;
    private double _playbackDurationMs = 1;
    private double _volumePercent = 42;
    private PlaybackOutput? _selectedOutput;
    private string _language = "nl";
    private string _logLevel = "info";
    private bool _isPaired;
    private bool _isDemoMode;
    private bool _backendAvailable;
    private bool _runtimeCompatible = true;
    private bool _localNetworkAvailable = true;
    private bool _hasActivePlayback;
    private bool _isPlaying;
    private bool _isOnboardingVisible = true;
    private bool _isPairingOverlayVisible;
    private bool _isPairingSuccessVisible;
    private bool _isFeedbackOverlayVisible;
    private bool _includePrivacySafeLogs;
    private bool _isFeedbackPreviewVisible;
    private bool _isCrashReportPending;
    private bool _isCrashReportPreviewVisible;
    private bool _isLogSearchVisible;
    private bool _localAudioReplayEnabled = true;
    private bool _wakewordEnabled;
    private bool _isWakewordPromptVisible;
    private bool _isPermissionExplanationVisible;
    private bool _isRuntimeSectionActive = true;
    private bool _isRefreshingVersionCheck;
    private bool _isWhatsNewVisible;
    private bool _isLoadingWhatsNew;
    private bool _isLoadingQueue;
    private bool _isLoadingPlaylists;
    private bool _suppressOutputCommand;
    private bool _suppressVolumeCommand;
    private int _selectedLogSearchResultIndex;
    private int _nextDiagnosticLogId;
    private CancellationTokenSource? _volumeDebounce;

    public MainViewModel()
    {
        SaveSettingsCommand = new AsyncCommand(SaveSettingsAsync);
        PairCommand = new AsyncCommand(PairAsync, () => !string.IsNullOrWhiteSpace(PairingCode));
        RefreshCommand = new AsyncCommand(RefreshAsync, () => IsPaired || IsDemoMode);
        SendAskDJCommand = new AsyncCommand(SendAskDJAsync, () => CanUseAskDJ && !string.IsNullOrWhiteSpace(AskDJText));
        ClearHistoryCommand = new AsyncCommand(ClearHistoryAsync, () => CanUseAskDJ);
        RefreshQueueCommand = new AsyncCommand(RefreshQueueAsync, () => CanUsePlaybackFeatures);
        RefreshPlaylistsCommand = new AsyncCommand(RefreshPlaylistsAsync, () => CanUsePlaybackFeatures);
        PlayCommand = new AsyncCommand(TogglePlaybackAsync, () => CanStartPlayback);
        PauseCommand = new AsyncCommand(TogglePlaybackAsync, () => CanStartPlayback);
        NextCommand = new AsyncCommand(() => RunPlaybackCommandAsync("next_track"), () => CanUsePlaybackFeatures);
        PreviousCommand = new AsyncCommand(() => RunPlaybackCommandAsync("previous_track"), () => CanUsePlaybackFeatures);
        StartDemoModeCommand = new AsyncCommand(StartDemoModeAsync);
        StopDemoModeCommand = new AsyncCommand(StopDemoModeAsync, () => IsDemoMode);
        CompleteOnboardingCommand = new AsyncCommand(CompleteOnboardingAsync);
        ShowPairingCommand = new AsyncCommand(ShowPairingAsync);
        HidePairingCommand = new AsyncCommand(HidePairingAsync);
        CompletePairingSuccessCommand = new AsyncCommand(CompletePairingSuccessAsync);
        ResetPairingCommand = new AsyncCommand(ResetPairingAsync);
        ShowFeedbackCommand = new AsyncCommand(ShowFeedbackAsync);
        HideFeedbackCommand = new AsyncCommand(HideFeedbackAsync);
        PreviewFeedbackCommand = new AsyncCommand(PreviewFeedbackAsync);
        CopyFeedbackCommand = new AsyncCommand(CopyFeedbackAsync, () => !string.IsNullOrWhiteSpace(FeedbackText) || !string.IsNullOrWhiteSpace(FeedbackPreviewText));
        OpenFeedbackIssueCommand = new AsyncCommand(OpenFeedbackIssueAsync, () => !string.IsNullOrWhiteSpace(FeedbackText) || !string.IsNullOrWhiteSpace(FeedbackPreviewText));
        PreviewCrashReportCommand = new AsyncCommand(PreviewCrashReportAsync);
        CopyCrashReportCommand = new AsyncCommand(CopyCrashReportAsync);
        OpenCrashReportIssueCommand = new AsyncCommand(OpenCrashReportIssueAsync);
        DismissCrashReportCommand = new AsyncCommand(DismissCrashReportAsync);
        CopyLogsCommand = new AsyncCommand(CopyLogsAsync);
        ClearLogsCommand = new AsyncCommand(ClearLogsAsync);
        ToggleLogSearchCommand = new AsyncCommand(ToggleLogSearchAsync);
        NextLogSearchResultCommand = new AsyncCommand(() => MoveLogSearchSelectionAsync(1), () => LogSearchResultCount > 0);
        PreviousLogSearchResultCommand = new AsyncCommand(() => MoveLogSearchSelectionAsync(-1), () => LogSearchResultCount > 0);
        EnableWakewordCommand = new AsyncCommand(EnableWakewordAsync, () => WakewordFeatureAvailable && CanUseAskDJ);
        DismissWakewordPromptCommand = new AsyncCommand(DismissWakewordPromptAsync);
        ShowPrivacyFromWakewordCommand = new AsyncCommand(ShowPrivacyFromWakewordAsync);
        TogglePushToTalkCommand = new AsyncCommand(TogglePushToTalkAsync, () => CanUseAskDJ);
        EnableNotificationsCommand = new AsyncCommand(EnableNotificationsAsync);
        ContinuePermissionExplanationCommand = new AsyncCommand(ContinuePermissionExplanationAsync);
        HidePermissionExplanationCommand = new AsyncCommand(HidePermissionExplanationAsync);
        OpenPermissionSettingsCommand = new AsyncCommand(OpenPermissionSettingsAsync);
        CopyClientAddressCommand = new AsyncCommand(CopyClientAddressAsync);
        RetryVersionCheckCommand = new AsyncCommand(RetryVersionCheckAsync, () => IsPaired || IsDemoMode);
        DismissWhatsNewCommand = new AsyncCommand(DismissWhatsNewAsync);
        _mdnsAdvertiser = new MdnsAdvertiser(AddDiagnostic);
    }

    public ObservableCollection<AskDJMessage> Messages { get; } = [];
    public ObservableCollection<PlaybackAction> Actions { get; } = [];
    public ObservableCollection<RecentItem> RecentItems { get; } = [];
    public ObservableCollection<QueueItem> QueueItems { get; } = [];
    public ObservableCollection<PlaylistItem> PlaylistItems { get; } = [];
    public ObservableCollection<PlaylistItem> FilteredPlaylistItems { get; } = [];
    public ObservableCollection<PlaybackOutput> OutputDevices { get; } = [];
    public ObservableCollection<DiagnosticLogEntry> DiagnosticLogLines { get; } = [];
    public ObservableCollection<DiagnosticLogEntry> FilteredDiagnosticLogLines { get; } = [];

    public string HomeAssistantUrl
    {
        get => _homeAssistantUrl;
        set => SetProperty(ref _homeAssistantUrl, value);
    }

    public string Token
    {
        get => _token;
        set => SetProperty(ref _token, value);
    }

    public string PairingCode
    {
        get => _pairingCode;
        set
        {
            if (SetProperty(ref _pairingCode, value))
            {
                PairCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string AskDJText
    {
        get => _askDJText;
        set
        {
            if (SetProperty(ref _askDJText, value))
            {
                OnPropertyChanged(nameof(CanSendAskDJ));
                SendAskDJCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string NowPlaying
    {
        get => _nowPlaying;
        set => SetProperty(ref _nowPlaying, value);
    }

    public string Notice
    {
        get => _notice;
        set
        {
            if (SetProperty(ref _notice, value))
            {
                OnPropertyChanged(nameof(HasNotice));
            }
        }
    }

    public bool HasNotice => !string.IsNullOrWhiteSpace(Notice);

    public string AskDJNotice
    {
        get => _askDJNotice;
        set
        {
            if (SetProperty(ref _askDJNotice, value))
            {
                OnPropertyChanged(nameof(HasAskDJNotice));
            }
        }
    }

    public bool HasAskDJNotice => !string.IsNullOrWhiteSpace(AskDJNotice);

    public string QueueNotice
    {
        get => _queueNotice;
        set
        {
            if (SetProperty(ref _queueNotice, value))
            {
                OnPropertyChanged(nameof(HasQueueNotice));
            }
        }
    }

    public bool HasQueueNotice => !string.IsNullOrWhiteSpace(QueueNotice);

    public bool IsLoadingQueue
    {
        get => _isLoadingQueue;
        set
        {
            if (SetProperty(ref _isLoadingQueue, value))
            {
                OnPropertyChanged(nameof(HasNoQueueItems));
            }
        }
    }

    public bool HasQueueItems => QueueItems.Count > 0;
    public bool HasNoQueueItems => QueueItems.Count == 0 && !IsLoadingQueue;

    public string PlaylistNotice
    {
        get => _playlistNotice;
        set
        {
            if (SetProperty(ref _playlistNotice, value))
            {
                OnPropertyChanged(nameof(HasPlaylistNotice));
            }
        }
    }

    public bool HasPlaylistNotice => !string.IsNullOrWhiteSpace(PlaylistNotice);

    public bool IsLoadingPlaylists
    {
        get => _isLoadingPlaylists;
        set
        {
            if (SetProperty(ref _isLoadingPlaylists, value))
            {
                OnPropertyChanged(nameof(HasNoPlaylistItems));
            }
        }
    }

    public string PlaylistSearchText
    {
        get => _playlistSearchText;
        set
        {
            if (SetProperty(ref _playlistSearchText, value))
            {
                ApplyPlaylistFilter();
            }
        }
    }

    public bool HasPlaylistItems => FilteredPlaylistItems.Count > 0;
    public bool HasNoPlaylistItems => FilteredPlaylistItems.Count == 0 && !IsLoadingPlaylists;

    public string LogNotice
    {
        get => _logNotice;
        set
        {
            if (SetProperty(ref _logNotice, value))
            {
                OnPropertyChanged(nameof(HasLogNotice));
            }
        }
    }

    public bool HasLogNotice => !string.IsNullOrWhiteSpace(LogNotice);

    public bool IsLogSearchVisible
    {
        get => _isLogSearchVisible;
        set => SetProperty(ref _isLogSearchVisible, value);
    }

    public string LogSearchText
    {
        get => _logSearchText;
        set
        {
            if (SetProperty(ref _logSearchText, value))
            {
                _selectedLogSearchResultIndex = 0;
                ApplyLogFilter();
            }
        }
    }

    public bool HasDiagnosticLogs => FilteredDiagnosticLogLines.Count > 0;
    public bool HasNoDiagnosticLogs => FilteredDiagnosticLogLines.Count == 0;
    public int LogSearchResultCount => DiagnosticLogLines.Count(entry => entry.IsSearchMatch);
    public string LogSearchResultLabel => string.IsNullOrWhiteSpace(LogSearchText)
        ? ""
        : LogSearchResultCount == 0 ? "0 resultaten" : $"{_selectedLogSearchResultIndex + 1} / {LogSearchResultCount}";

    public string VoiceStatus
    {
        get => _voiceStatus;
        set
        {
            if (SetProperty(ref _voiceStatus, value))
            {
                OnPropertyChanged(nameof(HasVoiceStatus));
            }
        }
    }

    public bool HasVoiceStatus => !string.IsNullOrWhiteSpace(VoiceStatus);

    public string PermissionNotice
    {
        get => _permissionNotice;
        set
        {
            if (SetProperty(ref _permissionNotice, value))
            {
                OnPropertyChanged(nameof(HasPermissionNotice));
            }
        }
    }

    public bool HasPermissionNotice => !string.IsNullOrWhiteSpace(PermissionNotice);

    public string UpdateRequiredMessage
    {
        get => _updateRequiredMessage;
        set
        {
            if (SetProperty(ref _updateRequiredMessage, value))
            {
                OnPropertyChanged(nameof(IsUpdateRequired));
                OnPropertyChanged(nameof(IsUpdateRequiredScreenVisible));
                OnPropertyChanged(nameof(UpdateRequiredTitle));
                OnPropertyChanged(nameof(UpdateRequiredDetail));
                RaisePlaybackStateProperties();
            }
        }
    }

    public bool IsUpdateRequired => !string.IsNullOrWhiteSpace(UpdateRequiredMessage);

    public bool IsRuntimeSectionActive
    {
        get => _isRuntimeSectionActive;
        set
        {
            if (SetProperty(ref _isRuntimeSectionActive, value))
            {
                OnPropertyChanged(nameof(IsUpdateRequiredScreenVisible));
            }
        }
    }

    public bool IsUpdateRequiredScreenVisible => IsUpdateRequired && IsRuntimeSectionActive && !IsOnboardingVisible && !IsPairingOverlayVisible;

    public bool IsRefreshingVersionCheck
    {
        get => _isRefreshingVersionCheck;
        set => SetProperty(ref _isRefreshingVersionCheck, value);
    }

    public string UpdateRequiredTitle => L("Update vereist", "Update Required");
    public string UpdateRequiredSubtitle => L(
        "Werk DJConnect bij voordat je verdergaat.",
        "Update DJConnect before continuing.");
    public string UpdateRequiredDetail => string.IsNullOrWhiteSpace(UpdateRequiredMessage)
        ? L(
            "De app en Home Assistant DJConnect integration gebruiken verschillende protocolversies.",
            "The app and Home Assistant DJConnect integration use different protocol versions.")
        : UpdateRequiredMessage;
    public string AppProtocolText => $"App: {DJConnectContract.ProtocolLine}.x";
    public string HomeAssistantVersionText
    {
        get => string.IsNullOrWhiteSpace(_homeAssistantVersionText) ? L("Home Assistant integration: onbekend", "Home Assistant integration: unknown") : _homeAssistantVersionText;
        set => SetProperty(ref _homeAssistantVersionText, value);
    }
    public string RequiredProtocolText => $"{L("Vereist", "Required")}: {DJConnectContract.ProtocolLine}.x";
    public bool IsWhatsNewVisible
    {
        get => _isWhatsNewVisible;
        set => SetProperty(ref _isWhatsNewVisible, value);
    }

    public bool IsLoadingWhatsNew
    {
        get => _isLoadingWhatsNew;
        set => SetProperty(ref _isLoadingWhatsNew, value);
    }

    public string WhatsNewTitle
    {
        get => string.IsNullOrWhiteSpace(_whatsNewTitle) ? L("Wat is er nieuw?", "What's New") : _whatsNewTitle;
        set => SetProperty(ref _whatsNewTitle, value);
    }

    public string WhatsNewSubtitle => $"DJConnect Windows {AppVersion}";

    public string WhatsNewBody
    {
        get => string.IsNullOrWhiteSpace(_whatsNewBody)
            ? L("Release notes konden niet worden geladen. Bekijk https://djconnect.dev voor meer informatie.", "Release notes could not be loaded. Visit https://djconnect.dev for more information.")
            : _whatsNewBody;
        set => SetProperty(ref _whatsNewBody, value);
    }

    public string WhatsNewOnlineUrl
    {
        get => _whatsNewOnlineUrl;
        set => SetProperty(ref _whatsNewOnlineUrl, value);
    }

    public string TrackTitle
    {
        get => string.IsNullOrWhiteSpace(_trackTitle) ? L("Geen actieve playback", "No active playback") : _trackTitle;
        set => SetProperty(ref _trackTitle, value);
    }

    public string TrackArtist
    {
        get => _trackArtist;
        set => SetProperty(ref _trackArtist, value);
    }

    public string TrackAlbum
    {
        get => _trackAlbum;
        set => SetProperty(ref _trackAlbum, value);
    }

    public string ArtworkUrl
    {
        get => _artworkUrl;
        set
        {
            if (SetProperty(ref _artworkUrl, value))
            {
                OnPropertyChanged(nameof(HasArtwork));
                OnPropertyChanged(nameof(HasNoArtwork));
            }
        }
    }

    public bool HasArtwork => !string.IsNullOrWhiteSpace(ArtworkUrl);

    public bool HasNoArtwork => !HasArtwork;

    public double PlaybackPositionMs
    {
        get => _playbackPositionMs;
        set
        {
            var bounded = Math.Clamp(value, 0, PlaybackDurationMs);
            if (SetProperty(ref _playbackPositionMs, bounded))
            {
                OnPropertyChanged(nameof(PlaybackProgress));
                OnPropertyChanged(nameof(PlaybackTimeLabel));
            }
        }
    }

    public double PlaybackDurationMs
    {
        get => Math.Max(1, _playbackDurationMs);
        set
        {
            if (SetProperty(ref _playbackDurationMs, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(PlaybackProgress));
                OnPropertyChanged(nameof(PlaybackTimeLabel));
            }
        }
    }

    public double PlaybackProgress => PlaybackDurationMs <= 1 ? 0 : PlaybackPositionMs / PlaybackDurationMs;

    public string PlaybackTimeLabel => $"{FormatTime(PlaybackPositionMs)} / {FormatTime(PlaybackDurationMs)}";

    public double VolumePercent
    {
        get => _volumePercent;
        set
        {
            var bounded = Math.Clamp(value, 0, 100);
            if (SetProperty(ref _volumePercent, bounded))
            {
                OnPropertyChanged(nameof(VolumeLabel));
                if (!_suppressVolumeCommand)
                {
                    QueueVolumeCommand();
                }
            }
        }
    }

    public string VolumeLabel => $"{Math.Round(VolumePercent):0}";

    public PlaybackOutput? SelectedOutput
    {
        get => _selectedOutput;
        set
        {
            if (SetProperty(ref _selectedOutput, value))
            {
                OnPropertyChanged(nameof(SelectedOutputText));
                RaiseCommandStates();
                if (!_suppressOutputCommand)
                {
                    _ = SelectOutputAsync(value);
                }
            }
        }
    }

    public string SelectedOutputText => SelectedOutput?.DisplayName ?? L("Geen uitvoerapparaat geselecteerd", "No output device selected");

    public bool HasActivePlayback
    {
        get => _hasActivePlayback;
        set
        {
            if (SetProperty(ref _hasActivePlayback, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                OnPropertyChanged(nameof(PlayPauseGlyph));
            }
        }
    }

    public string PlayPauseGlyph => IsPlaying ? "▮▮" : "▶";

    public string Language
    {
        get => _language;
        set
        {
            var normalized = string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "nl";
            if (SetProperty(ref _language, normalized))
            {
                _settings.Language = normalized;
                RaiseLocalizedProperties();
                _ = SaveSettingsIfMutableAsync();
            }
        }
    }

    public bool IsDutch
    {
        get => Language == "nl";
        set
        {
            Language = value ? "nl" : "en";
            OnPropertyChanged();
        }
    }

    public string LogLevel
    {
        get => _logLevel;
        set
        {
            if (SetProperty(ref _logLevel, value))
            {
                _settings.LogLevel = value;
                _ = SaveSettingsIfMutableAsync();
            }
        }
    }

    public bool LocalAudioReplayEnabled
    {
        get => _localAudioReplayEnabled;
        set => SetProperty(ref _localAudioReplayEnabled, value);
    }

    public string AskDJMood
    {
        get => _askDJMood;
        set => SetProperty(ref _askDJMood, string.IsNullOrWhiteSpace(value) ? "Groove" : value);
    }

    public bool WakewordFeatureAvailable => false;

    public bool WakewordEnabled
    {
        get => _wakewordEnabled;
        set
        {
            if (SetProperty(ref _wakewordEnabled, WakewordFeatureAvailable && value))
            {
                _settings.WakewordEnabled = _wakewordEnabled;
                _ = SaveSettingsIfMutableAsync();
                UpdateWakewordListening();
                RaiseWakewordProperties();
            }
        }
    }

    public string WakePhrase
    {
        get => _wakePhrase;
        set
        {
            var phrase = string.IsNullOrWhiteSpace(value) ? "Hey DJ" : value.Trim();
            if (SetProperty(ref _wakePhrase, phrase))
            {
                _settings.WakePhrase = phrase;
                _ = SaveSettingsIfMutableAsync();
                UpdateWakewordListening();
            }
        }
    }

    public string WakewordStatusText => WakewordFeatureAvailable
        ? WakewordEnabled ? L("Ingeschakeld terwijl de app open is", "Enabled while the app is open")
        : L("Uitgeschakeld", "Disabled")
        : L("Niet beschikbaar in deze build", "Not available in this build");

    public string WakewordNotice
    {
        get => _wakewordNotice;
        set
        {
            if (SetProperty(ref _wakewordNotice, value))
            {
                OnPropertyChanged(nameof(HasWakewordNotice));
            }
        }
    }

    public bool HasWakewordNotice => !string.IsNullOrWhiteSpace(WakewordNotice);
    public bool IsWakewordPromptVisible
    {
        get => _isWakewordPromptVisible && ShouldShowWakewordPrompt;
        set => SetProperty(ref _isWakewordPromptVisible, value);
    }

    public bool ShouldShowWakewordPrompt => WakewordFeatureAvailable
        && IsPaired
        && !IsPairingOverlayVisible
        && !IsPairingSuccessVisible
        && !IsOnboardingVisible
        && !IsDemoMode
        && !WakewordEnabled
        && !_settings.WakewordPromptDismissed
        && _runtimeCompatible
        && !ShouldSuppressCrashReportPrompt();

    public string DeviceId => _identity.DeviceId;
    public string ClientType => _identity.ClientType;
    public string AppVersion => "3.1.1";
    public string ProtocolVersion => $"{DJConnectContract.ProtocolLine}.x";
    public string BuildChannel => "debug";
    public string PlatformName => "Windows";
    public string WebsiteUrl => "https://djconnect.dev";
    public string ClientAddress => _localClientApi?.LocalUrl ?? L("Client adres wordt gestart...", "Starting Client address...");
    public string ClientAddressDisplay => _localClientApi?.LocalUrl ?? L("Client adres ophalen...", "Getting Client address...");
    public bool IsClientAddressAvailable => !string.IsNullOrWhiteSpace(_localClientApi?.LocalUrl);
    public bool IsPairable => IsPairingOverlayVisible && !IsPairingSuccessVisible && !IsOnboardingVisible && !IsDemoMode && !IsPaired;
    public bool IsPairingFormVisible => IsPairingOverlayVisible && !IsPairingSuccessVisible;
    public bool IsPairingWaitingVisible => IsPairingFormVisible && !IsPaired;
    public string PairingCodeDisplay => IsPairable ? PairingCode : "";
    public string LegalNotice => DJConnectContract.SpotifyNotice;

    public string Tagline => L("Muziekbediening met karakter", "Music control with character");
    public string NowPlayingTitle => L("Speelt Nu", "Now Playing");
    public string QueueTitle => L("Wachtrij", "Queue");
    public string PlaylistsTitle => L("Afspeellijsten", "Playlists");
    public string SettingsTitle => L("Instellingen", "Settings");
    public string AboutTitle => L("Over", "About");
    public string LegalTitle => L("Juridisch", "Legal");
    public string PrivacyTitle => L("Privacy", "Privacy");
    public string FeedbackTitle => L("Feedback delen", "Share Feedback");
    public string FeedbackContextSummary =>
        $"""
        App: DJConnect Windows {AppVersion}
        Protocol: {ProtocolVersion}
        Client type: windows
        OS: {FeedbackOsVersion}
        Pairing status: {FeedbackPairingStatus}
        Demo mode: {IsDemoMode.ToString().ToLowerInvariant()}
        Runtime compatible: {_runtimeCompatible.ToString().ToLowerInvariant()}
        Backend available: {_backendAvailable.ToString().ToLowerInvariant()}
        Device class: desktop
        """;
    public string FeedbackOsVersion => $"Windows {Environment.OSVersion.Version}";
    public string FeedbackPairingStatus => IsPaired ? "paired"
        : IsPairingOverlayVisible ? "pairing"
        : !string.IsNullOrWhiteSpace(Token) ? "stale"
        : "unpaired";
    public string SettingsPairingStatusText => IsDemoMode ? "demo mode"
        : IsUpdateRequired ? "update vereist"
        : IsPaired && _backendAvailable ? "gekoppeld"
        : IsPaired ? "verlopen/stale"
        : IsPairingOverlayVisible ? "koppelen"
        : "niet gekoppeld";
    public string SettingsRuntimeSummary => $"{RuntimeCompatibilityText} · backend {AboutBackendAvailabilityText} · lokaal netwerk {(_localNetworkAvailable ? "available" : "unavailable")}";
    public string PairingStatusText => IsPairingSuccessVisible ? L("Succesvol gekoppeld", "Successfully paired")
        : IsUpdateRequired ? L("Update vereist", "Update required")
        : IsPaired ? L("Gekoppeld", "Paired")
        : !IsClientAddressAvailable ? L("Client adres ophalen...", "Getting Client address...")
        : L("Wachten op Home Assistant...", "Waiting for Home Assistant...");
    public string PlaybackAvailabilityText => IsDemoMode || IsPaired ? L("Beschikbaar", "Available") : L("Niet beschikbaar", "Unavailable");
    public string ConnectionStatusText => IsDemoMode
        ? L("Demo mode", "Demo mode")
        : !IsPaired ? L("Niet gekoppeld", "Not paired")
        : !_backendAvailable ? L("Offline", "Offline")
        : !_runtimeCompatible ? L("Update vereist", "Update required")
        : L("Gekoppeld", "Paired");
    public string RuntimeCompatibilityText => _runtimeCompatible ? L("Compatible", "Compatible") : L("Update vereist", "Update required");
    public string AboutPairingStatusText => IsPaired ? "paired" : IsPairingOverlayVisible ? "pairing" : "unpaired";
    public string AboutBackendAvailabilityText => _backendAvailable ? "available" : "unavailable";
    public string AboutDemoModeText => IsDemoMode ? "true" : "false";
    public bool CanUsePlaybackFeatures => IsDemoMode || (IsPaired && _backendAvailable && _runtimeCompatible && _localNetworkAvailable);
    public bool CanStartPlayback => CanUsePlaybackFeatures && SelectedOutput is not null;
    public bool CanUseAskDJ => IsDemoMode || (IsPaired && _backendAvailable && _runtimeCompatible && _localNetworkAvailable);
    public bool CanSendAskDJ => CanUseAskDJ && !string.IsNullOrWhiteSpace(AskDJText);
    public string AskDJPlaceholder => L("Vraag Ask DJ iets...", "Ask DJ anything...");
    public bool ShouldAdvertiseMdns => IsPairable && (_localClientApi?.IsRunning == true);
    public bool IsMonkeyTestMode => MonkeyTestMode.IsEnabled;

    public bool IsPaired
    {
        get => _isPaired;
        set
        {
            if (SetProperty(ref _isPaired, value))
            {
                RaiseCommandStates();
                OnPropertyChanged(nameof(PairingStatusText));
                OnPropertyChanged(nameof(PlaybackAvailabilityText));
                OnPropertyChanged(nameof(AboutPairingStatusText));
                RaiseFeedbackContextProperties();
                EvaluateWakewordPrompt();
                RaisePairingProperties();
                _ = UpdateMdnsAdvertisingAsync();
            }
        }
    }

    public bool IsDemoMode
    {
        get => _isDemoMode;
        set
        {
            if (SetProperty(ref _isDemoMode, value))
            {
                _settings.IsDemoMode = false;
                RaiseCommandStates();
                StopDemoModeCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(PairingStatusText));
                OnPropertyChanged(nameof(PlaybackAvailabilityText));
                OnPropertyChanged(nameof(AboutDemoModeText));
                RaiseFeedbackContextProperties();
                EvaluateWakewordPrompt();
                RaisePairingProperties();
                _ = UpdateMdnsAdvertisingAsync();
            }
        }
    }

    public bool IsOnboardingVisible
    {
        get => _isOnboardingVisible;
        set
        {
            if (SetProperty(ref _isOnboardingVisible, value))
            {
                OnPropertyChanged(nameof(IsUpdateRequiredScreenVisible));
                OnPropertyChanged(nameof(AboutPairingStatusText));
                OnPropertyChanged(nameof(IsCrashReportPromptVisible));
                RaiseFeedbackContextProperties();
                EvaluateWakewordPrompt();
                RaisePairingProperties();
                _ = UpdateMdnsAdvertisingAsync();
            }
        }
    }

    public bool IsPairingOverlayVisible
    {
        get => _isPairingOverlayVisible;
        set
        {
            if (SetProperty(ref _isPairingOverlayVisible, value))
            {
                OnPropertyChanged(nameof(IsUpdateRequiredScreenVisible));
                RaiseFeedbackContextProperties();
                _ = UpdateMdnsAdvertisingAsync();
                RaisePairingProperties();
            }
        }
    }

    public bool IsPairingSuccessVisible
    {
        get => _isPairingSuccessVisible;
        set
        {
            if (SetProperty(ref _isPairingSuccessVisible, value))
            {
                RaisePairingProperties();
                OnPropertyChanged(nameof(IsCrashReportPromptVisible));
                EvaluateWakewordPrompt();
                _ = UpdateMdnsAdvertisingAsync();
            }
        }
    }

    public bool IsFeedbackOverlayVisible
    {
        get => _isFeedbackOverlayVisible;
        set => SetProperty(ref _isFeedbackOverlayVisible, value);
    }

    public string SelectedFeedbackType
    {
        get => _selectedFeedbackType;
        set
        {
            if (SetProperty(ref _selectedFeedbackType, string.IsNullOrWhiteSpace(value) ? "Bug" : value))
            {
                ResetFeedbackPreview();
            }
        }
    }

    public string FeedbackText
    {
        get => _feedbackText;
        set
        {
            if (SetProperty(ref _feedbackText, value))
            {
                CopyFeedbackCommand.RaiseCanExecuteChanged();
                OpenFeedbackIssueCommand.RaiseCanExecuteChanged();
                ResetFeedbackPreview();
            }
        }
    }

    public bool IncludePrivacySafeLogs
    {
        get => _includePrivacySafeLogs;
        set
        {
            if (SetProperty(ref _includePrivacySafeLogs, value))
            {
                ResetFeedbackPreview();
            }
        }
    }

    public bool IsFeedbackPreviewVisible
    {
        get => _isFeedbackPreviewVisible;
        set => SetProperty(ref _isFeedbackPreviewVisible, value);
    }

    public string FeedbackPreviewText
    {
        get => _feedbackPreviewText;
        set
        {
            if (SetProperty(ref _feedbackPreviewText, RedactFeedbackText(value)))
            {
                CopyFeedbackCommand.RaiseCanExecuteChanged();
                OpenFeedbackIssueCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FeedbackNotice
    {
        get => _feedbackNotice;
        set
        {
            if (SetProperty(ref _feedbackNotice, value))
            {
                OnPropertyChanged(nameof(HasFeedbackNotice));
            }
        }
    }

    public bool HasFeedbackNotice => !string.IsNullOrWhiteSpace(FeedbackNotice);

    public bool IsCrashReportPromptVisible => _isCrashReportPending && !IsOnboardingVisible && !IsPairingSuccessVisible;

    public bool IsCrashReportPreviewVisible
    {
        get => _isCrashReportPreviewVisible;
        set => SetProperty(ref _isCrashReportPreviewVisible, value);
    }

    public string CrashReportPreviewText
    {
        get => _crashReportPreviewText;
        set => SetProperty(ref _crashReportPreviewText, RedactFeedbackText(value));
    }

    public string CrashReportNotice
    {
        get => _crashReportNotice;
        set
        {
            if (SetProperty(ref _crashReportNotice, value))
            {
                OnPropertyChanged(nameof(HasCrashReportNotice));
            }
        }
    }

    public bool HasCrashReportNotice => !string.IsNullOrWhiteSpace(CrashReportNotice);

    public bool IsPermissionExplanationVisible
    {
        get => _isPermissionExplanationVisible;
        set => SetProperty(ref _isPermissionExplanationVisible, value);
    }

    public PermissionExplanationKind ActivePermissionKind
    {
        get => _activePermissionKind;
        private set
        {
            if (SetProperty(ref _activePermissionKind, value))
            {
                RaisePermissionExplanationProperties();
            }
        }
    }

    public PermissionExplanationMode ActivePermissionMode
    {
        get => _activePermissionMode;
        private set
        {
            if (SetProperty(ref _activePermissionMode, value))
            {
                RaisePermissionExplanationProperties();
            }
        }
    }

    public string PermissionIcon => ActivePermissionKind switch
    {
        PermissionExplanationKind.Microphone => "🎙",
        PermissionExplanationKind.Notifications => "🔔",
        PermissionExplanationKind.LocalNetwork => "🛡",
        _ => "🛡"
    };

    public string PermissionTitle => L("App-toestemmingen", "App permissions");

    public string PermissionIntro => L(
        "DJConnect vraagt alleen toestemming wanneer een functie die nodig heeft.",
        "DJConnect only asks permission when a feature needs it.");

    public string PermissionBodyPrimary => ActivePermissionKind switch
    {
        PermissionExplanationKind.Microphone => L(
            "Microfoon is nodig voor Ask DJ spraakverzoeken.",
            "Microphone is needed for Ask DJ voice requests."),
        PermissionExplanationKind.Notifications => L(
            "Notificaties zijn nodig om DJConnect-meldingen als Windows toast te tonen.",
            "Notifications are needed to show DJConnect messages as Windows toasts."),
        PermissionExplanationKind.LocalNetwork => L(
            "Voor pairing moet Home Assistant deze app op je lokale netwerk kunnen bereiken.",
            "For pairing, Home Assistant must be able to reach this app on your local network."),
        _ => ""
    };

    public string PermissionBodySecondary => ActivePermissionKind switch
    {
        PermissionExplanationKind.Microphone => L(
            "Tekstchat en muziekbediening werken ook zonder microfoon.",
            "Text chat and music controls also work without the microphone."),
        PermissionExplanationKind.Notifications => L(
            "Je kunt dit later aanpassen in Windows Meldingsinstellingen.",
            "You can change this later in Windows notification settings."),
        PermissionExplanationKind.LocalNetwork => L(
            "Windows kan vragen of DJConnect netwerktoegang mag krijgen.",
            "Windows may ask whether DJConnect can use network access."),
        _ => ""
    };

    public string PermissionBodyTertiary => ActivePermissionKind switch
    {
        PermissionExplanationKind.Microphone => L(
            "Je kunt dit later aanpassen in Windows Privacy-instellingen.",
            "You can change this later in Windows privacy settings."),
        PermissionExplanationKind.LocalNetwork => L(
            "Sta lokaal netwerk toe voor je privé/thuisnetwerk. Gebruik geen openbaar netwerk tenzij je weet wat je doet.",
            "Allow local network access for your private/home network. Do not use a public network unless you know what you are doing."),
        _ => ""
    };

    public bool HasPermissionBodyTertiary => !string.IsNullOrWhiteSpace(PermissionBodyTertiary);
    public bool IsPermissionSettingsMode => ActivePermissionMode == PermissionExplanationMode.Settings;
    public bool IsLocalNetworkPermission => ActivePermissionKind == PermissionExplanationKind.LocalNetwork;
    public bool CanCopyClientAddress => !string.IsNullOrWhiteSpace(_localClientApi?.LocalUrl);
    public string PermissionContinueText => IsPermissionSettingsMode ? L("Open Windows instellingen", "Open Windows settings") : L("Doorgaan", "Continue");
    public string PermissionSettingsText => ActivePermissionKind == PermissionExplanationKind.LocalNetwork
        ? L("Open firewall instellingen", "Open firewall settings")
        : L("Open Windows instellingen", "Open Windows settings");
    public string NotificationPermissionStatus => _settings.PermissionExplanationNotificationsSeen
        ? L("Uitleg getoond", "Explanation shown")
        : L("Niet ingeschakeld", "Not enabled");
    public string MicrophonePermissionStatus => _settings.PermissionExplanationMicrophoneSeen
        ? L("Wordt gevraagd bij push-to-talk", "Requested at push-to-talk")
        : L("Niet gevraagd", "Not requested");
    public string LocalNetworkPermissionStatus => _settings.PermissionExplanationLocalNetworkSeen
        ? L("Uitleg getoond", "Explanation shown")
        : L("Wordt getoond bij pairing", "Shown during pairing");

    public AsyncCommand SaveSettingsCommand { get; }
    public AsyncCommand PairCommand { get; }
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SendAskDJCommand { get; }
    public AsyncCommand ClearHistoryCommand { get; }
    public AsyncCommand RefreshQueueCommand { get; }
    public AsyncCommand RefreshPlaylistsCommand { get; }
    public AsyncCommand PlayCommand { get; }
    public AsyncCommand PauseCommand { get; }
    public AsyncCommand NextCommand { get; }
    public AsyncCommand PreviousCommand { get; }
    public AsyncCommand StartDemoModeCommand { get; }
    public AsyncCommand StopDemoModeCommand { get; }
    public AsyncCommand CompleteOnboardingCommand { get; }
    public AsyncCommand ShowPairingCommand { get; }
    public AsyncCommand HidePairingCommand { get; }
    public AsyncCommand CompletePairingSuccessCommand { get; }
    public AsyncCommand ResetPairingCommand { get; }
    public AsyncCommand ShowFeedbackCommand { get; }
    public AsyncCommand HideFeedbackCommand { get; }
    public AsyncCommand PreviewFeedbackCommand { get; }
    public AsyncCommand CopyFeedbackCommand { get; }
    public AsyncCommand OpenFeedbackIssueCommand { get; }
    public AsyncCommand PreviewCrashReportCommand { get; }
    public AsyncCommand CopyCrashReportCommand { get; }
    public AsyncCommand OpenCrashReportIssueCommand { get; }
    public AsyncCommand DismissCrashReportCommand { get; }
    public AsyncCommand CopyLogsCommand { get; }
    public AsyncCommand ClearLogsCommand { get; }
    public AsyncCommand ToggleLogSearchCommand { get; }
    public AsyncCommand NextLogSearchResultCommand { get; }
    public AsyncCommand PreviousLogSearchResultCommand { get; }
    public AsyncCommand EnableWakewordCommand { get; }
    public AsyncCommand DismissWakewordPromptCommand { get; }
    public AsyncCommand ShowPrivacyFromWakewordCommand { get; }
    public AsyncCommand TogglePushToTalkCommand { get; }
    public AsyncCommand EnableNotificationsCommand { get; }
    public AsyncCommand ContinuePermissionExplanationCommand { get; }
    public AsyncCommand HidePermissionExplanationCommand { get; }
    public AsyncCommand OpenPermissionSettingsCommand { get; }
    public AsyncCommand CopyClientAddressCommand { get; }
    public AsyncCommand RetryVersionCheckCommand { get; }
    public AsyncCommand DismissWhatsNewCommand { get; }

    public async Task InitializeAsync()
    {
        _settings = await _settingsStore.LoadAsync();
        var previousMayHaveCrashed = _settings.HasCleanShutdownState && !_settings.CleanShutdown;
        _isCrashReportPending = !ShouldSuppressCrashReportPrompt() && (previousMayHaveCrashed || _settings.CrashPromptPending);
        _settings.HasCleanShutdownState = true;
        _settings.CleanShutdown = false;
        _settings.CrashPromptPending = _isCrashReportPending;
        _identity = ClientIdentity.CreateOrLoad(_settings.InstallId, _settings.DeviceName);
        HomeAssistantUrl = _settings.HomeAssistantUrl;
        Language = string.IsNullOrWhiteSpace(_settings.Language) ? "nl" : _settings.Language;
        LogLevel = string.IsNullOrWhiteSpace(_settings.LogLevel) ? "info" : _settings.LogLevel;
        _wakewordEnabled = WakewordFeatureAvailable && _settings.WakewordEnabled;
        WakePhrase = string.IsNullOrWhiteSpace(_settings.WakePhrase) ? "Hey DJ" : _settings.WakePhrase;
        _settings.IsDemoMode = false;
        IsDemoMode = false;
        IsOnboardingVisible = !(_settings.DJConnectWelcomeSeen || _settings.HasCompletedOnboarding);
        Token = _credentialStore.ReadToken() ?? "";
        IsPaired = !string.IsNullOrWhiteSpace(Token);
        LoadPersistedDiagnosticLogs();
        PairingCode = string.IsNullOrWhiteSpace(_settings.PairingCode)
            ? PairingCodeGenerator.CreateCode()
            : _settings.PairingCode;
        _settings.PairingCode = PairingCode;
        IsPairingOverlayVisible = !IsOnboardingVisible && !IsPaired;
        if (!IsOnboardingVisible && !IsPaired && !IsDemoMode)
        {
            ShowPermissionExplanation(PermissionExplanationKind.LocalNetwork);
        }

        if (IsMonkeyTestMode)
        {
            Token = "";
            IsPaired = false;
            IsOnboardingVisible = false;
            IsPairingOverlayVisible = false;
            IsPairingSuccessVisible = false;
            IsFeedbackOverlayVisible = false;
            _isCrashReportPending = false;
            _settings.CrashPromptPending = false;
            IsCrashReportPreviewVisible = false;
            IsWhatsNewVisible = false;
            await StartDemoModeAsync();
            AddDiagnostic("INF Monkey test mode started in non-destructive Demo Mode.");
            return;
        }

        await SaveSettingsIfMutableAsync();
        ConfigureClient();
        OnPropertyChanged(nameof(DeviceId));
        OnPropertyChanged(nameof(ClientType));
        OnPropertyChanged(nameof(ClientAddress));
        OnPropertyChanged(nameof(IsCrashReportPromptVisible));
        EvaluateWakewordPrompt();
        await UpdateMdnsAdvertisingAsync();
        await PrepareWhatsNewAsync();
        if (IsPaired)
        {
            await RefreshAsync();
        }
    }

    private async Task SaveSettingsAsync()
    {
        if (IsMonkeyTestMode)
        {
            Status = L("Monkeytest: instellingen niet opgeslagen", "Monkey test: settings not saved");
            AddDiagnostic("INF Monkey test suppressed settings save.");
            return;
        }

        _settings.HomeAssistantUrl = HomeAssistantUrl;
        _settings.InstallId = _identity.InstallId;
        _settings.DeviceName = _identity.DeviceName;
        _settings.Language = Language;
        _settings.LogLevel = LogLevel;
        _settings.IsDemoMode = false;
        _settings.DJConnectWelcomeSeen = !IsOnboardingVisible;
        _settings.HasCompletedOnboarding = !IsOnboardingVisible;
        _settings.PairingCode = PairingCode;
        await SaveSettingsIfMutableAsync();
        if (!string.IsNullOrWhiteSpace(Token))
        {
            _credentialStore.SaveToken(Token.Trim());
            IsPaired = true;
            IsPairingOverlayVisible = IsPairingSuccessVisible;
            await UpdateMdnsAdvertisingAsync();
        }

        ConfigureClient();
        Status = L("Instellingen opgeslagen", "Settings saved");
        AddDiagnostic("INF Settings saved.");
    }

    private async Task PairAsync()
    {
        if (IsMonkeyTestMode)
        {
            Status = L("Monkeytest: pairing onderdrukt", "Monkey test: pairing suppressed");
            AddDiagnostic("INF Monkey test suppressed pairing.");
            return;
        }

        ConfigureClient();
        var payload = new PairingPayload(
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            PairingCode.Trim(),
            PairingCode.Trim(),
            PairingCode.Trim());
        var response = await _apiClient.PairAsync(payload, CancellationToken.None);
        if (!response.Success || string.IsNullOrWhiteSpace(response.DeviceToken))
        {
            Status = response.Error ?? response.Message ?? L("Pairing niet gelukt", "Pairing failed");
            IsPairingOverlayVisible = true;
            AddDiagnostic("WRN Pairing failed.");
            return;
        }

        Token = response.DeviceToken;
        try
        {
            _credentialStore.SaveToken(Token);
        }
        catch (Exception ex)
        {
            Status = L("Token opslagfout", "Token storage failed");
            AddDiagnostic("WRN Pairing token storage failed: " + ex.GetType().Name);
            return;
        }

        IsPaired = true;
        IsPairingSuccessVisible = true;
        IsPairingOverlayVisible = true;
        await UpdateMdnsAdvertisingAsync();
        await SaveSettingsAsync();
        Status = $"{L("Gekoppeld", "Paired")}: {response.PairingStatus ?? "paired"}";
        AddDiagnostic("INF Pairing completed.");
    }

    private async Task RefreshAsync()
    {
        if (IsDemoMode)
        {
            Status = L("Demo modus", "Demo Mode");
            Notice = "";
            _backendAvailable = true;
            _runtimeCompatible = true;
            RaisePlaybackStateProperties();
            AddDiagnostic("INF Demo refresh completed.");
            return;
        }

        ConfigureClient();
        StatusResponse response;
        try
        {
            response = await _apiClient.GetStatusAsync(_identity, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (ApplyVersionMismatch(ex))
            {
                Notice = L("Update vereist", "Update required");
                Status = ConnectionStatusText;
                AddDiagnostic("WRN Status refresh blocked by version mismatch.");
                return;
            }

            _backendAvailable = false;
            Notice = L("Home Assistant niet bereikbaar", "Home Assistant is unreachable");
            Status = Notice;
            AddDiagnostic("WRN Status refresh failed: " + ex.GetType().Name);
            RaisePlaybackStateProperties();
            return;
        }

        if (!response.Success)
        {
            if (string.Equals(response.Error, "version_mismatch", StringComparison.OrdinalIgnoreCase))
            {
                ApplyVersionCompatibility(response);
                Notice = L("Update vereist", "Update required");
                Status = ConnectionStatusText;
                AddDiagnostic("WRN Status response reported version mismatch.");
                return;
            }

            _backendAvailable = false;
            Notice = L("Home Assistant niet bereikbaar", "Home Assistant is unreachable");
            Status = Notice;
            AddDiagnostic("WRN Refresh failed.");
            RaisePlaybackStateProperties();
            return;
        }

        _backendAvailable = response.BackendAvailable ?? true;
        ApplyVersionCompatibility(response);
        ApplyPlaybackState(response.Playback);
        ReplaceOutputs(response.ResolvedOutputs());
        ReplaceQueueItems(response.ResolvedQueue());
        ReplacePlaylistItems(response.ResolvedPlaylists());
        Notice = _runtimeCompatible
            ? HasActivePlayback ? "" : L("Geen actieve playback", "No active playback")
            : L("Update vereist", "Update required");
        Status = ConnectionStatusText;
        AddDiagnostic("INF Status refreshed.");
        RaisePlaybackStateProperties();
        await SyncHistoryAsync(showStatus: false);
    }

    private async Task SendAskDJAsync()
    {
        var text = AskDJText.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (!CanUseAskDJ)
        {
            AskDJNotice = !_runtimeCompatible ? L("Update vereist", "Update required") : L("Ask DJ niet bereikbaar", "Ask DJ is unavailable");
            return;
        }

        var clientMessageId = Guid.NewGuid().ToString("N");
        AskDJText = "";
        var localUserMessage = new AskDJMessage(clientMessageId, "user", text, null, DateTimeOffset.Now, "user", null, null, null, ClientMessageId: clientMessageId, IsPending: true);
        MergeMessage(localUserMessage);

        if (IsDemoMode)
        {
            var answer = L(
                "Demo: Ask DJ geeft echte antwoorden zodra Home Assistant gekoppeld is. Ik zou nu muziekcontext, aanbevelingen en acties via je DJConnect-integratie ophalen.",
                "Demo: Ask DJ gives real answers once Home Assistant is paired. I would fetch music context, recommendations and actions through your DJConnect integration.");
            MarkMessageSent(clientMessageId);
            MergeMessage(new AskDJMessage(Guid.NewGuid().ToString("N"), "assistant", answer, null, DateTimeOffset.Now, "assistant", DemoPlaybackActions(), null, null));
            AskDJNotice = "";
            AddDiagnostic("INF Demo Ask DJ message added.");
            return;
        }

        ConfigureClient();
        var request = new AskDJRequest(
            clientMessageId,
            _identity.DeviceId,
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            text,
            text);

        AskDJMessageResponse response;
        try
        {
            response = await _apiClient.SendAskDJMessageAsync(request, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (ApplyVersionMismatch(ex))
            {
                MarkMessageFailed(clientMessageId);
                AskDJNotice = L("Update vereist", "Update required");
                AddDiagnostic("WRN Ask DJ request blocked by version mismatch.");
                return;
            }

            MarkMessageFailed(clientMessageId);
            AskDJNotice = L("Ask DJ niet bereikbaar", "Ask DJ is unavailable");
            AddDiagnostic("WRN Ask DJ request failed: " + ex.GetType().Name);
            return;
        }

        if (!response.Success)
        {
            MarkMessageFailed(clientMessageId);
            AskDJNotice = L("Home Assistant gaf geen antwoord", "Home Assistant did not answer");
            AddDiagnostic("WRN Ask DJ request failed.");
            return;
        }

        MarkMessageSent(clientMessageId);

        if (response.HistoryRevision.HasValue)
        {
            _settings.HistoryRevision = response.HistoryRevision.Value;
        }

        if (response.ClearRevision.HasValue)
        {
            _settings.ClearRevision = response.ClearRevision.Value;
        }

        var responseMessages = response.Messages is { Count: > 0 }
            ? response.Messages.Select(message => EnsureClientMessageId(message, clientMessageId)).ToList()
            : BuildLegacyAskDJResponseMessages(response, clientMessageId);

        foreach (var message in responseMessages)
        {
            MergeMessage(message);
        }

        if (responseMessages.Count == 0 && !string.IsNullOrWhiteSpace(response.Text ?? response.Message))
        {
            MergeMessage(new AskDJMessage(Guid.NewGuid().ToString("N"), "assistant", SafeDisplayText(response.Text ?? response.Message), null, DateTimeOffset.Now, "assistant", response.PlaybackActions, response.ConfirmationActions, response.Items, response.AudioUrl, ClientMessageId: clientMessageId));
        }

        var assistantMessage = responseMessages.LastOrDefault(message => message.IsAssistant);
        ReplaceActions(assistantMessage?.PlaybackActions ?? response.PlaybackActions, assistantMessage?.ConfirmationActions ?? response.ConfirmationActions);
        ReplaceRecentItems(assistantMessage?.Items ?? response.Items);
        await SaveSettingsIfMutableAsync();
        AskDJNotice = "";
        Status = L("Ask DJ bijgewerkt", "Ask DJ updated");
        AddDiagnostic("INF Ask DJ history updated through Home Assistant.");
    }

    private async Task ClearHistoryAsync()
    {
        if (IsDemoMode)
        {
            Messages.Clear();
            LoadDemoAskDJMessages();
            AskDJNotice = "";
            AddDiagnostic("INF Demo Ask DJ history cleared.");
            return;
        }

        ConfigureClient();
        var response = await _apiClient.ClearAskDJHistoryAsync(_identity, CancellationToken.None);
        if (!response.Success)
        {
            AskDJNotice = L("Ask DJ niet bereikbaar", "Ask DJ is unavailable");
            AddDiagnostic("WRN Ask DJ history clear failed.");
            return;
        }

        Messages.Clear();
        Actions.Clear();
        RecentItems.Clear();
        _settings.HistoryRevision = response.HistoryRevision;
        _settings.ClearRevision = response.ClearRevision;
        await SaveSettingsIfMutableAsync();
        AskDJNotice = "";
        Status = L("Ask DJ history gewist", "Ask DJ history cleared");
        AddDiagnostic("INF Ask DJ history clear requested.");
    }

    private async Task SyncHistoryAsync(bool showStatus)
    {
        if (!CanUseAskDJ || IsDemoMode)
        {
            return;
        }

        ConfigureClient();
        AskDJHistoryResponse response;
        try
        {
            response = await _apiClient.GetAskDJHistoryAsync(_settings.HistoryRevision, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (ApplyVersionMismatch(ex))
            {
                if (showStatus)
                {
                    AskDJNotice = L("Update vereist", "Update required");
                }

                AddDiagnostic("WRN Ask DJ history sync blocked by version mismatch.");
                return;
            }

            if (showStatus)
            {
                AskDJNotice = L("Ask DJ niet bereikbaar", "Ask DJ is unavailable");
            }

            AddDiagnostic("WRN Ask DJ history sync failed: " + ex.GetType().Name);
            return;
        }

        if (!response.Success)
        {
            if (showStatus)
            {
                AskDJNotice = L("Ask DJ niet bereikbaar", "Ask DJ is unavailable");
            }

            return;
        }

        if (response.ClearRevision > _settings.ClearRevision)
        {
            Messages.Clear();
            Actions.Clear();
            RecentItems.Clear();
        }

        for (var i = 0; i < response.Messages.Count; i++)
        {
            MergeMessage(response.Messages[i] with { ServerOrder = i });
        }

        PruneMessagesOlderThan(response.HistoryTrimmedBefore);
        SortMessages();

        _settings.HistoryRevision = response.HistoryRevision;
        _settings.ClearRevision = response.ClearRevision;
        await SaveSettingsIfMutableAsync();
    }

    private async Task RunCommandAsync(string command)
    {
        if (IsDemoMode)
        {
            Status = $"{L("Demo command", "Demo command")}: {command}";
            AddDiagnostic("INF Demo command executed: " + command);
            return;
        }

        ConfigureClient();
        CommandResponse response;
        try
        {
            response = await _apiClient.RunCommandAsync(_identity, command, CancellationToken.None);
        }
        catch (Exception ex) when (ApplyVersionMismatch(ex))
        {
            Status = L("Update vereist", "Update required");
            return;
        }

        ApplyVersionCompatibility(response);
        if (!_runtimeCompatible)
        {
            Status = L("Update vereist", "Update required");
            return;
        }

        Status = response.Success
            ? response.DjText ?? response.Message ?? $"{L("Command uitgevoerd", "Command executed")}: {command}"
            : response.Error ?? $"{L("Command mislukt", "Command failed")}: {command}";
        AddDiagnostic(response.Success ? "INF Command executed: " + command : "WRN Command failed: " + command);
        await RefreshAsync();
    }

    private async Task TogglePlaybackAsync()
    {
        if (!CanStartPlayback)
        {
            Notice = !CanUsePlaybackFeatures
                ? L("Playback niet beschikbaar", "Playback unavailable")
                : L("Geen uitvoerapparaat geselecteerd", "No output device selected");
            return;
        }

        await RunPlaybackCommandAsync("toggle_playback");
    }

    public async Task SeekAsync(double positionMs)
    {
        PlaybackPositionMs = positionMs;
        if (!CanUsePlaybackFeatures)
        {
            Notice = L("Playback niet beschikbaar", "Playback unavailable");
            return;
        }

        if (IsDemoMode)
        {
            AddDiagnostic("INF Demo seek updated.");
            return;
        }

        await RunPlaybackCommandAsync("seek", new { position_ms = (int)Math.Round(PlaybackPositionMs) });
    }

    private async Task RunPlaybackCommandAsync(string command, object? args = null)
    {
        if (!CanUsePlaybackFeatures)
        {
            Notice = L("Playback niet beschikbaar", "Playback unavailable");
            return;
        }

        if (IsDemoMode)
        {
            ApplyDemoPlaybackCommand(command, args);
            AddDiagnostic("INF Demo playback command executed: " + command);
            return;
        }

        ConfigureClient();
        CommandResponse response;
        try
        {
            response = await _apiClient.RunCommandAsync(_identity, command, args, CancellationToken.None);
        }
        catch (Exception ex) when (ApplyVersionMismatch(ex))
        {
            Notice = L("Update vereist", "Update required");
            AddDiagnostic("WRN Playback command blocked by version mismatch: " + command);
            return;
        }

        ApplyVersionCompatibility(response);
        if (!_runtimeCompatible)
        {
            Notice = L("Update vereist", "Update required");
            AddDiagnostic("WRN Playback command blocked by incompatible runtime: " + command);
            return;
        }

        if (!response.Success)
        {
            Notice = L("Playback niet beschikbaar", "Playback unavailable");
            AddDiagnostic("WRN Playback command failed: " + command);
            return;
        }

        Notice = "";
        Status = response.DjText ?? response.Message ?? L("Command uitgevoerd", "Command executed");
        AddDiagnostic("INF Playback command executed: " + command);
        await RefreshAsync();
    }

    private async Task RefreshQueueAsync()
    {
        if (!CanUsePlaybackFeatures)
        {
            QueueNotice = !_runtimeCompatible ? L("Update vereist", "Update required") : L("Playback niet beschikbaar", "Playback unavailable");
            return;
        }

        if (IsDemoMode)
        {
            LoadDemoQueueItems(reset: true);
            QueueNotice = QueueItems.Count == 0 ? L("Geen wachtrij", "No queue") : "";
            return;
        }

        IsLoadingQueue = true;
        try
        {
            ConfigureClient();
            var response = await _apiClient.GetStatusAsync(_identity, CancellationToken.None);
            if (!response.Success)
            {
                QueueNotice = L("Home Assistant niet bereikbaar", "Home Assistant is unreachable");
                AddDiagnostic("WRN Queue refresh failed.");
                return;
            }

            _backendAvailable = response.BackendAvailable ?? true;
            ApplyVersionCompatibility(response);
            ReplaceQueueItems(response.ResolvedQueue());
            QueueNotice = !_runtimeCompatible
                ? L("Update vereist", "Update required")
                : QueueItems.Count == 0 ? L("Geen wachtrij", "No queue") : "";
            RaisePlaybackStateProperties();
        }
        catch (Exception ex)
        {
            if (ApplyVersionMismatch(ex))
            {
                QueueNotice = L("Update vereist", "Update required");
                AddDiagnostic("WRN Queue refresh blocked by version mismatch.");
                return;
            }

            QueueNotice = L("Home Assistant niet bereikbaar", "Home Assistant is unreachable");
            AddDiagnostic("WRN Queue refresh failed: " + ex.GetType().Name);
        }
        finally
        {
            IsLoadingQueue = false;
            OnPropertyChanged(nameof(HasNoQueueItems));
        }
    }

    public async Task StartQueueItemAsync(QueueItem item)
    {
        if (!CanUsePlaybackFeatures)
        {
            QueueNotice = !_runtimeCompatible ? L("Update vereist", "Update required") : L("Playback niet beschikbaar", "Playback unavailable");
            return;
        }

        if (SelectedOutput is null)
        {
            QueueNotice = L("Geen uitvoerapparaat geselecteerd", "No output device selected");
            return;
        }

        if (!item.IsPlayable)
        {
            QueueNotice = L("Playback niet beschikbaar", "Playback unavailable");
            return;
        }

        if (IsDemoMode)
        {
            ApplyDemoQueueItem(item);
            QueueNotice = "";
            return;
        }

        if (item.PlaybackAction is not null)
        {
            await ExecutePlaybackActionAsync(item.PlaybackAction);
            await RefreshQueueAsync();
            return;
        }

        var args = new
        {
            item_id = item.StableId,
            uri = item.CommandUri,
            context_uri = item.ContextUri,
            output_id_or_name = SelectedOutput.CommandValue
        };
        await RunPlaybackCommandAsync("queue_item_play", args);
        await RefreshQueueAsync();
    }

    private async Task RefreshPlaylistsAsync()
    {
        if (!CanUsePlaybackFeatures)
        {
            PlaylistNotice = !_runtimeCompatible ? L("Update vereist", "Update required") : L("Playback niet beschikbaar", "Playback unavailable");
            return;
        }

        if (IsDemoMode)
        {
            LoadDemoPlaylists(reset: true);
            PlaylistNotice = FilteredPlaylistItems.Count == 0 ? L("Geen afspeellijsten", "No playlists") : "";
            return;
        }

        IsLoadingPlaylists = true;
        try
        {
            ConfigureClient();
            var response = await _apiClient.GetStatusAsync(_identity, CancellationToken.None);
            if (!response.Success)
            {
                ReplacePlaylistItems([]);
                PlaylistNotice = L("Home Assistant niet bereikbaar", "Home Assistant is unreachable");
                AddDiagnostic("WRN Playlist refresh failed.");
                return;
            }

            _backendAvailable = response.BackendAvailable ?? true;
            ApplyVersionCompatibility(response);
            ReplacePlaylistItems(response.ResolvedPlaylists());
            PlaylistNotice = !_runtimeCompatible
                ? L("Update vereist", "Update required")
                : PlaylistItems.Count == 0 ? L("Geen afspeellijsten", "No playlists") : "";
            RaisePlaybackStateProperties();
        }
        catch (Exception ex)
        {
            if (ApplyVersionMismatch(ex))
            {
                PlaylistNotice = L("Update vereist", "Update required");
                AddDiagnostic("WRN Playlist refresh blocked by version mismatch.");
                return;
            }

            ReplacePlaylistItems([]);
            PlaylistNotice = L("Home Assistant niet bereikbaar", "Home Assistant is unreachable");
            AddDiagnostic("WRN Playlist refresh failed: " + ex.GetType().Name);
        }
        finally
        {
            IsLoadingPlaylists = false;
            OnPropertyChanged(nameof(HasNoPlaylistItems));
        }
    }

    public async Task StartPlaylistAsync(PlaylistItem playlist)
    {
        if (!CanUsePlaybackFeatures)
        {
            PlaylistNotice = !_runtimeCompatible ? L("Update vereist", "Update required") : L("Playback niet beschikbaar", "Playback unavailable");
            return;
        }

        if (SelectedOutput is null)
        {
            PlaylistNotice = L("Geen uitvoerapparaat geselecteerd", "No output device selected");
            return;
        }

        if (!playlist.IsPlayable)
        {
            PlaylistNotice = L("Playback niet beschikbaar", "Playback unavailable");
            return;
        }

        if (IsDemoMode)
        {
            ApplyDemoPlaylist(playlist);
            PlaylistNotice = "";
            return;
        }

        if (playlist.PlaybackAction is not null)
        {
            await ExecutePlaybackActionAsync(playlist.PlaybackAction);
            await RefreshPlaylistsAsync();
            return;
        }

        var args = new
        {
            playlist_id = playlist.StableId,
            uri = playlist.CommandUri,
            context_uri = playlist.ContextUri,
            output_id_or_name = SelectedOutput.CommandValue
        };
        await RunPlaybackCommandAsync("playlist_start", args);
        await RefreshPlaylistsAsync();
    }

    private async Task StartDemoModeAsync()
    {
        Notice = "";
        AskDJNotice = "";
        QueueNotice = "";
        PlaylistNotice = "";
        IsPairingSuccessVisible = false;
        IsPairingOverlayVisible = false;
        IsDemoMode = true;
        _backendAvailable = true;
        _runtimeCompatible = true;
        Status = L("Demo modus", "Demo Mode");
        NowPlaying = "Midnight City - M83";
        ClearDemoState();
        ClearRuntimePlaybackState();
        LoadDemoData();
        LoadDemoAskDJMessages();
        AddDiagnostic("INF Demo mode started.");
        await UpdateMdnsAdvertisingAsync();
    }

    private async Task StopDemoModeAsync()
    {
        if (IsMonkeyTestMode)
        {
            Status = L("Monkeytest: Demo Mode blijft actief", "Monkey test: Demo Mode stays active");
            AddDiagnostic("INF Monkey test suppressed stopping Demo Mode.");
            return;
        }

        IsDemoMode = false;
        ClearDemoState();
        ClearRuntimePlaybackState();
        if (!IsPaired)
        {
            PairingCode = PairingCodeGenerator.CreateCode();
            _settings.PairingCode = PairingCode;
            IsOnboardingVisible = false;
            _settings.DJConnectWelcomeSeen = true;
            _settings.HasCompletedOnboarding = true;
            IsPairingOverlayVisible = true;
            Status = L("Niet gekoppeld", "Not paired");
            ShowPermissionExplanation(PermissionExplanationKind.LocalNetwork);
            await SaveSettingsIfMutableAsync();
        }
        else
        {
            IsPairingOverlayVisible = false;
            Status = L("Gekoppeld", "Paired");
            await RefreshAsync();
        }

        Status = IsPaired ? L("Gekoppeld", "Paired") : L("Niet gekoppeld", "Not paired");
        AddDiagnostic("INF Demo mode stopped.");
        await UpdateMdnsAdvertisingAsync();
    }

    private async Task CompleteOnboardingAsync()
    {
        IsOnboardingVisible = false;
        _settings.DJConnectWelcomeSeen = true;
        _settings.HasCompletedOnboarding = true;
        IsPairingOverlayVisible = !IsPaired && !IsDemoMode;
        if (IsPairingOverlayVisible)
        {
            ShowPermissionExplanation(PermissionExplanationKind.LocalNetwork);
        }
        else
        {
            await EnsureLocalClientApiAsync();
        }

        await SaveSettingsIfMutableAsync();
        await UpdateMdnsAdvertisingAsync();
        AddDiagnostic("INF Onboarding completed; local pairing screen can advertise discovery.");
    }

    private async Task PrepareWhatsNewAsync()
    {
        if (IsDemoMode)
        {
            _settings.LastSeenAppVersion = AppVersion;
            await SaveSettingsIfMutableAsync();
            return;
        }

        if (!_settings.DJConnectWelcomeSeen && !_settings.HasCompletedOnboarding)
        {
            _settings.LastSeenAppVersion = AppVersion;
            await SaveSettingsIfMutableAsync();
            return;
        }

        if (string.Equals(_settings.LastSeenAppVersion, AppVersion, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        IsWhatsNewVisible = true;
        await LoadWhatsNewAsync();
    }

    private async Task LoadWhatsNewAsync()
    {
        IsLoadingWhatsNew = true;
        WhatsNewTitle = L("Wat is er nieuw?", "What's New");
        WhatsNewBody = L("Release notes laden...", "Loading release notes...");
        var language = Language.StartsWith("nl", StringComparison.OrdinalIgnoreCase) ? "nl" : "en";
        var versionTag = $"v{AppVersion}";
        var candidates = new[]
        {
            $"https://djconnect.dev/release-notes/windows/{language}/{versionTag}.json",
            $"https://djconnect.dev/release-notes/windows/en/{versionTag}.json",
            $"https://djconnect.dev/release-notes/windows/{versionTag}.json"
        };

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        foreach (var url in candidates)
        {
            try
            {
                var note = await httpClient.GetFromJsonAsync<ReleaseNoteDocument>(url);
                if (!string.IsNullOrWhiteSpace(note?.Body))
                {
                    WhatsNewTitle = SafeReleaseText(note.Name) ?? $"{L("Wat is er nieuw in", "What's New in")} DJConnect {AppVersion}";
                    WhatsNewBody = NormalizeReleaseMarkdown(note.Body);
                    WhatsNewOnlineUrl = url;
                    AddDiagnostic("INF What's New loaded from djconnect.dev release-notes.");
                    IsLoadingWhatsNew = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                AddDiagnostic("WRN What's New fetch failed: " + ex.GetType().Name);
            }
        }

        WhatsNewTitle = L("Wat is er nieuw?", "What's New");
        WhatsNewBody = L(
            "Release notes konden niet worden geladen. Bekijk https://djconnect.dev voor meer informatie.",
            "Release notes could not be loaded. Visit https://djconnect.dev for more information.");
        WhatsNewOnlineUrl = "https://djconnect.dev";
        IsLoadingWhatsNew = false;
    }

    private async Task DismissWhatsNewAsync()
    {
        _settings.LastSeenAppVersion = AppVersion;
        IsWhatsNewVisible = false;
        await SaveSettingsIfMutableAsync();
    }

    private Task ShowPairingAsync()
    {
        IsPairingOverlayVisible = true;
        IsPairingSuccessVisible = false;
        ShowPermissionExplanation(PermissionExplanationKind.LocalNetwork);
        return UpdateMdnsAdvertisingAsync();
    }

    private Task HidePairingAsync()
    {
        IsPairingOverlayVisible = false;
        IsPairingSuccessVisible = false;
        return UpdateMdnsAdvertisingAsync();
    }

    private async Task CompletePairingSuccessAsync()
    {
        IsPairingSuccessVisible = false;
        IsPairingOverlayVisible = false;
        await UpdateMdnsAdvertisingAsync();
        await RefreshAsync();
        EvaluateWakewordPrompt();
    }

    private async Task ResetPairingAsync()
    {
        if (IsMonkeyTestMode)
        {
            Status = L("Monkeytest: opnieuw koppelen onderdrukt", "Monkey test: pairing reset suppressed");
            AddDiagnostic("INF Monkey test suppressed pairing reset.");
            return;
        }

        _credentialStore.DeleteToken();
        Token = "";
        IsPaired = false;
        IsDemoMode = false;
        _identity = ClientIdentity.CreateOrLoad(ClientIdentity.CreateInstallId(), _settings.DeviceName);
        PairingCode = PairingCodeGenerator.CreateCode();
        _settings.InstallId = _identity.InstallId;
        _settings.PairingCode = PairingCode;
        _settings.HistoryRevision = 0;
        _settings.ClearRevision = 0;
        _backendAvailable = false;
        _runtimeCompatible = true;
        HasActivePlayback = false;
        IsPlaying = false;
        TrackTitle = "";
        TrackArtist = "";
        TrackAlbum = "";
        ArtworkUrl = "";
        PlaybackPositionMs = 0;
        PlaybackDurationMs = 1;
        SelectedOutput = null;
        Messages.Clear();
        Actions.Clear();
        RecentItems.Clear();
        QueueItems.Clear();
        PlaylistItems.Clear();
        FilteredPlaylistItems.Clear();
        OutputDevices.Clear();
        IsOnboardingVisible = false;
        _settings.DJConnectWelcomeSeen = true;
        _settings.HasCompletedOnboarding = true;
        IsPairingOverlayVisible = true;
        ShowPermissionExplanation(PermissionExplanationKind.LocalNetwork);
        await SaveSettingsIfMutableAsync();
        OnPropertyChanged(nameof(DeviceId));
        OnPropertyChanged(nameof(ClientType));
        RaisePlaybackStateProperties();
        RaiseSettingsStatusProperties();
        await UpdateMdnsAdvertisingAsync();
        Status = L("Klaar om opnieuw te koppelen", "Ready to pair again");
        AddDiagnostic("INF Pairing reset: identity and pair code rotated.");
    }

    private async Task CopyLogsAsync()
    {
        if (IsMonkeyTestMode)
        {
            LogNotice = L("Monkeytest: klembord niet gewijzigd", "Monkey test: clipboard unchanged");
            AddDiagnostic("INF Monkey test suppressed log copy.");
            return;
        }

        await Clipboard.Default.SetTextAsync(RedactedDiagnosticExport());
        PermissionNotice = L("Logs gekopieerd met redactie.", "Logs copied with redaction.");
        LogNotice = L("Logs gekopieerd naar klembord", "Logs copied to clipboard");
        AddDiagnostic("INF Diagnostic logs copied with redaction.");
    }

    private async Task ClearLogsAsync()
    {
        if (IsMonkeyTestMode)
        {
            LogNotice = L("Monkeytest: logs niet gewist", "Monkey test: logs not cleared");
            AddDiagnostic("INF Monkey test suppressed log clear.");
            return;
        }

        DiagnosticLogLines.Clear();
        FilteredDiagnosticLogLines.Clear();
        _settings.DiagnosticLogLines.Clear();
        await SaveSettingsIfMutableAsync();
        OnPropertyChanged(nameof(HasDiagnosticLogs));
        OnPropertyChanged(nameof(HasNoDiagnosticLogs));
        OnPropertyChanged(nameof(LogSearchResultCount));
        OnPropertyChanged(nameof(LogSearchResultLabel));
        PermissionNotice = L("Logs gewist.", "Logs cleared.");
        LogNotice = L("Logs gewist", "Logs cleared");
    }

    private Task ToggleLogSearchAsync()
    {
        if (IsLogSearchVisible)
        {
            IsLogSearchVisible = false;
            LogSearchText = "";
            _selectedLogSearchResultIndex = 0;
            ApplyLogFilter();
        }
        else if (DiagnosticLogLines.Count > 0)
        {
            IsLogSearchVisible = true;
        }

        return Task.CompletedTask;
    }

    private Task MoveLogSearchSelectionAsync(int delta)
    {
        var matches = DiagnosticLogLines.Where(entry => entry.IsSearchMatch).ToList();
        if (matches.Count == 0)
        {
            return Task.CompletedTask;
        }

        _selectedLogSearchResultIndex = (_selectedLogSearchResultIndex + delta + matches.Count) % matches.Count;
        ApplyLogFilter();
        return Task.CompletedTask;
    }

    private Task EnableWakewordAsync()
    {
        if (!WakewordFeatureAvailable)
        {
            WakewordNotice = L("Stemactivatie is niet beschikbaar in deze build.", "Voice activation is not available in this build.");
            AddDiagnostic("INF Wakeword enable requested but feature is unavailable.");
            return Task.CompletedTask;
        }

        if (!_settings.PermissionExplanationMicrophoneSeen)
        {
            ShowPermissionExplanation(PermissionExplanationKind.Microphone);
            return Task.CompletedTask;
        }

        WakewordEnabled = true;
        _settings.WakewordPromptDismissed = false;
        IsWakewordPromptVisible = false;
        WakewordNotice = L("Stemactivatie ingeschakeld.", "Voice activation enabled.");
        AddDiagnostic("INF Wakeword foreground listener enabled.");
        return SaveSettingsIfMutableAsync();
    }

    private Task DismissWakewordPromptAsync()
    {
        _settings.WakewordPromptDismissed = true;
        IsWakewordPromptVisible = false;
        WakewordNotice = "";
        AddDiagnostic("INF Wakeword prompt dismissed.");
        return SaveSettingsIfMutableAsync();
    }

    private Task ShowPrivacyFromWakewordAsync()
    {
        WakewordNotice = L("Open Privacy voor details over microfoon en Ask DJ.", "Open Privacy for microphone and Ask DJ details.");
        return Task.CompletedTask;
    }

    private Task ShowFeedbackAsync()
    {
        IsFeedbackOverlayVisible = true;
        FeedbackNotice = "";
        if (string.IsNullOrWhiteSpace(FeedbackPreviewText))
        {
            IsFeedbackPreviewVisible = false;
        }

        AddDiagnostic("INF Feedback prompt opened. No diagnostics uploaded automatically.");
        return Task.CompletedTask;
    }

    private Task HideFeedbackAsync()
    {
        IsFeedbackOverlayVisible = false;
        return Task.CompletedTask;
    }

    private Task PreviewFeedbackAsync()
    {
        FeedbackPreviewText = BuildFeedbackBody();
        IsFeedbackPreviewVisible = true;
        FeedbackNotice = "Preview is privacy-safe geredigeerd. Er is niets verzonden.";
        AddDiagnostic("INF Feedback preview generated with redaction.");
        return Task.CompletedTask;
    }

    private async Task CopyFeedbackAsync()
    {
        if (IsMonkeyTestMode)
        {
            FeedbackNotice = "Monkeytest: klembord niet gewijzigd";
            AddDiagnostic("INF Monkey test suppressed feedback copy.");
            return;
        }

        var body = CurrentFeedbackBody();
        await Clipboard.Default.SetTextAsync(body);
        FeedbackNotice = "Feedback gekopieerd";
        AddDiagnostic("INF Feedback copied to clipboard after redaction.");
    }

    private async Task OpenFeedbackIssueAsync()
    {
        if (IsMonkeyTestMode)
        {
            FeedbackNotice = "Monkeytest: browser niet geopend";
            AddDiagnostic("INF Monkey test suppressed feedback issue launch.");
            return;
        }

        var body = CurrentFeedbackBody();
        var title = $"DJConnect Windows feedback: {SelectedFeedbackType}";
        var url = "https://github.com/pcvantol/djconnect-windows/issues/new"
            + $"?title={Uri.EscapeDataString(title)}"
            + $"&body={Uri.EscapeDataString(body)}";

        await Launcher.Default.OpenAsync(url);
        FeedbackNotice = "GitHub issue geopend. Controleer en verzend zelf.";
        AddDiagnostic("INF Feedback GitHub issue URL opened after redaction.");
    }

    private string CurrentFeedbackBody()
    {
        if (IsFeedbackPreviewVisible && !string.IsNullOrWhiteSpace(FeedbackPreviewText))
        {
            return RedactFeedbackText(FeedbackPreviewText);
        }

        var body = BuildFeedbackBody();
        FeedbackPreviewText = body;
        IsFeedbackPreviewVisible = true;
        return body;
    }

    private string BuildFeedbackBody()
    {
        var description = string.IsNullOrWhiteSpace(FeedbackText)
            ? "<beschrijf wat er gebeurde en wat je verwachtte>"
            : FeedbackText.Trim();

        var body = new StringBuilder();
        body.AppendLine("## Type");
        body.AppendLine(SelectedFeedbackType);
        body.AppendLine();
        body.AppendLine("## Beschrijving");
        body.AppendLine(description);
        body.AppendLine();
        body.AppendLine("## Context");
        body.AppendLine($"- App: DJConnect Windows {AppVersion}");
        body.AppendLine($"- Protocol: {ProtocolVersion}");
        body.AppendLine("- Client type: windows");
        body.AppendLine($"- OS: {FeedbackOsVersion}");
        body.AppendLine($"- Pairing status: {FeedbackPairingStatus}");
        body.AppendLine($"- Demo mode: {IsDemoMode.ToString().ToLowerInvariant()}");
        body.AppendLine($"- Runtime compatible: {_runtimeCompatible.ToString().ToLowerInvariant()}");
        body.AppendLine($"- Backend available: {_backendAvailable.ToString().ToLowerInvariant()}");
        body.AppendLine("- Device class: desktop");

        if (IncludePrivacySafeLogs)
        {
            body.AppendLine();
            body.AppendLine("## Diagnostics");
            body.AppendLine("Redaction: tokens, pairing codes, private URLs and secrets are redacted before preview/copy/open.");
            body.AppendLine();
            foreach (var line in DiagnosticLogLines.TakeLast(80))
            {
                body.AppendLine(line.ExportText);
            }
        }

        return RedactFeedbackText(body.ToString().TrimEnd());
    }

    private void ResetFeedbackPreview()
    {
        if (IsFeedbackPreviewVisible)
        {
            IsFeedbackPreviewVisible = false;
        }

        FeedbackNotice = "";
    }

    private Task PreviewCrashReportAsync()
    {
        CrashReportPreviewText = BuildCrashReportBody();
        IsCrashReportPreviewVisible = true;
        CrashReportNotice = "Preview is privacy-safe geredigeerd. Er is niets verzonden.";
        AddDiagnostic("INF Crash report preview generated with redaction.");
        return Task.CompletedTask;
    }

    private async Task CopyCrashReportAsync()
    {
        if (IsMonkeyTestMode)
        {
            CrashReportNotice = "Monkeytest: klembord niet gewijzigd";
            AddDiagnostic("INF Monkey test suppressed crash report copy.");
            return;
        }

        var body = CurrentCrashReportBody();
        await Clipboard.Default.SetTextAsync(body);
        CrashReportNotice = "Crashrapport gekopieerd";
        AddDiagnostic("INF Crash report copied to clipboard after redaction.");
        await DismissCrashReportAsync();
    }

    private async Task OpenCrashReportIssueAsync()
    {
        if (IsMonkeyTestMode)
        {
            CrashReportNotice = "Monkeytest: browser niet geopend";
            AddDiagnostic("INF Monkey test suppressed crash report issue launch.");
            return;
        }

        var body = CurrentCrashReportBody();
        var url = "https://github.com/pcvantol/djconnect-windows/issues/new"
            + $"?title={Uri.EscapeDataString("DJConnect Windows crash report")}"
            + $"&body={Uri.EscapeDataString(body)}";

        await Launcher.Default.OpenAsync(url);
        AddDiagnostic("INF Crash report GitHub issue URL opened after redaction.");
        await DismissCrashReportAsync();
    }

    private async Task DismissCrashReportAsync()
    {
        _isCrashReportPending = false;
        _settings.CrashPromptPending = false;
        IsCrashReportPreviewVisible = false;
        CrashReportNotice = "";
        OnPropertyChanged(nameof(IsCrashReportPromptVisible));
        await SaveSettingsIfMutableAsync();
    }

    public async Task MarkCleanShutdownAsync()
    {
        _settings.CleanShutdown = true;
        _settings.CrashPromptPending = false;
        await SaveSettingsIfMutableAsync();
    }

    private string CurrentCrashReportBody()
    {
        if (IsCrashReportPreviewVisible && !string.IsNullOrWhiteSpace(CrashReportPreviewText))
        {
            return RedactFeedbackText(CrashReportPreviewText);
        }

        var body = BuildCrashReportBody();
        CrashReportPreviewText = body;
        IsCrashReportPreviewVisible = true;
        return body;
    }

    private string BuildCrashReportBody()
    {
        var body = new StringBuilder();
        body.AppendLine("## Crash report");
        body.AppendLine();
        body.AppendLine("DJConnect Windows detected that the previous session may not have closed cleanly.");
        body.AppendLine();
        body.AppendLine("## Context");
        body.AppendLine($"- App version: {AppVersion}");
        body.AppendLine($"- Protocol version: {ProtocolVersion}");
        body.AppendLine("- Client type: windows");
        body.AppendLine($"- OS: {FeedbackOsVersion}");
        body.AppendLine($"- Pairing status: {FeedbackPairingStatus}");
        body.AppendLine($"- Demo mode: {IsDemoMode.ToString().ToLowerInvariant()}");
        body.AppendLine($"- Runtime compatible: {_runtimeCompatible.ToString().ToLowerInvariant()}");
        body.AppendLine($"- Backend available: {_backendAvailable.ToString().ToLowerInvariant()}");
        body.AppendLine();
        body.AppendLine("## Recent diagnostics");
        var recentLogs = DiagnosticLogLines.TakeLast(80).ToList();
        if (recentLogs.Count == 0)
        {
            body.AppendLine("No recent diagnostics were available.");
        }
        else
        {
            foreach (var line in recentLogs)
            {
                body.AppendLine(line.ExportText);
            }
        }

        return RedactFeedbackText(body.ToString().TrimEnd());
    }

    private async Task EnsureLocalClientApiAsync()
    {
        if (IsMonkeyTestMode)
        {
            AddDiagnostic("INF Monkey test suppressed local Client API start.");
            return;
        }

        if (_localClientApi is null)
        {
            _localClientApi = new LocalClientApiService(CreateLocalPairingSnapshot, HandleLocalPairAsync, AddDiagnostic);
        }

        if (!_localClientApi.IsRunning)
        {
            await _localClientApi.StartAsync();
            OnPropertyChanged(nameof(ClientAddress));
            OnPropertyChanged(nameof(CanCopyClientAddress));
        }
    }

    private LocalPairingSnapshot CreateLocalPairingSnapshot()
    {
        return new LocalPairingSnapshot(
            _identity,
            PairingCode,
            IsPaired ? "paired" : IsPairingOverlayVisible ? "pairing" : "unpaired",
            _localClientApi?.LocalUrl ?? "",
            IsPairable,
            DJConnectContract.ProtocolLine,
            AppVersion);
    }

    private async Task<LocalPairResponse> HandleLocalPairAsync(LocalPairRequest request)
    {
        if (IsMonkeyTestMode)
        {
            AddDiagnostic("INF Monkey test rejected local pair request.");
            return new LocalPairResponse(false, "monkey_test_mode", "Pairing is disabled during monkey tests.");
        }

        if (request.DeviceId != _identity.DeviceId)
        {
            AddDiagnostic("WRN Local pair rejected: wrong device_id.");
            return new LocalPairResponse(false, "wrong_device_id", "Pair request is for a different DJConnect device.");
        }

        if (!string.Equals(request.ClientType, _identity.ClientType, StringComparison.OrdinalIgnoreCase))
        {
            AddDiagnostic("WRN Local pair rejected: wrong client_type.");
            return new LocalPairResponse(false, "wrong_client_type", "Pair request is for a different DJConnect client type.");
        }

        if (request.ResolvedPairCode != PairingCode)
        {
            AddDiagnostic("WRN Local pair rejected: pair code mismatch.");
            return new LocalPairResponse(false, "pair_code_mismatch", "Pairing code does not match this app.");
        }

        var token = request.ResolvedDeviceToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            AddDiagnostic("WRN Local pair rejected: missing device token.");
            return new LocalPairResponse(false, "missing_token", "Pair request did not include a device token.");
        }

        try
        {
            _credentialStore.SaveToken(token);
        }
        catch (Exception ex)
        {
            AddDiagnostic("WRN Local pair token storage failed: " + ex.GetType().Name);
            return new LocalPairResponse(false, "token_storage_failed", "Token storage failed.");
        }

        Token = token;
        IsPaired = true;
        IsPairingOverlayVisible = true;
        IsPairingSuccessVisible = true;
        IsOnboardingVisible = false;
        if (!string.IsNullOrWhiteSpace(request.HomeAssistantLocalUrl))
        {
            HomeAssistantUrl = request.HomeAssistantLocalUrl;
        }

        _settings.HomeAssistantUrl = HomeAssistantUrl;
        _settings.InstallId = _identity.InstallId;
        _settings.PairingCode = "";
        _settings.DJConnectWelcomeSeen = true;
        _settings.HasCompletedOnboarding = true;
        await SaveSettingsIfMutableAsync();
        ConfigureClient();
        await UpdateMdnsAdvertisingAsync();
        Status = L("Gekoppeld met Home Assistant", "Paired with Home Assistant");
        AddDiagnostic("INF Local Client API completed pairing from Home Assistant.");
        return new LocalPairResponse(true, Message: "paired", DeviceId: _identity.DeviceId, ClientType: _identity.ClientType, Paired: true);
    }

    private async Task UpdateMdnsAdvertisingAsync()
    {
        OnPropertyChanged(nameof(ShouldAdvertiseMdns));
        if (IsMonkeyTestMode)
        {
            await _mdnsAdvertiser.StopAsync();
            return;
        }

        if (ShouldAdvertiseMdns)
        {
            await _mdnsAdvertiser.StartOrUpdateAsync(CreateLocalPairingSnapshot());
            return;
        }

        await _mdnsAdvertiser.StopAsync();
    }

    private void ConfigureClient()
    {
        _apiClient.Configure(HomeAssistantUrl, Token);
    }

    private void ApplyVersionCompatibility(StatusResponse response)
    {
        var result = VersionCompatibility.Evaluate(
            DJConnectContract.ProtocolLine,
            response.HaVersion ?? response.BackendVersion,
            response.HaMajorMinor,
            response.UpdateRequired == true || response.RuntimeCompatible == false || response.Compatible == false,
            response.Error);
        ApplyVersionCompatibilityResult(result);
    }

    private void ApplyVersionCompatibility(CommandResponse response)
    {
        var result = VersionCompatibility.Evaluate(
            DJConnectContract.ProtocolLine,
            response.HaVersion ?? response.BackendVersion,
            response.HaMajorMinor,
            response.UpdateRequired == true,
            response.Error);
        ApplyVersionCompatibilityResult(result);
    }

    private void ApplyVersionCompatibilityResult(VersionCompatibilityResult result)
    {
        HomeAssistantVersionText = string.IsNullOrWhiteSpace(result.HomeAssistantMajorMinor)
            ? L("Home Assistant integration: onbekend", "Home Assistant integration: unknown")
            : $"Home Assistant integration: {result.HomeAssistantMajorMinor}.x";

        if (result.IsCompatible)
        {
            UpdateRequiredMessage = "";
            _runtimeCompatible = true;
            RaisePlaybackStateProperties();
            return;
        }

        UpdateRequiredMessage = L(
            $"Update DJConnect voordat je verdergaat. Deze app vereist Home Assistant DJConnect {result.RequiredMajorMinor}.x.",
            $"Update DJConnect before continuing. This app requires Home Assistant DJConnect {result.RequiredMajorMinor}.x.");
        IsWhatsNewVisible = false;
        _runtimeCompatible = false;
        RaisePlaybackStateProperties();
    }

    private bool ApplyVersionMismatch(Exception ex)
    {
        if (ex is not DJConnectVersionMismatchException mismatch)
        {
            return false;
        }

        var result = VersionCompatibility.Evaluate(
            DJConnectContract.ProtocolLine,
            mismatch.HaVersion,
            mismatch.HaMajorMinor,
            updateRequired: true,
            mismatch.Error ?? "version_mismatch");
        ApplyVersionCompatibilityResult(result);
        _backendAvailable = true;
        Status = ConnectionStatusText;
        return true;
    }

    private async Task RetryVersionCheckAsync()
    {
        IsRefreshingVersionCheck = true;
        try
        {
            await RefreshAsync();
            if (!IsUpdateRequired)
            {
                Notice = "";
                QueueNotice = "";
                AskDJNotice = "";
            }
        }
        catch
        {
            Notice = L("Versiecontrole mislukt", "Version check failed");
        }
        finally
        {
            IsRefreshingVersionCheck = false;
        }
    }

    private void ReplaceActions(IReadOnlyList<PlaybackAction>? playbackActions, IReadOnlyList<PlaybackAction>? confirmationActions)
    {
        Actions.Clear();
        foreach (var action in (playbackActions ?? []).Concat(confirmationActions ?? []))
        {
            Actions.Add(action);
        }
    }

    public async Task ExecutePlaybackActionAsync(PlaybackAction action)
    {
        if (!CanUseAskDJ)
        {
            AskDJNotice = !_runtimeCompatible ? L("Update vereist", "Update required") : L("Ask DJ niet bereikbaar", "Ask DJ is unavailable");
            return;
        }

        if (IsDemoMode)
        {
            MergeMessage(new AskDJMessage(Guid.NewGuid().ToString("N"), "assistant", $"{action.DisplayLabel}: demo actie uitgevoerd.", null, DateTimeOffset.Now, "assistant", null, null, null));
            AddDiagnostic("INF Demo Ask DJ action executed.");
            return;
        }

        ConfigureClient();
        var response = await _apiClient.RunPlaybackActionAsync(_identity, action, CancellationToken.None);
        if (!response.Success)
        {
            AskDJNotice = L("Ask DJ niet bereikbaar", "Ask DJ is unavailable");
            AddDiagnostic("WRN Ask DJ action failed.");
            return;
        }

        AskDJNotice = "";
        Status = response.DjText ?? response.Message ?? L("Command uitgevoerd", "Command executed");
        await RefreshAsync();
    }

    private Task TogglePushToTalkAsync()
    {
        if (IsDemoMode)
        {
            VoiceStatus = L("Demo: microfoon niet gebruikt. Ask DJ zou nu luisteren via Home Assistant.", "Demo: microphone not used. Ask DJ would listen through Home Assistant.");
            MergeMessage(new AskDJMessage(Guid.NewGuid().ToString("N"), "assistant", "Demo Mode: voice requests work after pairing Home Assistant.", null, DateTimeOffset.Now, "assistant", DemoPlaybackActions(), null, null));
            AddDiagnostic("INF Demo push-to-talk simulated locally.");
            return Task.CompletedTask;
        }

        if (!_settings.PermissionExplanationMicrophoneSeen)
        {
            ShowPermissionExplanation(PermissionExplanationKind.Microphone);
            return Task.CompletedTask;
        }

        VoiceStatus = L("Microfoon niet beschikbaar", "Microphone unavailable");
        AddDiagnostic("INF Push-to-talk requested but no Windows capture backend is implemented yet.");
        return Task.CompletedTask;
    }

    private Task EnableNotificationsAsync()
    {
        ShowPermissionExplanation(PermissionExplanationKind.Notifications);
        return Task.CompletedTask;
    }

    private async Task ContinuePermissionExplanationAsync()
    {
        if (ActivePermissionMode == PermissionExplanationMode.Settings)
        {
            await OpenPermissionSettingsAsync();
            return;
        }

        var permission = ActivePermissionKind;
        MarkPermissionExplanationSeen(permission);
        IsPermissionExplanationVisible = false;

        switch (permission)
        {
            case PermissionExplanationKind.Microphone:
                VoiceStatus = L("Microfoon niet beschikbaar", "Microphone unavailable");
                AddDiagnostic("INF Microphone explanation accepted; capture backend is not implemented yet.");
                break;
            case PermissionExplanationKind.Notifications:
                PermissionNotice = L(
                    "Windows toast-meldingen zijn nog niet actief in deze build.",
                    "Windows toast notifications are not active in this build yet.");
                AddDiagnostic("INF Notification explanation accepted; toast backend is not implemented yet.");
                break;
            case PermissionExplanationKind.LocalNetwork:
                await EnsureLocalClientApiAsync();
                await UpdateMdnsAdvertisingAsync();
                AddDiagnostic("INF Local network explanation accepted; local Client API can run for pairing.");
                break;
        }
    }

    private Task HidePermissionExplanationAsync()
    {
        var permission = ActivePermissionKind;
        IsPermissionExplanationVisible = false;
        if (permission == PermissionExplanationKind.Microphone)
        {
            VoiceStatus = L("Microfoon niet beschikbaar", "Microphone unavailable");
        }
        else if (permission == PermissionExplanationKind.Notifications)
        {
            PermissionNotice = L("Notificaties blijven uitgeschakeld.", "Notifications remain disabled.");
        }

        AddDiagnostic("INF Permission explanation dismissed: " + permission);
        return Task.CompletedTask;
    }

    private async Task OpenPermissionSettingsAsync()
    {
        MarkPermissionExplanationSeen(ActivePermissionKind);
        IsPermissionExplanationVisible = false;
        AddDiagnostic("INF Windows settings requested for permission: " + ActivePermissionKind);
        await Task.CompletedTask;
    }

    private Task CopyClientAddressAsync()
    {
        PermissionNotice = string.IsNullOrWhiteSpace(_localClientApi?.LocalUrl)
            ? L("Client adres is nog niet beschikbaar.", "Client address is not available yet.")
            : L("Client adres is beschikbaar om te kopiëren.", "Client address is available to copy.");
        return Task.CompletedTask;
    }

    public void MarkPermissionDenied(PermissionExplanationKind permission)
    {
        ActivePermissionKind = permission;
        ActivePermissionMode = PermissionExplanationMode.Settings;
        IsPermissionExplanationVisible = true;
        MarkPermissionExplanationSeen(permission);
    }

    private void ShowPermissionExplanation(PermissionExplanationKind permission, PermissionExplanationMode mode = PermissionExplanationMode.Request)
    {
        if (IsMonkeyTestMode)
        {
            PermissionNotice = L("Monkeytest: permissieprompt onderdrukt", "Monkey test: permission prompt suppressed");
            AddDiagnostic("INF Monkey test suppressed permission explanation: " + permission);
            return;
        }

        if (permission == PermissionExplanationKind.LocalNetwork
            && _settings.PermissionExplanationLocalNetworkSeen
            && mode == PermissionExplanationMode.Request)
        {
            _ = EnsureLocalClientApiAsync();
            return;
        }

        ActivePermissionKind = permission;
        ActivePermissionMode = mode;
        IsPermissionExplanationVisible = true;
        PermissionNotice = "";
        AddDiagnostic("INF Permission explanation shown: " + permission);
    }

    private void MarkPermissionExplanationSeen(PermissionExplanationKind permission)
    {
        switch (permission)
        {
            case PermissionExplanationKind.Microphone:
                _settings.PermissionExplanationMicrophoneSeen = true;
                break;
            case PermissionExplanationKind.Notifications:
                _settings.PermissionExplanationNotificationsSeen = true;
                break;
            case PermissionExplanationKind.LocalNetwork:
                _settings.PermissionExplanationLocalNetworkSeen = true;
                break;
        }

        _ = SaveSettingsIfMutableAsync();
        RaisePermissionStatusProperties();
    }

    private void MergeMessage(AskDJMessage message)
    {
        var safe = message with { Text = SafeDisplayText(message.Text), Message = SafeDisplayText(message.Message) };
        for (var i = 0; i < Messages.Count; i++)
        {
            if (IsSameAskDJMessage(Messages[i], safe))
            {
                Messages[i] = MergeAskDJMessage(Messages[i], safe);
                SortMessages();
                return;
            }
        }

        Messages.Add(safe);
        SortMessages();
    }

    private void MarkMessageSent(string id)
    {
        ReplaceLocalMessage(id, message => message with { IsPending = false, IsFailed = false });
    }

    private void MarkMessageFailed(string id)
    {
        ReplaceLocalMessage(id, message => message with { IsPending = false, IsFailed = true });
    }

    private void ReplaceLocalMessage(string id, Func<AskDJMessage, AskDJMessage> update)
    {
        for (var i = 0; i < Messages.Count; i++)
        {
            if (Messages[i].Id == id)
            {
                Messages[i] = update(Messages[i]);
                return;
            }
        }
    }

    private void PruneMessagesOlderThan(DateTimeOffset? trimmedBefore)
    {
        if (trimmedBefore is null)
        {
            return;
        }

        for (var i = Messages.Count - 1; i >= 0; i--)
        {
            if (Messages[i].CreatedAt < trimmedBefore && !Messages[i].IsPending)
            {
                Messages.RemoveAt(i);
            }
        }
    }

    private void SortMessages()
    {
        var sorted = Messages
            .Select((message, index) => new { message, index })
            .OrderBy(item => MessageHistoryOrder(item.message, item.index))
            .ThenBy(item => ExchangeAnchorOrder(item.message, item.index))
            .ThenBy(item => ExchangeOrder(item.message))
            .ThenBy(item => item.message.CreatedAt ?? DateTimeOffset.MaxValue)
            .ThenBy(item => item.index)
            .Select(item => item.message)
            .ToList();

        for (var i = 0; i < sorted.Count; i++)
        {
            if (!ReferenceEquals(Messages[i], sorted[i]))
            {
                Messages.Move(Messages.IndexOf(sorted[i]), i);
            }
        }
    }

    private static IReadOnlyList<AskDJMessage> BuildLegacyAskDJResponseMessages(AskDJMessageResponse response, string clientMessageId)
    {
        var messages = new List<AskDJMessage>(2);
        if (response.UserMessage is not null)
        {
            messages.Add(EnsureClientMessageId(response.UserMessage, clientMessageId));
        }

        if (response.AssistantMessage is not null)
        {
            messages.Add(EnsureClientMessageId(response.AssistantMessage, clientMessageId));
        }

        return messages;
    }

    private static AskDJMessage EnsureClientMessageId(AskDJMessage message, string clientMessageId)
    {
        return string.IsNullOrWhiteSpace(message.ClientMessageId)
            ? message with { ClientMessageId = clientMessageId }
            : message;
    }

    private static bool IsSameAskDJMessage(AskDJMessage existing, AskDJMessage incoming)
    {
        if (!string.IsNullOrWhiteSpace(existing.Id)
            && !string.IsNullOrWhiteSpace(incoming.Id)
            && string.Equals(existing.Id, incoming.Id, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var existingStableKey = AskDJMessage.StableMessageKey(existing);
        var incomingStableKey = AskDJMessage.StableMessageKey(incoming);
        if (!string.IsNullOrWhiteSpace(existingStableKey)
            && !string.IsNullOrWhiteSpace(incomingStableKey)
            && string.Equals(existingStableKey, incomingStableKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(existing.ClientMessageId)
            && !string.IsNullOrWhiteSpace(incoming.ClientMessageId)
            && string.Equals(existing.ClientMessageId, incoming.ClientMessageId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(AskDJMessage.NormalizeRole(existing.Role), AskDJMessage.NormalizeRole(incoming.Role), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return existing.IsPending
            && existing.IsUser
            && incoming.IsUser
            && string.Equals(existing.Id, incoming.ClientMessageId, StringComparison.OrdinalIgnoreCase);
    }

    private static AskDJMessage MergeAskDJMessage(AskDJMessage existing, AskDJMessage incoming)
    {
        return incoming with
        {
            ServerOrder = incoming.ServerOrder ?? existing.ServerOrder,
            IsPending = incoming.IsPending,
            IsFailed = incoming.IsFailed
        };
    }

    private long MessageHistoryOrder(AskDJMessage message, int fallbackIndex)
    {
        if (!string.IsNullOrWhiteSpace(message.ExchangeId))
        {
            var exchangeOrders = Messages
                .Where(candidate => string.Equals(candidate.ExchangeId, message.ExchangeId, StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.HistoryRevision ?? (candidate.ServerOrder.HasValue ? candidate.ServerOrder.Value : long.MaxValue))
                .Where(order => order != long.MaxValue)
                .ToList();

            if (exchangeOrders.Count > 0)
            {
                return exchangeOrders.Min();
            }
        }

        return message.HistoryRevision ?? message.ServerOrder ?? (long)fallbackIndex;
    }

    private long ExchangeAnchorOrder(AskDJMessage message, int fallbackIndex)
    {
        if (string.IsNullOrWhiteSpace(message.ExchangeId))
        {
            return fallbackIndex;
        }

        return Messages
            .Where(candidate => string.Equals(candidate.ExchangeId, message.ExchangeId, StringComparison.OrdinalIgnoreCase))
            .Select(candidate => candidate.ServerOrder ?? fallbackIndex)
            .DefaultIfEmpty(fallbackIndex)
            .Min();
    }

    private static int ExchangeOrder(AskDJMessage message)
    {
        if (message.ExchangeOrder.HasValue)
        {
            return message.ExchangeOrder.Value;
        }

        return message.IsUser ? 0 : 1;
    }

    private static string? SafeDisplayText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        if (text.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || text.Contains("<script", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Traceback", StringComparison.OrdinalIgnoreCase)
            || text.Contains("JSONDecodeError", StringComparison.OrdinalIgnoreCase)
            || text.Contains("proxy error", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return "Home Assistant gaf geen antwoord";
        }

        return text.Replace("<", "").Replace(">", "");
    }

    private static string NormalizeReleaseMarkdown(string? text)
    {
        var safe = SafeReleaseText(text);
        if (string.IsNullOrWhiteSpace(safe))
        {
            return "";
        }

        var lines = safe
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => !line.Contains("<script", StringComparison.OrdinalIgnoreCase)
                && !line.Contains("<html", StringComparison.OrdinalIgnoreCase))
            .Select(line =>
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("### ", StringComparison.Ordinal))
                {
                    return trimmed[4..].ToUpperInvariant();
                }

                if (trimmed.StartsWith("## ", StringComparison.Ordinal))
                {
                    return trimmed[3..].ToUpperInvariant();
                }

                if (trimmed.StartsWith("# ", StringComparison.Ordinal))
                {
                    return trimmed[2..].ToUpperInvariant();
                }

                return line.Replace("**", "").Replace("__", "").Replace("*", "");
            });

        return string.Join('\n', lines).Trim();
    }

    private static string? SafeReleaseText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text
            .Replace("<", "")
            .Replace(">", "")
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<PlaybackAction> DemoPlaybackActions()
    {
        return
        [
            new PlaybackAction("demo-play-now", "playback", "ask_dj_play_recommendation", "Play Now", "Play Now", "Demo mix", null, null, null, null, null),
            new PlaybackAction("demo-yes", "confirmation", "ask_dj_followup_response", "Ja", "Ja", null, null, null, null, "yes", "confirmation", "yes"),
            new PlaybackAction("demo-no", "confirmation", "ask_dj_followup_response", "Nee", "Nee", null, null, null, null, "no", "confirmation", "no")
        ];
    }

    private void LoadDemoAskDJMessages()
    {
        MergeMessage(new AskDJMessage("demo-system", "assistant", "Demo mode is lokaal. Koppel Home Assistant voor echte Ask DJ antwoorden, audio en acties.", null, DateTimeOffset.Now.AddSeconds(-2), "system", null, null, null, Origin: "history_retention"));
        MergeMessage(new AskDJMessage("demo-assistant", "assistant", "Vraag om muziek, context of een gesproken antwoord. Ik toon hier hoe acties en antwoorden eruitzien.", null, DateTimeOffset.Now.AddSeconds(-1), "assistant", DemoPlaybackActions(), null, null));
    }

    private void ReplaceRecentItems(IReadOnlyList<RecentItem>? items)
    {
        RecentItems.Clear();
        foreach (var item in items ?? [])
        {
            RecentItems.Add(item);
        }
    }

    private void ApplyPlaybackState(PlaybackState? playback)
    {
        HasActivePlayback = playback is not null && !string.IsNullOrWhiteSpace(playback.Title ?? playback.Artist);
        TrackTitle = playback?.Title ?? "";
        TrackArtist = playback?.Artist ?? "";
        TrackAlbum = playback?.Album ?? "";
        ArtworkUrl = playback?.ImageUrl ?? playback?.AlbumArtworkUrl ?? playback?.ArtworkUrl ?? "";
        IsPlaying = playback?.IsPlaying == true;
        PlaybackPositionMs = playback?.PositionMs ?? playback?.ProgressMs ?? 0;
        PlaybackDurationMs = playback?.DurationMs ?? 1;
        _suppressVolumeCommand = true;
        VolumePercent = playback?.VolumePercent ?? VolumePercent;
        _suppressVolumeCommand = false;
        NowPlaying = HasActivePlayback
            ? $"{TrackTitle} - {TrackArtist}".Trim(' ', '-')
            : L("Geen actieve playback", "No active playback");

        var activeOutput = playback?.OutputDevice ?? playback?.ActiveOutput;
        if (activeOutput is not null)
        {
            _suppressOutputCommand = true;
            SelectedOutput = MatchOutput(activeOutput) ?? activeOutput;
            _suppressOutputCommand = false;
        }
        else if (!string.IsNullOrWhiteSpace(playback?.ActiveOutputDevice))
        {
            _suppressOutputCommand = true;
            SelectedOutput = OutputDevices.FirstOrDefault(output => string.Equals(output.DisplayName, playback.ActiveOutputDevice, StringComparison.OrdinalIgnoreCase))
                ?? new PlaybackOutput(playback.ActiveOutputDevice, playback.ActiveOutputDevice, playback.ActiveOutputDevice, true);
            _suppressOutputCommand = false;
        }
    }

    private void ReplaceOutputs(IReadOnlyList<PlaybackOutput>? outputs)
    {
        OutputDevices.Clear();
        foreach (var output in outputs ?? [])
        {
            if (!string.IsNullOrWhiteSpace(output.DisplayName))
            {
                OutputDevices.Add(output);
            }
        }

        _suppressOutputCommand = true;
        SelectedOutput = OutputDevices.FirstOrDefault(output => output.IsActive == true)
            ?? MatchOutput(SelectedOutput);
        _suppressOutputCommand = false;
        OnPropertyChanged(nameof(SelectedOutputText));
    }

    private void ReplaceQueueItems(IReadOnlyList<QueueItem>? items)
    {
        QueueItems.Clear();
        foreach (var item in QueueItemNormalizer.Normalize(items))
        {
            QueueItems.Add(item);
        }

        OnPropertyChanged(nameof(HasQueueItems));
        OnPropertyChanged(nameof(HasNoQueueItems));
    }

    private void ReplacePlaylistItems(IReadOnlyList<PlaylistItem>? items)
    {
        PlaylistItems.Clear();
        foreach (var item in PlaylistItemNormalizer.Normalize(items))
        {
            PlaylistItems.Add(item);
        }

        ApplyPlaylistFilter();
    }

    private void ApplyPlaylistFilter()
    {
        var query = PlaylistSearchText.Trim();
        FilteredPlaylistItems.Clear();
        foreach (var item in PlaylistItems)
        {
            if (string.IsNullOrWhiteSpace(query)
                || item.DisplayTitle.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.DisplaySubtitle.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                FilteredPlaylistItems.Add(item);
            }
        }

        OnPropertyChanged(nameof(HasPlaylistItems));
        OnPropertyChanged(nameof(HasNoPlaylistItems));
    }

    private PlaybackOutput? MatchOutput(PlaybackOutput? output)
    {
        if (output is null)
        {
            return null;
        }

        return OutputDevices.FirstOrDefault(candidate =>
            string.Equals(candidate.CommandValue, output.CommandValue, StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate.DisplayName, output.DisplayName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SelectOutputAsync(PlaybackOutput? output)
    {
        if (output is null || string.IsNullOrWhiteSpace(output.CommandValue))
        {
            Notice = L("Geen uitvoerapparaat geselecteerd", "No output device selected");
            return;
        }

        if (!CanUsePlaybackFeatures)
        {
            return;
        }

        if (IsDemoMode)
        {
            Notice = "";
            return;
        }

        await RunPlaybackCommandAsync("select_output", new { output_id_or_name = output.CommandValue });
    }

    private void QueueVolumeCommand()
    {
        _volumeDebounce?.Cancel();
        _volumeDebounce = new CancellationTokenSource();
        var token = _volumeDebounce.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, token);
                if (!token.IsCancellationRequested)
                {
                    await RunPlaybackCommandAsync("volume", new { volume_percent = (int)Math.Round(VolumePercent) });
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private void ApplyDemoPlaybackCommand(string command, object? args)
    {
        Notice = "";
        switch (command)
        {
            case "toggle_playback":
                IsPlaying = !IsPlaying;
                break;
            case "next_track":
                TrackTitle = "Sweet Disposition";
                TrackArtist = "The Temper Trap";
                TrackAlbum = "Conditions";
                PlaybackPositionMs = 0;
                IsPlaying = true;
                break;
            case "previous_track":
                TrackTitle = "Midnight City";
                TrackArtist = "M83";
                TrackAlbum = "Hurry Up, We're Dreaming";
                PlaybackPositionMs = 0;
                IsPlaying = true;
                break;
        }

        NowPlaying = $"{TrackTitle} - {TrackArtist}";
        HasActivePlayback = true;
        Status = L("Demo modus", "Demo Mode");
    }

    private void ClearRuntimePlaybackState()
    {
        HasActivePlayback = false;
        IsPlaying = false;
        TrackTitle = "";
        TrackArtist = "";
        TrackAlbum = "";
        ArtworkUrl = "";
        PlaybackPositionMs = 0;
        PlaybackDurationMs = 1;
        NowPlaying = L("Geen actieve playback", "No active playback");
        _suppressOutputCommand = true;
        SelectedOutput = null;
        _suppressOutputCommand = false;
        RaisePlaybackStateProperties();
    }

    private void ClearDemoState()
    {
        Messages.Clear();
        Actions.Clear();
        RecentItems.Clear();
        QueueItems.Clear();
        PlaylistItems.Clear();
        FilteredPlaylistItems.Clear();
        OutputDevices.Clear();
        AskDJNotice = "";
        QueueNotice = "";
        PlaylistNotice = "";
        Notice = "";
    }

    private void ApplyDemoQueueItem(QueueItem item)
    {
        TrackTitle = item.DisplayTitleValue;
        TrackArtist = item.DisplaySubtitle;
        TrackAlbum = item.DisplayAlbum;
        ArtworkUrl = item.Artwork;
        PlaybackPositionMs = 0;
        PlaybackDurationMs = item.DurationMs ?? 210_000;
        IsPlaying = true;
        HasActivePlayback = true;
        NowPlaying = $"{TrackTitle} - {TrackArtist}".Trim(' ', '-');
        Status = L("Demo modus", "Demo Mode");
        AddDiagnostic("INF Demo queue item started.");
    }

    private void ApplyDemoPlaylist(PlaylistItem playlist)
    {
        TrackTitle = playlist.DisplayTitle;
        TrackArtist = string.IsNullOrWhiteSpace(playlist.DisplaySubtitle) ? "DJConnect demo" : playlist.DisplaySubtitle;
        TrackAlbum = "Demo playlist";
        ArtworkUrl = playlist.Artwork;
        PlaybackPositionMs = 0;
        PlaybackDurationMs = 210_000;
        IsPlaying = true;
        HasActivePlayback = true;
        NowPlaying = $"{TrackTitle} - {TrackArtist}".Trim(' ', '-');
        Status = L("Demo modus", "Demo Mode");
        LoadDemoQueueItems(reset: true);
        AddDiagnostic("INF Demo playlist started.");
    }

    private void RaisePlaybackStateProperties()
    {
        OnPropertyChanged(nameof(ConnectionStatusText));
        OnPropertyChanged(nameof(CanUsePlaybackFeatures));
        OnPropertyChanged(nameof(CanStartPlayback));
        OnPropertyChanged(nameof(CanUseAskDJ));
        OnPropertyChanged(nameof(CanSendAskDJ));
        OnPropertyChanged(nameof(PlaybackAvailabilityText));
        OnPropertyChanged(nameof(IsUpdateRequiredScreenVisible));
        OnPropertyChanged(nameof(RuntimeCompatibilityText));
        OnPropertyChanged(nameof(AboutBackendAvailabilityText));
        OnPropertyChanged(nameof(AboutDemoModeText));
        RaiseFeedbackContextProperties();
        EvaluateWakewordPrompt();
        RaiseCommandStates();
    }

    private void EvaluateWakewordPrompt()
    {
        var shouldShow = ShouldShowWakewordPrompt;
        if (_isWakewordPromptVisible != shouldShow)
        {
            _isWakewordPromptVisible = shouldShow;
            OnPropertyChanged(nameof(IsWakewordPromptVisible));
        }

        RaiseWakewordProperties();
    }

    private void UpdateWakewordListening()
    {
        if (!WakewordFeatureAvailable || !WakewordEnabled || !CanUseAskDJ)
        {
            AddDiagnostic("DBG Wakeword listener stopped or unavailable.");
            return;
        }

        AddDiagnostic("DBG Wakeword listener would run in foreground.");
    }

    private void LoadDemoData()
    {
        LoadDemoQueueItems(reset: false);

        LoadDemoPlaylists(reset: false);

        if (OutputDevices.Count == 0)
        {
            OutputDevices.Add(new PlaybackOutput("living-room", "Woonkamer", "Woonkamer", true));
            OutputDevices.Add(new PlaybackOutput("kitchen", "Keuken", "Keuken", false));
        }

        _suppressOutputCommand = true;
        SelectedOutput ??= OutputDevices.FirstOrDefault();
        _suppressOutputCommand = false;
        _backendAvailable = IsDemoMode || IsPaired;
        _runtimeCompatible = true;
        HasActivePlayback = true;
        IsPlaying = true;
        TrackTitle = "Midnight City";
        TrackArtist = "M83";
        TrackAlbum = "Hurry Up, We're Dreaming";
        ArtworkUrl = "";
        PlaybackPositionMs = 67_000;
        PlaybackDurationMs = 244_000;
        _suppressVolumeCommand = true;
        VolumePercent = 42;
        _suppressVolumeCommand = false;
        NowPlaying = "Midnight City - M83";

        if (DiagnosticLogLines.Count == 0)
        {
            AddDiagnostic("INF App started with existing DJConnect bearer token for windows");
            AddDiagnostic("INF Local Wi-Fi/LAN available");
            AddDiagnostic("INF Local device API started at http://192.168.1.104:57756");
            AddDiagnostic("WRN Refresh failed: not configured: DJConnect is not configured.");
            AddDiagnostic("INF Polling Home Assistant pairing endpoint");
        }
    }

    private void LoadDemoQueueItems(bool reset)
    {
        if (reset)
        {
            QueueItems.Clear();
        }

        if (QueueItems.Count != 0)
        {
            return;
        }

        ReplaceQueueItems(
        [
            new QueueItem("demo-1", null, "Midnight City", null, null, "M83", null, "Hurry Up, We're Dreaming", 244_000, null, "demo:midnight-city", null, null, null, null, null, null, true, true, true, null),
            new QueueItem("demo-2", null, "Sweet Disposition", null, null, "The Temper Trap", null, "Conditions", 232_000, null, "demo:sweet-disposition", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-3", null, "Electric Feel", null, null, "MGMT", null, "Oracular Spectacular", 229_000, null, "demo:electric-feel", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-4", null, "1901", null, null, "Phoenix", null, "Wolfgang Amadeus Phoenix", 193_000, null, "demo:1901", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-5", null, "Tadow", null, null, "Masego & FKJ", null, "Lady Lady", 301_000, null, "demo:tadow", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-6", null, "Innerbloom", null, null, "RÜFÜS DU SOL", null, "Bloom", 589_000, null, "demo:innerbloom", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-7", null, "A Moment Apart", null, null, "ODESZA", null, "A Moment Apart", 234_000, null, "demo:a-moment-apart", null, null, null, null, null, null, false, false, true, null)
        ]);
    }

    private void LoadDemoPlaylists(bool reset)
    {
        if (reset)
        {
            PlaylistItems.Clear();
            FilteredPlaylistItems.Clear();
        }

        if (PlaylistItems.Count != 0)
        {
            ApplyPlaylistFilter();
            return;
        }

        ReplacePlaylistItems(
        [
            new PlaylistItem("demo-friday", null, "Vrijdagavond", null, null, null, "Demo collectie", null, null, "DJConnect", null, null, null, null, null, "demo:playlist:friday", "demo:playlist:friday", null, true, null),
            new PlaylistItem("demo-dinner", null, "Dinner vibes", null, null, null, "Rustig, warm en melodisch", null, null, "DJConnect", null, null, null, null, null, "demo:playlist:dinner", "demo:playlist:dinner", null, true, null),
            new PlaylistItem("demo-energy", null, "Energy reset", null, null, null, "Snellere tracks voor later op de avond", null, null, "DJConnect", null, null, null, null, null, "demo:playlist:energy", "demo:playlist:energy", null, true, null),
            new PlaylistItem("demo-focus", null, "Focus flow", null, null, null, "Instrumentaal en geconcentreerd", null, null, "DJConnect", null, null, null, null, null, "demo:playlist:focus", "demo:playlist:focus", null, true, null),
            new PlaylistItem("demo-late", null, "Late night drive", null, null, null, "Synths, neon en rustige baslijnen", null, null, "DJConnect", null, null, null, null, null, "demo:playlist:late", "demo:playlist:late", null, true, null),
            new PlaylistItem("demo-brunch", null, "Zondag brunch", null, null, null, "Licht, soulvol en niet te druk", null, null, "DJConnect", null, null, null, null, null, "demo:playlist:brunch", "demo:playlist:brunch", null, true, null),
            new PlaylistItem("demo-party", null, "House warmup", null, null, null, "Opbouwend voor een volle kamer", null, null, "DJConnect", null, null, null, null, null, "demo:playlist:party", "demo:playlist:party", null, true, null),
            new PlaylistItem("demo-after", null, "Afterglow", null, null, null, "Zacht landen na een lange avond", null, null, "DJConnect", null, null, null, null, null, "demo:playlist:after", "demo:playlist:after", null, true, null)
        ]);
    }

    private void AddDiagnostic(string message)
    {
        var redacted = RedactFeedbackText(message);
        var level = DiagnosticLogEntry.ParseLevel(redacted);
        if (!ShouldLogLevel(level))
        {
            return;
        }

        var entry = DiagnosticLogEntry.Create(++_nextDiagnosticLogId, DateTimeOffset.Now, redacted);
        DiagnosticLogLines.Add(entry);
        while (DiagnosticLogLines.Count > 500 || RedactedDiagnosticExport().Length > 128 * 1024)
        {
            DiagnosticLogLines.RemoveAt(0);
        }

        PersistDiagnosticLogs();
        ApplyLogFilter();
    }

    private string L(string dutch, string english) => Language == "nl" ? dutch : english;

    private static bool ShouldSuppressCrashReportPrompt()
    {
        if (Debugger.IsAttached)
        {
            return true;
        }

        return MonkeyTestMode.IsEnabled;
    }

    private static bool IsTruthyEnvironment(string name)
    {
        return MonkeyTestMode.IsTruthyEnvironment(name);
    }

    private void LoadPersistedDiagnosticLogs()
    {
        DiagnosticLogLines.Clear();
        foreach (var line in _settings.DiagnosticLogLines.TakeLast(500))
        {
            var redacted = RedactFeedbackText(line);
            DiagnosticLogLines.Add(DiagnosticLogEntry.Create(++_nextDiagnosticLogId, DateTimeOffset.Now, redacted));
        }

        ApplyLogFilter();
    }

    private void PersistDiagnosticLogs()
    {
        if (IsMonkeyTestMode)
        {
            return;
        }

        _settings.DiagnosticLogLines = DiagnosticLogLines
            .TakeLast(500)
            .Select(line => line.ExportText)
            .ToList();
        _ = SaveSettingsIfMutableAsync();
    }

    private Task SaveSettingsIfMutableAsync()
    {
        return IsMonkeyTestMode
            ? Task.CompletedTask
            : _settingsStore.SaveAsync(_settings);
    }

    private bool ShouldLogLevel(string level)
    {
        var threshold = LogLevelRank(LogLevel);
        return LogLevelRank(level) <= threshold;
    }

    private static int LogLevelRank(string? level)
    {
        return level?.ToLowerInvariant() switch
        {
            "error" or "err" => 0,
            "warning" or "warn" or "wrn" => 1,
            "info" or "inf" => 2,
            "debug" or "dbg" => 3,
            _ => 2
        };
    }

    private void ApplyLogFilter()
    {
        var query = LogSearchText.Trim();
        var matches = new List<DiagnosticLogEntry>();
        foreach (var entry in DiagnosticLogLines)
        {
            entry.SetSearch(query, isActive: false);
            if (entry.IsSearchMatch)
            {
                matches.Add(entry);
            }
        }

        if (matches.Count > 0)
        {
            _selectedLogSearchResultIndex = Math.Clamp(_selectedLogSearchResultIndex, 0, matches.Count - 1);
            matches[_selectedLogSearchResultIndex].SetSearch(query, isActive: true);
        }
        else
        {
            _selectedLogSearchResultIndex = 0;
        }

        FilteredDiagnosticLogLines.Clear();
        foreach (var entry in DiagnosticLogLines.TakeLast(120))
        {
            FilteredDiagnosticLogLines.Add(entry);
        }

        OnPropertyChanged(nameof(HasDiagnosticLogs));
        OnPropertyChanged(nameof(HasNoDiagnosticLogs));
        OnPropertyChanged(nameof(LogSearchResultCount));
        OnPropertyChanged(nameof(LogSearchResultLabel));
        NextLogSearchResultCommand.RaiseCanExecuteChanged();
        PreviousLogSearchResultCommand.RaiseCanExecuteChanged();
    }

    private string RedactedDiagnosticExport()
    {
        if (DiagnosticLogLines.Count == 0)
        {
            return L("Geen logs beschikbaar.", "No logs available.");
        }

        return RedactFeedbackText(string.Join(Environment.NewLine, DiagnosticLogLines.Select(line => line.ExportText)));
    }

    private static string RedactFeedbackText(string text) => DiagnosticRedactor.Redact(text);

    private static string FormatTime(double milliseconds)
    {
        var time = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }

    private void RaiseCommandStates()
    {
        PairCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
        RefreshQueueCommand.RaiseCanExecuteChanged();
        RefreshPlaylistsCommand.RaiseCanExecuteChanged();
        SendAskDJCommand.RaiseCanExecuteChanged();
        ClearHistoryCommand.RaiseCanExecuteChanged();
        PlayCommand.RaiseCanExecuteChanged();
        PauseCommand.RaiseCanExecuteChanged();
        NextCommand.RaiseCanExecuteChanged();
        PreviousCommand.RaiseCanExecuteChanged();
        RetryVersionCheckCommand.RaiseCanExecuteChanged();
    }

    private void RaiseLocalizedProperties()
    {
        OnPropertyChanged(nameof(IsDutch));
        OnPropertyChanged(nameof(Tagline));
        OnPropertyChanged(nameof(NowPlayingTitle));
        OnPropertyChanged(nameof(QueueTitle));
        OnPropertyChanged(nameof(PlaylistsTitle));
        OnPropertyChanged(nameof(SettingsTitle));
        OnPropertyChanged(nameof(AboutTitle));
        OnPropertyChanged(nameof(LegalTitle));
        OnPropertyChanged(nameof(PrivacyTitle));
        OnPropertyChanged(nameof(FeedbackTitle));
        OnPropertyChanged(nameof(AskDJPlaceholder));
        OnPropertyChanged(nameof(PairingStatusText));
        OnPropertyChanged(nameof(PlaybackAvailabilityText));
        OnPropertyChanged(nameof(RuntimeCompatibilityText));
        OnPropertyChanged(nameof(UpdateRequiredTitle));
        OnPropertyChanged(nameof(UpdateRequiredSubtitle));
        OnPropertyChanged(nameof(UpdateRequiredDetail));
        OnPropertyChanged(nameof(WhatsNewSubtitle));
        OnPropertyChanged(nameof(AppProtocolText));
        OnPropertyChanged(nameof(HomeAssistantVersionText));
        OnPropertyChanged(nameof(RequiredProtocolText));
        RaisePermissionExplanationProperties();
        RaisePermissionStatusProperties();
        RaiseFeedbackContextProperties();
        RaiseWakewordProperties();
    }

    private void RaiseWakewordProperties()
    {
        OnPropertyChanged(nameof(WakewordFeatureAvailable));
        OnPropertyChanged(nameof(WakewordEnabled));
        OnPropertyChanged(nameof(WakewordStatusText));
        OnPropertyChanged(nameof(ShouldShowWakewordPrompt));
        OnPropertyChanged(nameof(IsWakewordPromptVisible));
        EnableWakewordCommand.RaiseCanExecuteChanged();
    }

    private void RaiseFeedbackContextProperties()
    {
        OnPropertyChanged(nameof(FeedbackContextSummary));
        OnPropertyChanged(nameof(FeedbackOsVersion));
        OnPropertyChanged(nameof(FeedbackPairingStatus));
        RaiseSettingsStatusProperties();
    }

    private void RaiseSettingsStatusProperties()
    {
        OnPropertyChanged(nameof(SettingsPairingStatusText));
        OnPropertyChanged(nameof(SettingsRuntimeSummary));
    }

    private void RaisePermissionExplanationProperties()
    {
        OnPropertyChanged(nameof(PermissionIcon));
        OnPropertyChanged(nameof(PermissionTitle));
        OnPropertyChanged(nameof(PermissionIntro));
        OnPropertyChanged(nameof(PermissionBodyPrimary));
        OnPropertyChanged(nameof(PermissionBodySecondary));
        OnPropertyChanged(nameof(PermissionBodyTertiary));
        OnPropertyChanged(nameof(HasPermissionBodyTertiary));
        OnPropertyChanged(nameof(IsPermissionSettingsMode));
        OnPropertyChanged(nameof(IsLocalNetworkPermission));
        OnPropertyChanged(nameof(CanCopyClientAddress));
        OnPropertyChanged(nameof(PermissionContinueText));
        OnPropertyChanged(nameof(PermissionSettingsText));
    }

    private void RaisePermissionStatusProperties()
    {
        OnPropertyChanged(nameof(NotificationPermissionStatus));
        OnPropertyChanged(nameof(MicrophonePermissionStatus));
        OnPropertyChanged(nameof(LocalNetworkPermissionStatus));
    }

    private void RaisePairingProperties()
    {
        OnPropertyChanged(nameof(ClientAddressDisplay));
        OnPropertyChanged(nameof(IsClientAddressAvailable));
        OnPropertyChanged(nameof(IsPairable));
        OnPropertyChanged(nameof(IsPairingFormVisible));
        OnPropertyChanged(nameof(IsPairingWaitingVisible));
        OnPropertyChanged(nameof(PairingCodeDisplay));
        OnPropertyChanged(nameof(PairingStatusText));
        OnPropertyChanged(nameof(CanCopyClientAddress));
        OnPropertyChanged(nameof(ShouldAdvertiseMdns));
    }
}

internal sealed record ReleaseNoteDocument(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("tag_name")] string? TagName,
    [property: JsonPropertyName("body")] string? Body);

public enum PermissionExplanationKind
{
    None,
    Microphone,
    Notifications,
    LocalNetwork
}

public enum PermissionExplanationMode
{
    Request,
    Settings
}

public sealed class DiagnosticLogEntry : ObservableObject
{
    private bool _isSearchMatch;
    private bool _isActiveSearchMatch;

    private DiagnosticLogEntry(int id, DateTimeOffset timestamp, string level, string message, string category)
    {
        Id = id;
        Timestamp = timestamp;
        Level = level;
        Message = message;
        Category = category;
    }

    public int Id { get; }
    public DateTimeOffset Timestamp { get; }
    public string Level { get; }
    public string Message { get; }
    public string Category { get; }
    public string LineNumber => Id.ToString("000");
    public string TimestampText => Timestamp.ToString("HH:mm:ss");
    public string ExportText => $"{TimestampText} {Level} {Message}";
    public string DisplayText => string.IsNullOrWhiteSpace(Category) ? Message : $"[{Category}] {Message}";
    public string LevelColor => Level switch
    {
        "ERR" => "#FF8A7A",
        "WRN" => "#FBBF24",
        "DBG" => "#8A91B8",
        _ => "#DAD7FF"
    };
    public string RowBackground => IsActiveSearchMatch ? "#4A3219" : IsSearchMatch ? "#242A5B" : "Transparent";

    public bool IsSearchMatch
    {
        get => _isSearchMatch;
        private set
        {
            if (SetProperty(ref _isSearchMatch, value))
            {
                OnPropertyChanged(nameof(RowBackground));
            }
        }
    }

    public bool IsActiveSearchMatch
    {
        get => _isActiveSearchMatch;
        private set
        {
            if (SetProperty(ref _isActiveSearchMatch, value))
            {
                OnPropertyChanged(nameof(RowBackground));
            }
        }
    }

    public static DiagnosticLogEntry Create(int id, DateTimeOffset timestamp, string raw)
    {
        var line = raw.Trim();
        var level = ParseLevel(line);
        var message = line;
        if (line.Length > 4 && string.Equals(line[..3], level, StringComparison.OrdinalIgnoreCase))
        {
            message = line[3..].TrimStart();
        }
        else if (line.Length > 13 && TimeSpan.TryParse(line[..8], out _))
        {
            var rest = line[9..].TrimStart();
            level = ParseLevel(rest);
            message = rest.Length > 4 && string.Equals(rest[..3], level, StringComparison.OrdinalIgnoreCase)
                ? rest[3..].TrimStart()
                : rest;
        }

        return new DiagnosticLogEntry(id, timestamp, NormalizeLevel(level), message, "");
    }

    public static string ParseLevel(string? text)
    {
        var trimmed = text?.TrimStart() ?? "";
        if (trimmed.StartsWith("ERR", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return "ERR";
        }

        if (trimmed.StartsWith("WRN", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("WARN", StringComparison.OrdinalIgnoreCase))
        {
            return "WRN";
        }

        if (trimmed.StartsWith("DBG", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("DEBUG", StringComparison.OrdinalIgnoreCase))
        {
            return "DBG";
        }

        return "INF";
    }

    public void SetSearch(string query, bool isActive)
    {
        var isMatch = !string.IsNullOrWhiteSpace(query)
            && (Message.Contains(query, StringComparison.OrdinalIgnoreCase)
                || Level.Contains(query, StringComparison.OrdinalIgnoreCase)
                || Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        IsSearchMatch = isMatch;
        IsActiveSearchMatch = isMatch && isActive;
    }

    private static string NormalizeLevel(string level)
    {
        return level.ToUpperInvariant() switch
        {
            "ERROR" => "ERR",
            "WARNING" or "WARN" => "WRN",
            "DEBUG" => "DBG",
            "INFO" => "INF",
            _ => level.ToUpperInvariant()
        };
    }
}
