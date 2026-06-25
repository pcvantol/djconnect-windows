using DJConnect.Windows.Models;
using DJConnect.Windows.ViewModels;

namespace DJConnect.Windows;

public partial class MainPage : ContentPage
{
    private const string MitLicenseText = """
MIT License

Copyright (c) 2026 Peter van Tol.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
""";

    private const string ThirdPartyNoticesText = """
Third-Party Notices

DJConnect Windows uses .NET MAUI and platform frameworks supplied by Microsoft
and the target operating systems. No additional app-level third-party NuGet
dependencies are bundled in this scaffold.

See the repository THIRD_PARTY_NOTICES.md and docs/THIRD_PARTY_NOTICES.md for
the current source-of-truth notices.
""";

    private const string PaddleHighKey = "djconnect.windows.game.paddle.high";
    private const string MeteorHighKey = "djconnect.windows.game.meteor.high";
    private const string SkyHighKey = "djconnect.windows.game.sky.high";
    private const string MazeHighKey = "djconnect.windows.game.maze.high";

    private readonly WelcomeStep[] _welcomeSteps =
    [
        new("♪", "Speelt Nu", "Bedien playback, volume en het actieve uitvoerapparaat vanaf het hoofdscherm."),
        new("☵", "Ask DJ", "Vraag om muziek, context of een gesproken antwoord. DJConnect synchroniseert de chatgeschiedenis via Home Assistant."),
        new("▸☰", "Wachtrij", "Bekijk wat hierna komt en start wachtrij-items wanneer Home Assistant afspeelacties teruggeeft."),
        new("•••", "Meer en Instellingen", "Open afspeellijsten, instellingen, privacyopties en diagnostiek."),
        new("🎮", "Mini-games", "Speel lokale mini-games terwijl DJConnect klaar blijft voor je muziekopstelling.")
    ];

    private readonly MainViewModel _viewModel = new();
    private readonly Brush _activeNavBackground = new LinearGradientBrush(
        [
            new GradientStop(Color.FromArgb("#D536F6"), 0),
            new GradientStop(Color.FromArgb("#544DF4"), 1)
        ],
        new Point(0, 0),
        new Point(1, 0));
    private readonly Brush _inactiveNavBackground = new SolidColorBrush(Colors.Transparent);
    private readonly Brush _welcomeActiveBackground = new LinearGradientBrush(
        [
            new GradientStop(Color.FromArgb("#D536F6"), 0),
            new GradientStop(Color.FromArgb("#6A58FF"), 1)
        ],
        new Point(0, 0),
        new Point(1, 1));
    private readonly Brush _welcomeInactiveBackground = new SolidColorBrush(Color.FromArgb("#534C83"));
    private readonly Color _activeNavText = Colors.White;
    private readonly Color _inactiveNavText = Color.FromArgb("#F7F4FF");
    private readonly MiniGameDrawable _miniGameDrawable = new();
    private readonly Random _gameRandom = new(610);
    private readonly List<PointF> _meteors = [];
    private readonly List<PointF> _skyObstacles = [];
    private readonly HashSet<Point> _mazeCollectibles = [];
    private IDispatcherTimer? _gameTimer;
    private int _welcomeStepIndex;
    private MiniGameKind _selectedGame = MiniGameKind.Paddle;
    private MiniGameRunState _gameState = MiniGameRunState.Idle;
    private DateTimeOffset _lastGameTick;
    private int _gameScore;
    private float _inputX = 0.5f;
    private float _inputY = 0.5f;
    private bool _inputActive;
    private float _paddleY = 0.5f;
    private PointF _ball = new(0.5f, 0.5f);
    private PointF _ballVelocity = new(0.45f, 0.32f);
    private float _meteorPlayerX = 0.5f;
    private float _skyY = 0.5f;
    private float _skyVelocity;
    private Point _mazePlayer = new(1, 1);
    private Point _mazeChaser = new(6, 4);

