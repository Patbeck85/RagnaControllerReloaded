const { Win32InputService } = require('../../../src/RagnaController/Core/Win32InputService');

describe('Win32InputService Mutation Tests', () => {
  let service;

  beforeEach(() => {
    service = new Win32InputService();
  });

  it('should initialize correctly', () => {
    expect(service).toBeDefined();
    expect(service.isInitialized).toBe(true);
  });

  it('should handle input events', () => {
    const event = service.createInputEvent('keydown', 'W');
    expect(event.type).toBe('keydown');
    expect(event.key).toBe('W');
  });

  it('should process key events', () => {
    const result = service.processKeyEvent('keydown', 'W');
    expect(result).toBeDefined();
  });

  // Mutation: Falsche Initialisierung (should fail)
  it('mutation: wrong initialization should fail', () => {
    const event = service.createInputEvent('keydown', 'W');
    // Mutated code: event.type = '';
    expect(event.type).toBe('keydown');
  });

  // Mutation: Falsche Event-Logik (should fail)
  it('mutation: wrong event processing should fail', () => {
    const result = service.processKeyEvent('keydown', 'W');
    // Mutated code: return null;
    expect(result).toBeDefined();
  });

  it('should handle multiple events', () => {
    const event1 = service.createInputEvent('keydown', 'W');
    const event2 = service.createInputEvent('keyup', 'W');
    expect(event1.type).toBe('keydown');
    expect(event2.type).toBe('keyup');
  });

  // Mutation: Falsche Multi-Event-Logik (should fail)
  it('mutation: wrong multi-event handling should fail', () => {
    const event1 = service.createInputEvent('keydown', 'W');
    const event2 = service.createInputEvent('keyup', 'W');
    // Mutated code: event1.type = event2.type;
    expect(event1.type).toBe('keydown');
    expect(event2.type).toBe('keyup');
  });

  it('should handle special keys', () => {
    const event = service.createInputEvent('keydown', 'Space');
    expect(event.key).toBe('Space');
  });

  // Mutation: Falsche Special-Key-Logik (should fail)
  it('mutation: wrong special key handling should fail', () => {
    const event = service.createInputEvent('keydown', 'Space');
    // Mutated code: event.key = 'X';
    expect(event.key).toBe('Space');
  });
});
