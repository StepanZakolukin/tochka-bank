using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Program
{
    public static IEnumerable<string> Solve(List<(string, string)> edges)
    {
        var graph = new Graph();
        graph.AddNode("a");
        foreach (var edge in edges)
            graph.Connect(edge.Item1, edge.Item2);

        var virusPosition = graph.GetNode("a");
        var gateways = graph.Nodes.Where(node => node.IsGateway()).ToHashSet();
        
        while (true)
        {
            var paths = PathFinder.FindPaths(virusPosition, gateways);
            if (paths.Count == 0) break;

            yield return DisableCorridor(graph, paths).ToString();
            
            paths = PathFinder.FindPaths(virusPosition, gateways);
            if (paths.Count == 0) break;
            
            virusPosition = GetNextVirusNode(paths);
        }
    }

    public static void Main()
    {
        var edges = new List<(string, string)>();

        while (Console.ReadLine() is { } line)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var parts = line.Split('-');
            if (parts.Length == 2)
                edges.Add((parts[0], parts[1]));
        }

        var result = Solve(edges);
        foreach (var edge in result)
            Console.WriteLine(edge);
    }
    
    private static DisablingDto DisableCorridor(Graph graph, List<SinglyLinkedList<Node>> paths)
    {
        var (gateway, simpleNode) = GetCorridorToBlock(paths);

        var dto = new DisablingDto { GatewayName = gateway.Name, SimpleNodeName = simpleNode.Name, };
        
        graph.Disconnect(dto.GatewayName,  dto.SimpleNodeName);
        return dto;
    }

    private static Node GetNextVirusNode(List<SinglyLinkedList<Node>> paths)
    {
        if (paths.Count == 0) 
            throw new InvalidOperationException("Вирус не должен был ходить, путей нет.");

        return paths
            .Select(path => path.ToArray())
            .OrderBy(path => path[0].Name)
            .ThenBy(path => path[^2].Name)
            .Select(path => path[^2])
            .First();
    }

    private static (Node, Node) GetCorridorToBlock(List<SinglyLinkedList<Node>> paths)
    {
        if (paths.Count == 0) 
            throw new InvalidOperationException("Нечего блокировать, путей нет.");
        
        var result = paths
            .OrderBy(path => path.Value.Name)
            .ThenBy(path => path.Previous!.Value.Name)
            .First();
    
        return (result.Value, result.Previous!.Value);
    }
    
    public record DisablingDto
    {
        public required string GatewayName { get; init; }
        public required string SimpleNodeName { get; init; }

        public override string ToString()
        {
            return $"{GatewayName}-{SimpleNodeName}";
        }
    }
    
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

        public void Connect(Node node)
        {
            _incidentNodes.Add(node);
            node._incidentNodes.Add(this);
        }

        public bool Disconnect(Node node)
        {
            return _incidentNodes.Remove(node) && node._incidentNodes.Remove(this);
        }

        public bool IsGateway() => char.IsUpper(Name[0]);
    }
    
    public record SinglyLinkedList<T> : IEnumerable<T>
    {
        public T Value { get; }
        public SinglyLinkedList<T>? Previous { get; }
        public int Length { get; }

        public SinglyLinkedList(T value, SinglyLinkedList<T>? previous = null)
        {
            Value = value;
            Previous = previous;
            Length = previous?.Length + 1 ?? 1;
        }

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
        public static List<SinglyLinkedList<Node>> FindPaths(Node start, HashSet<Node> purposes)
        {
            var allFoundPaths = new List<SinglyLinkedList<Node>>();
            var queue = new Queue<SinglyLinkedList<Node>>();
            queue.Enqueue(new SinglyLinkedList<Node>(start));
            var distances = new Dictionary<Node, int> { [start] = 0 };
            var minDistanceToGateway = int.MaxValue;

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var currentDistance = path.Length - 1;
                
                if (currentDistance > minDistanceToGateway) continue;
                
                if (purposes.Contains(path.Value))
                {
                    if (currentDistance < minDistanceToGateway)
                    {
                        minDistanceToGateway = currentDistance;
                        allFoundPaths.Clear();
                        allFoundPaths.Add(path);
                    }
                    else if (currentDistance == minDistanceToGateway)
                    {
                        allFoundPaths.Add(path);
                    }
                    continue;
                }
                
                var newDistance = currentDistance + 1;
                foreach (var nextNode in path.Value.IncidentNodes)
                {
                    if (newDistance > distances.GetValueOrDefault(nextNode, int.MaxValue)) continue;
                    distances[nextNode] = newDistance;
                    queue.Enqueue(new SinglyLinkedList<Node>(nextNode, path));
                }
            }

            return allFoundPaths;
        }
    }
}