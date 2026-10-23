using System;
using RagnaController.Controller;
using RagnaController.Profiles;
using RagnaController.Core.Input;

namespace RagnaController.Core.Utilities.Coordinators
{
    /// <summary>
    /// Coordinates all combat-related engines:
    /// - CombatEngine: Core combat logic
    /// - AutoTargetEngine: Target acquisition
    /// - MageEngine: Mage-specific spell casting
    /// - ComboEngine: Combo execution
    /// - KiteEngine: Kiting behavior
    /// - SupportEngine: Support skills (heals, buffs)
    /// - MobSweepEngine: Mob sweeping/AOE
    /// - CooldownManager: Skill cooldown tracking
    /// - SkillOrchestrator: High-level skill coordination
    /// - GroundSpellEngine: Ground-targeted spells
    /// - BuffManager: Buff/debuff tracking
    /// - FeedbackSystem: Haptic/visual feedback
    /// </summary>
    public sealed class CombatCoordinator : EngineCoordinatorBase
    {
        private readonly InputCommandQueue _queue;
        private readonly AdvancedLogger _logger;

        // Engines
        private CombatEngine _combat;
        private AutoTargetEngine _autoTarget;
        private MageEngine _mage;
        private ComboEngine _combo;
        private KiteEngine _kite;
        private SupportEngine _support;
        private MobSweepEngine _mobSweep;
        private CooldownManager _cooldownManager;
        private SkillOrchestrator _skillOrchestrator;
        private GroundSpellEngine _groundSpell;
        private BuffManager _buffManager;
        private FeedbackSystem _feedback;
        private DefaultRotationProvider _rotationProvider;

        public CombatEngine Combat => _combat;
        public AutoTargetEngine AutoTarget => _autoTarget;
        public MageEngine Mage => _mage;
        public ComboEngine Combo => _combo;
        public KiteEngine Kite => _kite;
        public SupportEngine Support => _support;
        public MobSweepEngine MobSweep => _mobSweep;
        public CooldownManager CooldownManager => _cooldownManager;
        public SkillOrchestrator SkillOrchestrator => _skillOrchestrator;
        public GroundSpellEngine GroundSpell => _groundSpell;
        public BuffManager BuffManager => _buffManager;
        public FeedbackSystem Feedback => _feedback;

        public CombatCoordinator(InputCommandQueue queue, AdvancedLogger logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public override void Initialize()
        {
            _logger?.Info("[CombatCoordinator] Initializing combat engines...");

            _combat = new CombatEngine(_queue);
            _autoTarget = new AutoTargetEngine(_queue);
            _mage = new MageEngine();
            _combo = new ComboEngine(_queue);
            _kite = new KiteEngine(_queue);
            _support = new SupportEngine(_queue);
            _mobSweep = new MobSweepEngine(_queue);
            _cooldownManager = new CooldownManager(null, null); // messenger, feedback set later
            _feedback = new FeedbackSystem(null); // ctrl set later

            _rotationProvider = new DefaultRotationProvider();
            _skillOrchestrator = new SkillOrchestrator(_queue, _rotationProvider);
            _groundSpell = new GroundSpellEngine();
            _buffManager = new BuffManager();
        }

        public void InitializeDependencies(IMessenger messenger, IFeedbackProvider feedback, ControllerService ctrl)
        {
            // Re-create engines that need these dependencies
            _combat = new CombatEngine(_queue);
            _autoTarget = new AutoTargetEngine(_queue);
            _combo = new ComboEngine(_queue);
            _kite = new KiteEngine(_queue);
            _support = new SupportEngine(_queue);
            _mobSweep = new MobSweepEngine(_queue);

            _cooldownManager = new CooldownManager(messenger, feedback);
            _feedback = feedback;

            _skillOrchestrator = new SkillOrchestrator(_queue, _rotationProvider);
            _skillOrchestrator.SetDependencies(
                autoTarget: _autoTarget,
                mage: _mage,
                support: _support,
                combo: _combo,
                cooldownManager: _cooldownManager,
                groundSpell: _groundSpell);
        }

        public override bool Tick()
        {
            if (!_isRunning || _isPaused) return false;

            bool anyWork = false;

            // Tick combat engines in priority order
            anyWork |= _combat?.Tick() ?? false;
            anyWork |= _autoTarget?.Tick() ?? false;
            anyWork |= _mage?.Tick() ?? false;
            anyWork |= _combo?.Tick() ?? false;
            anyWork |= _kite?.Tick() ?? false;
            anyWork |= _support?.Tick() ?? false;
            anyWork |= _mobSweep?.Tick() ?? false;
            anyWork |= _cooldownManager?.Tick() ?? false;
            anyWork |= _skillOrchestrator?.Tick() ?? false;
            anyWork |= _groundSpell?.Tick() ?? false;
            anyWork |= _buffManager?.Tick() ?? false;

            return anyWork;
        }

        public override void ApplyProfile(Profile profile)
        {
            _combat?.ApplyProfile(profile);
            _autoTarget?.ApplyProfile(profile);
            _mage?.ApplyProfile(profile);
            _combo?.ApplyProfile(profile);
            _kite?.ApplyProfile(profile);
            _support?.ApplyProfile(profile);
            _mobSweep?.ApplyProfile(profile);
        }

        public override void Dispose()
        {
            if (_disposed) return;
            base.Dispose();

            _combat?.Dispose();
            _autoTarget?.Dispose();
            _mage?.Dispose();
            _combo?.Dispose();
            _kite?.Dispose();
            _support?.Dispose();
            _mobSweep?.Dispose();
            _cooldownManager?.Dispose();
            _skillOrchestrator?.Dispose();
            _groundSpell?.Dispose();
            _buffManager?.Dispose();
        }
    }
}