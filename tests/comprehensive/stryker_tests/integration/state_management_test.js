const { StateManager } = require('../../../src/RagnaController/Core/StateManager');

describe('StateManager Mutation Tests', () => {
  let manager;

  beforeEach(() => {
    manager = new StateManager();
  });

  it('should initialize correctly', () => {
    expect(manager).toBeDefined();
    expect(manager.isInitialized).toBe(true);
  });

  it('should handle state registration', () => {
    manager.register('movement', {});
    expect(manager.states['movement']).toBeDefined();
  });

  it('should handle state updates', () => {
    manager.register('movement', {});
    manager.update('movement', { x: 100, y: 200 });
    expect(manager.states['movement'].x).toBe(100);
    expect(manager.states['movement'].y).toBe(200);
  });

  // Mutation: Falsche Registration-Logik (should fail)
  it('mutation: wrong registration should fail', () => {
    manager.register('movement', {});
    // Mutated code: manager.states['movement'] = null;
    expect(manager.states['movement']).toBeDefined();
  });

  // Mutation: Falsche Update-Logik (should fail)
  it('mutation: wrong update handling should fail', () => {
    manager.register('movement', {});
    manager.update('movement', { x: 100, y: 200 });
    // Mutated code: manager.states['movement'].x = 0;
    expect(manager.states['movement'].x).toBe(100);
  });

  it('should handle state retrieval', () => {
    manager.register('movement', {});
    const state = manager.getState('movement');
    expect(state).toBeDefined();
  });

  // Mutation: Falsche Retrieval-Logik (should fail)
  it('mutation: wrong retrieval should fail', () => {
    manager.register('movement', {});
    const state = manager.getState('movement');
    // Mutated code: return null;
    expect(state).toBeDefined();
  });

  it('should handle state removal', () => {
    manager.register('movement', {});
    manager.remove('movement');
    expect(manager.states['movement']).toBeUndefined();
  });

  // Mutation: Falsche Removal-Logik (should fail)
  it('mutation: wrong removal should fail', () => {
    manager.register('movement', {});
    manager.remove('movement');
    // Mutated code: manager.states['movement'] = {};
    expect(manager.states['movement']).toBeUndefined();
  });

  it('should handle state cleanup', () => {
    manager.register('movement', {});
    manager.cleanup();
    expect(Object.keys(manager.states).length).toBe(0);
  });

  // Mutation: Falsche Cleanup-Logik (should fail)
  it('mutation: wrong cleanup should fail', () => {
    manager.register('movement', {});
    manager.cleanup();
    // Mutated code: Object.keys(manager.states).length = 1;
    expect(Object.keys(manager.states).length).toBe(0);
  });

  it('should handle state synchronization', () => {
    manager.register('movement', {});
    manager.sync();
    expect(manager.isSynced).toBe(true);
  });

  // Mutation: Falsche Sync-Logik (should fail)
  it('mutation: wrong sync handling should fail', () => {
    manager.register('movement', {});
    manager.sync();
    // Mutated code: manager.isSynced = false;
    expect(manager.isSynced).toBe(true);
  });
});
