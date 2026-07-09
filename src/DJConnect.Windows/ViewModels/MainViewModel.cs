using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DJConnect.Windows.Contracts;
using DJConnect.Windows.Models;
using DJConnect.Windows.Resources;
using DJConnect.Windows.Services;
using DJAnnouncementOutputKind = DJConnect.Windows.Models.DJAnnouncementOutput;

namespace DJConnect.Windows.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private static readonly DJAnnouncementOutputKind[] AllDJAnnouncementOutputs =
    [
        DJAnnouncementOutputKind.ClientDevice,
        DJAnnouncementOutputKind.Both,
        DJAnnouncementOutputKind.HaSpeaker,
        DJAnnouncementOutputKind.TextOnly
    ];
    private readonly SettingsStore _settingsStore = new();
    private readonly CredentialStore _credentialStore = new();
    private readonly DJConnectApiClient _apiClient = new(new HttpClient());
    private readonly HomeAssistantTransportManager _transportManager = new();
    private readonly DJConnectTransportOptions _transportOptions = DJConnectTransportOptions.FromEnvironment();
    private AppSettings _settings = new();
    private ClientIdentity _identity = ClientIdentity.CreateOrLoad(null);
    private string _homeAssistantUrl = DJConnectContract.DefaultHomeAssistantUrl;
    private string _homeAssistantRemoteUrl = "";
    private HomeAssistantConnectionMode _connectionMode = HomeAssistantConnectionMode.Offline;
    private MusicBackendSummary _musicBackendSummary = MusicBackendSummary.Empty;
    private DJAnnouncementCapabilities _djAnnouncementCapabilities = new(false, null, null, null, null, null, null, null);
    private string _token = "";
    private string _pairingCode = "";
    private string _askDJText = "";
    private string _status = AppStrings.Get("Status_NotPaired");
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
    private DJAnnouncementOutputKind _djAnnouncementOutput = DJAnnouncementOutputKind.ClientDevice;
    private string _wakePhrase = "Hey DJ";
    private string _wakewordNotice = "";
    private PermissionExplanationKind _activePermissionKind = PermissionExplanationKind.None;
    private PermissionExplanationMode _activePermissionMode = PermissionExplanationMode.Request;
    private string _trackTitle = "";
    private string _trackArtist = "";
    private string _trackAlbum = "";
    private string _trackInsightNotice = "";
    private string _musicDnaNotice = "";
    private string _discoverNotice = "";
    private string _artworkUrl = "";
    private TrackInsightPresentation? _trackInsight;
    private MusicDnaDashboard _musicDnaDashboard = new(false, "", [], "");
    private double _playbackPositionMs;
    private double _playbackDurationMs = 1;
    private double _volumePercent = 42;
    private PlaybackOutput? _selectedOutput;
    private string _language = "en";
    private string _logLevel = "info";
    private bool _isPaired;
    private bool _isDemoMode;
    private bool _backendAvailable;
    private bool _runtimeCompatible = true;
    private bool _hasActivePlayback;
    private bool _isPlaying;
    private bool _isOnboardingVisible = true;
    private bool _isPairingOverlayVisible;
    private bool _isPairingSuccessVisible;
    private bool _isPairingPending;
    private bool _isPairingWaitingForCompletion;
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
    private bool _isLoadingMusicDna;
    private bool _isLoadingDiscover;
    private bool _discoverConsentRejected;
    private bool _suppressOutputCommand;
    private bool _suppressVolumeCommand;
    private int _selectedLogSearchResultIndex;
    private int _nextDiagnosticLogId;
    private CancellationTokenSource? _volumeDebounce;

    public MainViewModel()
    {
        SaveSettingsCommand = new AsyncCommand(SaveSettingsAsync);
        PairCommand = new AsyncCommand(PairAsync, () => CanPair);
        RefreshCommand = new AsyncCommand(RefreshAsync, () => IsPaired || IsDemoMode);
        SendAskDJCommand = new AsyncCommand(SendAskDJAsync, () => CanUseAskDJ && !string.IsNullOrWhiteSpace(AskDJText));
        ClearHistoryCommand = new AsyncCommand(ClearHistoryAsync, () => CanUseAskDJ);
        RefreshQueueCommand = new AsyncCommand(RefreshQueueAsync, () => CanUsePlaybackFeatures);
        RefreshPlaylistsCommand = new AsyncCommand(RefreshPlaylistsAsync, () => CanUsePlaybackFeatures);
        PlayCommand = new AsyncCommand(TogglePlaybackAsync, () => CanStartPlayback);
        PauseCommand = new AsyncCommand(TogglePlaybackAsync, () => CanStartPlayback);
        NextCommand = new AsyncCommand(() => RunPlaybackCommandAsync("next_track"), () => CanUsePlaybackFeatures);
        PreviousCommand = new AsyncCommand(() => RunPlaybackCommandAsync("previous_track"), () => CanUsePlaybackFeatures);
        SeekBackwardCommand = new AsyncCommand(() => SeekRelativeAsync(-15_000), () => CanUsePlaybackFeatures);
        SeekForwardCommand = new AsyncCommand(() => SeekRelativeAsync(15_000), () => CanUsePlaybackFeatures);
        SaveCurrentTrackCommand = new AsyncCommand(SaveCurrentTrackAsync, () => CanUsePlaybackFeatures);
        OpenTrackInsightCommand = new AsyncCommand(OpenTrackInsightAsync, () => CanUsePlaybackFeatures);
        RefreshMusicDnaCommand = new AsyncCommand(RefreshMusicDnaAsync, () => CanUseMusicDna);
        EnableMusicDnaCommand = new AsyncCommand(() => UpdateMusicDnaEnabledAsync(true), () => CanUseMusicDna);
        DisableMusicDnaCommand = new AsyncCommand(() => UpdateMusicDnaEnabledAsync(false), () => CanUseMusicDna && MusicDnaDashboard.Enabled);
        ClearMusicDnaCommand = new AsyncCommand(ClearMusicDnaAsync, () => CanUseMusicDna && MusicDnaDashboard.Enabled);
        RefreshDiscoverCommand = new AsyncCommand(() => LoadDiscoverAsync(forceRefresh: true), () => CanUseMusicDna);
        EnableDiscoverMusicDnaCommand = new AsyncCommand(EnableDiscoverMusicDnaAsync, () => CanUseMusicDna);
        RejectDiscoverConsentCommand = new AsyncCommand(() =>
        {
            _discoverConsentRejected = true;
            RaiseDiscoverProperties();
            return Task.CompletedTask;
        });
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
        RetryVersionCheckCommand = new AsyncCommand(RetryVersionCheckAsync, () => IsPaired || IsDemoMode);
        DismissWhatsNewCommand = new AsyncCommand(DismissWhatsNewAsync);
    }

    public ObservableCollection<AskDJMessage> Messages { get; } = [];
    public ObservableCollection<PlaybackAction> Actions { get; } = [];
    public ObservableCollection<RecentItem> RecentItems { get; } = [];
    public ObservableCollection<QueueItem> QueueItems { get; } = [];
    public ObservableCollection<PlaylistItem> PlaylistItems { get; } = [];
    public ObservableCollection<PlaylistItem> FilteredPlaylistItems { get; } = [];
    public ObservableCollection<MusicDnaDashboardBlock> MusicDnaBlocks { get; } = [];
    public ObservableCollection<MusicDiscoveryItem> DiscoverItems { get; } = [];
    public ObservableCollection<PlaybackOutput> OutputDevices { get; } = [];
    public ObservableCollection<DiagnosticLogEntry> DiagnosticLogLines { get; } = [];
    public ObservableCollection<DiagnosticLogEntry> FilteredDiagnosticLogLines { get; } = [];
    public LocalizedTextCatalog T { get; } = new();

    public string HomeAssistantUrl
    {
        get => _homeAssistantUrl;
        set
        {
            if (SetProperty(ref _homeAssistantUrl, value))
            {
                RaisePairingInputProperties();
            }
        }
    }

    public string HomeAssistantRemoteUrl
    {
        get => _homeAssistantRemoteUrl;
        set => SetProperty(ref _homeAssistantRemoteUrl, value);
    }

    public string Token
    {
        get => _token;
        set
        {
            if (SetProperty(ref _token, value))
            {
                RaiseNowPlayingStatusProperties();
            }
        }
    }

    public string PairingCode
    {
        get => _pairingCode;
        set
        {
            if (SetProperty(ref _pairingCode, value))
            {
                RaisePairingInputProperties();
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
        : LogSearchResultCount == 0 ? "0" : $"{_selectedLogSearchResultIndex + 1} / {LogSearchResultCount}";

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

    public string UpdateRequiredTitle => P("Vm_Update_required");
    public string UpdateRequiredSubtitle => P("Vm_UpdateRequiredSubtitle");
    public string UpdateRequiredDetail => string.IsNullOrWhiteSpace(UpdateRequiredMessage)
        ? P("Vm_UpdateRequiredDetail")
        : UpdateRequiredMessage;
    public string AppProtocolText => AppStrings.Format("Format_AppProtocol", DJConnectContract.ProtocolLine);
    public string HomeAssistantVersionText
    {
        get => string.IsNullOrWhiteSpace(_homeAssistantVersionText) ? AppStrings.Format("Format_HomeAssistantIntegration", "unknown") : _homeAssistantVersionText;
        set => SetProperty(ref _homeAssistantVersionText, value);
    }
    public string RequiredProtocolText => AppStrings.Format("Format_RequiredProtocol", DJConnectContract.ProtocolLine);
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
        get => string.IsNullOrWhiteSpace(_whatsNewTitle) ? P("Vm_What_s_New") : _whatsNewTitle;
        set => SetProperty(ref _whatsNewTitle, value);
    }

    public string WhatsNewSubtitle => $"DJConnect Windows {AppVersion}";

    public string WhatsNewBody
    {
        get => string.IsNullOrWhiteSpace(_whatsNewBody)
            ? P("Vm_Release_notes_could_not_be_loaded_Visit_http")
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
        get => string.IsNullOrWhiteSpace(_trackTitle) ? P("Vm_No_active_playback") : _trackTitle;
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

    public TrackInsightPresentation? TrackInsightPanel
    {
        get => _trackInsight;
        set
        {
            if (SetProperty(ref _trackInsight, value))
            {
                OnPropertyChanged(nameof(HasTrackInsightPanel));
            }
        }
    }

    public bool HasTrackInsightPanel => TrackInsightPanel?.HasContent == true;

    public string TrackInsightNotice
    {
        get => _trackInsightNotice;
        set
        {
            if (SetProperty(ref _trackInsightNotice, value))
            {
                OnPropertyChanged(nameof(HasTrackInsightNotice));
            }
        }
    }

    public bool HasTrackInsightNotice => !string.IsNullOrWhiteSpace(TrackInsightNotice);

    public MusicDnaDashboard MusicDnaDashboard
    {
        get => _musicDnaDashboard;
        set
        {
            if (SetProperty(ref _musicDnaDashboard, value))
            {
                ReplaceMusicDnaBlocks(value.Blocks);
                RaiseMusicDnaProperties();
            }
        }
    }

    public bool IsMusicDnaEnabled => MusicDnaDashboard.Enabled;
    public bool IsMusicDnaDisabled => !MusicDnaDashboard.Enabled;
    public string MusicDnaSummary => MusicDnaDashboard.Summary;
    public bool HasMusicDnaSummary => MusicDnaDashboard.HasSummary;
    public bool HasMusicDnaBlocks => MusicDnaBlocks.Count > 0;
    public string MusicDnaUpdatedAt => MusicDnaDashboard.UpdatedAt;
    public bool HasMusicDnaUpdatedAt => !string.IsNullOrWhiteSpace(MusicDnaUpdatedAt);

    public string MusicDnaNotice
    {
        get => _musicDnaNotice;
        set
        {
            if (SetProperty(ref _musicDnaNotice, value))
            {
                OnPropertyChanged(nameof(HasMusicDnaNotice));
            }
        }
    }

    public bool HasMusicDnaNotice => !string.IsNullOrWhiteSpace(MusicDnaNotice);

    public bool IsLoadingMusicDna
    {
        get => _isLoadingMusicDna;
        set => SetProperty(ref _isLoadingMusicDna, value);
    }

    public string DiscoverNotice
    {
        get => _discoverNotice;
        set
        {
            if (SetProperty(ref _discoverNotice, value))
            {
                OnPropertyChanged(nameof(HasDiscoverNotice));
            }
        }
    }

    public bool HasDiscoverNotice => !string.IsNullOrWhiteSpace(DiscoverNotice);

    public bool IsLoadingDiscover
    {
        get => _isLoadingDiscover;
        set => SetProperty(ref _isLoadingDiscover, value);
    }

    public bool HasDiscoverItems => DiscoverItems.Count > 0;
    public bool HasNoDiscoverItems => DiscoverItems.Count == 0 && IsMusicDnaEnabled && !IsLoadingDiscover;
    public bool IsDiscoverConsentVisible => !IsMusicDnaEnabled && !_discoverConsentRejected;
    public bool IsDiscoverRejectedStateVisible => !IsMusicDnaEnabled && _discoverConsentRejected;

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

    public string SelectedOutputText => SelectedOutput?.DisplayName ?? P("Vm_No_output_device_selected");

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
            var normalized = AppStrings.NormalizeLanguage(value);
            if (SetProperty(ref _language, normalized))
            {
                AppStrings.UseLanguage(normalized);
                _settings.Language = normalized;
                T.Refresh();
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

    public IReadOnlyList<string> SupportedLanguages => AppStrings.SupportedLanguages;

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

    public IReadOnlyList<string> DJAnnouncementOutputOptions => AllDJAnnouncementOutputs
        .Where(output => _djAnnouncementCapabilities.Supports(output))
        .Select(DJAnnouncementOutputLabel)
        .ToList();

    public string DJAnnouncementOutput
    {
        get => DJAnnouncementOutputLabel(_djAnnouncementOutput);
        set
        {
            var output = DJAnnouncementOutputFromLabel(value);
            if (!_djAnnouncementCapabilities.Supports(output))
            {
                output = _djAnnouncementCapabilities.EffectiveDefaultOutput();
            }

            if (SetProperty(ref _djAnnouncementOutput, output))
            {
                _settings.DJAnnouncementOutput = DJAnnouncementOutputProtocol.Format(output);
                _ = SaveSettingsIfMutableAsync();
                OnPropertyChanged(nameof(DJAnnouncementOutputHelperText));
            }
        }
    }

    public string DJAnnouncementSpeakerText => _djAnnouncementCapabilities.HasSpeaker
        ? _djAnnouncementCapabilities.SpeakerDisplayName
        : "Geen Home Assistant speaker geconfigureerd";

    public string DJAnnouncementOutputHelperText => _djAnnouncementCapabilities.HasSpeaker
        ? $"DJ-aankondigingen gebruiken: {DJAnnouncementOutput}."
        : "Configureer een Home Assistant speaker in DJConnect opties in Home Assistant.";

    public string AskDJMood
    {
        get => _askDJMood;
        set => SetProperty(ref _askDJMood, string.IsNullOrWhiteSpace(value) ? "Groove" : value);
    }

    private int AskDJMoodValue() => AskDJMood switch
    {
        "Chill" => 12,
        "Energy" => 72,
        "Party" => 92,
        _ => 42
    };

    private string MusicDnaKey() => $"{_identity.DeviceId}:{AskDJMessage.MoodZoneFromValue(AskDJMoodValue())}";

    private MusicDnaProfileRequest MusicDnaProfileRequest()
    {
        var locale = AppStrings.NormalizeApiLocale(_language);
        return new MusicDnaProfileRequest(
            _identity.DeviceId,
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            locale,
            locale,
            AskDJMoodValue(),
            MusicDnaKey());
    }

    private MusicDnaSettingsRequest MusicDnaSettingsRequest(bool enabled)
    {
        var locale = AppStrings.NormalizeApiLocale(_language);
        return new MusicDnaSettingsRequest(
            _identity.DeviceId,
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            enabled,
            locale,
            locale,
            AskDJMoodValue(),
            MusicDnaKey());
    }

    private MusicDnaClearRequest MusicDnaClearRequest()
    {
        var locale = AppStrings.NormalizeApiLocale(_language);
        return new MusicDnaClearRequest(
            _identity.DeviceId,
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            locale,
            locale,
            AskDJMoodValue(),
            MusicDnaKey());
    }

    private MusicDiscoveryRequest MusicDiscoveryRequest()
    {
        var locale = AppStrings.NormalizeApiLocale(_language);
        return new MusicDiscoveryRequest(
            _identity.DeviceId,
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            locale,
            locale,
            AskDJMoodValue(),
            MusicDnaKey());
    }

    private MusicDiscoveryPlayRequest MusicDiscoveryPlayRequest(MusicDiscoveryItem item)
    {
        var locale = AppStrings.NormalizeApiLocale(_language);
        return new MusicDiscoveryPlayRequest(
            _identity.DeviceId,
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            item.Id,
            item.ItemId,
            item.DisplayKind,
            item.Uri,
            item.SpotifyUri,
            "music_discovery",
            locale,
            locale,
            AskDJMoodValue(),
            MusicDnaKey());
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
        ? WakewordEnabled ? P("Vm_Enabled_while_the_app_is_open")
        : P("Vm_Disabled")
        : P("Vm_Not_available_in_this_build");

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
    public string AppVersion => DJConnectContract.AppVersion;
    public string ProtocolVersion => $"{DJConnectContract.ProtocolLine}.x";
    public string BuildChannel => "debug";
    public string PlatformName => "Windows";
    public string WebsiteUrl => "https://djconnect.dev";
    public bool IsPairable => IsPairingOverlayVisible && !IsPairingSuccessVisible && !IsOnboardingVisible && !IsDemoMode && !IsPaired;
    public bool IsPairingFormVisible => IsPairingOverlayVisible && !IsPairingSuccessVisible;
    public bool IsPairingWaitingVisible => IsPairingFormVisible && (IsPairingPending || IsPairingWaitingForCompletion);
    public bool IsPairingPending
    {
        get => _isPairingPending;
        private set
        {
            if (SetProperty(ref _isPairingPending, value))
            {
                RaisePairingProperties();
                PairCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsPairingWaitingForCompletion
    {
        get => _isPairingWaitingForCompletion;
        private set
        {
            if (SetProperty(ref _isPairingWaitingForCompletion, value))
            {
                RaisePairingProperties();
                PairCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsHomeAssistantUrlValid => IsValidHomeAssistantUrl(HomeAssistantUrl);
    public bool IsPairingCodeValid => IsValidPairCode(PairingCode);
    public bool CanPair => IsPairingFormVisible && IsHomeAssistantUrlValid && IsPairingCodeValid && !IsPairingPending && !IsPairingWaitingForCompletion;
    public string PairingUrlValidationText => string.IsNullOrWhiteSpace(HomeAssistantUrl) || IsHomeAssistantUrlValid ? "" : AppStrings.Get("Pairing_InvalidUrl");
    public string PairingCodeValidationText => string.IsNullOrWhiteSpace(PairingCode) || IsPairingCodeValid ? "" : AppStrings.Get("Pairing_InvalidCode");
    public bool HasPairingUrlValidationText => !string.IsNullOrWhiteSpace(PairingUrlValidationText);
    public bool HasPairingCodeValidationText => !string.IsNullOrWhiteSpace(PairingCodeValidationText);
    public string LegalNotice => DJConnectContract.SpotifyNotice;

    public string Tagline => P("Vm_Music_control_with_character");
    public string NowPlayingTitle => P("Vm_Now_Playing");
    public string QueueTitle => P("Vm_Queue");
    public string PlaylistsTitle => P("Vm_Playlists");
    public string SettingsTitle => P("Vm_Settings");
    public string AboutTitle => P("Vm_About");
    public string LegalTitle => P("Vm_Legal");
    public string PrivacyTitle => P("Vm_Privacy");
    public string FeedbackTitle => P("Vm_Share_Feedback");
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
        Fast path transport: {_apiClient.FastPathDiagnostics.FastPathTransport}
        WebSocket connected: {_apiClient.FastPathDiagnostics.WebSocketConnected.ToString().ToLowerInvariant()}
        Device class: desktop
        """;
    public string FeedbackOsVersion => $"Windows {Environment.OSVersion.Version}";
    public string FeedbackPairingStatus => IsPaired ? "paired"
        : IsPairingOverlayVisible ? "pairing"
        : !string.IsNullOrWhiteSpace(Token) ? "stale"
        : "unpaired";
    public string SettingsPairingStatusText => IsDemoMode ? P("Vm_Demo_mode")
        : IsUpdateRequired ? P("Vm_Update_required")
        : IsPaired && _backendAvailable ? P("Vm_Paired")
        : IsPaired ? P("Vm_Stale")
        : IsPairingOverlayVisible ? P("Vm_Pairing")
        : P("Vm_Unpaired");
    public string SettingsRuntimeSummary => AppStrings.Format("Format_SettingsRuntimeSummary", RuntimeCompatibilityText, ConnectionModeText, MusicServiceStatusText);
    public string PairingStatusText => IsPairingSuccessVisible ? P("Vm_Successfully_paired")
        : IsPairingWaitingForCompletion ? AppStrings.Get("Pairing_Waiting")
        : IsPairingPending ? AppStrings.Get("Pairing_Pending")
        : IsUpdateRequired ? P("Vm_Update_required")
        : IsPaired ? P("Vm_Paired")
        : P("Vm_Enter_the_local_Home_Assistant_URL_and_pairi");
    public string PlaybackAvailabilityText => IsDemoMode || IsPaired ? P("Vm_Available") : P("Vm_Unavailable");
    public string ConnectionStatusText => IsDemoMode
        ? P("Vm_Demo_mode")
        : !IsPaired ? P("Vm_Not_paired")
        : !_backendAvailable ? P("Vm_Offline")
        : !_runtimeCompatible ? P("Vm_Update_required_3")
        : P("Vm_Paired_3");
    public string NowPlayingPairingStatusText => IsDemoMode
        ? P("Vm_Demo_mode")
        : IsUpdateRequired ? P("Vm_Update_required_4")
        : IsPaired ? P("Vm_Paired_4")
        : IsPairingOverlayVisible ? P("Vm_Pairing")
        : !string.IsNullOrWhiteSpace(Token) ? P("Vm_Stale")
        : P("Vm_Unpaired");
    public string NowPlayingPairingStatusIcon => IsDemoMode
        ? "▶"
        : IsPaired ? "✓"
        : IsPairingOverlayVisible ? "⛓"
        : !string.IsNullOrWhiteSpace(Token) ? "!"
        : "○";
    public string NowPlayingPairingStatusColor => IsDemoMode || (IsPaired && _backendAvailable && _runtimeCompatible)
        ? "#35E56B"
        : IsPairingOverlayVisible ? "#D93AF2"
        : IsUpdateRequired || !string.IsNullOrWhiteSpace(Token) ? "#F59E0B"
        : "#AAA2BE";
    public string NowPlayingMusicBackendStatusText => IsDemoMode
        ? P("Vm_Demo_music")
        : !IsPaired ? P("Vm_Music_not_paired")
        : !_runtimeCompatible ? P("Vm_Update_required_5")
        : !_backendAvailable ? P("Vm_Music_offline")
        : _musicBackendSummary.IsUnavailable ? P("Vm_Music_backend_unavailable")
        : P("Vm_Music_available");
    public string NowPlayingMusicBackendStatusColor => IsDemoMode || (IsPaired && _backendAvailable && _runtimeCompatible && !_musicBackendSummary.IsUnavailable)
        ? "#35E56B"
        : "#EF4444";
    public string RuntimeCompatibilityText => _runtimeCompatible ? P("Vm_Compatible") : P("Vm_Update_required_6");
    public string AboutPairingStatusText => IsPaired ? P("Vm_Paired") : IsPairingOverlayVisible ? P("Vm_Pairing") : P("Vm_Unpaired");
    public string AboutBackendAvailabilityText => _backendAvailable ? P("Vm_Available") : P("Vm_Unavailable");
    public string AboutDemoModeText => IsDemoMode ? P("Vm_Enabled") : P("Vm_Disabled");
    public string AboutConnectionTypeText => ConnectionModeText;
    public string AboutFastPathText
    {
        get
        {
            return FastPathDiagnosticsFormatter.AboutText(_apiClient.FastPathDiagnostics);
        }
    }
    public string ConnectionModeText => IsDemoMode ? P("Vm_Demo_mode") : _connectionMode switch
    {
        HomeAssistantConnectionMode.Local => P("Vm_Local"),
        HomeAssistantConnectionMode.Remote => P("Vm_Remote"),
        _ => P("Vm_Offline")
    };
    public string RemoteSupportText => _transportManager.Current.RemoteSupported
        ? P("Vm_Remote_fallback_available")
        : P("Vm_Local_only");
    public string MusicServiceStatusText => IsDemoMode
        ? P("Vm_Demo_music")
        : !IsPaired ? P("Vm_Music_not_paired")
        : !_runtimeCompatible ? P("Vm_Update_required_5")
        : !_backendAvailable || _musicBackendSummary.IsUnavailable ? P("Vm_Music_backend_unavailable")
        : P("Vm_Music_available");
    public string MusicBackendNameText => _musicBackendSummary.DisplayName;
    public string MusicBackendStatusText => string.IsNullOrWhiteSpace(_musicBackendSummary.ErrorText)
        ? _musicBackendSummary.AvailabilityText
        : _musicBackendSummary.ErrorText;
    public string MusicBackendRevisionText => _musicBackendSummary.Revision?.ToString() ?? "-";
    public string MusicTargetPlayerText => _musicBackendSummary.TargetPlayer is null
        ? "-"
        : $"{_musicBackendSummary.TargetPlayer.Name ?? _musicBackendSummary.TargetPlayer.Id} ({_musicBackendSummary.TargetPlayer.Id ?? "unknown"})";
    public string MusicBackendCapabilitiesText => string.IsNullOrWhiteSpace(_musicBackendSummary.Capabilities?.CompactSummary)
        ? "-"
        : _musicBackendSummary.Capabilities!.CompactSummary;
    public bool CanUsePlaybackFeatures => IsDemoMode || (IsPaired && _backendAvailable && _runtimeCompatible && !_musicBackendSummary.IsUnavailable && _connectionMode != HomeAssistantConnectionMode.Offline);
    public bool CanStartPlayback => CanUsePlaybackFeatures && SelectedOutput is not null;
    public bool CanUseAskDJ => IsDemoMode || (IsPaired && _backendAvailable && _runtimeCompatible && _connectionMode != HomeAssistantConnectionMode.Offline);
    public bool CanUseMusicDna => IsDemoMode || (IsPaired && _runtimeCompatible && _connectionMode != HomeAssistantConnectionMode.Offline);
    public bool CanSendAskDJ => CanUseAskDJ && !string.IsNullOrWhiteSpace(AskDJText);
    public string AskDJPlaceholder => P("Vm_Ask_DJ_anything_e_g_save_this_track_to_liked");
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
                RaiseNowPlayingStatusProperties();
                RaiseFeedbackContextProperties();
                EvaluateWakewordPrompt();
                RaisePairingProperties();
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
                OnPropertyChanged(nameof(IsNotDemoMode));
                RaiseNowPlayingStatusProperties();
                RaiseFeedbackContextProperties();
                EvaluateWakewordPrompt();
                RaisePairingProperties();
            }
        }
    }

    public bool IsNotDemoMode => !IsDemoMode;

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
                RaisePairingProperties();
                RaiseNowPlayingStatusProperties();
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

    public string PermissionTitle => P("Vm_App_permissions");

    public string PermissionIntro => P("Vm_PermissionIntro");

    public string PermissionBodyPrimary => ActivePermissionKind switch
    {
        PermissionExplanationKind.Microphone => P("Vm_PermissionMicrophonePrimary"),
        PermissionExplanationKind.Notifications => P("Vm_PermissionNotificationsPrimary"),
        PermissionExplanationKind.LocalNetwork => P("Vm_PermissionLocalNetworkPrimary"),
        _ => ""
    };

    public string PermissionBodySecondary => ActivePermissionKind switch
    {
        PermissionExplanationKind.Microphone => P("Vm_PermissionMicrophoneSecondary"),
        PermissionExplanationKind.Notifications => P("Vm_PermissionNotificationsSecondary"),
        PermissionExplanationKind.LocalNetwork => P("Vm_PermissionLocalNetworkSecondary"),
        _ => ""
    };

    public string PermissionBodyTertiary => ActivePermissionKind switch
    {
        PermissionExplanationKind.Microphone => P("Vm_PermissionMicrophoneTertiary"),
        PermissionExplanationKind.LocalNetwork => P("Vm_PermissionLocalNetworkTertiary"),
        _ => ""
    };

    public bool HasPermissionBodyTertiary => !string.IsNullOrWhiteSpace(PermissionBodyTertiary);
    public bool IsPermissionSettingsMode => ActivePermissionMode == PermissionExplanationMode.Settings;
    public bool IsLocalNetworkPermission => ActivePermissionKind == PermissionExplanationKind.LocalNetwork;
    public string PermissionContinueText => IsPermissionSettingsMode ? P("Vm_Open_Windows_settings") : P("Vm_Continue");
    public string PermissionSettingsText => ActivePermissionKind == PermissionExplanationKind.LocalNetwork
        ? P("Vm_Open_firewall_settings")
        : P("Vm_Open_Windows_settings_3");
    public string NotificationPermissionStatus => _settings.PermissionExplanationNotificationsSeen
        ? P("Vm_Explanation_shown")
        : P("Vm_Not_enabled");
    public string MicrophonePermissionStatus => _settings.PermissionExplanationMicrophoneSeen
        ? P("Vm_Requested_at_push_to_talk")
        : P("Vm_Not_requested");
    public string LocalNetworkPermissionStatus => _settings.PermissionExplanationLocalNetworkSeen
        ? P("Vm_Explanation_shown_3")
        : P("Vm_Shown_during_pairing");

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
    public AsyncCommand SeekBackwardCommand { get; }
    public AsyncCommand SeekForwardCommand { get; }
    public AsyncCommand SaveCurrentTrackCommand { get; }
    public AsyncCommand OpenTrackInsightCommand { get; }
    public AsyncCommand RefreshMusicDnaCommand { get; }
    public AsyncCommand EnableMusicDnaCommand { get; }
    public AsyncCommand DisableMusicDnaCommand { get; }
    public AsyncCommand ClearMusicDnaCommand { get; }
    public AsyncCommand RefreshDiscoverCommand { get; }
    public AsyncCommand EnableDiscoverMusicDnaCommand { get; }
    public AsyncCommand RejectDiscoverConsentCommand { get; }
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
        HomeAssistantUrl = string.IsNullOrWhiteSpace(_settings.HomeAssistantLocalUrl) ? _settings.HomeAssistantUrl : _settings.HomeAssistantLocalUrl;
        HomeAssistantRemoteUrl = _settings.HomeAssistantRemoteUrl;
        _transportManager.UpdateUrls(HomeAssistantUrl, HomeAssistantRemoteUrl, _settings.RemoteSupported);
        Language = string.IsNullOrWhiteSpace(_settings.Language) ? "en" : _settings.Language;
        LogLevel = string.IsNullOrWhiteSpace(_settings.LogLevel) ? "info" : _settings.LogLevel;
        _djAnnouncementOutput = DJAnnouncementOutputProtocol.Parse(_settings.DJAnnouncementOutput);
        OnPropertyChanged(nameof(DJAnnouncementOutput));
        _wakewordEnabled = WakewordFeatureAvailable && _settings.WakewordEnabled;
        WakePhrase = string.IsNullOrWhiteSpace(_settings.WakePhrase) ? "Hey DJ" : _settings.WakePhrase;
        _settings.IsDemoMode = false;
        IsDemoMode = false;
        IsOnboardingVisible = !(_settings.DJConnectWelcomeSeen || _settings.HasCompletedOnboarding);
        Token = _credentialStore.ReadToken() ?? "";
        IsPaired = !string.IsNullOrWhiteSpace(Token);
        LoadPersistedDiagnosticLogs();
        PairingCode = "";
        _settings.PairingCode = "";
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
        OnPropertyChanged(nameof(IsCrashReportPromptVisible));
        EvaluateWakewordPrompt();
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
            Status = P("Vm_Monkey_test_settings_not_saved");
            AddDiagnostic("INF Monkey test suppressed settings save.");
            return;
        }

        _settings.HomeAssistantUrl = HomeAssistantUrl;
        _settings.HomeAssistantLocalUrl = HomeAssistantUrl;
        _settings.HomeAssistantRemoteUrl = HomeAssistantRemoteUrl;
        _settings.RemoteSupported = _transportManager.Current.RemoteSupported;
        _settings.InstallId = _identity.InstallId;
        _settings.DeviceName = _identity.DeviceName;
        _settings.Language = Language;
        _settings.LogLevel = LogLevel;
        _settings.DJAnnouncementOutput = DJAnnouncementOutputProtocol.Format(_djAnnouncementOutput);
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
        }

        await ConfigureClientAsync(pairingOnly: false);
        Status = P("Vm_Settings_saved");
        AddDiagnostic("INF Settings saved.");
    }

    private async Task PairAsync()
    {
        if (IsMonkeyTestMode)
        {
            Status = P("Vm_Monkey_test_pairing_suppressed");
            AddDiagnostic("INF Monkey test suppressed pairing.");
            return;
        }

        if (!IsValidHomeAssistantUrl(HomeAssistantUrl))
        {
            Status = P("Vm_Enter_a_valid_local_Home_Assistant_URL");
            Notice = Status;
            AddDiagnostic("WRN Pairing blocked because Home Assistant URL is invalid.");
            return;
        }

        if (!IsValidPairCode(PairingCode))
        {
            Status = P("Vm_Enter_the_6_digit_pairing_code_from_Home_Ass");
            Notice = Status;
            AddDiagnostic("WRN Pairing blocked because pair code is invalid.");
            return;
        }

        if (!IsWindowsIdentity(_identity))
        {
            Status = P("Vm_Internal_client_identity_is_invalid_Restart");
            Notice = Status;
            AddDiagnostic("ERR Windows client identity invariant failed.");
            return;
        }

        var pairingTransport = await _transportManager.ResolvePairingAsync(HomeAssistantUrl, CancellationToken.None);
        if (pairingTransport.Mode != HomeAssistantConnectionMode.Local || string.IsNullOrWhiteSpace(pairingTransport.ActiveUrl))
        {
            Status = P("Vm_Home_Assistant_is_unreachable_on_your_local");
            Notice = Status;
            AddDiagnostic("WRN Pairing blocked because local Home Assistant URL is unreachable.");
            return;
        }

        ConfigureClient(pairingTransport.ActiveUrl);
        var payload = new PairingPayload(
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            PairingCode.Trim(),
            DJConnectContract.AppVersion,
            Platform: "windows",
            Locale: Language,
            Language: Language,
            Build: AppVersion);

        IsPairingPending = true;
        PairingResponse response;
        try
        {
            response = await _apiClient.PairAsync(payload, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Status = ApiErrorLocalizer.Pairing(ex);
            Notice = Status;
            IsPairingOverlayVisible = true;
            AddDiagnostic("WRN Pairing request failed: " + ex.GetType().Name);
            IsPairingPending = false;
            return;
        }

        if (!response.Success || string.IsNullOrWhiteSpace(response.DeviceToken))
        {
            Status = PairingErrorMessage(response.Error, response.Message);
            Notice = Status;
            IsPairingOverlayVisible = true;
            AddDiagnostic("WRN Pairing failed.");
            IsPairingPending = false;
            return;
        }

        if (!IsValidPairingResponse(response))
        {
            Status = P("Vm_Home_Assistant_returned_an_invalid_Windows_p");
            Notice = Status;
            IsPairingOverlayVisible = true;
            AddDiagnostic("ERR Pairing response client identity invariant failed.");
            IsPairingPending = false;
            return;
        }

        Token = response.DeviceToken;
        ApplyPairingTransport(response.HomeAssistantLocalUrl, response.HomeAssistantRemoteUrl, response.RemoteSupported);
        ApplyBackendSummary(BackendSummaryFrom(response));
        ApplyDJAnnouncementCapabilities(AnnouncementCapabilitiesFrom(response));
        ConfigureClient();
        IsPairingPending = false;
        IsPairingWaitingForCompletion = true;
        Status = AppStrings.Get("Pairing_Waiting");

        StatusResponse statusResponse;
        try
        {
            statusResponse = await _apiClient.GetStatusAsync(_identity, _language, AskDJMoodValue(), CancellationToken.None);
        }
        catch (Exception ex)
        {
            Token = "";
            Status = ApiErrorLocalizer.Pairing(ex);
            Notice = Status;
            IsPairingWaitingForCompletion = false;
            AddDiagnostic("WRN Pairing verification failed: " + ex.GetType().Name);
            return;
        }

        if (!statusResponse.Success)
        {
            Token = "";
            Status = PairingErrorMessage(statusResponse.Error, null);
            Notice = Status;
            IsPairingWaitingForCompletion = false;
            AddDiagnostic("WRN Pairing verification returned unsuccessful status.");
            return;
        }

        _backendAvailable = statusResponse.BackendAvailable ?? true;
        ApplyVersionCompatibility(statusResponse);
        ApplyBackendSummary(BackendSummaryFrom(statusResponse));
        ApplyDJAnnouncementCapabilities(AnnouncementCapabilitiesFrom(statusResponse));
        ApplyPlaybackState(statusResponse.Playback);
        ReplaceOutputs(statusResponse.ResolvedOutputs());
        ReplaceQueueItems(statusResponse.ResolvedQueue());
        ReplacePlaylistItems(statusResponse.ResolvedPlaylists());

        try
        {
            _credentialStore.SaveToken(Token);
        }
        catch (Exception ex)
        {
            Status = P("Vm_Token_storage_failed");
            AddDiagnostic("WRN Pairing token storage failed: " + ex.GetType().Name);
            IsPairingWaitingForCompletion = false;
            return;
        }

        PairingCode = "";
        _settings.PairingCode = "";
        _settings.HomeAssistantUrl = HomeAssistantUrl;
        _settings.HomeAssistantLocalUrl = HomeAssistantUrl;
        _settings.HomeAssistantRemoteUrl = HomeAssistantRemoteUrl;
        _settings.RemoteSupported = _transportManager.Current.RemoteSupported;
        _settings.InstallId = _identity.InstallId;
        _settings.DeviceName = _identity.DeviceName;
        _settings.Language = Language;
        _settings.LogLevel = LogLevel;
        _settings.DJAnnouncementOutput = DJAnnouncementOutputProtocol.Format(_djAnnouncementOutput);
        _settings.IsDemoMode = false;
        _settings.DJConnectWelcomeSeen = true;
        _settings.HasCompletedOnboarding = true;
        await SaveSettingsIfMutableAsync();
        IsPaired = true;
        IsPairingSuccessVisible = true;
        IsPairingOverlayVisible = true;
        IsPairingWaitingForCompletion = false;
        Status = P("Vm_Successfully_paired");
        AddDiagnostic("INF Pairing completed.");
    }

    private async Task RefreshAsync()
    {
        if (IsDemoMode)
        {
            Status = P("Vm_Demo_mode_3");
            Notice = "";
            _backendAvailable = true;
            _runtimeCompatible = true;
            RaisePlaybackStateProperties();
            AddDiagnostic("INF Demo refresh completed.");
            return;
        }

        var transport = await ConfigureClientAsync(pairingOnly: false);
        if (!transport.IsOnline)
        {
            _backendAvailable = false;
            _connectionMode = HomeAssistantConnectionMode.Offline;
            Notice = P("Vm_Home_Assistant_is_unreachable_through_local");
            Status = Notice;
            RaisePlaybackStateProperties();
            RaiseSettingsStatusProperties();
            return;
        }
        StatusResponse response;
        try
        {
            response = await _apiClient.GetStatusAsync(_identity, _language, AskDJMoodValue(), CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                Notice = AppStrings.Get("Status_PairAgain");
                Status = Notice;
                RaisePlaybackStateProperties();
                return;
            }

            if (ApplyVersionMismatch(ex))
            {
                Notice = P("Vm_Update_required_7");
                Status = ConnectionStatusText;
                AddDiagnostic("WRN Status refresh blocked by version mismatch.");
                return;
            }

            _backendAvailable = false;
            Notice = P("Vm_Home_Assistant_is_unreachable");
            Status = Notice;
            AddDiagnostic("WRN Status refresh failed: " + ex.GetType().Name);
            RaisePlaybackStateProperties();
            return;
        }

        if (!response.Success)
        {
            if (await ApplyStalePairingAsync(response.Error))
            {
                Notice = AppStrings.Get("Status_PairAgain");
                Status = Notice;
                RaisePlaybackStateProperties();
                return;
            }

            if (string.Equals(response.Error, "version_mismatch", StringComparison.OrdinalIgnoreCase))
            {
                ApplyVersionCompatibility(response);
                Notice = P("Vm_Update_required_8");
                Status = ConnectionStatusText;
                AddDiagnostic("WRN Status response reported version mismatch.");
                return;
            }

            _backendAvailable = false;
            Notice = P("Vm_Home_Assistant_is_unreachable_3");
            Status = Notice;
            AddDiagnostic("WRN Refresh failed.");
            RaisePlaybackStateProperties();
            return;
        }

        _backendAvailable = response.BackendAvailable ?? true;
        ApplyPairingTransport(response.HomeAssistantLocalUrl, response.HomeAssistantRemoteUrl, response.RemoteSupported);
        ApplyBackendSummary(BackendSummaryFrom(response));
        ApplyDJAnnouncementCapabilities(AnnouncementCapabilitiesFrom(response));
        ApplyVersionCompatibility(response);
        ApplyPlaybackState(response.Playback);
        ReplaceOutputs(response.ResolvedOutputs());
        ReplaceQueueItems(response.ResolvedQueue());
        ReplacePlaylistItems(response.ResolvedPlaylists());
        Notice = _runtimeCompatible
            ? HasActivePlayback ? "" : P("Vm_No_active_playback_3")
            : P("Vm_Update_required_9");
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
            AskDJNotice = !_runtimeCompatible ? P("Vm_Update_required_10") : P("Vm_Ask_DJ_is_unavailable");
            return;
        }

        var clientMessageId = Guid.NewGuid().ToString("N");
        AskDJText = "";
        var localUserMessage = new AskDJMessage(clientMessageId, "user", text, null, DateTimeOffset.Now, "user", null, null, null, null, null, ClientMessageId: clientMessageId, IsPending: true);
        MergeMessage(localUserMessage);

        if (IsDemoMode)
        {
            var answer = P("Vm_DemoAskDJAnswer");
            MarkMessageSent(clientMessageId);
            MergeMessage(new AskDJMessage(Guid.NewGuid().ToString("N"), "assistant", answer, null, DateTimeOffset.Now, "assistant", DemoPlaybackActions(), null, null, null, null));
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
            Mood: AskDJMoodValue(),
            AppVersion: AppVersion,
            ProtocolVersion: DJConnectContract.ProtocolLine,
            Language: AppStrings.NormalizeApiLocale(_language),
            Locale: AppStrings.NormalizeApiLocale(_language),
            MusicDnaKey: MusicDnaKey(),
            DJAnnouncementOutput: _djAnnouncementOutput);

        AskDJMessageResponse response;
        try
        {
            response = await _apiClient.SendAskDJMessageAsync(request, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                MarkMessageFailed(clientMessageId);
                AskDJNotice = AppStrings.Get("Status_PairAgain");
                return;
            }

            if (ApplyVersionMismatch(ex))
            {
                MarkMessageFailed(clientMessageId);
                AskDJNotice = P("Vm_Update_required_11");
                AddDiagnostic("WRN Ask DJ request blocked by version mismatch.");
                return;
            }

            MarkMessageFailed(clientMessageId);
            AskDJNotice = P("Vm_Ask_DJ_is_unavailable_3");
            AddDiagnostic("WRN Ask DJ request failed: " + ex.GetType().Name);
            return;
        }

        if (!response.Success)
        {
            if (await ApplyStalePairingAsync(response.Error))
            {
                MarkMessageFailed(clientMessageId);
                AskDJNotice = AppStrings.Get("Status_PairAgain");
                return;
            }

            MarkMessageFailed(clientMessageId);
            AskDJNotice = BackendActionErrorMessage(response.Error, response.Message);
            AddDiagnostic("WRN Ask DJ request failed.");
            return;
        }

        ApplyPairingTransport(response.HomeAssistantLocalUrl, response.HomeAssistantRemoteUrl, response.RemoteSupported);
        ApplyBackendSummary(BackendSummaryFrom(response));
        ApplyDJAnnouncementCapabilities(response.DJAnnouncementCapabilities ?? response.DJAnnouncement ?? _djAnnouncementCapabilities);
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
        responseMessages = responseMessages
            .Select(message => message.IsAssistant && message.Mood is null ? message with { Mood = AskDJMoodValue() } : message)
            .ToList();

        foreach (var message in responseMessages)
        {
            MergeMessage(message);
        }

        if (responseMessages.Count == 0 && (!string.IsNullOrWhiteSpace(response.Text ?? response.DjText ?? response.Message) || response.TrackInsightData is not null))
        {
            var fallbackMessage = new AskDJMessage(
                Guid.NewGuid().ToString("N"),
                "assistant",
                SafeDisplayText(response.Text ?? response.DjText ?? response.Message),
                null,
                DateTimeOffset.Now,
                "assistant",
                response.PlaybackActions,
                response.ConfirmationActions,
                response.Items,
                response.Images,
                response.Sources,
                response.AudioUrl,
                response.Announcement,
                ClientMessageId: clientMessageId,
                Intent: response.Intent,
                Action: response.Action,
                Type: response.Type,
                OpenScreen: response.OpenScreen,
                TrackInsightData: response.TrackInsightData,
                Links: response.Links,
                Mood: AskDJMoodValue())
            {
                IsGeneratedText = response.AssistantMessage is null && response.IsGeneratedText == true
            };
            MergeMessage(fallbackMessage);
            responseMessages = [fallbackMessage];
        }

        var assistantMessage = responseMessages.LastOrDefault(message => message.IsAssistant);
        var trackInsightMessage = responseMessages.LastOrDefault(message => message.IsTrackInsight && message.TrackInsightData is not null);
        if (trackInsightMessage?.TrackInsight is not null)
        {
            TrackInsightPanel = trackInsightMessage.TrackInsight;
            TrackInsightNotice = "";
        }

        ReplaceActions(assistantMessage?.PlaybackActions ?? response.PlaybackActions, assistantMessage?.ConfirmationActions ?? response.ConfirmationActions);
        ReplaceRecentItems(assistantMessage?.Items ?? response.Items);
        await SaveSettingsIfMutableAsync();
        AskDJNotice = "";
        Status = P("Vm_Ask_DJ_updated");
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
        if (!response.RequiresLocalClearAfterClearResponse(_settings.ClearRevision))
        {
            AskDJNotice = P("Vm_Ask_DJ_is_unavailable_4");
            AddDiagnostic("WRN Ask DJ history clear failed.");
            return;
        }

        ClearLocalAskDJState();
        ApplyHistoryRevisions(response);
        await SaveSettingsIfMutableAsync();
        AskDJNotice = "";
        Status = P("Vm_Ask_DJ_history_cleared");
        AddDiagnostic("INF Ask DJ history clear requested.");
    }

    public async Task<string?> ExportAskDJHistoryAsync()
    {
        if (!CanUseAskDJ || IsDemoMode)
        {
            AskDJNotice = P("Vm_Ask_DJ_is_unavailable_4");
            return null;
        }

        ConfigureClient();
        try
        {
            var exportJson = await _apiClient.ExportAskDJHistoryAsync(_identity, CancellationToken.None);
            if (RequiresPairingRecovery(exportJson))
            {
                await ApplyStalePairingAsync("not_configured");
                AskDJNotice = AppStrings.Get("Status_PairAgain");
                return null;
            }

            if (!IsSuccessfulExport(exportJson))
            {
                AskDJNotice = P("Vm_Ask_DJ_export_failed");
                AddDiagnostic("WRN Ask DJ history export failed.");
                return null;
            }

            AskDJNotice = "";
            Status = P("Vm_Ask_DJ_history_exported");
            AddDiagnostic("INF Ask DJ history export downloaded from Home Assistant.");
            return exportJson;
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                AskDJNotice = AppStrings.Get("Status_PairAgain");
                return null;
            }

            if (ApplyVersionMismatch(ex))
            {
                AskDJNotice = P("Vm_Update_required_12");
                AddDiagnostic("WRN Ask DJ history export blocked by version mismatch.");
                return null;
            }

            AskDJNotice = P("Vm_Ask_DJ_export_failed");
            AddDiagnostic("WRN Ask DJ history export failed: " + ex.GetType().Name);
            return null;
        }
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
            if (await ApplyStalePairingAsync(ex))
            {
                if (showStatus)
                {
                    AskDJNotice = AppStrings.Get("Status_PairAgain");
                }

                return;
            }

            if (ApplyVersionMismatch(ex))
            {
                if (showStatus)
                {
                    AskDJNotice = P("Vm_Update_required_12");
                }

                AddDiagnostic("WRN Ask DJ history sync blocked by version mismatch.");
                return;
            }

            if (showStatus)
            {
                AskDJNotice = P("Vm_Ask_DJ_is_unavailable_5");
            }

            AddDiagnostic("WRN Ask DJ history sync failed: " + ex.GetType().Name);
            return;
        }

        if (!response.Success)
        {
            if (await ApplyStalePairingAsync(response.Error))
            {
                if (showStatus)
                {
                    AskDJNotice = P("Vm_Pair_again_to_continue");
                }

                return;
            }

            if (showStatus)
            {
                AskDJNotice = P("Vm_Ask_DJ_is_unavailable_6");
            }

            return;
        }

        if (response.RequiresLocalClearBeforeHistoryMerge(_settings.ClearRevision))
        {
            ClearLocalAskDJState();
        }

        for (var i = 0; i < response.Messages.Count; i++)
        {
            MergeMessage(response.Messages[i] with { ServerOrder = i });
        }

        PruneMessagesOlderThan(response.HistoryTrimmedBefore);
        SortMessages();

        ApplyHistoryRevisions(response);
        await SaveSettingsIfMutableAsync();
    }

    private void ClearLocalAskDJState()
    {
        Messages.Clear();
        Actions.Clear();
        RecentItems.Clear();
    }

    private void ApplyHistoryRevisions(AskDJHistoryResponse response)
    {
        _settings.HistoryRevision = Math.Max(_settings.HistoryRevision, response.HistoryRevision);
        _settings.ClearRevision = Math.Max(_settings.ClearRevision, response.ClearRevision);
    }

    private async Task RunCommandAsync(string command)
    {
        if (IsDemoMode)
        {
            Status = $"{P("Vm_Demo_command")}: {command}";
            AddDiagnostic("INF Demo command executed: " + command);
            return;
        }

        ConfigureClient();
        CommandResponse response;
        try
        {
            response = await _apiClient.RunCommandAsync(_identity, command, null, _language, AskDJMoodValue(), _djAnnouncementOutput, CancellationToken.None);
        }
        catch (Exception ex) when (ApplyVersionMismatch(ex))
        {
            Status = P("Vm_Update_required_13");
            return;
        }

        ApplyVersionCompatibility(response);
        ApplyPairingTransport(response.HomeAssistantLocalUrl, response.HomeAssistantRemoteUrl, response.RemoteSupported);
        ApplyBackendSummary(BackendSummaryFrom(response));
        ApplyDJAnnouncementCapabilities(AnnouncementCapabilitiesFrom(response));
        if (!_runtimeCompatible)
        {
            Status = P("Vm_Update_required_14");
            return;
        }

        Status = response.Success
            ? response.DjText ?? response.Message ?? $"{P("Vm_Command_executed")}: {command}"
            : BackendActionErrorMessage(response.Error, response.Message);
        AddDiagnostic(response.Success ? "INF Command executed: " + command : "WRN Command failed: " + command);
        await RefreshAsync();
    }

    private async Task TogglePlaybackAsync()
    {
        if (!CanStartPlayback)
        {
            Notice = !CanUsePlaybackFeatures
                ? P("Vm_Playback_unavailable")
                : P("Vm_No_output_device_selected_3");
            return;
        }

        await RunPlaybackCommandAsync("toggle_playback");
    }

    public async Task SeekAsync(double positionMs)
    {
        PlaybackPositionMs = positionMs;
        if (!CanUsePlaybackFeatures)
        {
            Notice = P("Vm_Playback_unavailable_3");
            return;
        }

        if (IsDemoMode)
        {
            AddDiagnostic("INF Demo seek updated.");
            return;
        }

        await RunPlaybackCommandAsync("seek", new { position_ms = (int)Math.Round(PlaybackPositionMs) });
    }

    private async Task SeekRelativeAsync(int offsetMs)
    {
        var target = Math.Clamp(PlaybackPositionMs + offsetMs, 0, Math.Max(PlaybackDurationMs, 1));
        await SeekAsync(target);
    }

    private async Task RunPlaybackCommandAsync(string command, object? args = null)
    {
        if (!CanUsePlaybackFeatures)
        {
            Notice = P("Vm_Playback_unavailable_4");
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
            response = await _apiClient.RunCommandAsync(_identity, command, args, _language, AskDJMoodValue(), _djAnnouncementOutput, CancellationToken.None);
        }
        catch (Exception ex) when (ApplyVersionMismatch(ex))
        {
            Notice = P("Vm_Update_required_15");
            AddDiagnostic("WRN Playback command blocked by version mismatch: " + command);
            return;
        }

        ApplyVersionCompatibility(response);
        ApplyPairingTransport(response.HomeAssistantLocalUrl, response.HomeAssistantRemoteUrl, response.RemoteSupported);
        ApplyBackendSummary(BackendSummaryFrom(response));
        ApplyDJAnnouncementCapabilities(AnnouncementCapabilitiesFrom(response));
        if (!_runtimeCompatible)
        {
            Notice = P("Vm_Update_required_16");
            AddDiagnostic("WRN Playback command blocked by incompatible runtime: " + command);
            return;
        }

        if (!response.Success)
        {
            Notice = BackendActionErrorMessage(response.Error, response.Message);
            AddDiagnostic("WRN Playback command failed: " + command);
            return;
        }

        Notice = "";
        Status = response.DjText ?? response.Message ?? P("Vm_Command_executed_3");
        AddDiagnostic("INF Playback command executed: " + command);
        await RefreshAsync();
    }

    private async Task SaveCurrentTrackAsync()
    {
        await RunSaveCurrentTrackAsync(fromAskDJ: false);
    }

    private async Task OpenTrackInsightAsync()
    {
        TrackInsightNotice = "";
        TrackInsightPanel = null;

        if (!CanUsePlaybackFeatures)
        {
            TrackInsightNotice = !_runtimeCompatible ? P("Vm_Update_required_17") : P("Vm_Track_Insight_is_unavailable");
            return;
        }

        if (IsDemoMode)
        {
            TrackInsightPanel = TrackInsightPresentation.From(new TrackInsightResult(
                new TrackInsightTrack(TrackTitle, TrackArtist, TrackAlbum),
                1,
                "available",
                "demo",
                "medium",
                new TrackInsightAnalysis(
                    [
                        new TrackInsightSection("vibe", "mood", "Vibe", "Vibe", null, "Shimmering synth-pop with a night-drive pulse.", null, "demo", "medium", null)
                    ],
                    null,
                    [
                        new TrackInsightTip("fit", "music_dna", "Why it fits you", null, "This expands your Music DNA.", "demo", "medium")
                    ],
                    null,
                    null),
                null,
                null,
                null,
                null,
                null,
                new TrackInsightMusicDna(84, "Matches your energetic synth-pop lane.", null),
                new TrackInsightVisualProfile("Neon, wistful, driving", ["indigo", "cyan"], "slow pulse"),
                new TrackInsightCache(false, DateTimeOffset.Now),
                null));
            TrackInsightNotice = "";
            return;
        }

        ConfigureClient();
        var request = new TrackInsightRequest(
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            new TrackInsightRequestTrack(
                string.IsNullOrWhiteSpace(_trackTitle) ? null : _trackTitle,
                string.IsNullOrWhiteSpace(_trackArtist) ? null : _trackArtist,
                string.IsNullOrWhiteSpace(_trackAlbum) ? null : _trackAlbum,
                string.IsNullOrWhiteSpace(_artworkUrl) ? null : _artworkUrl),
            MusicBackend: _musicBackendSummary.Backend,
            Language: AppStrings.NormalizeApiLocale(_language),
            Locale: AppStrings.NormalizeApiLocale(_language),
            Mood: AskDJMoodValue(),
            MusicDnaKey: MusicDnaKey(),
            IncludeVisualProfile: true,
            ClientId: _identity.DeviceId);

        try
        {
            var response = await _apiClient.GetTrackInsightAsync(request, CancellationToken.None);
            if (!response.Success)
            {
                TrackInsightNotice = string.Equals(response.Error, "no_track_playing", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(response.Error, "track_insight_failed", StringComparison.OrdinalIgnoreCase)
                    ? P("Vm_No_active_track_to_show")
                    : response.Message ?? response.Error ?? P("Vm_Track_Insight_is_unavailable_3");
                return;
            }

            TrackInsightPanel = TrackInsightPresentation.From(response.ResolvedTrackInsight);
            TrackInsightNotice = HasTrackInsightPanel ? "" : P("Vm_No_Track_Insight_available");
            AddDiagnostic("INF Track Insight loaded from Home Assistant.");
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                TrackInsightNotice = P("Vm_Pair_again_to_continue_3");
                return;
            }

            if (ApplyVersionMismatch(ex))
            {
                TrackInsightNotice = P("Vm_Update_required_18");
                AddDiagnostic("WRN Track Insight blocked by version mismatch.");
                return;
            }

            TrackInsightNotice = P("Vm_Track_Insight_is_unavailable_4");
            AddDiagnostic("WRN Track Insight failed: " + ex.GetType().Name);
        }
    }

    private async Task RefreshMusicDnaAsync()
    {
        MusicDnaNotice = "";

        if (IsDemoMode)
        {
            MusicDnaDashboard = MusicDnaDashboard.From(new MusicDnaProfileResponse(true, true, DemoMusicDnaProfile()));
            AddDiagnostic("INF Demo Music DNA loaded.");
            return;
        }

        if (!CanUseMusicDna)
        {
            MusicDnaNotice = !_runtimeCompatible ? P("Vm_Update_required_17") : "Music DNA is unavailable";
            return;
        }

        IsLoadingMusicDna = true;
        try
        {
            ConfigureClient();
            var response = await _apiClient.GetMusicDnaProfileAsync(MusicDnaProfileRequest(), CancellationToken.None);
            if (!response.Success)
            {
                if (await ApplyStalePairingAsync(response.Error))
                {
                    MusicDnaNotice = AppStrings.Get("Status_PairAgain");
                    return;
                }

                MusicDnaNotice = response.Message ?? response.Error ?? "Music DNA is unavailable";
                AddDiagnostic("WRN Music DNA profile request failed.");
                return;
            }

            MusicDnaDashboard = MusicDnaDashboard.From(response);
            MusicDnaNotice = response.Enabled == false ? "Music DNA is off. Enable it to build your profile in Home Assistant." : "";
            AddDiagnostic("INF Music DNA profile refreshed.");
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                MusicDnaNotice = AppStrings.Get("Status_PairAgain");
                return;
            }

            if (ApplyVersionMismatch(ex))
            {
                MusicDnaNotice = P("Vm_Update_required_18");
                return;
            }

            MusicDnaNotice = "Music DNA is unavailable";
            AddDiagnostic("WRN Music DNA profile request failed: " + ex.GetType().Name);
        }
        finally
        {
            IsLoadingMusicDna = false;
        }
    }

    private async Task UpdateMusicDnaEnabledAsync(bool enabled)
    {
        if (!CanUseMusicDna)
        {
            MusicDnaNotice = "Music DNA is unavailable";
            return;
        }

        if (IsDemoMode)
        {
            MusicDnaDashboard = enabled
                ? MusicDnaDashboard.From(new MusicDnaProfileResponse(true, true, DemoMusicDnaProfile()))
                : new MusicDnaDashboard(false, "", [], "");
            MusicDnaNotice = enabled ? "Music DNA enabled in Demo Mode." : "Music DNA disabled in Demo Mode.";
            return;
        }

        IsLoadingMusicDna = true;
        try
        {
            ConfigureClient();
            var response = await _apiClient.UpdateMusicDnaSettingsAsync(MusicDnaSettingsRequest(enabled), CancellationToken.None);
            if (!response.Success)
            {
                if (await ApplyStalePairingAsync(response.Error))
                {
                    MusicDnaNotice = AppStrings.Get("Status_PairAgain");
                    return;
                }

                MusicDnaNotice = response.Message ?? response.Error ?? "Music DNA settings could not be saved.";
                return;
            }

            MusicDnaNotice = enabled ? "Music DNA enabled." : "Music DNA disabled and server knowledge cleared.";
            await RefreshMusicDnaAsync();
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                MusicDnaNotice = AppStrings.Get("Status_PairAgain");
                return;
            }

            MusicDnaNotice = "Music DNA settings could not be saved.";
            AddDiagnostic("WRN Music DNA settings request failed: " + ex.GetType().Name);
        }
        finally
        {
            IsLoadingMusicDna = false;
        }
    }

    private async Task ClearMusicDnaAsync()
    {
        if (!CanUseMusicDna || !MusicDnaDashboard.Enabled)
        {
            return;
        }

        if (IsDemoMode)
        {
            MusicDnaDashboard = MusicDnaDashboard.From(new MusicDnaProfileResponse(
                true,
                true,
                new MusicDnaProfile(
                    "Music DNA is enabled. New listening signals will appear here after playback.",
                    FavoriteGenres: null,
                    FavoriteArtists: null,
                    RecentTracks: null,
                    RecentFavoriteTracks: null,
                    EnergyProfile: null,
                    MoodProfile: null,
                    Mood: null,
                    TasteDirection: null,
                    BasedOn: null,
                    UpdatedAt: null)));
            MusicDnaNotice = "Music DNA profile cleared in Demo Mode.";
            return;
        }

        IsLoadingMusicDna = true;
        try
        {
            ConfigureClient();
            var response = await _apiClient.ClearMusicDnaAsync(MusicDnaClearRequest(), CancellationToken.None);
            if (!response.Success)
            {
                if (await ApplyStalePairingAsync(response.Error))
                {
                    MusicDnaNotice = AppStrings.Get("Status_PairAgain");
                    return;
                }

                MusicDnaNotice = response.Message ?? response.Error ?? "Music DNA could not be cleared.";
                return;
            }

            MusicDnaNotice = "Music DNA profile cleared. Opt-in setting was kept.";
            await RefreshMusicDnaAsync();
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                MusicDnaNotice = AppStrings.Get("Status_PairAgain");
                return;
            }

            MusicDnaNotice = "Music DNA could not be cleared.";
            AddDiagnostic("WRN Music DNA clear request failed: " + ex.GetType().Name);
        }
        finally
        {
            IsLoadingMusicDna = false;
        }
    }

    public async Task OpenDiscoverAsync()
    {
        DiscoverNotice = "";
        if (!MusicDnaDashboard.Enabled)
        {
            await RefreshMusicDnaAsync();
        }

        if (MusicDnaDashboard.Enabled)
        {
            await LoadDiscoverAsync(forceRefresh: false);
        }
        else
        {
            ReplaceDiscoverItems([]);
            RaiseDiscoverProperties();
        }
    }

    private async Task EnableDiscoverMusicDnaAsync()
    {
        _discoverConsentRejected = false;
        await UpdateMusicDnaEnabledAsync(true);
        if (MusicDnaDashboard.Enabled)
        {
            await LoadDiscoverAsync(forceRefresh: false);
        }
        RaiseDiscoverProperties();
    }

    private async Task LoadDiscoverAsync(bool forceRefresh)
    {
        DiscoverNotice = "";
        if (!CanUseMusicDna)
        {
            DiscoverNotice = "Ontdek is unavailable.";
            return;
        }

        if (!MusicDnaDashboard.Enabled)
        {
            ReplaceDiscoverItems([]);
            RaiseDiscoverProperties();
            return;
        }

        IsLoadingDiscover = true;
        try
        {
            ConfigureClient();
            var response = forceRefresh
                ? await _apiClient.RefreshMusicDiscoveryAsync(MusicDiscoveryRequest(), CancellationToken.None)
                : await _apiClient.GetMusicDiscoveryAsync(MusicDiscoveryRequest(), CancellationToken.None);
            if (!response.Success)
            {
                if (await ApplyStalePairingAsync(response.Error))
                {
                    DiscoverNotice = AppStrings.Get("Status_PairAgain");
                    return;
                }

                if (response.Enabled == false || string.Equals(response.Error, "music_dna_disabled", StringComparison.OrdinalIgnoreCase))
                {
                    MusicDnaDashboard = new MusicDnaDashboard(false, "", [], "");
                    DiscoverNotice = "Ontdek werkt alleen als Music DNA is geactiveerd.";
                    return;
                }

                DiscoverNotice = response.EmptyState ?? response.Message ?? response.Error ?? "Ontdek kon niet worden geladen.";
                return;
            }

            if (!response.CanRenderFeed)
            {
                MusicDnaDashboard = new MusicDnaDashboard(false, "", [], "");
                ReplaceDiscoverItems([]);
                DiscoverNotice = "Ontdek werkt alleen als Music DNA is geactiveerd.";
                return;
            }

            ReplaceDiscoverItems(response.DisplayItems);
            DiscoverNotice = DiscoverItems.Count == 0 ? response.EmptyState ?? response.Message ?? "Nog geen aanbevelingen beschikbaar." : "";
            AddDiagnostic(forceRefresh ? "INF Music Discovery refreshed." : "INF Music Discovery loaded.");
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                DiscoverNotice = AppStrings.Get("Status_PairAgain");
                return;
            }

            DiscoverNotice = DiscoverItems.Count == 0 ? "Ontdek kon niet worden geladen." : "Ontdek refresh is niet gelukt.";
            AddDiagnostic("WRN Music Discovery request failed: " + ex.GetType().Name);
        }
        finally
        {
            IsLoadingDiscover = false;
            RaiseDiscoverProperties();
        }
    }

    public async Task PlayDiscoveryItemAsync(MusicDiscoveryItem item)
    {
        if (!MusicDnaDashboard.Enabled || !item.HasContent)
        {
            return;
        }

        ReplaceDiscoverItem(item, item with { IsPlaying = true, PlaySucceeded = false });
        try
        {
            ConfigureClient();
            var response = await _apiClient.PlayMusicDiscoveryAsync(MusicDiscoveryPlayRequest(item), CancellationToken.None);
            if (!response.Success)
            {
                if (await ApplyStalePairingAsync(response.Error))
                {
                    DiscoverNotice = AppStrings.Get("Status_PairAgain");
                    return;
                }

                DiscoverNotice = response.Message ?? response.Error ?? "Play Now failed.";
                ReplaceDiscoverItem(item, item with { IsPlaying = false, PlaySucceeded = false });
                return;
            }

            ReplaceDiscoverItem(item, item with { IsPlaying = false, PlaySucceeded = true });
            DiscoverNotice = response.Message ?? "Started from Ontdek.";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                DiscoverNotice = AppStrings.Get("Status_PairAgain");
                return;
            }

            DiscoverNotice = "Play Now failed.";
            ReplaceDiscoverItem(item, item with { IsPlaying = false, PlaySucceeded = false });
            AddDiagnostic("WRN Music Discovery play failed: " + ex.GetType().Name);
        }
    }

    private async Task<CommandResponse?> RunSaveCurrentTrackAsync(bool fromAskDJ)
    {
        if (!CanUsePlaybackFeatures)
        {
            SetSaveCurrentTrackFailureNotice(fromAskDJ);
            return null;
        }

        if (IsDemoMode)
        {
            var message = P("Vm_Track_saved_to_favorites");
            if (fromAskDJ)
            {
                AskDJNotice = message;
            }
            else
            {
                Notice = message;
            }

            AddDiagnostic("INF Demo save current track executed.");
            return new CommandResponse(true, message, message, null);
        }

        ConfigureClient();
        CommandResponse response;
        try
        {
            response = await _apiClient.RunCommandAsync(_identity, "save_current_track", null, _language, AskDJMoodValue(), _djAnnouncementOutput, CancellationToken.None);
        }
        catch (Exception ex) when (ApplyVersionMismatch(ex))
        {
            if (fromAskDJ)
            {
                AskDJNotice = P("Vm_Update_required_19");
            }
            else
            {
                Notice = P("Vm_Update_required_20");
            }

            AddDiagnostic("WRN Save current track blocked by version mismatch.");
            return null;
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                if (fromAskDJ)
                {
                    AskDJNotice = P("Vm_Pair_again_to_continue_4");
                }
                else
                {
                    Notice = P("Vm_Pair_again_to_continue_5");
                }

                return null;
            }

            SetSaveCurrentTrackFailureNotice(fromAskDJ);
            AddDiagnostic("WRN Save current track failed: " + ex.GetType().Name);
            return null;
        }

        ApplyVersionCompatibility(response);
        if (!_runtimeCompatible)
        {
            if (fromAskDJ)
            {
                AskDJNotice = P("Vm_Update_required_21");
            }
            else
            {
                Notice = P("Vm_Update_required_22");
            }

            AddDiagnostic("WRN Save current track blocked by incompatible runtime.");
            return null;
        }

        if (!response.Success)
        {
            SetSaveCurrentTrackFailureNotice(fromAskDJ);
            AddDiagnostic("WRN Save current track failed.");
            return response;
        }

        var success = response.DjText ?? response.Message ?? P("Vm_Track_saved_to_favorites_3");
        if (fromAskDJ)
        {
            AskDJNotice = success;
        }
        else
        {
            Notice = success;
        }

        Status = success;
        AddDiagnostic("INF Save current track executed.");
        await RefreshAsync();
        return response;
    }

    private void SetSaveCurrentTrackFailureNotice(bool fromAskDJ)
    {
        var message = P("Vm_The_track_could_not_be_saved");
        if (fromAskDJ)
        {
            AskDJNotice = message;
        }
        else
        {
            Notice = message;
        }
    }

    private async Task RefreshQueueAsync()
    {
        if (!CanUsePlaybackFeatures)
        {
            QueueNotice = !_runtimeCompatible ? P("Vm_Update_required_23") : P("Vm_Playback_unavailable_5");
            return;
        }

        if (IsDemoMode)
        {
            LoadDemoQueueItems(reset: true);
            QueueNotice = QueueItems.Count == 0 ? P("Vm_No_queue") : "";
            return;
        }

        IsLoadingQueue = true;
        try
        {
            ConfigureClient();
            var response = await _apiClient.RunCommandAsync(_identity, "queue", new { limit = 100 }, _language, AskDJMoodValue(), _djAnnouncementOutput, CancellationToken.None);
            if (!response.Success)
            {
                QueueNotice = P("Vm_Home_Assistant_is_unreachable_4");
                AddDiagnostic("WRN Queue refresh failed.");
                return;
            }

            ApplyVersionCompatibility(response);
            ReplaceQueueItems(response.ResolvedQueue());
            QueueNotice = !_runtimeCompatible
                ? P("Vm_Update_required_24")
                : QueueItems.Count == 0 ? P("Vm_No_queue_3") : "";
            RaisePlaybackStateProperties();
        }
        catch (Exception ex)
        {
            if (ApplyVersionMismatch(ex))
            {
                QueueNotice = P("Vm_Update_required_25");
                AddDiagnostic("WRN Queue refresh blocked by version mismatch.");
                return;
            }

            QueueNotice = P("Vm_Home_Assistant_is_unreachable_5");
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
            QueueNotice = !_runtimeCompatible ? P("Vm_Update_required_26") : P("Vm_Playback_unavailable_6");
            return;
        }

        if (SelectedOutput is null)
        {
            QueueNotice = P("Vm_No_output_device_selected_4");
            return;
        }

        if (!item.IsPlayable)
        {
            QueueNotice = P("Vm_Playback_unavailable_7");
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
            PlaylistNotice = !_runtimeCompatible ? P("Vm_Update_required_27") : P("Vm_Playback_unavailable_8");
            return;
        }

        if (IsDemoMode)
        {
            LoadDemoPlaylists(reset: true);
            PlaylistNotice = FilteredPlaylistItems.Count == 0 ? P("Vm_No_playlists") : "";
            return;
        }

        IsLoadingPlaylists = true;
        try
        {
            ConfigureClient();
            var response = await _apiClient.GetStatusAsync(_identity, _language, AskDJMoodValue(), CancellationToken.None);
            if (!response.Success)
            {
                ReplacePlaylistItems([]);
                PlaylistNotice = P("Vm_Home_Assistant_is_unreachable_6");
                AddDiagnostic("WRN Playlist refresh failed.");
                return;
            }

            _backendAvailable = response.BackendAvailable ?? true;
            ApplyVersionCompatibility(response);
            ReplacePlaylistItems(response.ResolvedPlaylists());
            PlaylistNotice = !_runtimeCompatible
                ? P("Vm_Update_required_28")
                : PlaylistItems.Count == 0 ? P("Vm_No_playlists_3") : "";
            RaisePlaybackStateProperties();
        }
        catch (Exception ex)
        {
            if (ApplyVersionMismatch(ex))
            {
                PlaylistNotice = P("Vm_Update_required_29");
                AddDiagnostic("WRN Playlist refresh blocked by version mismatch.");
                return;
            }

            ReplacePlaylistItems([]);
            PlaylistNotice = P("Vm_Home_Assistant_is_unreachable_7");
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
            PlaylistNotice = !_runtimeCompatible ? P("Vm_Update_required_30") : P("Vm_Playback_unavailable_9");
            return;
        }

        if (SelectedOutput is null)
        {
            PlaylistNotice = P("Vm_No_output_device_selected_5");
            return;
        }

        if (!playlist.IsPlayable)
        {
            PlaylistNotice = P("Vm_Playback_unavailable_10");
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
        Status = P("Vm_Demo_mode_4");
        NowPlaying = "Midnight City - M83";
        ClearDemoState();
        ClearRuntimePlaybackState();
        LoadDemoData();
        LoadDemoAskDJMessages();
        AddDiagnostic("INF Demo mode started.");
    }

    private async Task StopDemoModeAsync()
    {
        if (IsMonkeyTestMode)
        {
            Status = P("Vm_Monkey_test_Demo_Mode_stays_active");
            AddDiagnostic("INF Monkey test suppressed stopping Demo Mode.");
            return;
        }

        IsDemoMode = false;
        ClearDemoState();
        ClearRuntimePlaybackState();
        if (!IsPaired)
        {
            PairingCode = "";
            _settings.PairingCode = PairingCode;
            IsOnboardingVisible = false;
            _settings.DJConnectWelcomeSeen = true;
            _settings.HasCompletedOnboarding = true;
            IsPairingOverlayVisible = true;
            Status = P("Vm_Not_paired_3");
            await SaveSettingsIfMutableAsync();
        }
        else
        {
            IsPairingOverlayVisible = false;
            Status = P("Vm_Paired_5");
            await RefreshAsync();
        }

        Status = IsPaired ? P("Vm_Paired_6") : P("Vm_Not_paired_4");
        AddDiagnostic("INF Demo mode stopped.");
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
            await Task.CompletedTask;
        }

        await SaveSettingsIfMutableAsync();
        AddDiagnostic("INF Onboarding completed; local HA pairing screen is available.");
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
        WhatsNewTitle = P("Vm_What_s_New_3");
        WhatsNewBody = P("Vm_Loading_release_notes");
        var language = AppStrings.NormalizeLanguage(Language);
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
                    WhatsNewTitle = SafeReleaseText(note.Name) ?? $"{P("Vm_What_s_New_in")} DJConnect {AppVersion}";
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

        WhatsNewTitle = P("Vm_What_s_New_4");
        WhatsNewBody = P("Vm_WhatsNewLoadFailedBody");
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
        return Task.CompletedTask;
    }

    public async Task ApplyPairingDeepLinkAsync(string payload)
    {
        if (!PairingDeepLinkPayload.TryParse(payload, out var parsed, out var failureReason))
        {
            Status = failureReason switch
            {
                "client_type" => ApiErrorLocalizer.Pairing("invalid_client_type"),
                "pair_path" => ApiErrorLocalizer.Pairing("invalid_client_type"),
                "pair_code" => AppStrings.Get("Pairing_InvalidCode"),
                "ha_url" => AppStrings.Get("Pairing_InvalidUrl"),
                _ => ApiErrorLocalizer.Pairing((string?)null)
            };
            Notice = Status;
            AddDiagnostic("WRN Pairing deeplink rejected: " + failureReason);
            return;
        }

        HomeAssistantUrl = parsed.HomeAssistantUrl;
        PairingCode = parsed.PairCode;
        IsOnboardingVisible = false;
        IsPairingOverlayVisible = true;
        IsPairingSuccessVisible = false;
        AddDiagnostic("INF Pairing deeplink accepted for Windows setup flow.");
        await PairAsync();
    }

    private Task HidePairingAsync()
    {
        IsPairingOverlayVisible = false;
        IsPairingSuccessVisible = false;
        return Task.CompletedTask;
    }

    private async Task CompletePairingSuccessAsync()
    {
        IsPairingSuccessVisible = false;
        IsPairingOverlayVisible = false;
        await RefreshAsync();
        EvaluateWakewordPrompt();
    }

    private async Task ResetPairingAsync()
    {
        if (IsMonkeyTestMode)
        {
            Status = P("Vm_Monkey_test_pairing_reset_suppressed");
            AddDiagnostic("INF Monkey test suppressed pairing reset.");
            return;
        }

        _credentialStore.DeleteToken();
        Token = "";
        IsPaired = false;
        IsDemoMode = false;
        _identity = ClientIdentity.CreateOrLoad(ClientIdentity.CreateInstallId(), _settings.DeviceName);
        PairingCode = "";
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
        Status = P("Vm_Ready_to_pair_again");
        AddDiagnostic("INF Pairing reset: identity rotated and local pair code entry cleared.");
    }

    private async Task CopyLogsAsync()
    {
        if (IsMonkeyTestMode)
        {
            LogNotice = P("Vm_Monkey_test_clipboard_unchanged");
            AddDiagnostic("INF Monkey test suppressed log copy.");
            return;
        }

        await Clipboard.Default.SetTextAsync(RedactedDiagnosticExport());
        PermissionNotice = P("Vm_Logs_copied_with_redaction");
        LogNotice = P("Vm_Logs_copied_to_clipboard");
        AddDiagnostic("INF Diagnostic logs copied with redaction.");
    }

    public async Task ClearLogsAsync()
    {
        if (IsMonkeyTestMode)
        {
            LogNotice = P("Vm_Monkey_test_logs_not_cleared");
            AddDiagnostic("INF Monkey test suppressed log clear.");
            return;
        }

        LogSearchText = "";
        _selectedLogSearchResultIndex = 0;
        DiagnosticLogLines.Clear();
        FilteredDiagnosticLogLines.Clear();
        _settings.DiagnosticLogLines.Clear();
        await SaveSettingsIfMutableAsync();
        ApplyLogFilter();
        OnPropertyChanged(nameof(HasDiagnosticLogs));
        OnPropertyChanged(nameof(HasNoDiagnosticLogs));
        OnPropertyChanged(nameof(LogSearchResultCount));
        OnPropertyChanged(nameof(LogSearchResultLabel));
        PermissionNotice = P("Vm_Logs_cleared");
        LogNotice = P("Vm_Logs_cleared_3");
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
            WakewordNotice = P("Vm_Voice_activation_is_not_available_in_this_bu");
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
        WakewordNotice = P("Vm_Voice_activation_enabled");
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
        WakewordNotice = P("Vm_Open_Privacy_for_microphone_and_Ask_DJ_detai");
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
            ? P("Vm_FeedbackDescriptionPlaceholder")
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
        AppendFastPathDiagnostics(body);
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
        AppendFastPathDiagnostics(body);
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

    private void AppendFastPathDiagnostics(StringBuilder body)
    {
        FastPathDiagnosticsFormatter.AppendTo(body, _apiClient.FastPathDiagnostics);
    }

    private async Task<HomeAssistantTransportState> ConfigureClientAsync(bool pairingOnly)
    {
        _transportManager.UpdateUrls(HomeAssistantUrl, HomeAssistantRemoteUrl, _settings.RemoteSupported);
        var state = pairingOnly
            ? await _transportManager.ResolvePairingAsync(HomeAssistantUrl, CancellationToken.None)
            : await _transportManager.ResolveRuntimeAsync(CancellationToken.None);
        _connectionMode = state.Mode;
        if (!string.IsNullOrWhiteSpace(state.ActiveUrl))
        {
            ConfigureApiClientForTransport(state.ActiveUrl, state.Mode);
        }

        RaiseTransportProperties();
        return state;
    }

    private void ConfigureClient()
    {
        var activeUrl = _transportManager.Current.ActiveUrl ?? HomeAssistantUrl;
        ConfigureApiClientForTransport(activeUrl, _transportManager.Current.Mode);
    }

    private void ConfigureClient(string activeUrl)
    {
        ConfigureApiClientForTransport(activeUrl, HomeAssistantConnectionMode.Local);
        _connectionMode = HomeAssistantConnectionMode.Local;
        RaiseTransportProperties();
    }

    private void ConfigureApiClientForTransport(string activeUrl, HomeAssistantConnectionMode mode)
    {
        _apiClient.Configure(new DJConnectClientConfiguration(
            activeUrl,
            Token,
            _transportOptions.AllowsWebSocketFastPath(mode),
            _transportOptions.HomeAssistantWebSocketAuthToken,
            _identity.DeviceId));
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

    private void ApplyPairingTransport(string? localUrl, string? remoteUrl, bool? remoteSupported)
    {
        _transportManager.UpdateUrls(localUrl, remoteUrl, remoteSupported);
        HomeAssistantUrl = _transportManager.Current.LocalUrl;
        HomeAssistantRemoteUrl = _transportManager.Current.RemoteUrl;
        _settings.HomeAssistantUrl = HomeAssistantUrl;
        _settings.HomeAssistantLocalUrl = HomeAssistantUrl;
        _settings.HomeAssistantRemoteUrl = HomeAssistantRemoteUrl;
        _settings.RemoteSupported = _transportManager.Current.RemoteSupported;
        RaiseTransportProperties();
    }

    private void ApplyBackendSummary(MusicBackendSummary summary)
    {
        _musicBackendSummary = summary;
        _settings.MusicBackendRevision = summary.Revision?.ToString() ?? "";
        RaiseTransportProperties();
        RaisePlaybackStateProperties();
    }

    private static MusicBackendSummary BackendSummaryFrom(PairingResponse response) => new(
        response.MusicBackend,
        response.MusicBackendName,
        response.MusicBackendAvailable,
        response.MusicBackendRevision,
        response.MusicBackendCapabilities,
        response.MusicTargetPlayer,
        response.MusicBackendError);

    private static MusicBackendSummary BackendSummaryFrom(StatusResponse response) => new(
        response.MusicBackend,
        response.MusicBackendName,
        response.MusicBackendAvailable,
        response.MusicBackendRevision,
        response.MusicBackendCapabilities,
        response.MusicTargetPlayer,
        response.MusicBackendError);

    private static MusicBackendSummary BackendSummaryFrom(CommandResponse response) => new(
        response.MusicBackend,
        response.MusicBackendName,
        response.MusicBackendAvailable,
        response.MusicBackendRevision,
        response.MusicBackendCapabilities,
        response.MusicTargetPlayer,
        response.MusicBackendError);

    private static MusicBackendSummary BackendSummaryFrom(AskDJMessageResponse response) => new(
        response.MusicBackend,
        response.MusicBackendName,
        response.MusicBackendAvailable,
        response.MusicBackendRevision,
        response.MusicBackendCapabilities,
        response.MusicTargetPlayer,
        response.MusicBackendError);

    private static DJAnnouncementCapabilities AnnouncementCapabilitiesFrom(PairingResponse response) =>
        response.DJAnnouncementCapabilities ?? response.DJAnnouncement ?? new DJAnnouncementCapabilities(false, null, null, null, null, null, null, null);

    private static DJAnnouncementCapabilities AnnouncementCapabilitiesFrom(StatusResponse response) =>
        response.DJAnnouncementCapabilities ?? response.DJAnnouncement ?? new DJAnnouncementCapabilities(false, null, null, null, null, null, null, null);

    private static DJAnnouncementCapabilities AnnouncementCapabilitiesFrom(CommandResponse response) =>
        response.DJAnnouncementCapabilities ?? response.DJAnnouncement ?? new DJAnnouncementCapabilities(false, null, null, null, null, null, null, null);

    private void ApplyDJAnnouncementCapabilities(DJAnnouncementCapabilities capabilities)
    {
        _djAnnouncementCapabilities = capabilities;
        if (!_djAnnouncementCapabilities.Supports(_djAnnouncementOutput))
        {
            _djAnnouncementOutput = _djAnnouncementCapabilities.EffectiveDefaultOutput();
            _settings.DJAnnouncementOutput = DJAnnouncementOutputProtocol.Format(_djAnnouncementOutput);
            OnPropertyChanged(nameof(DJAnnouncementOutput));
        }

        OnPropertyChanged(nameof(DJAnnouncementOutputOptions));
        OnPropertyChanged(nameof(DJAnnouncementSpeakerText));
        OnPropertyChanged(nameof(DJAnnouncementOutputHelperText));
    }

    private static string DJAnnouncementOutputLabel(DJAnnouncementOutputKind output) => output switch
    {
        DJAnnouncementOutputKind.Both => "Dit apparaat + Home Assistant speaker",
        DJAnnouncementOutputKind.HaSpeaker => "Alleen Home Assistant speaker",
        DJAnnouncementOutputKind.TextOnly => "Alleen tekst",
        _ => "Alleen dit apparaat"
    };

    private static DJAnnouncementOutputKind DJAnnouncementOutputFromLabel(string? label)
    {
        return label switch
        {
            "Dit apparaat + Home Assistant speaker" => DJAnnouncementOutputKind.Both,
            "Alleen Home Assistant speaker" => DJAnnouncementOutputKind.HaSpeaker,
            "Alleen tekst" => DJAnnouncementOutputKind.TextOnly,
            _ => DJAnnouncementOutputKind.ClientDevice
        };
    }

    private void ApplyVersionCompatibilityResult(VersionCompatibilityResult result)
    {
        HomeAssistantVersionText = string.IsNullOrWhiteSpace(result.HomeAssistantMajorMinor)
            ? P("Vm_Home_Assistant_integration_unknown")
            : $"Home Assistant integration: {result.HomeAssistantMajorMinor}.x";

        if (result.IsCompatible)
        {
            UpdateRequiredMessage = "";
            _runtimeCompatible = true;
            RaisePlaybackStateProperties();
            return;
        }

        UpdateRequiredMessage = AppStrings.Format("Format_UpdateRequiredMessage", result.RequiredMajorMinor);
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

    private async Task<bool> ApplyStalePairingAsync(Exception ex)
    {
        return await ApplyStalePairingAsync(ex.Message);
    }

    private async Task<bool> ApplyStalePairingAsync(string? error)
    {
        if (!IsStalePairingError(error))
        {
            return false;
        }

        if (!IsMonkeyTestMode)
        {
            _credentialStore.DeleteToken();
        }

        Token = "";
        IsPaired = false;
        IsDemoMode = false;
        _settings.HistoryRevision = 0;
        _settings.ClearRevision = 0;
        _backendAvailable = false;
        Messages.Clear();
        Actions.Clear();
        RecentItems.Clear();
        MusicDnaDashboard = new MusicDnaDashboard(false, "", [], "");
        ReplaceDiscoverItems([]);
        IsPairingSuccessVisible = false;
        IsPairingOverlayVisible = !IsOnboardingVisible;
        await SaveSettingsIfMutableAsync();
        RaisePlaybackStateProperties();
        RaiseSettingsStatusProperties();
        Status = AppStrings.Get("Status_PairAgain");
        AddDiagnostic("WRN Stale pairing cleared local token and Ask DJ cache.");
        return true;
    }

    private static bool IsStalePairingError(string? error)
    {
        return PairingStatePolicy.RequiresLocalPairingCleanup(error);
    }

    private static bool RequiresPairingRecovery(string json)
    {
        return TryReadExportString(json, "error", out var error) && IsStalePairingError(error);
    }

    private static bool IsSuccessfulExport(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("success", out var success)
                && success.ValueKind == JsonValueKind.True
                && root.TryGetProperty("format", out var format)
                && string.Equals(format.GetString(), "djconnect.ask_dj.history.export", StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadExportString(string json, string propertyName, out string? value)
    {
        value = null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty(propertyName, out var property)
                || property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = property.GetString();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsValidHomeAssistantUrl(string? url)
    {
        return PairingDeepLinkPayload.IsValidHomeAssistantUrl(url);
    }

    private static bool IsValidPairCode(string? pairCode)
    {
        return PairingDeepLinkPayload.IsValidPairCode(pairCode);
    }

    private static bool IsWindowsIdentity(ClientIdentity identity)
    {
        return string.Equals(identity.ClientType, DJConnectContract.ClientType, StringComparison.Ordinal)
            && identity.DeviceId.StartsWith($"{DJConnectContract.DeviceIdPrefix}-", StringComparison.Ordinal);
    }

    private static bool IsValidPairingResponse(PairingResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.ClientType)
            && !string.Equals(response.ClientType, DJConnectContract.ClientType, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(response.DeviceId)
            && !response.DeviceId.StartsWith($"{DJConnectContract.DeviceIdPrefix}-", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private string PairingErrorMessage(string? error, string? message)
    {
        if (string.Equals(error, "not_configured", StringComparison.OrdinalIgnoreCase)
            || string.Equals(message, "not_configured", StringComparison.OrdinalIgnoreCase))
        {
            return ApiErrorLocalizer.Pairing(error, message);
        }

        if (string.Equals(error, "invalid_pair_code", StringComparison.OrdinalIgnoreCase)
            || string.Equals(error, "wrong_pair_code", StringComparison.OrdinalIgnoreCase)
            || string.Equals(error, "invalid_code", StringComparison.OrdinalIgnoreCase))
        {
            return ApiErrorLocalizer.Pairing(error, message);
        }

        if (IsStalePairingError(error) || IsStalePairingError(message))
        {
            return ApiErrorLocalizer.StaleAuth();
        }

        return ApiErrorLocalizer.Pairing(error, message);
    }

    private string BackendActionErrorMessage(string? error, string? message)
    {
        if (string.Equals(error, "stale_backend_action", StringComparison.OrdinalIgnoreCase))
        {
            return ApiErrorLocalizer.BackendAction(error, message);
        }

        if (string.Equals(error, "unsupported_backend_capability", StringComparison.OrdinalIgnoreCase))
        {
            return ApiErrorLocalizer.BackendAction(error, message);
        }

        return ApiErrorLocalizer.BackendAction(error, message);
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
            Notice = P("Vm_Version_check_failed");
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
        if (action.IsSaveCurrentTrackControl)
        {
            await RunSaveCurrentTrackAsync(fromAskDJ: true);
            return;
        }

        if (!CanUseAskDJ)
        {
            AskDJNotice = !_runtimeCompatible ? P("Vm_Update_required_31") : P("Vm_Ask_DJ_is_unavailable_7");
            return;
        }

        if (IsDemoMode)
        {
            MergeMessage(new AskDJMessage(Guid.NewGuid().ToString("N"), "assistant", $"{action.DisplayLabel}: demo actie uitgevoerd.", null, DateTimeOffset.Now, "assistant", null, null, null, null, null));
            AddDiagnostic("INF Demo Ask DJ action executed.");
            return;
        }

        ConfigureClient();
        CommandResponse response;
        try
        {
            response = string.Equals(action.Command, "ask_dj_message", StringComparison.OrdinalIgnoreCase)
                ? await _apiClient.RunAskDJMessageActionAsync(_identity, action, _language, AskDJMoodValue(), _djAnnouncementOutput, CancellationToken.None)
                : await _apiClient.RunPlaybackActionAsync(_identity, action, _language, AskDJMoodValue(), _djAnnouncementOutput, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (await ApplyStalePairingAsync(ex))
            {
                AskDJNotice = P("Vm_Pair_again_to_continue_6");
                return;
            }

            if (ApplyVersionMismatch(ex))
            {
                AskDJNotice = P("Vm_Update_required_32");
                return;
            }

            AskDJNotice = P("Vm_Ask_DJ_is_unavailable_8");
            AddDiagnostic("WRN Ask DJ action failed: " + ex.GetType().Name);
            return;
        }
        if (!response.Success)
        {
            if (await ApplyStalePairingAsync(response.Error))
            {
                AskDJNotice = P("Vm_Pair_again_to_continue_7");
                return;
            }

            AskDJNotice = BackendActionErrorMessage(response.Error, response.Message);
            AddDiagnostic("WRN Ask DJ action failed.");
            return;
        }

        ApplyPairingTransport(response.HomeAssistantLocalUrl, response.HomeAssistantRemoteUrl, response.RemoteSupported);
        ApplyBackendSummary(BackendSummaryFrom(response));
        AskDJNotice = "";
        Status = response.DjText ?? response.Message ?? P("Vm_Command_executed_4");
        await RefreshAsync();
    }

    private Task TogglePushToTalkAsync()
    {
        if (IsDemoMode)
        {
            VoiceStatus = P("Vm_Demo_microphone_not_used_Ask_DJ_would_listen");
            MergeMessage(new AskDJMessage(Guid.NewGuid().ToString("N"), "assistant", "Demo Mode: voice requests work after pairing Home Assistant.", null, DateTimeOffset.Now, "assistant", DemoPlaybackActions(), null, null, null, null));
            AddDiagnostic("INF Demo push-to-talk simulated locally.");
            return Task.CompletedTask;
        }

        if (!_settings.PermissionExplanationMicrophoneSeen)
        {
            ShowPermissionExplanation(PermissionExplanationKind.Microphone);
            return Task.CompletedTask;
        }

        VoiceStatus = P("Vm_Microphone_unavailable");
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
                VoiceStatus = P("Vm_Microphone_unavailable_3");
                AddDiagnostic("INF Microphone explanation accepted; capture backend is not implemented yet.");
                break;
            case PermissionExplanationKind.Notifications:
                PermissionNotice = P("Vm_NotificationsNotActiveYet");
                AddDiagnostic("INF Notification explanation accepted; toast backend is not implemented yet.");
                break;
            case PermissionExplanationKind.LocalNetwork:
                AddDiagnostic("INF Local network explanation accepted; local Home Assistant pairing can continue.");
                break;
        }
    }

    private Task HidePermissionExplanationAsync()
    {
        var permission = ActivePermissionKind;
        IsPermissionExplanationVisible = false;
        if (permission == PermissionExplanationKind.Microphone)
        {
            VoiceStatus = P("Vm_Microphone_unavailable_4");
        }
        else if (permission == PermissionExplanationKind.Notifications)
        {
            PermissionNotice = P("Vm_Notifications_remain_disabled");
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
            PermissionNotice = P("Vm_Monkey_test_permission_prompt_suppressed");
            AddDiagnostic("INF Monkey test suppressed permission explanation: " + permission);
            return;
        }

        if (permission == PermissionExplanationKind.LocalNetwork
            && _settings.PermissionExplanationLocalNetworkSeen
            && mode == PermissionExplanationMode.Request)
        {
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
        else if (response.IsGeneratedText == true)
        {
            messages.AddRange(messages.Select(message => message.IsAssistant ? message with { IsGeneratedText = true } : message));
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

        var withoutMarkup = text.Replace("<", "").Replace(">", "");
        var withoutSpotifyUris = Regex.Replace(withoutMarkup, @"\bspotify:(track|album|artist|playlist|show|episode|user):[A-Za-z0-9:_-]+", "[verborgen Spotify-id]", RegexOptions.IgnoreCase);
        var withoutOpenUrls = Regex.Replace(withoutSpotifyUris, @"https?://open\.spotify\.com/[^\s)\]]+", "[verborgen Spotify-link]", RegexOptions.IgnoreCase);
        return Regex.Replace(withoutOpenUrls, @"\b(uri|context_uri|track_uri|playlist_uri|spotify_id|backend_id|device_id)\s*[:=]\s*[""']?[A-Za-z0-9:._/-]+", "$1: [verborgen]", RegexOptions.IgnoreCase);
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
        MergeMessage(new AskDJMessage("demo-system", "assistant", P("Vm_DemoAskDJSystemMessage"), null, DateTimeOffset.Now.AddSeconds(-2), "system", null, null, null, null, null, Origin: "history_retention"));
        MergeMessage(new AskDJMessage("demo-assistant", "assistant", P("Vm_DemoAskDJAssistantMessage"), null, DateTimeOffset.Now.AddSeconds(-1), "assistant", DemoPlaybackActions(), null, null, null, null));
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
            : P("Vm_No_active_playback_4");

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

    private void ReplaceMusicDnaBlocks(IReadOnlyList<MusicDnaDashboardBlock>? blocks)
    {
        MusicDnaBlocks.Clear();
        foreach (var block in blocks ?? [])
        {
            MusicDnaBlocks.Add(block);
        }

        OnPropertyChanged(nameof(HasMusicDnaBlocks));
    }

    private void ReplaceDiscoverItems(IReadOnlyList<MusicDiscoveryItem>? items)
    {
        DiscoverItems.Clear();
        foreach (var item in items ?? [])
        {
            if (item.HasContent)
            {
                DiscoverItems.Add(item);
            }
        }

        RaiseDiscoverProperties();
    }

    private void ReplaceDiscoverItem(MusicDiscoveryItem original, MusicDiscoveryItem updated)
    {
        for (var i = 0; i < DiscoverItems.Count; i++)
        {
            if (string.Equals(DiscoverItems[i].StableId, original.StableId, StringComparison.OrdinalIgnoreCase))
            {
                DiscoverItems[i] = updated;
                return;
            }
        }
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
            Notice = P("Vm_No_output_device_selected_6");
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
        Status = P("Vm_Demo_mode_5");
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
        NowPlaying = P("Vm_No_active_playback_5");
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
        Status = P("Vm_Demo_mode_6");
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
        Status = P("Vm_Demo_mode_7");
        LoadDemoQueueItems(reset: true);
        AddDiagnostic("INF Demo playlist started.");
    }

    private void RaisePlaybackStateProperties()
    {
        OnPropertyChanged(nameof(ConnectionStatusText));
        RaiseNowPlayingStatusProperties();
        OnPropertyChanged(nameof(CanUsePlaybackFeatures));
        OnPropertyChanged(nameof(CanStartPlayback));
        OnPropertyChanged(nameof(CanUseAskDJ));
        OnPropertyChanged(nameof(CanUseMusicDna));
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

    private void RaiseMusicDnaProperties()
    {
        OnPropertyChanged(nameof(IsMusicDnaEnabled));
        OnPropertyChanged(nameof(IsMusicDnaDisabled));
        OnPropertyChanged(nameof(MusicDnaSummary));
        OnPropertyChanged(nameof(HasMusicDnaSummary));
        OnPropertyChanged(nameof(HasMusicDnaBlocks));
        OnPropertyChanged(nameof(MusicDnaUpdatedAt));
        OnPropertyChanged(nameof(HasMusicDnaUpdatedAt));
        DisableMusicDnaCommand.RaiseCanExecuteChanged();
        ClearMusicDnaCommand.RaiseCanExecuteChanged();
        RaiseDiscoverProperties();
    }

    private void RaiseDiscoverProperties()
    {
        OnPropertyChanged(nameof(HasDiscoverItems));
        OnPropertyChanged(nameof(HasNoDiscoverItems));
        OnPropertyChanged(nameof(IsDiscoverConsentVisible));
        OnPropertyChanged(nameof(IsDiscoverRejectedStateVisible));
        RefreshDiscoverCommand.RaiseCanExecuteChanged();
        EnableDiscoverMusicDnaCommand.RaiseCanExecuteChanged();
    }

    private void RaiseNowPlayingStatusProperties()
    {
        OnPropertyChanged(nameof(NowPlayingPairingStatusText));
        OnPropertyChanged(nameof(NowPlayingPairingStatusIcon));
        OnPropertyChanged(nameof(NowPlayingPairingStatusColor));
        OnPropertyChanged(nameof(NowPlayingMusicBackendStatusText));
        OnPropertyChanged(nameof(NowPlayingMusicBackendStatusColor));
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

    private static MusicDnaProfile DemoMusicDnaProfile()
    {
        return new MusicDnaProfile(
            "Your recent listening leans toward neon synth-pop, melodic electronic tracks and high-energy favorites.",
            FavoriteGenres:
            [
                new MusicDnaProfileItem("Synth-pop", null, null, null, 8, null, null, null, null, null, null, null, null, null, null),
                new MusicDnaProfileItem("Electronic", null, null, null, 6, null, null, null, null, null, null, null, null, null, null)
            ],
            FavoriteArtists:
            [
                new MusicDnaProfileItem("M83", null, null, null, 5, null, null, null, null, null, null, null, null, null, null),
                new MusicDnaProfileItem("ODESZA", null, null, null, 4, null, null, null, null, null, null, null, null, null, null)
            ],
            RecentTracks:
            [
                new MusicDnaProfileItem(null, "Midnight City", "Hurry Up, We're Dreaming", "M83", null, null, null, null, null, null, null, null, null, null, null),
                new MusicDnaProfileItem(null, "A Moment Apart", "A Moment Apart", "ODESZA", null, null, null, null, null, null, null, null, null, null, null)
            ],
            RecentFavoriteTracks: null,
            EnergyProfile: new MusicDnaProfileItem("Energy", null, null, null, 6, null, null, 6, null, null, null, "Energy", 72, 68, 74),
            MoodProfile: new MusicDnaProfileItem("Groove", null, null, null, 6, null, null, 6, 57, null, "Groove", null, null, null, null),
            Mood: null,
            TasteDirection: null,
            BasedOn: "demo listening signals",
            UpdatedAt: DateTimeOffset.UtcNow);
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
            new QueueItem("demo-1", null, "Midnight City", null, null, "M83", null, null, "Hurry Up, We're Dreaming", null, 244_000, null, "demo:midnight-city", null, null, null, null, null, null, true, true, true, null),
            new QueueItem("demo-2", null, "Sweet Disposition", null, null, "The Temper Trap", null, null, "Conditions", null, 232_000, null, "demo:sweet-disposition", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-3", null, "Electric Feel", null, null, "MGMT", null, null, "Oracular Spectacular", null, 229_000, null, "demo:electric-feel", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-4", null, "1901", null, null, "Phoenix", null, null, "Wolfgang Amadeus Phoenix", null, 193_000, null, "demo:1901", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-5", null, "Tadow", null, null, "Masego & FKJ", null, null, "Lady Lady", null, 301_000, null, "demo:tadow", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-6", null, "Innerbloom", null, null, "RÜFÜS DU SOL", null, null, "Bloom", null, 589_000, null, "demo:innerbloom", null, null, null, null, null, null, false, false, true, null),
            new QueueItem("demo-7", null, "A Moment Apart", null, null, "ODESZA", null, null, "A Moment Apart", null, 234_000, null, "demo:a-moment-apart", null, null, null, null, null, null, false, false, true, null)
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

    private string S(string key) => AppStrings.Get(key);

    private string P(string key) => T.Get(key);

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
            return P("Vm_No_logs_available");
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
        SeekBackwardCommand.RaiseCanExecuteChanged();
        SeekForwardCommand.RaiseCanExecuteChanged();
        SaveCurrentTrackCommand.RaiseCanExecuteChanged();
        OpenTrackInsightCommand.RaiseCanExecuteChanged();
        RefreshMusicDnaCommand.RaiseCanExecuteChanged();
        EnableMusicDnaCommand.RaiseCanExecuteChanged();
        DisableMusicDnaCommand.RaiseCanExecuteChanged();
        ClearMusicDnaCommand.RaiseCanExecuteChanged();
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
        RaisePairingInputProperties();
        OnPropertyChanged(nameof(PlaybackAvailabilityText));
        RaiseNowPlayingStatusProperties();
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
        RaiseTransportProperties();
    }

    private void RaiseTransportProperties()
    {
        OnPropertyChanged(nameof(ConnectionModeText));
        OnPropertyChanged(nameof(AboutConnectionTypeText));
        OnPropertyChanged(nameof(AboutFastPathText));
        OnPropertyChanged(nameof(RemoteSupportText));
        OnPropertyChanged(nameof(MusicServiceStatusText));
        OnPropertyChanged(nameof(MusicBackendNameText));
        OnPropertyChanged(nameof(MusicBackendStatusText));
        OnPropertyChanged(nameof(MusicBackendRevisionText));
        OnPropertyChanged(nameof(MusicTargetPlayerText));
        OnPropertyChanged(nameof(MusicBackendCapabilitiesText));
        OnPropertyChanged(nameof(CanUsePlaybackFeatures));
        OnPropertyChanged(nameof(CanUseAskDJ));
        OnPropertyChanged(nameof(CanSendAskDJ));
        OnPropertyChanged(nameof(NowPlayingMusicBackendStatusText));
        OnPropertyChanged(nameof(NowPlayingMusicBackendStatusColor));
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
        OnPropertyChanged(nameof(IsPairable));
        OnPropertyChanged(nameof(IsPairingFormVisible));
        OnPropertyChanged(nameof(IsPairingWaitingVisible));
        OnPropertyChanged(nameof(IsPairingPending));
        OnPropertyChanged(nameof(IsPairingWaitingForCompletion));
        OnPropertyChanged(nameof(PairingStatusText));
        RaisePairingInputProperties();
    }

    private void RaisePairingInputProperties()
    {
        OnPropertyChanged(nameof(IsHomeAssistantUrlValid));
        OnPropertyChanged(nameof(IsPairingCodeValid));
        OnPropertyChanged(nameof(CanPair));
        OnPropertyChanged(nameof(PairingUrlValidationText));
        OnPropertyChanged(nameof(PairingCodeValidationText));
        OnPropertyChanged(nameof(HasPairingUrlValidationText));
        OnPropertyChanged(nameof(HasPairingCodeValidationText));
        PairCommand.RaiseCanExecuteChanged();
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
