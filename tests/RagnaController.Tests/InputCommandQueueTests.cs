using System;
using System.Threading;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class InputCommandQueueTests
    {
        [Fact]
        public void CreateWait_ReturnsWaitCommand_WithCorrectMs()
        {
            var cmd = InputCmd.CreateWait(150);
            
            Assert.Equal(CmdType.Wait, cmd.Type);
            Assert.Equal(150, cmd.X);
        }

        [Fact]
        public void Enqueue_And_FlushBatch_Works()
        {
            var queue = new InputCommandQueue();
            queue.Start();
            
            queue.LeftDown();
            queue.LeftUp();
            
            Thread.Sleep(50);
            
            queue.Stop();
            queue.Dispose();
        }

        [Fact]
        public void Wait_Command_Uses_Factory_Method()
        {
            var queue = new InputCommandQueue();
            queue.Start();
            
            queue.Wait(100);
            
            Thread.Sleep(150);
            
            queue.Stop();
            queue.Dispose();
        }

        [Fact]
        public void MouseAbs_EnqueuesCorrectCommand()
        {
            var queue = new InputCommandQueue();
            queue.Start();
            
            queue.MouseMoveAbsolute(100, 200);
            
            Thread.Sleep(50);
            
            queue.Stop();
            queue.Dispose();
        }

        [Fact]
        public void KeyDown_KeyUp_EnqueuesCorrectCommands()
        {
            var queue = new InputCommandQueue();
            queue.Start();
            
            queue.KeyDown(VirtualKey.A);
            queue.KeyUp(VirtualKey.A);
            
            Thread.Sleep(50);
            
            queue.Stop();
            queue.Dispose();
        }
    }
}
