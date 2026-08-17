const { EventBus } = require('../../../src/RagnaController/Core/EventBus');

describe('EventBus Mutation Tests', () => {
  let bus;

  beforeEach(() => {
    bus = new EventBus();
  });

  it('should initialize correctly', () => {
    expect(bus).toBeDefined();
    expect(bus.isInitialized).toBe(true);
  });

  it('should handle event subscription', () => {
    const handler = jest.fn();
    bus.subscribe('test', handler);
    expect(bus.subscribers['test']).toBeDefined();
  });

  it('should handle event publishing', () => {
    const handler = jest.fn();
    bus.subscribe('test', handler);
    bus.publish('test', { data: 'test' });
    expect(handler).toHaveBeenCalled();
  });

  // Mutation: Falsche Subscription-Logik (should fail)
  it('mutation: wrong subscription should fail', () => {
    const handler = jest.fn();
    bus.subscribe('test', handler);
    // Mutated code: bus.subscribers['test'] = [];
    expect(bus.subscribers['test']).toBeDefined();
  });

  // Mutation: Falsche Publishing-Logik (should fail)
  it('mutation: wrong publishing should fail', () => {
    const handler = jest.fn();
    bus.subscribe('test', handler);
    bus.publish('test', { data: 'test' });
    // Mutated code: return;
    expect(handler).toHaveBeenCalled();
  });

  it('should handle multiple subscribers', () => {
    const handler1 = jest.fn();
    const handler2 = jest.fn();
    bus.subscribe('test', handler1);
    bus.subscribe('test', handler2);
    bus.publish('test', { data: 'test' });
    expect(handler1).toHaveBeenCalled();
    expect(handler2).toHaveBeenCalled();
  });

  // Mutation: Falsche Multi-Subscriber-Logik (should fail)
  it('mutation: wrong multi-subscriber handling should fail', () => {
    const handler1 = jest.fn();
    const handler2 = jest.fn();
    bus.subscribe('test', handler1);
    bus.subscribe('test', handler2);
    bus.publish('test', { data: 'test' });
    // Mutated code: handler1.mockClear();
    expect(handler1).toHaveBeenCalled();
    expect(handler2).toHaveBeenCalled();
  });

  it('should handle event unsubscription', () => {
    const handler = jest.fn();
    bus.subscribe('test', handler);
    bus.unsubscribe('test', handler);
    bus.publish('test', { data: 'test' });
    expect(handler).not.toHaveBeenCalled();
  });

  // Mutation: Falsche Unsubscription-Logik (should fail)
  it('mutation: wrong unsubscription should fail', () => {
    const handler = jest.fn();
    bus.subscribe('test', handler);
    bus.unsubscribe('test', handler);
    bus.publish('test', { data: 'test' });
    // Mutated code: handler.mockClear();
    expect(handler).not.toHaveBeenCalled();
  });

  it('should handle event cleanup', () => {
    const handler = jest.fn();
    bus.subscribe('test', handler);
    bus.cleanup();
    expect(bus.subscribers['test']).toBeUndefined();
  });

  // Mutation: Falsche Cleanup-Logik (should fail)
  it('mutation: wrong cleanup should fail', () => {
    const handler = jest.fn();
    bus.subscribe('test', handler);
    bus.cleanup();
    // Mutated code: bus.subscribers['test'] = [];
    expect(bus.subscribers['test']).toBeUndefined();
  });
});
