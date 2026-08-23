using System;
using System.Reflection;

class Test {
    static void Main() {
        var asm = Assembly.LoadFrom(@"C:\Users\patbe\.nuget\packages\hexa.net.sdl2\1.2.17\lib\net8.0\Hexa.NET.SDL2.dll");
        var sdlType = asm.GetType("Hexa.NET.SDL2.SDL");
        var method = sdlType.GetMethod("JoystickGetGUID");
        if (method != null) {
            Console.WriteLine($"Return type: {method.ReturnType}");
            Console.WriteLine($"Return type name: {method.ReturnType.FullName}");
            Console.WriteLine($"IsValueType: {method.ReturnType.IsValueType}");
        }
        
        // Also check all types with GUID in name
        foreach (var t in asm.GetTypes()) {
            if (t.Name.Contains("GUID") || t.Name.Contains("Guid")) {
                Console.WriteLine($"Type: {t.FullName}");
            }
        }
    }
}
