namespace SortingInMaze;

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

    public int EnergyToOrganise()
    {
        var comparer = new DwellerArrayComparer();
        var energySpent = new Dictionary<Dweller[], int>(comparer)
        {
            [Dwellers] = 0
        };
        var queue = new PriorityQueue<State, int>();
        queue.Enqueue(State.Create(Dwellers, Map, 0), 0);

        while (queue.TryDequeue(out var state, out _))
        {
            if (state.IsOrganised(Map)) return state.EnergySpent;
            
            if (state.EnergySpent > energySpent.GetValueOrDefault(state.Dwellers, int.MaxValue)) continue;

            for (var i = 0; i < state.Dwellers.Length; i++)
            {
                var currentDweller = state.Dwellers[i];
                foreach (var move in currentDweller.PossibleDestinations(state.Dwellers, Map))
                {
                    var dwellers = state.Dwellers.ToArray();
                    dwellers[i] = currentDweller with { X = move.X, Y = move.Y };
                    var energy = state.EnergySpent + move.Steps * currentDweller.Type.EnergyConsumption();

                    if (energy >= energySpent.GetValueOrDefault(dwellers, int.MaxValue)) continue;
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