    public MainPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
        MiniGameSurface.Drawable = _miniGameDrawable;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
        ShowSection(NowPlayingPanel, NowPlayingNavButton);
        SelectMiniGame(MiniGameKind.Paddle);
        SetWelcomeStep(0, animate: false);
    }

    public Task MarkCleanShutdownAsync() => _viewModel.MarkCleanShutdownAsync();

    private void ShowNowPlaying(object sender, EventArgs e) => ShowSection(NowPlayingPanel, NowPlayingNavButton);

    private void ShowAskDJ(object sender, EventArgs e) => ShowSection(AskDJPanel, AskDJNavButton);

    private void ShowQueue(object sender, EventArgs e) => ShowSection(QueuePanel, QueueNavButton);

    private void ShowPlaylists(object sender, EventArgs e) => ShowSection(PlaylistsPanel, PlaylistsNavButton);

    private void ShowGames(object sender, EventArgs e) => ShowSection(GamesPanel, GamesNavButton);

    private void ShowSettings(object sender, EventArgs e) => ShowSection(SettingsPanel, SettingsNavButton);

    private void ShowLogs(object sender, EventArgs e) => ShowSection(LogsPanel, LogsNavButton);

    private void ShowAbout(object sender, EventArgs e) => ShowSection(AboutPanel, AboutNavButton);

    private void ShowLegal(object sender, EventArgs e) => ShowSection(LegalPanel, LegalNavButton);

    private void ShowPrivacy(object sender, EventArgs e) => ShowSection(PrivacyPanel, PrivacyNavButton);

    private void SelectWelcomeStepNowPlaying(object sender, EventArgs e) => SetWelcomeStep(0);

    private void SelectWelcomeStepAskDJ(object sender, EventArgs e) => SetWelcomeStep(1);

    private void SelectWelcomeStepQueue(object sender, EventArgs e) => SetWelcomeStep(2);

    private void SelectWelcomeStepMore(object sender, EventArgs e) => SetWelcomeStep(3);

    private void SelectWelcomeStepGames(object sender, EventArgs e) => SetWelcomeStep(4);

    private void WelcomePrevious(object sender, EventArgs e) => SetWelcomeStep(Math.Max(0, _welcomeStepIndex - 1));

    private async void PlaybackSeekCompleted(object sender, EventArgs e)
    {
        await _viewModel.SeekAsync(PlaybackPositionSlider.Value);
    }

    private async void AskDJActionClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: PlaybackAction action })
        {
            await _viewModel.ExecutePlaybackActionAsync(action);
        }
    }

    private async void QueueItemStartClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: QueueItem item })
        {
            await _viewModel.StartQueueItemAsync(item);
        }
    }

    private async void PlaylistStartClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: PlaylistItem item })
        {
            await _viewModel.StartPlaylistAsync(item);
        }
    }

    private void StartDemoModeClicked(object sender, EventArgs e)
    {
        if (_viewModel.StartDemoModeCommand.CanExecute(null))
        {
            _viewModel.StartDemoModeCommand.Execute(null);
            ShowSection(NowPlayingPanel, NowPlayingNavButton);
        }
    }

    private async void ExitAppClicked(object sender, EventArgs e)
    {
        await MarkCleanShutdownAsync();
        Application.Current?.Quit();
    }

    private async void ReplayAudioClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: AskDJMessage { AudioUrl.Length: > 0 } message })
        {
            await Launcher.Default.OpenAsync(message.AudioUrl);
        }
    }

    private async void ContinuePermissionExplanationClicked(object sender, EventArgs e)
    {
        if (_viewModel.ActivePermissionKind == PermissionExplanationKind.Microphone
            && _viewModel.ActivePermissionMode == PermissionExplanationMode.Request)
        {
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                _viewModel.MarkPermissionDenied(PermissionExplanationKind.Microphone);
                return;
            }
        }

        if (_viewModel.ContinuePermissionExplanationCommand.CanExecute(null))
        {
            _viewModel.ContinuePermissionExplanationCommand.Execute(null);
        }
    }

    private async void OpenPermissionSettingsClicked(object sender, EventArgs e)
    {
        var uri = _viewModel.ActivePermissionKind switch
        {
            PermissionExplanationKind.Microphone => "ms-settings:privacy-microphone",
            PermissionExplanationKind.Notifications => "ms-settings:notifications",
            PermissionExplanationKind.LocalNetwork => "ms-settings:network-firewall",
            _ => "ms-settings:"
        };

        if (_viewModel.OpenPermissionSettingsCommand.CanExecute(null))
        {
            _viewModel.OpenPermissionSettingsCommand.Execute(null);
        }

        await Launcher.Default.OpenAsync(uri);
    }

    private async void CopyClientAddressClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.ClientAddress)
            && !_viewModel.ClientAddress.Contains("wordt gestart", StringComparison.OrdinalIgnoreCase)
            && !_viewModel.ClientAddress.Contains("Starting", StringComparison.OrdinalIgnoreCase))
        {
            await Clipboard.Default.SetTextAsync(_viewModel.ClientAddress);
        }

        if (_viewModel.CopyClientAddressCommand.CanExecute(null))
        {
            _viewModel.CopyClientAddressCommand.Execute(null);
        }
    }

    private async void CopyPairingCodeClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.PairingCodeDisplay))
        {
            await Clipboard.Default.SetTextAsync(_viewModel.PairingCodeDisplay);
        }
    }

    private void SelectPaddleGame(object sender, EventArgs e) => SelectMiniGame(MiniGameKind.Paddle);

    private void SelectMeteorGame(object sender, EventArgs e) => SelectMiniGame(MiniGameKind.Meteor);

    private void SelectSkyGame(object sender, EventArgs e) => SelectMiniGame(MiniGameKind.Sky);

    private void SelectMazeGame(object sender, EventArgs e) => SelectMiniGame(MiniGameKind.Maze);

    private void StartMiniGameClicked(object sender, EventArgs e) => StartMiniGame();

    private void PauseMiniGameClicked(object sender, EventArgs e)
    {
        if (_gameState == MiniGameRunState.Running)
        {
            _gameState = MiniGameRunState.Paused;
            StopGameTimer();
            UpdateMiniGameUi();
        }
    }

    private void RestartMiniGameClicked(object sender, EventArgs e)
    {
        ResetSelectedGameToIdle();
        StartMiniGame();
    }

    private void MiniGameLeftClicked(object sender, EventArgs e) => MoveMiniGame(MiniGameDirection.Left);

    private void MiniGameUpClicked(object sender, EventArgs e) => MoveMiniGame(MiniGameDirection.Up);

    private void MiniGameDownClicked(object sender, EventArgs e) => MoveMiniGame(MiniGameDirection.Down);

    private void MiniGameRightClicked(object sender, EventArgs e) => MoveMiniGame(MiniGameDirection.Right);

    private void MiniGameActionClicked(object sender, EventArgs e) => FireMiniGameAction();

    private async void ClearGameHighscoresClicked(object sender, EventArgs e)
    {
        var clear = await DisplayAlertAsync("Highscores wissen", "Alle lokale mini-game highscores wissen?", "Wissen", "Niet nu");
        if (!clear)
        {
            return;
        }

        Preferences.Remove(PaddleHighKey);
        Preferences.Remove(MeteorHighKey);
        Preferences.Remove(SkyHighKey);
        Preferences.Remove(MazeHighKey);
        UpdateMiniGameUi();
    }

    private void MiniGameSurfaceStartInteraction(object sender, TouchEventArgs e)
    {
        UpdateMiniGamePointer(e.Touches.FirstOrDefault(), isDrag: false);
        if (_gameState is MiniGameRunState.Idle or MiniGameRunState.GameOver)
        {
            StartMiniGame();
        }
    }

    private void MiniGameSurfaceDragInteraction(object sender, TouchEventArgs e)
    {
        UpdateMiniGamePointer(e.Touches.FirstOrDefault(), isDrag: true);
    }

    private void MiniGameSurfaceEndInteraction(object sender, TouchEventArgs e)
    {
        if (_selectedGame is MiniGameKind.Paddle or MiniGameKind.Meteor)
        {
            _inputActive = false;
        }
    }

    private void SelectMiniGame(MiniGameKind game)
    {
        StopGameTimer();
        _selectedGame = game;
        ResetSelectedGameToIdle();
    }

    private void StartMiniGame()
    {
        ResetSelectedGameState();
        _gameState = MiniGameRunState.Running;
        _lastGameTick = DateTimeOffset.Now;
        GameIdleButton.IsVisible = false;
        EnsureGameTimer().Start();
        MiniGameSurface.Focus();
        UpdateMiniGameUi();
    }

    private void MoveMiniGame(MiniGameDirection direction)
    {
        EnsureMiniGameRunning();

        switch (_selectedGame)
        {
            case MiniGameKind.Paddle:
                if (direction == MiniGameDirection.Up)
                {
                    _paddleY = Math.Clamp(_paddleY - 0.12f, 0.12f, 0.88f);
                }
                else if (direction == MiniGameDirection.Down)
                {
                    _paddleY = Math.Clamp(_paddleY + 0.12f, 0.12f, 0.88f);
                }

                _inputActive = false;
                break;
            case MiniGameKind.Meteor:
                if (direction == MiniGameDirection.Left)
                {
                    _meteorPlayerX = Math.Clamp(_meteorPlayerX - 0.12f, 0.08f, 0.92f);
                }
                else if (direction == MiniGameDirection.Right)
                {
                    _meteorPlayerX = Math.Clamp(_meteorPlayerX + 0.12f, 0.08f, 0.92f);
                }

                _inputActive = false;
                break;
            case MiniGameKind.Sky:
                if (direction == MiniGameDirection.Up || direction == MiniGameDirection.Down)
                {
                    _skyVelocity = direction == MiniGameDirection.Up ? -0.52f : 0.32f;
                }
                break;
            case MiniGameKind.Maze:
                QueueMazeMove(direction);
                break;
        }

        UpdateMiniGameUi();
    }

    private void FireMiniGameAction()
    {
        EnsureMiniGameRunning();
        if (_selectedGame == MiniGameKind.Sky)
        {
            _skyVelocity = -0.52f;
        }
        else if (_selectedGame == MiniGameKind.Maze)
        {
            QueueMazeMove(MiniGameDirection.Right);
        }

        UpdateMiniGameUi();
    }

    private void EnsureMiniGameRunning()
    {
        if (_gameState is MiniGameRunState.Idle or MiniGameRunState.GameOver)
        {
            StartMiniGame();
        }
        else if (_gameState == MiniGameRunState.Paused)
        {
            _gameState = MiniGameRunState.Running;
            _lastGameTick = DateTimeOffset.Now;
            EnsureGameTimer().Start();
            GameIdleButton.IsVisible = false;
        }
    }

    private void QueueMazeMove(MiniGameDirection direction)
    {
        var next = direction switch
        {
            MiniGameDirection.Left => new Point(_mazePlayer.X - 1, _mazePlayer.Y),
            MiniGameDirection.Right => new Point(_mazePlayer.X + 1, _mazePlayer.Y),
            MiniGameDirection.Up => new Point(_mazePlayer.X, _mazePlayer.Y - 1),
            MiniGameDirection.Down => new Point(_mazePlayer.X, _mazePlayer.Y + 1),
            _ => _mazePlayer
        };

        if (IsMazeOpen(next))
        {
            _mazePlayer = next;
            if (_mazeCollectibles.Remove(next))
            {
                _gameScore++;
                if (_mazeCollectibles.Count == 0)
                {
                    _mazeCollectibles.Add(new Point(1 + _gameRandom.Next(6), 1 + _gameRandom.Next(4)));
                }
            }
        }
    }

    private void ResetSelectedGameToIdle()
    {
        StopGameTimer();
        ResetSelectedGameState();
        _gameState = MiniGameRunState.Idle;
        UpdateMiniGameUi();
    }

    private void ResetAllGamesToIdle()
    {
        StopGameTimer();
        ResetSelectedGameState();
        _gameState = MiniGameRunState.Idle;
        UpdateMiniGameUi();
    }

    private void ResetSelectedGameState()
    {
        _gameScore = 0;
        _inputX = 0.5f;
        _inputY = 0.5f;
        _inputActive = false;
        _paddleY = 0.5f;
        _ball = new PointF(0.5f, 0.5f);
        _ballVelocity = new PointF(0.5f, 0.34f);
        _meteorPlayerX = 0.5f;
        _meteors.Clear();
        _meteors.Add(new PointF(0.25f, -0.1f));
        _meteors.Add(new PointF(0.7f, -0.45f));
        _skyY = 0.5f;
        _skyVelocity = 0;
        _skyObstacles.Clear();
        _skyObstacles.Add(new PointF(0.8f, 0.32f));
        _skyObstacles.Add(new PointF(1.3f, 0.68f));
        _mazePlayer = new Point(1, 1);
        _mazeChaser = new Point(6, 4);
        _mazeCollectibles.Clear();
        _mazeCollectibles.Add(new Point(3, 1));
        _mazeCollectibles.Add(new Point(5, 2));
        _mazeCollectibles.Add(new Point(2, 4));
    }

    private IDispatcherTimer EnsureGameTimer()
    {
        if (_gameTimer is not null)
        {
            return _gameTimer;
        }

        _gameTimer = Dispatcher.CreateTimer();
        _gameTimer.Interval = TimeSpan.FromMilliseconds(16);
        _gameTimer.Tick += (_, _) => TickMiniGame();
        return _gameTimer;
    }

    private void StopGameTimer()
    {
        _gameTimer?.Stop();
    }

    private void TickMiniGame()
    {
        if (_gameState != MiniGameRunState.Running)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var delta = Math.Clamp((float)(now - _lastGameTick).TotalSeconds, 0.001f, 0.05f);
        _lastGameTick = now;

        switch (_selectedGame)
        {
            case MiniGameKind.Paddle:
                TickPaddle(delta);
                break;
            case MiniGameKind.Meteor:
                TickMeteor(delta);
                break;
            case MiniGameKind.Sky:
                TickSky(delta);
                break;
            case MiniGameKind.Maze:
                TickMaze(delta);
                break;
        }

        UpdateMiniGameUi();
    }

    private void TickPaddle(float delta)
    {
        if (_inputActive)
        {
            _paddleY = Math.Clamp(_inputY, 0.12f, 0.88f);
        }

        _ball = new PointF(_ball.X + _ballVelocity.X * delta, _ball.Y + _ballVelocity.Y * delta);
        if (_ball.Y < 0.04f || _ball.Y > 0.96f)
        {
            _ballVelocity = new PointF(_ballVelocity.X, -_ballVelocity.Y);
        }

        if (_ball.X > 0.96f)
        {
            _ballVelocity = new PointF(-Math.Abs(_ballVelocity.X), _ballVelocity.Y);
        }

        if (_ball.X < 0.08f && Math.Abs(_ball.Y - _paddleY) < 0.16f)
        {
            _ballVelocity = new PointF(Math.Abs(_ballVelocity.X) + 0.025f, _ballVelocity.Y + (_ball.Y - _paddleY) * 0.85f);
            _gameScore++;
        }

        if (_ball.X < -0.05f)
        {
            EndMiniGame();
        }
    }

    private void TickMeteor(float delta)
    {
        if (_inputActive)
        {
            _meteorPlayerX = Math.Clamp(_inputX, 0.08f, 0.92f);
        }

        for (var i = 0; i < _meteors.Count; i++)
        {
            var meteor = _meteors[i];
            meteor = new PointF(meteor.X, meteor.Y + (0.38f + _gameScore * 0.012f) * delta);
            if (meteor.Y > 1.08f)
            {
                meteor = new PointF((float)_gameRandom.NextDouble() * 0.86f + 0.07f, -0.12f);
                _gameScore++;
            }

            _meteors[i] = meteor;
            if (Math.Abs(meteor.X - _meteorPlayerX) < 0.08f && meteor.Y > 0.78f)
            {
                EndMiniGame();
            }
        }
    }

    private void TickSky(float delta)
    {
        if (_inputActive)
        {
            _skyVelocity = -0.48f;
            _inputActive = false;
        }

        _skyVelocity += 1.1f * delta;
        _skyY += _skyVelocity * delta;
        if (_skyY < 0.06f || _skyY > 0.94f)
        {
            EndMiniGame();
            return;
        }

        for (var i = 0; i < _skyObstacles.Count; i++)
        {
            var obstacle = _skyObstacles[i];
            obstacle = new PointF(obstacle.X - (0.32f + _gameScore * 0.006f) * delta, obstacle.Y);
            if (obstacle.X < -0.1f)
            {
                obstacle = new PointF(1.05f, (float)_gameRandom.NextDouble() * 0.7f + 0.15f);
                _gameScore++;
            }

            _skyObstacles[i] = obstacle;
            if (Math.Abs(obstacle.X - 0.22f) < 0.06f && Math.Abs(obstacle.Y - _skyY) < 0.16f)
            {
                EndMiniGame();
            }
        }
    }

    private void TickMaze(float delta)
    {
        _gameScore = Math.Max(_gameScore, 0);
        if (_gameScore % 2 == 1)
        {
            var dx = Math.Sign(_mazePlayer.X - _mazeChaser.X);
            var dy = Math.Sign(_mazePlayer.Y - _mazeChaser.Y);
            var next = Math.Abs(dx) > Math.Abs(dy) ? new Point(_mazeChaser.X + dx, _mazeChaser.Y) : new Point(_mazeChaser.X, _mazeChaser.Y + dy);
            if (IsMazeOpen(next))
            {
                _mazeChaser = next;
            }
        }

        if (_mazeChaser == _mazePlayer)
        {
            EndMiniGame();
        }
    }

    private static bool IsMazeOpen(Point cell)
    {
        return cell.X >= 0 && cell.X <= 7 && cell.Y >= 0 && cell.Y <= 5
            && cell is not { X: 3, Y: >= 1 and <= 4 }
            && cell is not { X: 5, Y: >= 0 and <= 2 };
    }

    private void EndMiniGame()
    {
        _gameState = MiniGameRunState.GameOver;
        StopGameTimer();
        SaveSelectedHighScore();
    }

    private void SaveSelectedHighScore()
    {
        var key = SelectedHighScoreKey();
        if (_gameScore > Preferences.Get(key, 0))
        {
            Preferences.Set(key, _gameScore);
        }
    }

    private void UpdateMiniGamePointer(PointF point, bool isDrag)
    {
        var width = Math.Max(1, MiniGameSurface.Width);
        var height = Math.Max(1, MiniGameSurface.Height);
        _inputX = Math.Clamp((float)(point.X / width), 0, 1);
        _inputY = Math.Clamp((float)(point.Y / height), 0, 1);
        switch (_selectedGame)
        {
            case MiniGameKind.Paddle:
            case MiniGameKind.Meteor:
                _inputActive = true;
                break;
            case MiniGameKind.Sky:
                if (!isDrag)
                {
                    _skyVelocity = -0.52f;
                }

                break;
            case MiniGameKind.Maze:
                var dx = _inputX - ((_mazePlayer.X + 0.5f) / 8f);
                var dy = _inputY - ((_mazePlayer.Y + 0.5f) / 6f);
                QueueMazeMove(Math.Abs(dx) > Math.Abs(dy)
                    ? dx < 0 ? MiniGameDirection.Left : MiniGameDirection.Right
                    : dy < 0 ? MiniGameDirection.Up : MiniGameDirection.Down);
                break;
        }
    }

    private string SelectedHighScoreKey() => _selectedGame switch
    {
        MiniGameKind.Paddle => PaddleHighKey,
        MiniGameKind.Meteor => MeteorHighKey,
        MiniGameKind.Sky => SkyHighKey,
        _ => MazeHighKey
    };

    private int SelectedHighScore() => Preferences.Get(SelectedHighScoreKey(), 0);

    private void UpdateMiniGameUi()
    {
        var high = SelectedHighScore();
        var title = _selectedGame switch
        {
            MiniGameKind.Paddle => "Paddle Rally",
            MiniGameKind.Meteor => "Meteor Run",
            MiniGameKind.Sky => "Sky Dash",
            _ => "Maze Chase"
        };
        GameTitleLabel.Text = title;
        GameHintLabel.Text = _selectedGame switch
        {
            MiniGameKind.Paddle => "Gebruik ↑/↓ of sleep de paddle. Houd de bal in het spel.",
            MiniGameKind.Meteor => "Gebruik ←/→ of sleep je ship. Ontwijk vallende meteors.",
            MiniGameKind.Sky => "Tik of Space om te boosten. Vlieg langs de obstakels.",
            _ => "Gebruik pijltjes/WASD of klik richting. Verzamel punten en ontwijk de chaser."
        };
        GameScoreLabel.Text = $"Score {_gameScore}   High {high}";
        GameHighSummaryLabel.Text = $"Highscores: {Preferences.Get(PaddleHighKey, 0)} / {Preferences.Get(MeteorHighKey, 0)} / {Preferences.Get(SkyHighKey, 0)} / {Preferences.Get(MazeHighKey, 0)}";
        PaddleGameButton.Text = $"▭  Paddle Rally\nHigh {Preferences.Get(PaddleHighKey, 0)}";
        MeteorGameButton.Text = $"☄  Meteor Run\nHigh {Preferences.Get(MeteorHighKey, 0)}";
        SkyGameButton.Text = $"✦  Sky Dash\nHigh {Preferences.Get(SkyHighKey, 0)}";
        MazeGameButton.Text = $"▦  Maze Chase\nHigh {Preferences.Get(MazeHighKey, 0)}";
        GameIdleButton.IsVisible = _gameState is MiniGameRunState.Idle or MiniGameRunState.GameOver;
        GameIdleButton.Text = _gameState == MiniGameRunState.GameOver ? "↻  Game over - opnieuw" : "▶  Tik of klik om te spelen";
        _miniGameDrawable.State = new MiniGameRenderState(
            _selectedGame,
            _gameState,
            _gameScore,
            high,
            _paddleY,
            _ball,
            _meteorPlayerX,
            _meteors.ToArray(),
            _skyY,
            _skyObstacles.ToArray(),
            _mazePlayer,
            _mazeChaser,
            _mazeCollectibles.ToArray());
        MiniGameSurface.Invalidate();
    }

    private void WelcomeNext(object sender, EventArgs e)
    {
        if (_welcomeStepIndex >= _welcomeSteps.Length - 1)
        {
            if (_viewModel.CompleteOnboardingCommand.CanExecute(null))
            {
                _viewModel.CompleteOnboardingCommand.Execute(null);
            }

            return;
        }

        SetWelcomeStep(_welcomeStepIndex + 1);
    }

    private async void OpenSetupLink(object sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync("https://djconnect.dev/start");
    }

    private void OpenLogsFromUpdateRequired(object sender, EventArgs e) => ShowSection(LogsPanel, LogsNavButton);

    private async void OpenIntegrationLink(object sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync("https://djconnect.dev/start");
    }

    private async void OpenWhatsNewLink(object sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync("https://djconnect.dev/release-notes/windows/nl/v3.1.1.json");
    }

    private async void OpenWhatsNewOnline(object sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync(_viewModel.WhatsNewOnlineUrl);
    }

    private async void OpenSecurityLink(object sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync("mailto:security@djconnect.dev");
    }

    private async void OpenProjectLink(object sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync("https://github.com/pcvantol/djconnect-windows");
    }

    private async void OpenPrivacyPolicyLink(object sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync("https://djconnect.dev/start");
    }

    private async void OpenCodeOfConductLink(object sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync("https://github.com/pcvantol/djconnect-windows/blob/main/CODE_OF_CONDUCT.md");
    }

    private async void OpenLicenseLink(object sender, EventArgs e)
    {
        await Launcher.Default.OpenAsync("https://github.com/pcvantol/djconnect-windows/blob/main/LICENSE");
    }

    private async void ShowLicenseDetail(object sender, EventArgs e)
    {
        await DisplayAlertAsync("MIT License", MitLicenseText, "Sluiten");
    }

    private async void CopyLicenseText(object sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(MitLicenseText);
        await DisplayAlertAsync("Licentie", "Licentie gekopieerd", "OK");
    }

    private async void ShowThirdPartyNotices(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Third-party notices", ThirdPartyNoticesText, "Sluiten");
    }

    private async void CopyThirdPartyNotices(object sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(ThirdPartyNoticesText);
        await DisplayAlertAsync("Notices", "Notices gekopieerd", "OK");
    }

    private void OpenFeedbackFromAbout(object sender, EventArgs e)
    {
        if (_viewModel.ShowFeedbackCommand.CanExecute(null))
        {
            _viewModel.ShowFeedbackCommand.Execute(null);
        }
    }

    private async void ResetPairingFromSettingsClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "App opnieuw koppelen",
            "Dit wist de lokale DJConnect pairing en roteert de clientidentiteit. Je koppelt daarna opnieuw via Home Assistant.",
            "Opnieuw koppelen",
            "Annuleren");

        if (confirmed && _viewModel.ResetPairingCommand.CanExecute(null))
        {
            _viewModel.ResetPairingCommand.Execute(null);
            ShowSection(NowPlayingPanel, NowPlayingNavButton);
        }
    }

    private async void OpenMicrophoneSettingsFromSettings(object sender, EventArgs e)
    {
        _viewModel.MarkPermissionDenied(PermissionExplanationKind.Microphone);
        await Launcher.Default.OpenAsync("ms-settings:privacy-microphone");
    }

    private async void OpenNotificationSettingsFromPrivacy(object sender, EventArgs e)
    {
        if (_viewModel.EnableNotificationsCommand.CanExecute(null))
        {
            _viewModel.EnableNotificationsCommand.Execute(null);
        }

        await Launcher.Default.OpenAsync("ms-settings:notifications");
    }

    private async void OpenFirewallSettingsFromPrivacy(object sender, EventArgs e)
    {
        _viewModel.MarkPermissionDenied(PermissionExplanationKind.LocalNetwork);
        await Launcher.Default.OpenAsync("ms-settings:network-firewall");
    }

    private async void ClearLogsFromPrivacyClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Logs wissen",
            "Dit wist de lokale diagnostiek die in de app zichtbaar is.",
            "Logs wissen",
            "Annuleren");

        if (confirmed)
        {
            await _viewModel.ClearLogsAsync();
        }
    }

    private async void ClearAskDJHistoryFromPrivacyClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Ask DJ geschiedenis wissen",
            "In demo mode wordt de lokale cache gewist. Gekoppeld vraagt DJConnect Home Assistant om de geschiedenis te wissen.",
            "Geschiedenis wissen",
            "Annuleren");

        if (confirmed && _viewModel.ClearHistoryCommand.CanExecute(null))
        {
            _viewModel.ClearHistoryCommand.Execute(null);
        }
    }

    private void ToggleLogSearchClicked(object sender, EventArgs e)
    {
        if (_viewModel.IsLogSearchVisible)
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), () => LogSearchEntry.Focus());
        }
    }

    private void LogSearchEntryCompleted(object sender, EventArgs e)
    {
        if (_viewModel.NextLogSearchResultCommand.CanExecute(null))
        {
            _viewModel.NextLogSearchResultCommand.Execute(null);
        }
    }

    private void OpenLogsFromAbout(object sender, EventArgs e) => ShowSection(LogsPanel, LogsNavButton);

    private void OpenPrivacyFromAbout(object sender, EventArgs e) => ShowSection(PrivacyPanel, PrivacyNavButton);

    private void OpenLegalFromAbout(object sender, EventArgs e) => ShowSection(LegalPanel, LegalNavButton);

    private void ShowSection(View activePanel, Button activeButton)
    {
        if (activePanel != GamesPanel)
        {
            ResetAllGamesToIdle();
        }

        _viewModel.IsRuntimeSectionActive = activePanel == NowPlayingPanel
            || activePanel == AskDJPanel
            || activePanel == QueuePanel
            || activePanel == PlaylistsPanel;

        foreach (var panel in new View[]
        {
            NowPlayingPanel,
            AskDJPanel,
            QueuePanel,
            PlaylistsPanel,
            GamesPanel,
            SettingsPanel,
            LogsPanel,
            AboutPanel,
            LegalPanel,
            PrivacyPanel
        })
        {
            panel.IsVisible = panel == activePanel;
        }

        foreach (var button in new[]
        {
            NowPlayingNavButton,
            AskDJNavButton,
            QueueNavButton,
            PlaylistsNavButton,
            GamesNavButton,
            SettingsNavButton,
            LogsNavButton,
            AboutNavButton,
            LegalNavButton,
            PrivacyNavButton
        })
        {
            var isActive = button == activeButton;
            button.Background = isActive ? _activeNavBackground : _inactiveNavBackground;
            button.TextColor = isActive ? _activeNavText : _inactiveNavText;
            button.BorderColor = Colors.Transparent;
        }
    }

    private void SetWelcomeStep(int index, bool animate = true)
    {
        _welcomeStepIndex = Math.Clamp(index, 0, _welcomeSteps.Length - 1);
        var step = _welcomeSteps[_welcomeStepIndex];
        WelcomePreviewIconLabel.Text = step.Icon;
        WelcomePreviewTitleLabel.Text = step.Title;
        WelcomeTitleLabel.Text = $"{step.Icon}  {step.Title}";
        WelcomeBodyLabel.Text = step.Body;
        WelcomePreviousButton.IsEnabled = _welcomeStepIndex > 0;
        WelcomePreviousButton.Opacity = _welcomeStepIndex > 0 ? 1 : 0.55;
        WelcomeNextButton.Text = _welcomeStepIndex == _welcomeSteps.Length - 1 ? "Aan de slag" : "›  Volgende";

        var buttons = new[]
        {
            WelcomeStepNowPlayingButton,
            WelcomeStepAskDjButton,
            WelcomeStepQueueButton,
            WelcomeStepMoreButton,
            WelcomeStepGamesButton
        };

        var dots = new[]
        {
            WelcomeProgressDot0,
            WelcomeProgressDot1,
            WelcomeProgressDot2,
            WelcomeProgressDot3,
            WelcomeProgressDot4
        };

        for (var i = 0; i < buttons.Length; i++)
        {
            var isActive = i == _welcomeStepIndex;
            buttons[i].Background = isActive ? _welcomeActiveBackground : _welcomeInactiveBackground;
            buttons[i].TextColor = isActive ? Colors.White : Color.FromArgb("#C7C0DD");
            buttons[i].BorderColor = isActive ? Color.FromArgb("#F05BFF") : Color.FromArgb("#665E92");
            dots[i].Color = isActive ? Color.FromArgb("#D536F6") : Color.FromArgb("#6B638E");
            dots[i].WidthRequest = isActive ? 38 : 10;
        }

        if (animate)
        {
            _ = PulseWelcomeStepAsync(buttons[_welcomeStepIndex]);
        }
    }

    private static async Task PulseWelcomeStepAsync(Button button)
    {
        await button.ScaleToAsync(1.08, 110, Easing.CubicOut);
        await button.ScaleToAsync(1.0, 160, Easing.CubicIn);
    }

    private sealed record WelcomeStep(string Icon, string Title, string Body);
}

