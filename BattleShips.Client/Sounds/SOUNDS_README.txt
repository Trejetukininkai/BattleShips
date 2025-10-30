This directory contains sound effect files used by the SFXService.

Required .wav sound files:
- hit.wav: Played when a hit is scored (both your hits and opponent's hits)
- miss.wav: Played when a shot misses (both your misses and opponent's misses)
- ship_placed.wav: Played when a ship is successfully placed on the board
- button_click.wav: Played when UI buttons are clicked (not currently implemented)
- placement_start.wav: Played when entering placement phase
- game_start.wav: Played when game begins (duplicate for now)
- game_over.wav: Played when game ends (duplicate for now)
- turn_change.wav: Played when turn control changes
- disaster.wav: Played when disaster starts

The SFXService will automatically play these sounds when corresponding game events occur.
If sound files are missing, the system will log warnings but continue normally without audio.
