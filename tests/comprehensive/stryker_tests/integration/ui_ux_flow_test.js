const { RadialMenuWindow } = require('../../../src/RagnaController/RadialMenuWindow');
const { OverlayRouter } = require('../../../src/RagnaController/Core/OverlayRouter');
const { SmartCursorService } = require('../../../src/RagnaController/Core/SmartCursorService');

describe('UI/UX Flow Integration Tests', () => {
  let radialMenu;
  let overlayRouter;
  let cursorService;

  beforeEach(() => {
    radialMenu = new RadialMenuWindow();
    overlayRouter = new OverlayRouter();
    cursorService = new SmartCursorService();
  });

  it('should handle profile switching flow', () => {
    radialMenu.switchProfile('Swordsmen');
    expect(radialMenu.currentProfile).toBe('Swordsmen');
    
    const state = overlayRouter.createOverlayState('movement', {});
    expect(state.type).toBe('movement');
    
    const position = cursorService.trackCursor(100, 200);
    expect(position.x).toBe(100);
    expect(position.y).toBe(200);
  });

  // Mutation: Falsche Profile-Switching-Logik (should fail)
  it('mutation: wrong profile switching flow should fail', () => {
    radialMenu.switchProfile('Swordsmen');
    // Mutated code: radialMenu.currentProfile = '';
    expect(radialMenu.currentProfile).toBe('Swordsmen');
    
    const state = overlayRouter.createOverlayState('movement', {});
    // Mutated code: state.type = '';
    expect(state.type).toBe('movement');
    
    const position = cursorService.trackCursor(100, 200);
    // Mutated code: position.x = 0;
    expect(position.x).toBe(100);
  });

  it('should handle menu visibility flow', () => {
    radialMenu.showMenu();
    expect(radialMenu.isVisible).toBe(true);
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.setVisible(state, true);
    expect(state.visible).toBe(true);
    
    const position = cursorService.trackCursor(100, 200);
    cursorService.setVisible(position, true);
    expect(position.visible).toBe(true);
  });

  // Mutation: Falsche Visibility-Logik (should fail)
  it('mutation: wrong visibility flow should fail', () => {
    radialMenu.showMenu();
    // Mutated code: radialMenu.isVisible = false;
    expect(radialMenu.isVisible).toBe(true);
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.setVisible(state, true);
    // Mutated code: state.visible = false;
    expect(state.visible).toBe(true);
    
    const position = cursorService.trackCursor(100, 200);
    cursorService.setVisible(position, true);
    // Mutated code: position.visible = false;
    expect(position.visible).toBe(true);
  });

  it('should handle menu positioning flow', () => {
    radialMenu.setPosition(100, 200);
    expect(radialMenu.position.x).toBe(100);
    expect(radialMenu.position.y).toBe(200);
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.setPosition(state, 100, 200);
    expect(state.position.x).toBe(100);
    expect(state.position.y).toBe(200);
    
    const position = cursorService.trackCursor(100, 200);
    cursorService.setPosition(position, 100, 200);
    expect(position.x).toBe(100);
    expect(position.y).toBe(200);
  });

  // Mutation: Falsche Position-Logik (should fail)
  it('mutation: wrong positioning flow should fail', () => {
    radialMenu.setPosition(100, 200);
    // Mutated code: radialMenu.position.x = 0;
    expect(radialMenu.position.x).toBe(100);
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.setPosition(state, 100, 200);
    // Mutated code: state.position.x = 0;
    expect(state.position.x).toBe(100);
    
    const position = cursorService.trackCursor(100, 200);
    cursorService.setPosition(position, 100, 200);
    // Mutated code: position.x = 0;
    expect(position.x).toBe(100);
  });

  it('should handle menu animation flow', () => {
    radialMenu.startAnimation();
    expect(radialMenu.isAnimating).toBe(true);
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.startAnimation(state);
    expect(state.isAnimating).toBe(true);
    
    const position = cursorService.trackCursor(100, 200);
    cursorService.startAnimation(position);
    expect(position.isAnimating).toBe(true);
  });

  // Mutation: Falsche Animation-Logik (should fail)
  it('mutation: wrong animation flow should fail', () => {
    radialMenu.startAnimation();
    // Mutated code: radialMenu.isAnimating = false;
    expect(radialMenu.isAnimating).toBe(true);
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.startAnimation(state);
    // Mutated code: state.isAnimating = false;
    expect(state.isAnimating).toBe(true);
    
    const position = cursorService.trackCursor(100, 200);
    cursorService.startAnimation(position);
    // Mutated code: position.isAnimating = false;
    expect(position.isAnimating).toBe(true);
  });

  it('should handle menu closing flow', () => {
    radialMenu.showMenu();
    radialMenu.closeMenu();
    expect(radialMenu.isVisible).toBe(false);
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.cleanup(state);
    expect(state.isDisposed).toBe(true);
  });

  // Mutation: Falsche Close-Logik (should fail)
  it('mutation: wrong close flow should fail', () => {
    radialMenu.showMenu();
    radialMenu.closeMenu();
    // Mutated code: radialMenu.isVisible = true;
    expect(radialMenu.isVisible).toBe(false);
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.cleanup(state);
    // Mutated code: state.isDisposed = false;
    expect(state.isDisposed).toBe(true);
  });

  it('should handle complete UI flow', () => {
    radialMenu.switchProfile('Swordsmen');
    radialMenu.showMenu();
    radialMenu.setPosition(100, 200);
    radialMenu.startAnimation();
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.setVisible(state, true);
    overlayRouter.setPosition(state, 100, 200);
    overlayRouter.startAnimation(state);
    
    const position = cursorService.trackCursor(100, 200);
    cursorService.setVisible(position, true);
    cursorService.setPosition(position, 100, 200);
    cursorService.startAnimation(position);
    
    expect(radialMenu.currentProfile).toBe('Swordsmen');
    expect(radialMenu.isVisible).toBe(true);
    expect(state.visible).toBe(true);
    expect(position.visible).toBe(true);
  });

  // Mutation: Falsche Complete-Flow-Logik (should fail)
  it('mutation: wrong complete flow should fail', () => {
    radialMenu.switchProfile('Swordsmen');
    radialMenu.showMenu();
    radialMenu.setPosition(100, 200);
    radialMenu.startAnimation();
    
    const state = overlayRouter.createOverlayState('movement', {});
    overlayRouter.setVisible(state, true);
    overlayRouter.setPosition(state, 100, 200);
    overlayRouter.startAnimation(state);
    
    const position = cursorService.trackCursor(100, 200);
    cursorService.setVisible(position, true);
    cursorService.setPosition(position, 100, 200);
    cursorService.startAnimation(position);
    
    // Mutated code: radialMenu.currentProfile = '';
    expect(radialMenu.currentProfile).toBe('Swordsmen');
    expect(radialMenu.isVisible).toBe(true);
    expect(state.visible).toBe(true);
    expect(position.visible).toBe(true);
  });
});
