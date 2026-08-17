const { HybridEngine } = require('../../../src/RagnaController/Core/HybridEngine');

describe('HybridEngine Mutation Tests', () => {
  let engine;

  beforeEach(() => {
    engine = new HybridEngine();
  });

  it('should initialize correctly', () => {
    expect(engine).toBeDefined();
    expect(engine.isInitialized).toBe(true);
  });

  it('should handle movement updates', () => {
    const state = engine.createMovementState(100, 200);
    engine.updatePosition(state, 5, 3);
    expect(state.x).toBe(105);
    expect(state.y).toBe(203);
  });

  it('should handle target updates', () => {
    const state = engine.createMovementState(100, 200);
    engine.updateTarget(state, 300, 400);
    expect(state.targetX).toBe(300);
    expect(state.targetY).toBe(400);
  });

  // Mutation: Falsche Initialisierung (should fail)
  it('mutation: wrong initialization should fail', () => {
    const state = engine.createMovementState(100, 200);
    // Mutated code: state.x = 0; state.y = 0;
    expect(state.x).toBe(100);
    expect(state.y).toBe(200);
  });

  // Mutation: Falsche Update-Logik (should fail)
  it('mutation: wrong movement update should fail', () => {
    const state = engine.createMovementState(100, 200);
    engine.updatePosition(state, 5, 3);
    // Mutated code: state.x -= 5; state.y -= 3;
    expect(state.x).toBe(105);
    expect(state.y).toBe(203);
  });

  it('should handle distance calculations', () => {
    const result = engine.calculateDistance(100, 200, 300, 400);
    expect(result).toBeCloseTo(412.31, 2);
  });

  // Mutation: Falsche Distanz-Logik (should fail)
  it('mutation: wrong distance calculation should fail', () => {
    const result = engine.calculateDistance(100, 200, 300, 400);
    // Mutated code: return Math.sqrt((300-100) + (400-200));
    expect(result).toBeCloseTo(412.31, 2);
  });

  it('should handle range checks', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 500);
    expect(inRange).toBe(true);
  });

  // Mutation: Falsche Range-Logik (should fail)
  it('mutation: wrong range check should fail', () => {
    const inRange = engine.isWithinRange(100, 200, 300, 400, 500);
    // Mutated code: return false;
    expect(inRange).toBe(true);
  });
});
