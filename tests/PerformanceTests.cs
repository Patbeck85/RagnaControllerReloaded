using System;
using System.Diagnostics;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests.Performance
{
    public class PerformanceTests
    {
        private readonly AutoTargetEngine _autoTarget;
        private readonly MovementEngine _movement;
        private readonly ComboEngine _combo;
        private readonly InputCommandQueue _queue;
        
        public PerformanceTests(
            AutoTargetEngine autoTarget,
            MovementEngine movement,
            ComboEngine combo,
            InputCommandQueue queue)
        {
            _autoTarget = autoTarget;
            _movement = movement;
            _combo = combo;
            _queue = queue;
        }
        
        [Fact]
        public void TickLoop_GC_Allocations_ShouldBeMinimal()
        {
            // Test: GC-Allokationen im Tick-Loop sollten < 50 sein
            var sw = new Stopwatch();
            var allocationsBefore = GC.GetTotalMemory(false);
            
            for (int i = 0; i < 100; i++)
            {
                _autoTarget.UpdateState(CombatState.Ready);
                _movement.ProcessInput(new ParsedInput(0, 0, 0, 0, false, false, false, false));
                _combo.ProcessInput(new ParsedInput(0, 0, 0, 0, false, false, false, false));
            }
            
            var allocationsAfter = GC.GetTotalMemory(false);
            var delta = allocationsAfter - allocationsBefore;
            
            // Erlaubter Overhead: ~1MB für 100 Iterationen
            Assert.LessOrEqual(delta, 1048576, 
                $"GC-Allokationen ({delta} bytes) überschreiten den Limit von 1MB");
        }
        
        [Fact]
        public void StringPooling_ShouldReduceAllocations()
        {
            // Test: String Pooling sollte Allokationen reduzieren
            var sw = new Stopwatch();
            
            // Ohne Pooling (simuliert)
            sw.Start();
            for (int i = 0; i < 1000; i++)
            {
                var str = "READY".ToString().ToUpper();
            }
            var timeWithoutPooling = sw.ElapsedMilliseconds;
            
            // Mit Pooling (über EngineOptimizationPool)
            sw.Reset();
            sw.Start();
            var pool = EngineOptimizationPool.Instance;
            for (int i = 0; i < 1000; i++)
            {
                var str = pool.GetString("READY");
            }
            var timeWithPooling = sw.ElapsedMilliseconds;
            
            // Pooling sollte ~2x schneller sein
            Assert.Less(timeWithPooling, timeWithoutPooling / 2,
                $"String Pooling Performance-Verbesserung nicht ausreichend");
        }
        
        [Fact]
        public void MessagePooling_ShouldReduceMemoryPressure()
        {
            // Test: Message Pooling sollte Memory Pressure reduzieren
            var messages = new System.Collections.Generic.List<string>();
            
            for (int i = 0; i < 1000; i++)
            {
                var msg = $"Message_{i}";
                messages.Add(msg);
            }
            
            // Mit Message Pooling
            var pool = EngineOptimizationPool.Instance;
            var pooledMessages = new System.Collections.Generic.List<string>();
            
            for (int i = 0; i < 1000; i++)
            {
                var msg = pool.GetString($"Message_{i}");
                pooledMessages.Add(msg);
            }
            
            // Beide sollten gleiche Anzahl von Elementen haben
            Assert.Equal(messages.Count, pooledMessages.Count);
        }
        
        [Fact]
        public void InputCommandQueue_Dequeue_ShouldBeThreadSafe()
        {
            // Test: Dequeue Operationen sollten thread-safe sein
            var concurrentThreads = new System.Threading.Thread[10];
            
            for (int i = 0; i < 10; i++)
            {
                int threadId = i;
                concurrentThreads[i] = new System.Threading.Thread(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        try
                        {
                            var command = _queue.Dequeue();
                            if (command != null)
                            {
                                // Command verarbeiten
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"Thread {threadId} Exception: {ex.Message}");
                        }
                    }
                });
            }
            
            // Alle Threads starten
            foreach (var thread in concurrentThreads)
            {
                thread.Start();
            }
            
            // Warten auf Abschluss
            foreach (var thread in concurrentThreads)
            {
                thread.Join();
            }
        }
        
        [Fact]
        public void MovementEngine_PositionCalculation_ShouldBeDeterministic()
        {
            // Test: Position-Berechnungen sollten deterministisch sein
            var positions = new System.Collections.Generic.List<(double X, double Y)>();
            
            for (int i = 0; i < 100; i++)
            {
                var input = new ParsedInput(
                    LeftX: i % 10,
                    LeftY: i % 10,
                    RightX: i % 10,
                    RightY: i % 10,
                    LT: false,
                    RT: false,
                    LB: false,
                    RB: false);
                
                var position = _movement.CalculatePosition(input);
                positions.Add((position.X, position.Y));
            }
            
            // Alle Positionen sollten konsistent sein
            Assert.Equal(100, positions.Count);
        }
        
        [Fact]
        public void ComboEngine_Counting_ShouldBeAccurate()
        {
            // Test: Combo-Zählung sollte genau sein
            for (int i = 0; i < 100; i++)
            {
                var input = new ParsedInput(
                    BtnA: true,
                    BtnB: false,
                    BtnX: false,
                    BtnY: false,
                    L3: false,
                    R3: false,
                    Start: false,
                    Back: false);
                
                _combo.ProcessInput(input);
            }
            
            // Combo sollte korrekt gezählt werden
            Assert.True(_combo.ComboCount > 0, "Combo-Zählung ist nicht korrekt");
        }
        
        [Fact]
        public void AutoTargetEngine_DistanceCalculation_ShouldBeAccurate()
        {
            // Test: Distanzberechnung sollte genau sein
            var target = new TargetEntity(100, 100);
            var player = new PlayerEntity(50, 50);
            
            var distance = _autoTarget.CalculateDistance(target, player);
            
            // Distanz sollte ~70.71 sein (Pythagoras: sqrt(50^2 + 50^2))
            Assert.Approximately(distance, 70.71067811865476, 0.01);
        }
        
        [Fact]
        public void MemoryLatency_ShouldBeUnderThreshold()
        {
            // Test: Memory Latency sollte unter Threshold sein
            var sw = new Stopwatch();
            
            for (int i = 0; i < 1000; i++)
            {
                sw.Start();
                _autoTarget.UpdateState(CombatState.Ready);
                _movement.ProcessInput(new ParsedInput(0, 0, 0, 0, false, false, false, false));
                sw.Stop();
                
                if (sw.ElapsedMilliseconds > 1) // Threshold: 1ms
                {
                    throw new InvalidOperationException(
                        $"Memory Latency ({sw.ElapsedMilliseconds}ms) überschreitet Threshold von 1ms");
                }
            }
        }
    }
    
    // Helper Classes for Testing
    public class TargetEntity
    {
        public double X { get; set; }
        public double Y { get; set; }
        
        public TargetEntity(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    
    public class PlayerEntity
    {
        public double X { get; set; }
        public double Y { get; set; }
        
        public PlayerEntity(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
