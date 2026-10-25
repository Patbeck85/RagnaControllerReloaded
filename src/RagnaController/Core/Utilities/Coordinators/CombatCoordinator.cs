using System;
using System.Collections.Generic;







using RagnaController.Models;
using RagnaController.Profiles;
using RagnaController.Core;
using RagnaController.Controller;
using RagnaController.Coordinators;

namespace RagnaController.Coordinators
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
        private readonly IMessenger _messenger;
        private readonly IFeedbackProvider _feedbackProvider;

        // Engines (nullable — initialized in Initialize())
        private CombatEngine? _combat;
        private AutoTargetEngine? _autoTarget;
        private MageEngine? _mage;
        private ComboEngine? _combo;
        private KiteEngine? _kite;
        private SupportEngine? _support;
        private MobSweepEngine? _mobSweep;
        private CooldownManager? _cooldownManager;
        private SkillOrchestrator? _skillOrchestrator;
        private GroundSpellEngine? _groundSpell;
        private BuffManager? _buffManager;
        private FeedbackSystem? _feedbackSystem;
        private DefaultRotationProvider? _rotationProvider;

        public CombatEngine? Combat => _combat;
        public AutoTargetEngine? AutoTarget => _autoTarget;
        public MageEngine? Mage => _mage;
        public ComboEngine? Combo => _combo;
        public KiteEngine? Kite => _kite;
        public SupportEngine? Support => _support;
        public MobSweepEngine? MobSweep => _mobSweep;
        public CooldownManager? CooldownManager => _cooldownManager;
        public SkillOrchestrator? SkillOrchestrator => _skillOrchestrator;
        public GroundSpellEngine? GroundSpell => _groundSpell;
        public BuffManager? BuffManager => _buffManager;
        public FeedbackSystem? Feedback => _feedbackSystem;

        // State passed from EngineOrchestrator for Tick
        private ParsedInput _lastInput = new ParsedInput();
        private int _lastDeltaMs = 16;
        private bool _lastIsMoving;

        public CombatCoordinator(
            InputCommandQueue queue,
            AdvancedLogger logger,
            IMessenger messenger,
            IFeedbackProvider feedbackProvider)
        {
            _queue = queue;
            _logger = logger;
            _messenger = messenger;
            _feedbackProvider = feedbackProvider;
        }

        public override void Initialize()
        {
            _logger?.Info("[CombatCoordinator] Initializing combat engines...");

            _cooldownManager = new CooldownManager(_messenger, _feedbackProvider);
            _autoTarget = new AutoTargetEngine(_queue);
            // CombatEngine needs WindowTracker — pass null! (set via InitializeDependencies)
            _combat = new CombatEngine(null!, _queue);
            _kite = new KiteEngine(_queue);
            _mobSweep = new MobSweepEngine(_queue);
            _mage = new MageEngine();
            _support = new SupportEngine(_queue);
            _combo = new ComboEngine(_queue);

            _rotationProvider = new DefaultRotationProvider();
            _skillOrchestrator = new SkillOrchestrator(_queue, _rotationProvider);
            _groundSpell = new GroundSpellEngine(_queue);
            _buffManager = new BuffManager(_queue, _cooldownManager);
        }

        public void InitializeDependencies(ControllerService ctrl)
        {
            _feedbackSystem = new FeedbackSystem(ctrl);

            if (_skillOrchestrator != null && _autoTarget != null && _mage != null
                && _support != null && _combo != null && _cooldownManager != null && _groundSpell != null)
            {
                _skillOrchestrator.SetDependencies(
                    autoTarget: _autoTarget,
                    mage: _mage,
                    support: _support,
                    combo: _combo,
                    cooldownManager: _cooldownManager,
                    groundSpell: _groundSpell);
            }
        }

        public override bool Tick()
        {
            if (!_isRunning || _isPaused) return false;

            bool anyWork = false;

            // IInputHandler engines — driven by Handle(ParsedInput, int)
            anyWork |= _autoTarget?.Handle(_lastInput, _lastDeltaMs) ?? false;
            anyWork |= _mage?.Handle(_lastInput, _lastDeltaMs) ?? false;
            anyWork |= _kite?.Handle(_lastInput, _lastDeltaMs) ?? false;
            anyWork |= _support?.Handle(_lastInput, _lastDeltaMs) ?? false;
            anyWork |= _mobSweep?.Handle(_lastInput, _lastDeltaMs) ?? false;

            // CooldownManager — Tick() every frame (no args)
            _cooldownManager?.Tick();

            // ComboEngine — driven by SkillOrchestrator (not directly here).
            // ParsedInput is gamepad-only; attack state comes from skill orchestration.
            if (_combo != null && _lastInput.TriggerRight > 0.5f)
            {
                _combo.Update(true, _lastDeltaMs);
            }

            // SkillOrchestrator — needs game state (void return, no bool OR)
            if (_skillOrchestrator != null && _autoTarget != null && _combat != null
                && _support != null && _groundSpell != null)
            {
                var activeBuffs = _support.ActiveBuffs ?? new List<string>();
                var activeDebuffs = _support.ActiveDebuffs ?? new List<string>();
                var groundSpells = _groundSpell.GetActiveSpellNames() ?? new List<string>();

                _skillOrchestrator.Update(
                    _lastInput, _lastDeltaMs,
                    _autoTarget.CurrentTarget != null,
                    _autoTarget.CurrentTargetDistance,
                    _combat.CurrentSP,
                    _combat.CurrentHPPercent,
                    _lastIsMoving,
                    _autoTarget.IsFacingTarget,
                    activeBuffs,
                    activeDebuffs,
                    _autoTarget.NearbyEnemyCount,
                    groundSpells);
            }

            anyWork |= _groundSpell?.Handle(_lastInput, _lastDeltaMs) ?? false;

            // BuffManager — Update(int deltaMs), always considered work
            if (_buffManager != null)
            {
                _buffManager.Update(_lastDeltaMs);
                anyWork = true;
            }

            return anyWork;
        }

        public void SetTickState(ParsedInput input, int deltaMs, bool isMoving)
        {
            _lastInput = input;
            _lastDeltaMs = deltaMs;
            _lastIsMoving = isMoving;
        }

        public override void ApplyProfile(Profile profile)
        {
            // Only CombatEngine has a profile-loading method (LoadProfile)
            _combat?.LoadProfile(profile);
        }

        public override void Dispose()
        {
            if (_disposed) return;
            base.Dispose();

            _feedbackSystem?.Dispose();
            _buffManager?.Dispose();
            _groundSpell?.Dispose();
            _kite?.Dispose();
            _combo?.Dispose();
            _autoTarget?.Dispose();
            _combat?.Dispose();
        }
    }
}
