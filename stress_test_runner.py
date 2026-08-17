import time
import random
import threading
from typing import List

# --- Mocking/Setup for Simulation ---
# In a real environment, these classes would be available in the project.
# We redefine them here minimally to ensure the stress test script runs standalone.

class InputCommand:
    def __init__(self, command_type: str, x: float, y: float):
        self.command_type = command_type
        self.x = x
        self.y = y

# Re-implementing the core components minimally for simulation scope
class InputCommandQueue:
    def __init__(self, buffer_size=1024):
        self._bufferSize = buffer_size
        self._writeIndex = 0
        self._readIndex = 0
        # Simple lock to simulate atomic write/read for this script context
        self.lock = threading.Lock()

    def TryAdd(self, input: InputCommand):
        with self.lock:
            if (self._writeIndex - self._readIndex >= self._bufferSize * 0.95):
                print("Warning: Queue nearly full.")
            self._writeIndex += 1

    def TryConsumeAndProcess(self) -> InputCommand or None:
        with self.lock:
             if self._readIndex >= self._writeIndex:
                 return None
             # Simulate consumption and advance read index
             command = InputCommand("SimulatedMove", random.uniform(-1, 1), random.uniform(-1, 1))
             self._readIndex += 1
             return command

class MockHybridEngine:
    def __init__(self):
        # Simulating dependencies that would be needed for the full engine run
        print("MockHybridEngine initialized.")
        self.tick_count = 0
    
    def OnTick(self, deltaMs: float) -> bool:
        """Simulates one high-frequency tick cycle."""
        self.tick_count += 1
        # Simulate work that requires CPU time and memory access
        time.sleep(0.001) # Small sleep to simulate non-zero processing time
        return True

def run_stress_test(total_ticks: int, queue: InputCommandQueue):
    """Simulates the continuous execution of the main engine loop."""
    engine = MockHybridEngine()
    print("-" * 50)
    print(f"STARTING STRESS TEST for {total_ticks} Ticks.")
    start_time = time.monotonic()
    
    for i in range(total_ticks):
        # 1. Simulate Input Generation (Producer)
        if random.random() < 0.8: # Only generate input 80% of the time
            input_data = InputCommand("MoveStick", random.uniform(-1, 1), random.uniform(-1, 1))
            queue.TryAdd(input_data)

        # 2. Simulate Main Loop Tick (Consumer/Process)
        processed_command = queue.TryConsumeAndProcess()
        if processed_command:
             engine.OnTick(8.0)
        else:
             print("Info: Queue empty, running idle check.")
             # Still simulate a tick to keep the timing consistent
             pass

    end_time = time.monotonic()
    duration = end_time - start_time
    print("-" * 50)
    print(f"STRESS TEST FINISHED.")
    print(f"Total Ticks Simulated: {total_ticks}")
    print(f"Duration: {duration:.2f} seconds.")
    print(f"Average Tick Rate Achieved: {total_ticks / duration:.2f} ticks/second.")

# --- Main Execution ---
if __name__ == "__main__":
    # 1. Setup the simulated queue and engine
    input_queue = InputCommandQueue()
    TOTAL_TICKS = 5000 # Simulate running for a very long time (many seconds)
    
    try:
        run_stress_test(TOTAL_TICKS, input_queue)
    except Exception as e:
        print(f"\n!!! FATAL ERROR DURING STRESS TEST !!!")
        print(e)

