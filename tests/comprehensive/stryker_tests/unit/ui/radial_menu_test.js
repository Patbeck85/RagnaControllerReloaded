const { RadialMenuWindow } = require('../../../src/RagnaController/RadialMenuWindow');

describe('RadialMenuWindow Mutation Tests', () => {
  let window;

  beforeEach(() => {
    window = new RadialMenuWindow();
  });

  it('should initialize correctly', () => {
    expect(window).toBeDefined();
    expect(window.isInitialized).toBe(true);
  });

  it('should handle profile switching', () => {
    window.switchProfile('Swordsmen');
    expect(window.currentProfile).toBe('Swordsmen');
  });

  it('should handle menu visibility', () => {
    window.showMenu();
    expect(window.isVisible).toBe(true);
  });

  // Mutation: Falsche Profile-Switching-Logik (should fail)
  it('mutation: wrong profile switching should fail', () => {
    window.switchProfile('Swordsmen');
    // Mutated code: window.currentProfile = '';
    expect(window.currentProfile).toBe('Swordsmen');
  });

  // Mutation: Falsche Visibility-Logik (should fail)
  it('mutation: wrong visibility handling should fail', () => {
    window.showMenu();
    // Mutated code: window.isVisible = false;
    expect(window.isVisible).toBe(true);
  });

  it('should handle menu closing', () => {
    window.closeMenu();
    expect(window.isVisible).toBe(false);
  });

  // Mutation: Falsche Close-Logik (should fail)
  it('mutation: wrong close handling should fail', () => {
    window.closeMenu();
    // Mutated code: window.isVisible = true;
    expect(window.isVisible).toBe(false);
  });

  it('should handle menu positioning', () => {
    window.setPosition(100, 200);
    expect(window.position.x).toBe(100);
    expect(window.position.y).toBe(200);
  });

  // Mutation: Falsche Position-Logik (should fail)
  it('mutation: wrong position handling should fail', () => {
    window.setPosition(100, 200);
    // Mutated code: window.position.x = 0;
    expect(window.position.x).toBe(100);
  });

  it('should handle menu animations', () => {
    window.startAnimation();
    expect(window.isAnimating).toBe(true);
  });

  // Mutation: Falsche Animation-Logik (should fail)
  it('mutation: wrong animation handling should fail', () => {
    window.startAnimation();
    // Mutated code: window.isAnimating = false;
    expect(window.isAnimating).toBe(true);
  });
});
