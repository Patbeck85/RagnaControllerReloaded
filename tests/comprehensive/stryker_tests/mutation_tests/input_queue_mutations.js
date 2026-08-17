const { InputCommandQueue } = require('../../../src/RagnaController/Core/InputCommandQueue');

describe('InputCommandQueue Mutation Tests', () => {
  let queue;

  beforeEach(() => {
    queue = new InputCommandQueue();
  });

  it('should initialize correctly', () => {
    expect(queue).toBeDefined();
    expect(queue.size).toBe(0);
  });

  it('should enqueue commands', () => {
    const command = { type: 'move', x: 100, y: 200 };
    queue.enqueue(command);
    expect(queue.size).toBe(1);
  });

  it('should dequeue commands', () => {
    const command = { type: 'move', x: 100, y: 200 };
    queue.enqueue(command);
    const dequeued = queue.dequeue();
    expect(dequeued.type).toBe('move');
    expect(dequeued.x).toBe(100);
    expect(dequeued.y).toBe(200);
  });

  it('should handle empty queue', () => {
    const result = queue.dequeue();
    expect(result).toBeNull();
  });

  // Mutation: Falsche Enqueue-Logik (should fail)
  it('mutation: wrong enqueue should fail', () => {
    const command = { type: 'move', x: 100, y: 200 };
    queue.enqueue(command);
    // Mutated code: queue.commands = [];
    expect(queue.size).toBe(1);
  });

  // Mutation: Falsche Dequeue-Logik (should fail)
  it('mutation: wrong dequeue should fail', () => {
    const command = { type: 'move', x: 100, y: 200 };
    queue.enqueue(command);
    const dequeued = queue.dequeue();
    // Mutated code: return null;
    expect(dequeued).toBeDefined();
    expect(dequeued.type).toBe('move');
  });

  it('should handle multiple commands', () => {
    queue.enqueue({ type: 'move', x: 100, y: 200 });
    queue.enqueue({ type: 'attack', target: 500 });
    expect(queue.size).toBe(2);
  });

  // Mutation: Falsche Multi-Command-Logik (should fail)
  it('mutation: wrong multi-command handling should fail', () => {
    queue.enqueue({ type: 'move', x: 100, y: 200 });
    queue.enqueue({ type: 'attack', target: 500 });
    // Mutated code: queue.size = 1;
    expect(queue.size).toBe(2);
  });

  it('should clear queue', () => {
    queue.enqueue({ type: 'move', x: 100, y: 200 });
    queue.clear();
    expect(queue.size).toBe(0);
  });

  // Mutation: Falsche Clear-Logik (should fail)
  it('mutation: wrong clear should fail', () => {
    queue.enqueue({ type: 'move', x: 100, y: 200 });
    queue.clear();
    // Mutated code: queue.commands = [queue.commands[0]];
    expect(queue.size).toBe(0);
  });
});
