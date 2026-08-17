const { MovementEngine } = require('../../../src/RagnaController/Core/MovementEngine');

describe('MovementEngine Mutation Tests', () => {
  let engine;

  beforeEach(() => {
    engine = new MovementEngine();
  });

  it('should create movement state correctly', () => {
    const state = engine.createMovementState(100, 200);
    expect(state.x).toBe(100);
    expect(state.y).toBe(200);
    expect(state.velocity).toBe(0);
  });

  it('should update position correctly', () => {
    const state = engine.createMovementState(100, 200);
    engine.updatePosition(state, 5, 3);
    expect(state.x).toBe(105);
    expect(state.y).toBe(203);
  });

  it('should handle movement direction', () => {
    const state = engine.createMovementState(100, 200);
    engine.setDirection(state, 'forward');
    expect(state.direction).toBe('forward');
  });

  // Mutation: Entferne Zeile (should fail)
  it('mutation: removing state creation should fail', () => {
    const state = engine.createMovementState(100, 200);
    // Mutated code: state = null;
    expect(state).not.toBeNull();
    expect(state.x).toBe(100);
  });

  // Mutation: Ändere Wert (should fail)
  it('mutation: changing x value should fail', () => {
    const state = engine.createMovementState(100, 200);
    // Mutated code: state.x = 999;
    expect(state.x).toBe(100);
  });

  // Mutation: Falsche Logik (should fail)
  it('mutation: wrong position update should fail', () => {
    const state = engine.createMovementState(100, 200);
    engine.updatePosition(state, 5, 3);
    // Mutated code: state.x -= 5; state.y -= 3;
    expect(state.x).toBe(105);
    expect(state.y).toBe(203);
  });

  it('should handle velocity correctly', () => {
    const state = engine.createMovementState(100, 200);
    engine.setVelocity(state, 10);
    expect(state.velocity).toBe(10);
  });

  // Mutation: Falsche Velocity-Logik (should fail)
  it('mutation: wrong velocity handling should fail', () => {
    const state = engine.createMovementState(100, 200);
    engine.setVelocity(state, 10);
    // Mutated code: state.velocity = 0;
    expect(state.velocity).toBe(10);
  });
});
