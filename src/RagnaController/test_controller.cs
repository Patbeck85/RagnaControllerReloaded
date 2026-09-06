using Hexa.NET.SDL2;

class TestController
{
    static void Main()
    {
        Console.WriteLine("Testing SDL2 Controller Detection...");
        
        // Initialize with VIDEO flag (required for Bluetooth controllers on Windows)
        int result = SDL.Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_JOYSTICK | SDL.SDL_INIT_HAPTIC | SDL.SDL_INIT_EVENTS);
        if (result < 0)
        {
            Console.WriteLine($"SDL.Init failed: {SDL.GetErrorS()}");
            return;
        }
        
        Console.WriteLine("SDL2 initialized successfully!");
        
        // Pump events to detect controllers
        SDL.PumpEvents();
        
        int numJoysticks = SDL.NumJoysticks();
        Console.WriteLine($"Joysticks found: {numJoysticks}");
        
        for (int i = 0; i < numJoysticks; i++)
        {
            Console.WriteLine($"\n--- Joystick {i} ---");
            Console.WriteLine($"  Name: {SDL.JoystickNameForIndex(i) ?? "Unknown"}");
            Console.WriteLine($"  Is GameController: {SDL.IsGameController(i)}");
            
            if (SDL.IsGameController(i) == SDLBool.True)
            {
                var ctrl = SDL.GameControllerOpen(i);
                if (ctrl != null)
                {
                    string name = SDL.GameControllerNameS(ctrl) ?? "Unknown Gamepad";
                    Console.WriteLine($"  Controller Name: {name}");
                    
                    var joy = SDL.GameControllerGetJoystick(ctrl);
                    if (joy != null)
                    {
                        Guid guid = SDL.JoystickGetGUID(joy);
                        Console.WriteLine($"  GUID: {guid.ToString("N").ToLowerInvariant()}");
                        
                        var power = SDL.JoystickCurrentPowerLevel(joy);
                        Console.WriteLine($"  Battery: {power}");
                    }
                    
                    SDL.GameControllerClose(ctrl);
                }
            }
        }
        
        SDL.Quit();
        Console.WriteLine("\nTest complete. Press Enter to exit...");
        Console.ReadLine();
    }
}