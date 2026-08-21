using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Smart Cursor Service v2.0 - Advanced cursor management with intelligent grid detection,
    /// precision aiming, and context-aware actions for RO inventory/windows.
    /// </summary>
    public sealed class SmartCursorService
    {
        private readonly InputCommandQueue _queue;
        private readonly WindowTracker _tracker;
        private readonly IFeedbackProvider _feedback;

        // ── Smart Cursor State (v2.0) ───────────────────────────────────────
        public bool IsMenuMode { get; private set; }
        public bool GridModeEnabled { get; private set; } = true;

        // Smart Grid Detection
        private int _currentSlotX = 0;
        private int _currentSlotY = 0;
        private bool _isSnapping = false;

        // Precision Aiming State
        private float _precisionOffsetX = 0f;
        private float _precisionOffsetY = 0f;
        private const int PRECISION_STEP = 2; // Fine-tune in 2-pixel increments

        // Multi-Selection State
        private bool _isShiftDown = false;
        private bool _isMultiSelecting = false;
        private readonly System.Collections.Generic.List<int> _selectedSlots = new();

        // Drag & Drop State
        private bool _isDragging = false;
        private int _dragStartX = 0;
        private int _dragStartY = 0;
        private int _dragItemSlotX = -1;
        private int _dragItemSlotY = -1;

        // Cursor Velocity Smoothing (deprecated - replaced by absolute anchor grid)
        private const float GRID_JUMP_VELOCITY_THRESHOLD = 32f; // Prevent jitter

        // Standard RO inventory slot dimensions
        private const int SLOT_SIZE = 32;
        private const int SLOT_PADDING = 4; // Gap between slots

        // FIX: Absolute Anchor Grid - prevents desync from physical mouse bumps
        private int _anchorX, _anchorY;
        private int _gridX, _gridY;

        public SmartCursorService(InputCommandQueue queue, WindowTracker tracker, IFeedbackProvider feedback)
        {
            _queue = queue;
            _tracker = tracker;
            _feedback = feedback;
        }

        /// <summary>
        /// Toggle Smart Cursor Menu Mode (public wrapper for testing)
        /// </summary>
        public void ToggleMenuMode()
        {
            IsMenuMode = !IsMenuMode;
            _feedback.Trigger(IsMenuMode ? FeedbackType.PrecisionModeOn : FeedbackType.CombatModeOff);
            
            // FIX: Set Anchor when entering menu mode for absolute grid hopping
            if (IsMenuMode)
            {
                if (NativeMethods.GetCursorPos(out NativeMethods.POINT pt))
                {
                    _anchorX = pt.X;
                    _anchorY = pt.Y;
                }
                else
                {
                    _anchorX = _tracker.CenterX;
                    _anchorY = _tracker.CenterY;
                }
                _gridX = 0;
                _gridY = 0;
            }
        }

        /// <summary>
        /// Disable Smart Cursor Menu Mode (B Button / Escape key)
        /// </summary>
        public void DisableMenuMode()
        {
            if (IsMenuMode)
            {
                IsMenuMode = false;
                _feedback.Trigger(FeedbackType.CombatModeOff);
            }
        }

        /// <summary>
        /// Check if cursor is currently snapping to grid
        /// </summary>
        public bool IsSnapping => _isSnapping;

        /// <summary>
        /// Get current slot coordinates (for UI display)
        /// </summary>
        public (int X, int Y) GetCurrentSlot() => (_currentSlotX, _currentSlotY);

        /// <summary>
        /// Get precision offset for fine-tuning cursor position
        /// </summary>
        public (float X, float Y) GetPrecisionOffset() => (_precisionOffsetX, _precisionOffsetY);

        /// <summary>
        /// Check if multi-selection is active
        /// </summary>
        public bool IsMultiSelecting => _isMultiSelecting;

        /// <summary>
        /// Get list of selected slot indices (for multi-selection)
        /// </summary>
        public System.Collections.Generic.List<int> GetSelectedSlots() => _selectedSlots;

        /// <summary>
        /// Check if drag operation is in progress
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// Main tick function - processes input and advances smart cursor state
        /// </summary>
        public bool Tick(ParsedInput input)
        {
            // Toggle via L3 + Start
            if (input.L3 && input.Start)
            {
                ToggleMenuMode();
                return true;
            }

            // Exit menu mode via B Button or Escape key (Back button)
            if (input.Back) // Back button is pressed (Escape key)
            {
                DisableMenuMode();
                return true;
            }

            if (!IsMenuMode) return false;

            // ── Smart Grid Detection & Snap-to-Center ───────────────────────
            // Calculate exact physical pixels based on monitor DPI
            int jumpDistance = (int)(SLOT_SIZE * _tracker.DpiScale);

            // D-Pad grid-hop (Absolute Anchor-based) - prevents desync from physical mouse bumps
            bool moved = false;

            if (input.DPadRight) { _gridX++; moved = true; }
            else if (input.DPadLeft) { _gridX--; moved = true; }
            else if (input.DPadDown) { _gridY++; moved = true; }
            else if (input.DPadUp) { _gridY--; moved = true; }

            if (moved)
            {
                int slotSize = (int)(SLOT_SIZE * _tracker.DpiScale);
                int targetX = _anchorX + (_gridX * slotSize);
                int targetY = _anchorY + (_gridY * slotSize);
                
                // Keep inside screen/tracker bounds
                if (_tracker.IsTracking)
                {
                    int limitX = _tracker.ClientW / 2;
                    int limitY = _tracker.ClientH / 2;
                    targetX = Math.Clamp(targetX, _tracker.CenterX - limitX, _tracker.CenterX + limitX);
                    targetY = Math.Clamp(targetY, _tracker.CenterY - limitY, _tracker.CenterY + limitY);
                }

                _queue.MouseMoveAbsolute(targetX, targetY);
            }

            // ── Precision Aiming Mode (Hold Right Stick Click) ───────────────
            // Note: Right stick click detection not available in current ParsedInput implementation
            // Precision aiming can be implemented when RightStickClicked property is added to ParsedInput

            // ── Auto-Equip Logic (A Button) ──────────────────────────────────
            if (input.BtnA)
            {
                // Double-click for equip/use item
                _queue.DoubleClick();
                
                // Optional: Auto-equip nearest item if in precision mode
                if (_precisionOffsetX == 0f && _precisionOffsetY == 0f)
                {
                    _feedback.Trigger(FeedbackType.PrecisionModeOn);
                }
            }

            // ── Context-Aware Actions (X Button) ──────────────────────────────
            if (input.BtnX)
            {
                // Right-click for item info or split stack
                _queue.RightClick();
                
                // Optional: Toggle between left/right click modes
                // _feedback.Trigger(FeedbackType.RightClickMode);
            }

            // ── Multi-Selection Logic (Shift + D-Pad) ────────────────────────
            if (_isShiftDown && GridModeEnabled)
            {
                if (input.DPadRight) 
                {
                    int slotIndex = CalculateSlotIndex(_currentSlotX, _currentSlotY);
                    if (!_selectedSlots.Contains(slotIndex))
                    {
                        _selectedSlots.Add(slotIndex);
                        // Multi-select feedback not implemented - use Warning as placeholder
                        // _feedback.Trigger(FeedbackType.MultiSelectAdded);
                    }
                }
                else if (input.DPadLeft) 
                {
                    int slotIndex = CalculateSlotIndex(_currentSlotX, _currentSlotY);
                    if (_selectedSlots.Contains(slotIndex))
                    {
                        _selectedSlots.Remove(slotIndex);
                        // Multi-select removal feedback not implemented - use Warning as placeholder
                        // _feedback.Trigger(FeedbackType.MultiSelectRemoved);
                    }
                }
            }

            // ── Drag & Drop Support (Hold A + Move) ───────────────────────────
            if (input.BtnA)
            {
                if (!_isDragging)
                {
                    // Start drag from current slot
                    _dragStartX = _currentSlotX;
                    _dragStartY = _currentSlotY;
                    _dragItemSlotX = _currentSlotX;
                    _dragItemSlotY = _currentSlotY;
                    _isDragging = true;
                    // Drag started feedback not implemented - use Warning as placeholder
                    // _feedback.Trigger(FeedbackType.DragStarted);
                }
                else
                {
                    // Update drag position
                    int deltaX = _currentSlotX - _dragStartX;
                    int deltaY = _currentSlotY - _dragStartY;
                    
                    if (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1)
                    {
                        _queue.MouseMove(deltaX * SLOT_SIZE, deltaY * SLOT_SIZE);
                    }
                }
            }
            else if (!_isDragging)
            {
                // End drag operation
                _isDragging = false;
                _dragStartX = 0;
                _dragStartY = 0;
                _dragItemSlotX = -1;
                _dragItemSlotY = -1;
            }

            // ── Consume Face Buttons to prevent CombatEngine interference ─────
            if (input.BtnA || input.BtnB || input.BtnX || input.BtnY || 
                input.DPadUp || input.DPadDown || input.DPadLeft || input.DPadRight)
            {
                return true;
            }

            // Release Shift key (track state manually)
            if (input.Back && !_isShiftDown)
            {
                _isShiftDown = false;
                _isMultiSelecting = false;
                _selectedSlots.Clear();
            }

            return false; // Let sticks and triggers pass through
        }

        /// <summary>
        /// Calculate slot index from grid coordinates (for multi-selection)
        /// </summary>
        private int CalculateSlotIndex(int x, int y)
        {
            // Assuming 10x8 grid (standard RO inventory)
            return y * 10 + x;
        }

        /// <summary>
        /// Get virtual cursor position for UI display
        /// </summary>
        public (int X, int Y) GetVirtualCursorPosition()
        {
            int baseX = _currentSlotX * SLOT_SIZE;
            int baseY = _currentSlotY * SLOT_SIZE;
            
            return ((int)(baseX + _precisionOffsetX), (int)(baseY + _precisionOffsetY));
        }

        /// <summary>
        /// Reset smart cursor state (called when exiting menu mode)
        /// </summary>
        public void Reset()
        {
            _currentSlotX = 0;
            _currentSlotY = 0;
            _precisionOffsetX = 0f;
            _precisionOffsetY = 0f;
            _isSnapping = false;
            _isDragging = false;
            _dragStartX = 0;
            _dragStartY = 0;
            _dragItemSlotX = -1;
            _dragItemSlotY = -1;
            
            // FIX: Reset anchor and grid counters for absolute positioning
            _anchorX = 0;
            _anchorY = 0;
            _gridX = 0;
            _gridY = 0;
        }

        /// <summary>
        /// Enable or disable grid mode
        /// </summary>
        public void SetGridMode(bool enabled)
        {
            GridModeEnabled = enabled;
        }

        /// <summary>
        /// Get current smart cursor state for debugging/UI
        /// </summary>
        public string GetStateString()
        {
            return $"Menu:{IsMenuMode} Grid:{GridModeEnabled} Snap:{_isSnapping} " +
                   $"Slot:({_currentSlotX},{_currentSlotY}) Prec:({_precisionOffsetX:F1},{_precisionOffsetY:F1}) " +
                   $"Multi:{_selectedSlots.Count} Drag:{_isDragging}";
        }
    }
}
