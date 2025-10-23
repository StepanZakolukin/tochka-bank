namespace SortingInMaze;

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

        var stepsToThreshold = Y - 1;
        foreach (var tuple in PossibleDestinationsFromCorridor(-1, stepsToThreshold, dwellers, map))
            yield return tuple;
        
        foreach (var tuple in PossibleDestinationsFromCorridor(1, stepsToThreshold, dwellers, map))
            yield return tuple;
    }

    private IEnumerable<Move> PossibleDestinationsFromCorridor(
        int direction,
        int steps,
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
                if (map[2, x] == Type.Room())
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
        if (map[Y, X] != Type.Room()) return true;
        
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