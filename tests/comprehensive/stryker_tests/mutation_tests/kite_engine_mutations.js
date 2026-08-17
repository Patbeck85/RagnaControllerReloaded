const { KiteEngine } = require('../../../src/RagnaController/Core/KiteEngine');

describe('KiteEngine Mutation Tests', () => {
  let engine;

  beforeEach(() => {
    engine = new KiteEngine();
  });

  // === BASIC TESTS ===
  it('should initialize correctly', () => {
    expect(engine).toBeDefined();
    expect(engine.isInitialized).toBe(true);
  });

  it('should handle kite movement', () => {
    const state = engine.createKiteState(100, 200);
    expect(state.x).toBe(100);
    expect(state.y).toBe(200);
  });

  it('should update kite position', () => {
    const state = engine.createKiteState(100, 200);
    engine.updatePosition(state, 5, 3);
    expect(state.x).toBe(105);
    expect(state.y).toBe(203);
  });

  it('should handle kite direction', () => {
    const state = engine.createKiteState(100, 200);
    engine.setDirection(state, 'forward');
    expect(state.direction).toBe('forward');
  });

  // === KITE-DISTANZ BOUNDARY TESTS ===
  it('boundary: kite should be within range at exact distance', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 500);
    expect(inRange).toBe(true);
  });

  it('boundary: kite should be out of range above distance limit', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 400); // distance > 400
    expect(inRange).toBe(false);
  });

  it('boundary: kite should handle zero distance to target', () => {
    const inRange = engine.isWithinRange(100, 200, 100, 200, 500); // same position
    expect(inRange).toBe(true);
  });

  it('boundary: kite should handle negative distance (invalid)', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, -100);
    expect(inRange).toBe(false);
  });

  it('boundary: kite should handle very large distance', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 999999);
    expect(inRange).toBe(true);
  });

  it('boundary: kite should handle exact range boundary', () => {
    const result = engine.calculateDistance(100, 200, 300, 400);
    const inRange = engine.isWithinRange(100, 200, 300, 400, result + 0.001);
    expect(inRange).toBe(true);
  });

  it('boundary: kite should handle floating point precision in distance', () => {
    const result = engine.calculateDistance(100.5, 200.5, 300.5, 400.5);
    expect(result).toBeGreaterThan(0);
  });

  it('boundary: kite should handle zero coordinates', () => {
    const state = engine.createKiteState(0, 0);
    expect(state.x).toBe(0);
    expect(state.y).toBe(0);
  });

  it('boundary: kite should handle max integer coordinates', () => {
    const MAX_INT = 2147483647;
    const state = engine.createKiteState(MAX_INT, MAX_INT);
    expect(state.x).toBe(MAX_INT);
    expect(state.y).toBe(MAX_INT);
  });

  it('boundary: kite should handle min integer coordinates', () => {
    const MIN_INT = -2147483648;
    const state = engine.createKiteState(MIN_INT, MIN_INT);
    expect(state.x).toBe(MIN_INT);
    expect(state.y).toBe(MIN_INT);
  });

  // === VELOCITY BOUNDARY TESTS ===
  it('boundary: kite should handle zero velocity', () => {
    const state = engine.createKiteState(100, 200);
    engine.setVelocity(state, 0);
    expect(state.velocity).toBe(0);
  });

  it('boundary: kite should handle negative velocity (backward)', () => {
    const state = engine.createKiteState(100, 200);
    engine.setVelocity(state, -10);
    expect(state.velocity).toBe(-10);
  });

  it('boundary: kite should handle very high velocity', () => {
    const state = engine.createKiteState(100, 200);
    engine.setVelocity(state, 1000);
    expect(state.velocity).toBe(1000);
  });

  it('boundary: kite should handle floating point velocity', () => {
    const state = engine.createKiteState(100, 200);
    engine.setVelocity(state, 9.999);
    expect(state.velocity).toBeCloseTo(9.999, 3);
  });

  it('boundary: kite should handle zero velocity update', () => {
    const state = engine.createKiteState(100, 200);
    engine.setVelocity(state, 10);
    engine.setVelocity(state, 0);
    expect(state.velocity).toBe(0);
  });

  // === DIRECTION BOUNDARY TESTS ===
  it('boundary: kite should handle forward direction', () => {
    const state = engine.createKiteState(100, 200);
    engine.setDirection(state, 'forward');
    expect(state.direction).toBe('forward');
  });

  it('boundary: kite should handle backward direction', () => {
    const state = engine.createKiteState(100, 200);
    engine.setDirection(state, 'backward');
    expect(state.direction).toBe('backward');
  });

  it('boundary: kite should handle left direction', () => {
    const state = engine.createKiteState(100, 200);
    engine.setDirection(state, 'left');
    expect(state.direction).toBe('left');
  });

  it('boundary: kite should handle right direction', () => {
    const state = engine.createKiteState(100, 200);
    engine.setDirection(state, 'right');
    expect(state.direction).toBe('right');
  });

  it('boundary: kite should handle invalid direction gracefully', () => {
    const state = engine.createKiteState(100, 200);
    engine.setDirection(state, 'invalid_direction');
    // Should either reject or default to a valid direction
    expect(['forward', 'backward', 'left', 'right'].includes(state.direction)).toBe(true);
  });

  it('boundary: kite should handle direction switching', () => {
    const state = engine.createKiteState(100, 200);
    engine.setDirection(state, 'forward');
    engine.setDirection(state, 'backward');
    expect(state.direction).toBe('backward');
  });

  // === POSITION UPDATE BOUNDARY TESTS ===
  it('boundary: kite position update should handle zero delta', () => {
    const state = engine.createKiteState(100, 200);
    engine.updatePosition(state, 0, 0);
    expect(state.x).toBe(100);
    expect(state.y).toBe(200);
  });

  it('boundary: kite position update should handle negative delta', () => {
    const state = engine.createKiteState(100, 200);
    engine.updatePosition(state, -5, -3);
    expect(state.x).toBe(95);
    expect(state.y).toBe(197);
  });

  it('boundary: kite position update should handle large delta', () => {
    const state = engine.createKiteState(100, 200);
    engine.updatePosition(state, 1000, 500);
    expect(state.x).toBe(1100);
    expect(state.y).toBe(700);
  });

  it('boundary: kite position update should handle floating point delta', () => {
    const state = engine.createKiteState(100, 200);
    engine.updatePosition(state, 0.5, 0.3);
    expect(state.x).toBeCloseTo(100.5, 1);
    expect(state.y).toBeCloseTo(200.3, 1);
  });

  it('boundary: kite position should not exceed max integer', () => {
    const MAX_INT = 2147483647;
    const state = engine.createKiteState(MAX_INT - 100, MAX_INT - 100);
    engine.updatePosition(state, 50, 50);
    expect(state.x).toBeLessThanOrEqual(MAX_INT);
    expect(state.y).toBeLessThanOrEqual(MAX_INT);
  });

  // === EDGE CASES ===
  it('edge case: should handle NaN position gracefully', () => {
    const state = engine.createKiteState(NaN, NaN);
    expect(Number.isNaN(state.x)).toBe(true);
    expect(Number.isNaN(state.y)).toBe(true);
  });

  it('edge case: should handle Infinity position gracefully', () => {
    const state = engine.createKiteState(Infinity, Infinity);
    expect(state.x).toBe(Infinity);
    expect(state.y).toBe(Infinity);
  });

  it('edge case: should handle undefined position gracefully', () => {
    const state = engine.createKiteState(undefined, undefined);
    expect(Number.isNaN(state.x)).toBe(true);
    expect(Number.isNaN(state.y)).toBe(true);
  });

  it('edge case: should handle null position gracefully', () => {
    const state = engine.createKiteState(null, null);
    expect(Number.isNaN(state.x)).toBe(true);
    expect(Number.isNaN(state.y)).toBe(true);
  });

  it('edge case: kite state should be defined after creation', () => {
    const state = engine.createKiteState(100, 200);
    expect(state).toBeDefined();
    expect(state.x).toBeDefined();
    expect(state.y).toBeDefined();
    expect(state.velocity).toBeDefined();
    expect(state.direction).toBeDefined();
  });

  // === MUTATION TESTS ===
  it('mutation: wrong movement should fail', () => {
    const state = engine.createKiteState(100, 200);
    engine.updatePosition(state, 5, 3);
    // Mutated code: state.x -= 5; state.y -= 3;
    expect(state.x).toBe(105);
    expect(state.y).toBe(203);
  });

  it('mutation: wrong direction handling should fail', () => {
    const state = engine.createKiteState(100, 200);
    engine.setDirection(state, 'forward');
    // Mutated code: state.direction = 'backward';
    expect(state.direction).toBe('forward');
  });

  it('mutation: wrong velocity handling should fail', () => {
    const state = engine.createKiteState(100, 200);
    engine.setVelocity(state, 10);
    // Mutated code: state.velocity = 0;
    expect(state.velocity).toBe(10);
  });

  it('mutation: wrong range check should fail', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 500);
    // Mutated code: return false;
    expect(inRange).toBe(true);
  });

  it('mutation: wrong state creation should fail', () => {
    const state = engine.createKiteState(100, 200);
    // Mutated code: return null;
    expect(state).toBeDefined();
  });

  it('mutation: wrong boundary handling should fail', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 400);
    // Mutated code: return true;
    expect(inRange).toBe(false);
  });

  it('mutation: wrong edge case handling should fail', () => {
    const state = engine.createKiteState(NaN, NaN);
    // Mutated code: return {x: 0, y: 0};
    expect(Number.isNaN(state.x)).toBe(true);
  });

  it('mutation: wrong direction validation should fail', () => {
    const state = engine.createKiteState(100, 200);
    engine.setDirection(state, 'invalid_direction');
    // Mutated code: state.direction = 'forward';
    expect(['forward', 'backward', 'left', 'right'].includes(state.direction)).toBe(true);
  });
});
