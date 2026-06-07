using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using AgentPetCore;

namespace AgentPetApp
{
    public class PetController
    {
        public static PetController Shared { get; } = new PetController();

        private AgentPetCore.ImagePetPack? _currentPack;
        public AgentPetCore.ImagePetPack? CurrentPack => _currentPack;
        private PetMood _mood = PetMood.Idle;
        private DispatcherTimer? _celebrateTimer;

        // Action to call when the clip changes, so the UI can update
        public Action<List<System.Windows.Media.Imaging.BitmapSource>>? OnClipChanged;
        public Action<string, string>? OnMoodChanged;

        public void LoadPack(string directory)
        {
            _currentPack = SpriteSlicer.LoadPack(directory);
            UpdateClip();
        }

        public void UpdateSessions(IEnumerable<AgentSession> sessions)
        {
            var resolved = AggregateMood(sessions);
            
            if (resolved == PetMood.Done && _mood != PetMood.Done)
            {
                SetMood(PetMood.Celebrate);
                _celebrateTimer?.Stop();
                _celebrateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _celebrateTimer.Tick += (s, e) => 
                {
                    _celebrateTimer.Stop();
                    SetMood(AggregateMood(sessions));
                };
                _celebrateTimer.Start();
                return;
            }

            if (_mood == PetMood.Celebrate && resolved == PetMood.Done)
            {
                return; // Let celebration finish
            }

            _celebrateTimer?.Stop();
            SetMood(resolved);
        }

        private void SetMood(PetMood newMood)
        {
            if (_mood != newMood || _currentPack != null) // Always update clip if pack loaded recently
            {
                _mood = newMood;
                UpdateClip();
                
                string chatText = newMood switch
                {
                    PetMood.Working => "I'm working on it...",
                    PetMood.Waiting => "Waiting for you...",
                    PetMood.Done => "Done!",
                    PetMood.Celebrate => "YAY! We did it!",
                    _ => "Just chilling..."
                };
                OnMoodChanged?.Invoke(newMood.ToString(), chatText);
            }
        }

        private void UpdateClip()
        {
            if (_currentPack == null) return;
            
            int clipIndex = PetBindingsStore.Shared.ClipIndex(_currentPack.Id, _currentPack.ClipCount, _mood);
            var clip = _currentPack.GetClip(clipIndex);
            
            OnClipChanged?.Invoke(clip);
        }

        private PetMood AggregateMood(IEnumerable<AgentSession> sessions)
        {
            var active = sessions.Where(s => s.State != AgentState.Idle).ToList();
            if (!active.Any()) return PetMood.Idle;

            if (active.Any(s => s.State == AgentState.Working)) return PetMood.Working;
            if (active.Any(s => s.State == AgentState.Waiting)) return PetMood.Waiting;
            if (active.All(s => s.State == AgentState.Done)) return PetMood.Done;

            return PetMood.Idle;
        }
    }
}