internal enum MiniGameKind
{
    Paddle,
    Meteor,
    Sky,
    Maze
}

internal enum MiniGameRunState
{
    Idle,
    Running,
    Paused,
    GameOver
}

internal enum MiniGameDirection
{
    Left,
    Up,
    Down,
    Right
}

internal sealed record MiniGameRenderState(
    MiniGameKind Kind,
    MiniGameRunState RunState,
    int Score,
    int HighScore,
    float PaddleY,
    PointF Ball,
    float MeteorPlayerX,
    PointF[] Meteors,
    float SkyY,
    PointF[] SkyObstacles,
    Point MazePlayer,
    Point MazeChaser,
    Point[] MazeCollectibles);

internal sealed class MiniGameDrawable : IDrawable
{
    private readonly Color _accent = Color.FromArgb("#D536F6");
    private readonly Color _blue = Color.FromArgb("#5D6BFF");
    private readonly Color _warning = Color.FromArgb("#F59E0B");
    private readonly Color _surface = Color.FromArgb("#070817");
    private readonly Color _grid = Color.FromArgb("#262A58");
    private readonly Color _text = Color.FromArgb("#F7F4FF");

    public MiniGameRenderState State { get; set; } = new(
        MiniGameKind.Paddle,
        MiniGameRunState.Idle,
        0,
        0,
        0.5f,
        new PointF(0.5f, 0.5f),
        0.5f,
        [],
        0.5f,
        [],
        new Point(1, 1),
        new Point(6, 4),
        []);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = _surface;
        canvas.FillRectangle(dirtyRect);

