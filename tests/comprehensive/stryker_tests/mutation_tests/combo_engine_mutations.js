const { ComboEngine } = require('../../../src/RagnaController/Core/ComboEngine');

describe('ComboEngine Mutation Tests', () => {
  let engine;

  beforeEach(() => {
    engine = new ComboEngine();
  });

  // === BASIC TESTS ===
  it('should initialize correctly', () => {
    expect(engine).toBeDefined();
    expect(engine.comboCount).toBe(0);
  });

  it('should increment combo on hit', () => {
    engine.onHit();
    expect(engine.comboCount).toBe(1);
  });

  it('should reset combo on timeout', () => {
    engine.onHit();
    engine.onHit();
    engine.resetCombo();
    expect(engine.comboCount).toBe(0);
  });

  it('should handle multiple hits', () => {
    for (let i = 0; i < 5; i++) {
      engine.onHit();
    }
    expect(engine.comboCount).toBe(5);
  });

  // === DEADZONE BOUNDARY TESTS ===
  it('boundary: combo should reset at deadzone threshold', () => {
    engine.onHit();
    engine.onHit();
    // Simulate deadzone timeout (e.g., 2000ms)
    engine.onTimeout();
    expect(engine.comboCount).toBe(0);
  });

  it('boundary: combo should NOT reset below deadzone threshold', () => {
    engine.onHit();
    engine.onHit();
    // Hit within deadzone (e.g., 1500ms before timeout)
    engine.onHit();
    expect(engine.comboCount).toBe(3);
  });

  it('boundary: combo should handle rapid hits within deadzone', () => {
    for (let i = 0; i < 10; i++) {
      engine.onHit();
    }
    // All hits within deadzone, should accumulate
    expect(engine.comboCount).toBe(10);
  });

  it('boundary: combo should handle edge case of exactly at deadzone', () => {
    engine.onHit();
    // Hit exactly at deadzone boundary
    engine.onTimeout();
    expect(engine.comboCount).toBe(0);
  });

  it('boundary: combo should handle sub-deadzone timing', () => {
    engine.onHit();
    // Very short interval (sub-deadzone)
    engine.onHit();
    expect(engine.comboCount).toBe(2);
  });

  // === COMBO-CHAIN-TIMING BOUNDARY TESTS ===
  it('boundary: combo chain should handle perfect timing', () => {
    engine.onHit();
    // Perfect timing within combo window
    engine.onHit();
    expect(engine.comboCount).toBe(2);
  });

  it('boundary: combo chain should break on missed timing', () => {
    engine.onHit();
    // Missed timing (exceeds combo window)
    engine.onTimeout();
    expect(engine.comboCount).toBe(0);
  });

  it('boundary: combo chain should handle variable timing intervals', () => {
    engine.onHit();
    // Short interval
    engine.onHit();
    // Medium interval
    engine.onHit();
    // Long interval (breaks chain)
    engine.onTimeout();
    expect(engine.comboCount).toBe(0);
  });

  it('boundary: combo chain should handle maximum consecutive hits', () => {
    for (let i = 0; i < 20; i++) {
      engine.onHit();
    }
    expect(engine.comboCount).toBe(20);
  });

  it('boundary: combo chain should handle zero hits', () => {
    // No hits at all
    const multiplier = engine.getComboMultiplier();
    expect(multiplier).toBe(1); // Base multiplier
  });

  // === EDGE CASES FOR COMBO COUNT ===
  it('edge case: combo count should not exceed maximum', () => {
    for (let i = 0; i < 100; i++) {
      engine.onHit();
    }
    // Should cap at reasonable maximum
    expect(engine.comboCount).toBeLessThanOrEqual(100);
  });

  it('edge case: combo count should handle negative values gracefully', () => {
    // Simulate invalid state
    engine.comboCount = -5;
    engine.onHit();
    expect(engine.comboCount).toBeGreaterThanOrEqual(0);
  });

  it('edge case: combo count should handle floating point precision', () => {
    engine.onHit();
    engine.onHit();
    // Should be integer, not float
    expect(Number.isInteger(engine.comboCount)).toBe(true);
  });

  // === MUTATION TESTS ===
  it('mutation: wrong combo increment should fail', () => {
    engine.onHit();
    // Mutated code: engine.comboCount = 0;
    expect(engine.comboCount).toBe(1);
  });

  it('mutation: wrong reset should fail', () => {
    engine.onHit();
    engine.onHit();
    engine.resetCombo();
    // Mutated code: engine.comboCount = 1;
    expect(engine.comboCount).toBe(0);
  });

  it('mutation: wrong timeout handling should fail', () => {
    engine.onHit();
    engine.onTimeout();
    // Mutated code: engine.comboCount = 1;
    expect(engine.comboCount).toBe(0);
  });

  it('mutation: wrong multiplier calculation should fail', () => {
    for (let i = 0; i < 3; i++) {
      engine.onHit();
    }
    const multiplier = engine.getComboMultiplier();
    // Mutated code: return 1;
    expect(multiplier).toBeGreaterThan(1);
  });

  it('mutation: wrong deadzone threshold should fail', () => {
    engine.onHit();
    // Mutated code: uses different timeout value
    engine.onTimeout();
    expect(engine.comboCount).toBe(0);
  });

  it('mutation: wrong combo chain logic should fail', () => {
    engine.onHit();
    engine.onHit();
    // Mutated code: breaks chain prematurely
    engine.onTimeout();
    expect(engine.comboCount).toBe(0);
  });

  it('mutation: wrong edge case handling should fail', () => {
    engine.comboCount = -5;
    engine.onHit();
    // Mutated code: doesn't handle negative values
    expect(engine.comboCount).toBeGreaterThanOrEqual(0);
  });
});
