using System;
using RagnaController.Core.Input;

namespace RagnaController.Core.Utilities.Coordinators
{
    /// <summary>
    /// Coordinates advanced feature engines:
    /// - ItemManagerEngine: Auto-potion & item management
    /// - PartyManager: Party auto-heal loop
    /// - GroundSpellEngine: Ground-targeted spells
    /// - ChordInputService: Complex input combinations
    /// - SessionRecorder: Session replay recording
    /// </summary>
    public sealed class AdvancedCoordinator : EngineCoordinatorBase
    {
        private readonly AdvancedLogger _logger;
        private readonly InputCommandQueue _queue;

        // Engines
        private ItemManagerEngine _itemManager;
        private PartyManager _partyManager;
        private GroundSpellEngine _groundSpell;
        private ChordInputService _chordInput;
        private SessionRecorder _sessionRecorder;

        public ItemManagerEngine ItemManager => _itemManager;
        public PartyManager PartyManager => _partyManager;
        public GroundSpellEngine GroundSpell => _groundSpell;
        public ChordInputService ChordInput => _chordInput;
        public SessionRecorder SessionRecorder => _sessionRecorder;

        public AdvancedCoordinator(InputCommandQueue queue, AdvancedLogger logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public override void Initialize()
        {
            _logger?.Info("[AdvancedCoordinator] Initializing advanced engines...");

            _itemManager = new ItemManagerEngine(_queue);
            _partyManager = new PartyManager(_queue);
            _groundSpell = new GroundSpellEngine();
            _chordInput = new ChordInputService();
            // _sessionRecorder = new SessionRecorder(); // optional, enabled via settings
        }

        public void InitializeDependencies(
            IMessenger messenger,
            IFeedbackProvider feedback,
            CooldownManager cooldownManager,
            AutoTargetEngine autoTarget,
            MageEngine mage,
            SupportEngine support,
            ComboEngine combo,
            SkillOrchestrator skillOrchestrator)
        {
            _itemManager = new ItemManagerEngine(_queue);
            _partyManager = new PartyManager(_queue);
            _groundSpell = new GroundSpellEngine();

            if (skillOrchestrator != null)
            {
                skillOrchestrator.SetDependencies(
                    autoTarget: autoTarget,
                    mage: mage,
                    support: support,
                    combo: combo,
                    cooldownManager: cooldownManager,
                    groundSpell: _groundSpell);
            }
        }

        public override bool Tick()
        {
            if (!_isRunning || _isPaused) return false;

            bool anyWork = false;

            anyWork |= _itemManager?.Tick() ?? false;
            anyWork |= _partyManager?.Tick() ?? false;
            anyWork |= _groundSpell?.Tick() ?? false;
            anyWork |= _chordInput?.Tick() ?? false;
            anyWork |= _sessionRecorder?.Tick() ?? false;

            return anyWork;
        }

        public override void ApplyProfile(Profile profile)
        {
            _itemManager?.ApplyProfile(profile);
            _partyManager?.ApplyProfile(profile);
            _groundSpell?.ApplyProfile(profile);
            _chordInput?.ApplyProfile(profile);
        }

        public override void Dispose()
        {
            if (_disposed) return;
            base.Dispose();

            _itemManager?.Dispose();
            _partyManager?.Dispose();
            _groundSpell?.Dispose();
            _chordInput?.Dispose();
            _sessionRecorder?.Dispose();
        }
    }
}