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
        
        return PathFinder.FindPaths(virusPosition, gateways)
            .OrderBy(path => path.Length)
            .ThenBy(path => (path.Value.Name, path.Previous!.Value.Name))
            .Select(path => $"{path.Value.Name}-{path.Previous!.Value.Name}");
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
                    if (!nextNode.IsGateway()) visited.Add(nextNode);
                    queue.Enqueue(new SinglyLinkedList<Node>(nextNode, node));
                }
            }
        }
    }
}