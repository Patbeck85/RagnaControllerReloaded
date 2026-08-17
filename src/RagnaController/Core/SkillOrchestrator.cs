using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Zeitkritische Skill-Sequenzen über die InputCommandQueue (deterministisch,
    /// kein Task.Run/Task.Delay-Jitter).
    /// </summary>
    public sealed class SkillOrchestrator
    {
        private readonly InputCommandQueue _queue;
        private bool _isCasting;
        private bool _isActive;
        public int Priority { get; set; } = 50;
        public bool SkillEnabled { get; set; }

        public SkillOrchestrator(InputCommandQueue queue) => _queue = queue;

        public void Activate() => _isActive = true;
        public void Deactivate() => _isActive = false;
        public bool IsActive => _isActive;

        public void Update(int deltaMs)
        {
            if (!_isActive || !SkillEnabled) return;
        }

        public void CastSkill(VirtualKey key, bool isGroundSpell, float lx, float ly, bool wasWalking)
        {
            if (_isCasting) return;
            _isCasting = true;

            // 1. Laufen stoppen — LMB release via Queue (deterministisch)
            if (wasWalking) _queue.LeftUp();

            // 2. Skill-Taste via Queue (gleicher Thread wie Maus-Moves)
            _queue.Action(() => _queue.TapKey(key));

            // 3. Bodenzauber: Jitter-Puffer + Klick, alles in der Queue
            if (isGroundSpell)
            {
                _queue.Wait(JitterService.Apply(20, 5));
                _queue.LeftClick();
            }

            // 4. Animations-Puffer — dann Casting-Flag freigeben
            _queue.Wait(JitterService.Apply(100, 20));
            _queue.Action(() => _isCasting = false);
        }
    }
}
