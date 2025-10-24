using System;
using System.Linq;
using System.Collections.Generic;

public class Program
{
    private static long Solve(List<string> lines)
    {
        var burrow = new Maze(lines.ToArray());
        var result = burrow.EnergyToOrganise();
        return result;
    }

    public static void Main()
    {
        var lines = new List<string>();

        while (Console.ReadLine() is { } line)
            lines.Add(line);

        var result = Solve(lines);
        Console.WriteLine(result);
    }

    private static long EnergyConsumption(DwellerType type) => type switch
    {
        DwellerType.A => 1,
        DwellerType.B => 10,
        DwellerType.C => 100,
        DwellerType.D => 1000,
        _ => throw new ArgumentOutOfRangeException()
    };

    private static Location Room(DwellerType type) => type switch
    {
        DwellerType.A => Location.RoomA,
        DwellerType.B => Location.RoomB,
        DwellerType.C => Location.RoomC,
        DwellerType.D => Location.RoomD,
        _ => throw new ArgumentOutOfRangeException()
    };

    private static int SequenceCompareTo<T>(IEnumerable<T> a, IEnumerable<T> b)
        where T : IComparable<T>
    {
        using var e1 = a.GetEnumerator();
        using var e2 = b.GetEnumerator();
        while (true)
        {
            var m1 = e1.MoveNext();
            var m2 = e2.MoveNext();
            if (!m1 && !m2) return 0;
            if (!m1) return -1;
            if (!m2) return 1;
            var cmp = e1.Current.CompareTo(e2.Current);
            if (cmp != 0) return cmp;
        }
    }
    
    public record struct Dweller(DwellerType Type, int X, int Y) : IComparable<Dweller>
    {
        public IEnumerable<Move> PossibleDestinations(
            IReadOnlyList<Dweller> dwellers,
            Location[,] map)
        {
            if (!IsNecessaryToLeaveTheRoom(dwellers, map))
               yield break;
            
            foreach (var dweller in dwellers)
                if (dweller.X == X && dweller.Y < Y)
                    yield break;

            long stepsToThreshold = Y - 1;
            foreach (var tuple in PossibleDestinationsFromCorridor(-1, stepsToThreshold, dwellers, map))
                yield return tuple;
            
            foreach (var tuple in PossibleDestinationsFromCorridor(1, stepsToThreshold, dwellers, map))
                yield return tuple;
        }

        private IEnumerable<Move> PossibleDestinationsFromCorridor(
            int direction,
            long steps,
            IReadOnlyList<Dweller> dwellers,
            Location[,] map)
        {
            var x = X + direction;
            while (map[1, x] != Location.Wall && !dwellers.Any(dweller => dweller.X == x && dweller.Y == 1))
            {
                steps++;
                
                if (map[1, x] != Location.Threshold)
                {
                    if (map[Y, X] != Location.Corridor)
                        yield return new Move(x, 1, steps);
                }
                else
                {
                    if (map[2, x] == Room(Type))
                    {
                        var y = 2;
                        var destY = int.MaxValue;
                        var canEnter = true;
                        var stopped = false;
                        
                        while (map[y, x] != Location.Wall)
                        {
                            var occupant = dwellers.FirstOrDefault(dweller => dweller.X == x && dweller.Y == y);
                            if (occupant != default)
                            {
                                if (occupant.Type != Type)
                                {
                                    canEnter = false;
                                    break;
                                }
                                stopped = true;
                            }
                            else if (!stopped)
                            {
                                destY = y;
                            }
                            y++;
                        }
                        
                        if (canEnter) yield return new Move(x, destY, steps + destY - 1);
                    }
                }

                x += direction;
            }
        }
        
        private bool IsNecessaryToLeaveTheRoom(IEnumerable<Dweller> dwellers, Location[,] map)
        {
            if (map[Y, X] != Room(Type)) return true;
            
            foreach (var dweller in dwellers)
                if (dweller.X == X && dweller.Y > Y && dweller.Type != Type)
                    return true;

            return false;
        }
        
        public int CompareTo(Dweller other)
        {
            var cmp = Type.CompareTo(other.Type);
            if (cmp != 0) return cmp;
            cmp = X.CompareTo(other.X);
            if (cmp != 0) return cmp;
            return Y.CompareTo(other.Y);
        }
    }

    public enum DwellerType
    {
        A,
        B,
        C,
        D
    }

    public enum Location
    {
        Wall,
        Corridor,
        Threshold,
        RoomA,
        RoomB,
        RoomC,
        RoomD
    }

    public class Maze
    {
        public Dweller[] Dwellers { get; }
        public Location[,] Map { get; }

