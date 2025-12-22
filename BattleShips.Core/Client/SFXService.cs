using System;
using System.Collections.Generic;
using NAudio.Wave;
using BattleShips.Core;
using BattleShips.Core.Client;

namespace BattleShips.Client
{
    public class SFXService
    {

        private static class SoundFiles
        {
            public const string PlacementStart = "placement_start.wav";
            public const string GameStart = "game_start.wav";
            public const string GameOver = "game_over.wav";
            public const string Disaster = "disaster.wav";
            public const string DisasterStart = "disaster_start.wav";
            public const string DisasterEnd = "disaster_end.wav";
            public const string TurnChange = "turn_change.wav";
            public const string Hit = "hit.mp3";
            public const string Miss = "miss.mp3";
            public const string ShipPlaced = "ship_placed.wav";
            public const string ButtonClick = "button_click.wav";
        }

        private readonly IGameMediator _mediator;
        private readonly GameModel _model;
        private bool _isEnabled = true;
        private float _volume = 1.0f; // 0.0 to 1.0
        private readonly List<WaveOutEvent> _activeOutputs = new();

        public SFXService(IGameMediator mediator = null)
        {
            _mediator = mediator;

            if (_mediator != null)
            {
                _mediator.RegisterSFXService(this);
            }
        }

        public SFXService(GameModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _model.PropertyChanged += OnModelPropertyChanged;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0.0f, 1.0f);
                foreach (var output in _activeOutputs.ToList())
                {
                    output.Volume = _volume;
                }
            }
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_isEnabled) return;

            switch (e.PropertyName)
            {
                case nameof(_model.LastMoveResult):
                    OnMoveResult(_model.LastMoveResult);
                    break;
                case nameof(_model.State):
                    OnStateChanged(_model.State);
                    break;
                case nameof(_model.IsMyTurn):
                    OnTurnChanged(_model.IsMyTurn);
                    break;
                // case nameof(_model.CurrentDisasterName):
                //     if (!string.IsNullOrEmpty(_model.CurrentDisasterName))
                //         PlaySound(SoundFiles.Disaster);
                //     break;
                case nameof(_model.IsDisasterAnimating):
                    if (_model.IsDisasterAnimating)
                        PlaySound(SoundFiles.Disaster);
                    break;

            }
        }

        private void OnStateChanged(AppState newState)
        {
            string? soundFile = newState switch
            {
                AppState.Placement => SoundFiles.PlacementStart,
                AppState.Playing => SoundFiles.GameStart,
                AppState.GameOver => SoundFiles.GameOver,
                _ => null
            };

            if (soundFile != null)
                PlaySound(soundFile);
        }

        private void OnTurnChanged(bool isMyTurn)
        {
            // For now, use the same sound for turn change
            // can use different sounds via isMyTurn if desired
            PlaySound(SoundFiles.TurnChange);
            //PlaySound(isMyTurn ? SoundFiles. : SoundFiles.);
        }

        private void OnMoveResult(bool? hit)
        {
            if (hit == true) PlaySound(SoundFiles.Hit);
            else if (hit == false) PlaySound(SoundFiles.Miss);
        }

        // Public methods to play sounds for events not covered by property changes
        public void PlayHitSound() => PlaySound(SoundFiles.Hit);
        public void PlayMissSound() => PlaySound(SoundFiles.Miss);
        public void PlayShipPlacedSound() => PlaySound(SoundFiles.ShipPlaced);
        public void PlayButtonClickSound() => PlaySound(SoundFiles.ButtonClick);

        public void OnSoundEvent(string soundType)
        {
            Console.WriteLine($"[SFX Service] Received sound event: {soundType}");

            switch (soundType)
            {
                case "PlayHitSound":
                    PlayHitSound();
                    break;
                case "PlayMissSound":
                    PlayMissSound();
                    break;
            }
        }

        private void PlaySound(string fileName)
        {
            if (!_isEnabled || string.IsNullOrEmpty(fileName)) return;

            try
            {
                string soundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", fileName);
                if (System.IO.File.Exists(soundPath))
                {
                    var outputDevice = new WaveOutEvent();
                    _activeOutputs.Add(outputDevice);
                    outputDevice.Volume = _volume;
                    outputDevice.PlaybackStopped += (sender, args) =>
                    {
                        if (sender is WaveOutEvent waveOut)
                        {
                            _activeOutputs.Remove(waveOut);
                            waveOut.Dispose();
                        }
                    };

                    var audioFile = new MediaFoundationReader(soundPath);
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                }
                else
                {
                    Console.WriteLine($"Sound file not found: {soundPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound {fileName}: {ex.Message}");
            }
        }
    }
}