        DrawGrid(canvas, dirtyRect);
        switch (State.Kind)
        {
            case MiniGameKind.Paddle:
                DrawPaddle(canvas, dirtyRect);
                break;
            case MiniGameKind.Meteor:
                DrawMeteor(canvas, dirtyRect);
                break;
            case MiniGameKind.Sky:
                DrawSky(canvas, dirtyRect);
                break;
            case MiniGameKind.Maze:
                DrawMaze(canvas, dirtyRect);
                break;
        }

        canvas.FontColor = _text;
        canvas.FontSize = 16;
        canvas.DrawString($"Score {State.Score}   High {State.HighScore}", dirtyRect.Left + 18, dirtyRect.Top + 18, HorizontalAlignment.Left);
        if (State.RunState == MiniGameRunState.Paused)
        {
            DrawCenterText(canvas, dirtyRect, "Pauze");
        }
        else if (State.RunState == MiniGameRunState.GameOver)
        {
            DrawCenterText(canvas, dirtyRect, "Game over");
        }
    }

    private void DrawGrid(ICanvas canvas, RectF rect)
    {
        canvas.StrokeColor = _grid;
        canvas.StrokeSize = 1;
        for (var x = rect.Left; x < rect.Right; x += 42)
        {
            canvas.DrawLine(x, rect.Top, x, rect.Bottom);
        }

        for (var y = rect.Top; y < rect.Bottom; y += 42)
        {
            canvas.DrawLine(rect.Left, y, rect.Right, y);
        }
    }

    private void DrawPaddle(ICanvas canvas, RectF rect)
    {
        var paddleHeight = rect.Height * 0.24f;
        var paddleY = rect.Top + State.PaddleY * rect.Height - paddleHeight / 2;
        canvas.FillColor = _accent;
        canvas.FillRoundedRectangle(rect.Left + 26, paddleY, 16, paddleHeight, 8);

        canvas.FillColor = _blue;
        canvas.FillCircle(rect.Left + State.Ball.X * rect.Width, rect.Top + State.Ball.Y * rect.Height, 9);

        canvas.StrokeColor = Color.FromArgb("#665E92");
        canvas.StrokeSize = 4;
        canvas.DrawLine(rect.Right - 28, rect.Top + 28, rect.Right - 28, rect.Bottom - 28);
    }

    private void DrawMeteor(ICanvas canvas, RectF rect)
    {
        canvas.FillColor = _blue;
        var playerX = rect.Left + State.MeteorPlayerX * rect.Width;
        var playerY = rect.Bottom - 48;
        var ship = new PathF();
        ship.MoveTo(playerX, playerY - 24);
        ship.LineTo(playerX - 22, playerY + 22);
        ship.LineTo(playerX + 22, playerY + 22);
        ship.Close();
        canvas.FillPath(ship);

        canvas.FillColor = _warning;
        foreach (var meteor in State.Meteors)
        {
            canvas.FillCircle(rect.Left + meteor.X * rect.Width, rect.Top + meteor.Y * rect.Height, 18);
        }
    }

    private void DrawSky(ICanvas canvas, RectF rect)
    {
        canvas.FillColor = _accent;
        canvas.FillCircle(rect.Left + rect.Width * 0.22f, rect.Top + State.SkyY * rect.Height, 15);

        canvas.FillColor = _warning;
        foreach (var obstacle in State.SkyObstacles)
        {
            var x = rect.Left + obstacle.X * rect.Width;
            var gapY = rect.Top + obstacle.Y * rect.Height;
            canvas.FillRoundedRectangle(x - 14, rect.Top, 28, gapY - 62 - rect.Top, 6);
            canvas.FillRoundedRectangle(x - 14, gapY + 62, 28, rect.Bottom - gapY - 62, 6);
        }
    }

    private void DrawMaze(ICanvas canvas, RectF rect)
    {
        var cellW = rect.Width / 8f;
        var cellH = rect.Height / 6f;
        canvas.StrokeColor = Color.FromArgb("#3E4277");
        canvas.StrokeSize = 2;
        for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < 6; y++)
            {
                var blocked = (x == 3 && y >= 1 && y <= 4) || (x == 5 && y <= 2);
                canvas.FillColor = blocked ? Color.FromArgb("#171A3A") : Color.FromArgb("#0D1024");
                canvas.FillRectangle(rect.Left + x * cellW + 2, rect.Top + y * cellH + 2, cellW - 4, cellH - 4);
            }
        }

        canvas.FillColor = _warning;
        foreach (var collectible in State.MazeCollectibles)
        {
            canvas.FillCircle(rect.Left + ((float)collectible.X + 0.5f) * cellW, rect.Top + ((float)collectible.Y + 0.5f) * cellH, 8);
        }

        canvas.FillColor = _accent;
        canvas.FillCircle(rect.Left + ((float)State.MazePlayer.X + 0.5f) * cellW, rect.Top + ((float)State.MazePlayer.Y + 0.5f) * cellH, 15);
        canvas.FillColor = Color.FromArgb("#FF4D6D");
        canvas.FillCircle(rect.Left + ((float)State.MazeChaser.X + 0.5f) * cellW, rect.Top + ((float)State.MazeChaser.Y + 0.5f) * cellH, 15);
    }

    private void DrawCenterText(ICanvas canvas, RectF rect, string text)
    {
        canvas.FillColor = Color.FromArgb("#AA050717");
        canvas.FillRectangle(rect);
        canvas.FontColor = _text;
        canvas.FontSize = 34;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.DrawString(text, rect, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
