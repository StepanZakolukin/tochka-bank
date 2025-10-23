namespace SortingInMaze;

internal static class DwellerTypeExtensions
{
    public static int EnergyConsumption(this DwellerType s) => s switch
    {
        DwellerType.A => 1,
        DwellerType.B => 10,
        DwellerType.C => 100,
        DwellerType.D => 1000,
        _ => throw new ArgumentOutOfRangeException()
    };

    public static Location Room(this DwellerType s) => s switch
    {
        DwellerType.A => Location.RoomA,
        DwellerType.B => Location.RoomB,
        DwellerType.C => Location.RoomC,
        DwellerType.D => Location.RoomD,
        _ => throw new ArgumentOutOfRangeException()
    };
}