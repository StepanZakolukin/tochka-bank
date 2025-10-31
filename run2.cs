using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Program
{
    public static IEnumerable<PathToCutDto> Solve(List<EdgeDto> edges)
    {
        var graph = new Graph();
        graph.AddNode("a");
        foreach (var edge in edges)
            graph.Connect(edge.FirstNodeName, edge.SecondNodeName);

        var virusPosition = graph.GetNode("a");
        var gateways = graph.Nodes.Where(node => node.IsGateway()).ToHashSet();
        
        while (virusPosition != null)
        {
            var pathToCut = DisableCorridor(graph, virusPosition, gateways);
            
            if (pathToCut is null) break;
            
            yield return pathToCut;
            
            virusPosition = GetNextVirusNode(virusPosition, gateways);
        }
    }

    public static void Main()
    {
        var edges = new List<EdgeDto>();

        while (Console.ReadLine() is { } line)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var parts = line.Split('-');
            if (parts.Length == 2)
                edges.Add(new EdgeDto(parts[0], parts[1]));
        }

        var result = Solve(edges);
        foreach (var edge in result)
            Console.WriteLine(edge.ToString());
    }
    
    private static PathToCutDto? DisableCorridor(Graph graph, Node virusPosition, HashSet<Node> gateways)
    {
        var dto = GetCorridorToBlock(virusPosition, gateways);
        
        if (dto is not null)
            graph.Disconnect(dto.GatewayName,  dto.SimpleNodeName);
        
        return dto;
    }

    private static Node? GetNextVirusNode(Node virusPosition, HashSet<Node> gateways)
    {
        var paths = PathFinder.FindPaths(virusPosition, gateways);
        
        if (!paths.Any()) return null;
        var shortestPath = paths.First();

        return paths
            .Where(path => path.Length == shortestPath.Length)
            .Select(path => path.ToArray())
            .OrderBy(path => path[0].Name)
            .ThenBy(path => path[^2].Name)
            .Select(path => path[^2])
            .First();
    }

    private static PathToCutDto? GetCorridorToBlock(Node virusPosition, HashSet<Node> gateways)
    {
        var paths = PathFinder.FindPaths(virusPosition, gateways);
        if (!paths.Any()) return null;
        var shortestPath = paths.First();
        if (shortestPath.Length == 1)
        {
            return new PathToCutDto
            {
                GatewayName = shortestPath.Value.Name,
                SimpleNodeName = shortestPath.Previous!.Value.Name,
            };
        }

        return paths
            .Select(path => new PathToCutDto
            {
                GatewayName = path.Value.Name, 
                SimpleNodeName = path.Previous!.Value.Name
            })
            .OrderBy(dto => dto)
            .First();
    }
    
    public record PathToCutDto : IComparable<PathToCutDto>
    {
        public required string GatewayName { get; init; }
        public required string SimpleNodeName { get; init; }

        public override string ToString()
        {
            return $"{GatewayName}-{SimpleNodeName}";
        }

        public int CompareTo(PathToCutDto? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var gatewayNameComparison = string.Compare(GatewayName, other.GatewayName, StringComparison.Ordinal);
            if (gatewayNameComparison != 0) return gatewayNameComparison;
            return string.Compare(SimpleNodeName, other.SimpleNodeName, StringComparison.Ordinal);
        }
    }

    public record EdgeDto(string FirstNodeName, string SecondNodeName);
    
    public class Graph
    {
        private readonly Dictionary<string, Node> _nodes = new();
        public IEnumerable<Node> Nodes => _nodes.Values;

        public bool AddNode(string name)
        {
            if (_nodes.ContainsKey(name))
                return false;
            
            _nodes[name] = new Node(name);
            return true;
        }
        
        public void Connect(string firstNodeName, string secondNodeName)
        {
            if (!_nodes.TryGetValue(firstNodeName, out var firstNode))
            {
                firstNode = new Node(firstNodeName);
                _nodes.Add(firstNodeName, firstNode);
            }

            if (!_nodes.TryGetValue(secondNodeName, out var secondNode))
            {
                secondNode = new Node(secondNodeName);
                _nodes.Add(secondNodeName, secondNode);
            }
        
            firstNode.Connect(secondNode);
        }

        public bool Disconnect(string firstNodeName, string secondNodeName)
        {
            if (!_nodes.TryGetValue(firstNodeName, out var firstNode) || !_nodes.TryGetValue(secondNodeName, out var secondNode))
                return false;
            return firstNode.Disconnect(secondNode);
        }

        public Node GetNode(string nodeName)
        {
            return !_nodes.TryGetValue(nodeName, out var node) 
                ? throw new ArgumentException($"Узла с {nameof(nodeName)} не найдено") 
                : node;
        }
    }

    public record Node
    {
        public string Name { get; }
        private readonly List<Node> _incidentNodes = [];
        public IEnumerable<Node> IncidentNodes => _incidentNodes;

        public Node(string name)
        {
            Name = name;
        }

        public void Connect(Node nodeBase)
        {
            _incidentNodes.Add(nodeBase);
            nodeBase._incidentNodes.Add(this);
        }

        public bool Disconnect(Node nodeBase)
        {
            return _incidentNodes.Remove(nodeBase) && nodeBase._incidentNodes.Remove(this);
        }
        
        public bool IsGateway() => char.IsUpper(Name[0]);
    }
    
    public record SinglyLinkedList<T>(T Value, SinglyLinkedList<T>? Previous = null) : IEnumerable<T>
    {
        public int Length { get; } = Previous?.Length + 1 ?? 1;

        public IEnumerator<T> GetEnumerator()
        {
            yield return Value;
            var pathItem = Previous;
            while (pathItem != null)
            {
                yield return pathItem.Value;
                pathItem = pathItem.Previous;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    public class PathFinder
    {
        public static IEnumerable<SinglyLinkedList<Node>> FindPaths(Node start, IEnumerable<Node> purposes)
        {
            var queue = new Queue<SinglyLinkedList<Node>>();
            queue.Enqueue(new SinglyLinkedList<Node>(start));
            HashSet<Node> visited = [start];
            var hashSetPurposes = purposes.ToHashSet();

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                if (hashSetPurposes.Contains(node.Value)) yield return node;

                foreach (var nextNode in node.Value.IncidentNodes.Where(currentNode => !visited.Contains(currentNode)))
                {
                    if (!hashSetPurposes.Contains(nextNode)) visited.Add(nextNode);
                    queue.Enqueue(new SinglyLinkedList<Node>(nextNode, node));
                }
            }
        }
    }
}