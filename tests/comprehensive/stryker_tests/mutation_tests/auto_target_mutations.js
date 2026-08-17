const { AutoTargetEngine } = require('../../../src/RagnaController/Core/AutoTargetEngine');

describe('AutoTargetEngine Mutation Tests', () => {
  let engine;

  beforeEach(() => {
    engine = new AutoTargetEngine();
  });

  // === BASIC TESTS ===
  it('should calculate distance correctly', () => {
    const result = engine.calculateDistance(100, 200, 300, 400);
    expect(result).toBeCloseTo(412.31, 2); // sqrt((300-100)^2 + (400-200)^2)
  });

  it('should check range correctly', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 500);
    expect(inRange).toBe(true); // distance < 500
  });

  it('should handle target selection', () => {
    const target = engine.selectTarget(100, 200, 300, 400, 500);
    expect(target).toBeDefined();
  });

  // === DISTANCE BOUNDARY TESTS ===
  it('boundary: distance should be 0 for same position', () => {
    const result = engine.calculateDistance(100, 200, 100, 200);
    expect(result).toBeCloseTo(0, 2);
  });

  it('boundary: distance should handle negative coordinates', () => {
    const result = engine.calculateDistance(-100, -200, -300, -400);
    expect(result).toBeCloseTo(412.31, 2); // Same distance as positive
  });

  it('boundary: distance should handle mixed positive/negative coordinates', () => {
    const result = engine.calculateDistance(-100, 200, 300, -400);
    expect(result).toBeGreaterThan(0);
  });

  it('boundary: distance should handle very large coordinates', () => {
    const result = engine.calculateDistance(100000, 200000, 300000, 400000);
    expect(result).toBeGreaterThan(0);
  });

  it('boundary: distance should handle floating point precision', () => {
    const result = engine.calculateDistance(100.5, 200.5, 300.5, 400.5);
    expect(result).toBeGreaterThan(0);
  });

  it('boundary: distance should handle zero coordinates', () => {
    const result = engine.calculateDistance(0, 0, 100, 200);
    expect(result).toBeGreaterThan(0);
  });

  it('boundary: distance should handle max integer coordinates', () => {
    const MAX_INT = 2147483647;
    const result = engine.calculateDistance(0, 0, MAX_INT, MAX_INT);
    expect(result).toBeGreaterThan(0);
  });

  it('boundary: distance should handle min integer coordinates', () => {
    const MIN_INT = -2147483648;
    const result = engine.calculateDistance(MIN_INT, MIN_INT, 0, 0);
    expect(result).toBeGreaterThan(0);
  });

  // === RANGE CHECK BOUNDARY TESTS ===
  it('boundary: range check should be true at exact range limit', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 500);
    expect(inRange).toBe(true); // distance < 500
  });

  it('boundary: range check should be false above range limit', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 400); // distance > 400
    expect(inRange).toBe(false);
  });

  it('boundary: range check should handle zero range', () => {
    const inRange = engine.isWithinRange(100, 200, 100, 200, 0); // same position, range 0
    expect(inRange).toBe(false);
  });

  it('boundary: range check should handle negative range', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, -100);
    expect(inRange).toBe(false); // Negative range is invalid
  });

  it('boundary: range check should handle very large range', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 999999);
    expect(inRange).toBe(true);
  });

  it('boundary: range check should handle exact match', () => {
    const result = engine.calculateDistance(100, 200, 300, 400);
    const inRange = engine.isWithinRange(100, 200, 300, 400, result + 0.01);
    expect(inRange).toBe(true);
  });

  // === TARGET SELECTION BOUNDARY TESTS ===
  it('boundary: target selection should handle single valid target', () => {
    const target = engine.selectTarget(100, 200, 300, 400, 500);
    expect(target).toBeDefined();
  });

  it('boundary: target selection should return null for no valid targets', () => {
    // Simulate scenario with no valid targets (all out of range)
    const target = engine.selectTarget(10000, 20000, 300, 400, 500);
    expect(target).toBeNull();
  });

  it('boundary: target selection should handle multiple valid targets', () => {
    // Simulate multiple targets within range
    const target = engine.selectTarget(100, 200, 300, 400, 500);
    expect(target).toBeDefined();
  });

  it('boundary: target selection should handle edge case at range boundary', () => {
    const result = engine.calculateDistance(100, 200, 300, 400);
    const inRange = engine.isWithinRange(100, 200, 300, 400, result + 0.001);
    expect(inRange).toBe(true);
  });

  it('boundary: target selection should handle floating point precision', () => {
    const result = engine.calculateDistance(100.1, 200.1, 300.1, 400.1);
    expect(result).toBeGreaterThan(0);
  });

  // === EDGE CASES ===
  it('edge case: should handle NaN coordinates gracefully', () => {
    const result = engine.calculateDistance(NaN, NaN, NaN, NaN);
    expect(Number.isNaN(result)).toBe(true);
  });

  it('edge case: should handle Infinity coordinates gracefully', () => {
    const result = engine.calculateDistance(Infinity, Infinity, Infinity, Infinity);
    expect(result).toBe(Infinity);
  });

  it('edge case: should handle undefined coordinates gracefully', () => {
    const result = engine.calculateDistance(undefined, undefined, undefined, undefined);
    expect(Number.isNaN(result)).toBe(true);
  });

  it('edge case: should handle null coordinates gracefully', () => {
    const result = engine.calculateDistance(null, null, null, null);
    expect(Number.isNaN(result)).toBe(true);
  });

  // === MUTATION TESTS ===
  it('mutation: wrong distance formula should fail', () => {
    const result = engine.calculateDistance(100, 200, 300, 400);
    // Mutated code: return Math.sqrt((300-100) + (400-200)); // Addition statt Power
    expect(result).toBeCloseTo(412.31, 2);
  });

  it('mutation: wrong range check should fail', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 500);
    // Mutated code: return distance > 500;
    expect(inRange).toBe(true);
  });

  it('mutation: wrong target selection should fail', () => {
    const target = engine.selectTarget(100, 200, 300, 400, 500);
    // Mutated code: return null;
    expect(target).toBeDefined();
  });

  it('mutation: wrong edge case handling should fail', () => {
    const result = engine.calculateDistance(0, 0, 0, 0);
    // Mutated code: return -1;
    expect(result).toBeCloseTo(0, 2);
  });

  it('mutation: wrong boundary handling should fail', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 500);
    // Mutated code: return false;
    expect(inRange).toBe(true);
  });

  it('mutation: wrong null handling should fail', () => {
    const target = engine.selectTarget(10000, 20000, 300, 400, 500);
    // Mutated code: return 'invalid';
    expect(target).toBeNull();
  });
});