        public Maze(string[] lines)
        {
            var dwellerList = new List<Dweller>();
            var height = lines.Length;
            var width = lines.Max(line => line.Length);
            Map = new Location[height, width];

            for (var y = 0; y < height; y++)
            {
                var line = lines[y].PadRight(width, ' ');
                for (var x = 0; x < width; x++)
                {
                    Map[y, x] = line[x] switch
                    {
                        '#' or ' ' => Location.Wall,
                        '.' => x is 3 or 5 or 7 or 9 ? Location.Threshold : Location.Corridor,
                        'A' => Register(x, y, DwellerType.A),
                        'B' => Register(x, y, DwellerType.B),
                        'C' => Register(x, y, DwellerType.C),
                        'D' => Register(x, y, DwellerType.D),
                        _ => Location.Wall
                    };
                }
            }

            Location Register(int x, int y, DwellerType dwellerType)
            {
                dwellerList.Add(new Dweller(dwellerType, x, y));
                return x switch
                {
                    3 => Location.RoomA,
                    5 => Location.RoomB,
                    7 => Location.RoomC,
                    9 => Location.RoomD,
                    _ => Location.Wall
                };
            }

            Dwellers = dwellerList.ToArray();
        }

        public long EnergyToOrganise()
        {
            var comparer = new DwellerArrayComparer();
            var energySpent = new Dictionary<Dweller[], long>(comparer)
            {
                [Dwellers] = 0
            };
            var queue = new PriorityQueue<State, long>();
            queue.Enqueue(State.Create(Dwellers, Map, 0), 0);

            while (queue.TryDequeue(out var state, out _))
            {
                if (state.IsOrganised(Map)) return state.EnergySpent;
                
                if (state.EnergySpent > energySpent.GetValueOrDefault(state.Dwellers, long.MaxValue)) continue;

                for (var i = 0; i < state.Dwellers.Length; i++)
                {
                    var currentDweller = state.Dwellers[i];
                    foreach (var move in currentDweller.PossibleDestinations(state.Dwellers, Map))
                    {
                        var dwellers = state.Dwellers.ToArray();
                        dwellers[i] = currentDweller with { X = move.X, Y = move.Y };
                        var energy = state.EnergySpent + move.Steps * EnergyConsumption(currentDweller.Type);

                        if (energy >= energySpent.GetValueOrDefault(dwellers, long.MaxValue)) continue;
                        energySpent[dwellers] = energy;
                        var newState = State.Create(dwellers, Map, energy);
                        queue.Enqueue(newState, newState.EnergySpent + newState.EstimatedRemaining);
                    }
                }
            }

            throw new InvalidOperationException("Не удалось выполнить подсчет энергии");
        }

        private class DwellerArrayComparer : IEqualityComparer<Dweller[]>
        {
            public bool Equals(Dweller[]? a, Dweller[]? b)
                => a != null && b != null && a.Length == b.Length && !a.Where((dweller, i) => !dweller.Equals(b[i])).Any();

            public int GetHashCode(Dweller[] obj)
            {
                unchecked
                {
                    var hash = 17;
                    foreach (var dweller in obj)
                        hash = hash * 31 + dweller.GetHashCode();
                    return hash;
                }
            }
        }
    }

    public record struct Move(int X, int Y, long Steps);

    public record struct State(Dweller[] Dwellers, long EnergySpent, long EstimatedRemaining)
        : IComparable<State>
    {
        public bool IsOrganised(Location[,] map) => Dwellers.All(dweller => map[dweller.Y, dweller.X] == Room(dweller.Type));

        public static State Create(Dweller[] dwellers, Location[,] map, long energySpent)
        {
            var totalEnergy = 0L;
            var alreadyInPlace = new Dictionary<DwellerType, int>();

            foreach (var dweller in dwellers)
            {
                var location = map[dweller.Y, dweller.X];
                if (location == Room(dweller.Type))
                {
                    alreadyInPlace[dweller.Type] = alreadyInPlace.GetValueOrDefault(dweller.Type, 0) + 1;
                    continue;
                }

                var targetX = dweller.Type switch
                {
                    DwellerType.A => 3,
                    DwellerType.B => 5,
                    DwellerType.C => 7,
                    DwellerType.D => 9,
                    _ => 0
                };

                long distance = Math.Abs(dweller.X - targetX) + Math.Abs(dweller.Y - 2);
                totalEnergy += distance * EnergyConsumption(dweller.Type);
            }

            foreach (var type in Enum.GetValues<DwellerType>())
            {
                var toFill = dwellers.Length / 4 - alreadyInPlace.GetValueOrDefault(type, 0);
                totalEnergy += toFill * (toFill + 1) / 2 * EnergyConsumption(type);
            }

            return new State(dwellers, energySpent, totalEnergy);
        }
        
        public int CompareTo(State other)
        {
            var cmp = (EnergySpent + EstimatedRemaining)
                .CompareTo(other.EnergySpent + other.EstimatedRemaining);
            if (cmp != 0) return cmp;
            return SequenceCompareTo(Dwellers, other.Dwellers);
        }
    }
}
