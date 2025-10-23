namespace SortingInMaze;

public record struct State(Dweller[] Dwellers, int EnergySpent, int EstimatedRemaining)
    : IComparable<State>
{
    public bool IsOrganised(Location[,] map) => Dwellers.All(dweller => map[dweller.Y, dweller.X] == dweller.Type.Room());

    public static State Create(Dweller[] dwellers, Location[,] map, int energySpent)
    {
        var totalEnergy = 0;
        var alreadyInPlace = new Dictionary<DwellerType, int>();

        foreach (var dweller in dwellers)
        {
            var location = map[dweller.Y, dweller.X];
            if (location == dweller.Type.Room())
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

            var distance = Math.Abs(dweller.X - targetX) + Math.Abs(dweller.Y - 2);
            totalEnergy += distance * dweller.Type.EnergyConsumption();
        }

        foreach (var type in Enum.GetValues<DwellerType>())
        {
            var toFill = dwellers.Length / 4 - alreadyInPlace.GetValueOrDefault(type, 0);
            totalEnergy += toFill * (toFill + 1) / 2 * type.EnergyConsumption();
        }

        return new State(dwellers, energySpent, totalEnergy);
    }
    
    public int CompareTo(State other)
    {
        var cmp = (EnergySpent + EstimatedRemaining)
            .CompareTo(other.EnergySpent + other.EstimatedRemaining);
        if (cmp != 0) return cmp;
        return Dwellers.SequenceCompareTo(other.Dwellers);
    }
}