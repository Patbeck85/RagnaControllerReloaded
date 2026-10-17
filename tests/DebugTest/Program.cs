using System;
using RagnaController.Core;
using RagnaController.Models;

namespace DebugTest
{
    class Program
    {
        static void Main()
        {
            var queue = new InputCommandQueue();
            queue.OnCommandEnqueued += cmd => Console.WriteLine($"Enqueued: {cmd.Type} Key={cmd.Key} X={cmd.X} Y={cmd.Y}");
            queue.Start();

            var e = new AutoTargetEngine(queue);
            e.AutoAttackEnabled = true;
            e.AutoRetargetEnabled = true;

            Console.WriteLine($"Initial State: {e.State}, IsTargetLocked: {e.IsTargetLocked}");

            e.ToggleCombatMode();
            Console.WriteLine($"After ToggleCombatMode: State={e.State}, IsTargetLocked={e.IsTargetLocked}");

            var input = new ParsedInput { R3 = true, R3Prev = false };
            e.Handle(input, 16);
            Console.WriteLine($"After Handle(R3): State={e.State}, IsTargetLocked={e.IsTargetLocked}");

            queue.Stop();
            queue.Dispose();
        }
    }
}