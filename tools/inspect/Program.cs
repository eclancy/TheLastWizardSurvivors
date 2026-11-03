using System;
using System.Linq;
using System.Reflection;

class Program
{
    static void Main()
    {
    // Run from the repo root: dotnet build puts the game's assembly here.
    var asmPath = System.IO.Path.Combine(Environment.CurrentDirectory, ".godot", "mono", "temp", "bin", "Debug", "TheOneHeMissed.dll");
        Console.WriteLine($"Looking for assembly: {asmPath}");
        if (!System.IO.File.Exists(asmPath)) { Console.WriteLine("Assembly not found"); return; }
        var asm = Assembly.LoadFile(asmPath);
        var types = asm.GetTypes();
        foreach (var t in types.Where(t => t.Name.Contains("TitleScreen")))
        {
            Console.WriteLine($"Found type: {t.FullName}");
        }
    }
}
