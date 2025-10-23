using System;
using System.Collections.Generic;
using System.Linq;
using SortingInMaze;

namespace SortingInMaze;

public class Program
{
    private static long Solve(List<string> lines)
    {
        var maze = new Maze(lines.ToArray());
        return maze.EnergyToOrganise();
    }

    public static void Main()
    {
        var lines = new List<string>();
        string line;

        while ((line = Console.ReadLine()) != null)
            lines.Add(line);

        var result = Solve(lines);
        Console.WriteLine(result);
    }
}

