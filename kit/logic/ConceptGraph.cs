using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace ConceptPoCs;

// The browser follows this exported graph; it does not implement a second rules engine.
public record State(int Step = 0, int A = 0, int B = 0, int C = 0, int Flags = 0);
public record Item(string Id, string Title, string Text, string Detail);
public record Palette(string Background, string Surface, string Paper, string Accent, string Muted);
public record Scenario(string Slug, string Kind, string Title, string English, string Tagline,
    string Instructions, string Question, Palette Palette, Item[] Items, int[] Values);
public record Stat(string Label, string Value);
public record Choice(string Id, string Label, string Hint, State Next);
public record View(string Title, string Prompt, string Feedback, Stat[] Stats,
    Choice[] Choices, bool Terminal = false, string Ending = "");
public record Link(string Id, string Label, string Hint, string Target);
public record Node(string Id, State State, string Title, string Prompt, string Feedback,
    Stat[] Stats, Link[] Actions, bool Terminal, string Ending);
public record Graph(int Version, Scenario Scenario, string Start, Node[] Nodes);

public interface IGame
{
    State Start(Scenario data);
    View Describe(State state, Scenario data);
}

public static class GraphBuilder
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static Scenario Read(string file) => JsonSerializer.Deserialize<Scenario>(File.ReadAllText(file), Json)!;

    public static Graph Build(IGame game, Scenario data)
    {
        var states = new List<State> { game.Start(data) };
        var ids = new Dictionary<State, int> { [states[0]] = 0 };
        var nodes = new List<Node>();
        for (int i = 0; i < states.Count; i++)
        {
            if (states.Count > 25000) throw new InvalidOperationException("State budget exceeded: " + data.Slug);
            var s = states[i];
            var v = game.Describe(s, data);
            var links = new List<Link>();
            foreach (var c in v.Choices)
            {
                if (!ids.TryGetValue(c.Next, out int id))
                {
                    id = states.Count;
                    ids[c.Next] = id;
                    states.Add(c.Next);
                }
                links.Add(new(c.Id, c.Label, c.Hint, id.ToString()));
            }
            nodes.Add(new(i.ToString(), s, v.Title, v.Prompt, v.Feedback, v.Stats, links.ToArray(), v.Terminal, v.Ending));
        }
        return new(1, data, "0", nodes.ToArray());
    }

    public static void Validate(Graph graph)
    {
        if (graph.Nodes.Length < 4) throw new InvalidOperationException("Not a playable slice");
        var endings = graph.Nodes.Where(n => n.Terminal).Select(n => n.Ending).Distinct().ToArray();
        if (endings.Length < 2) throw new InvalidOperationException("Need meaningfully different endings");
        var known = graph.Nodes.Select(n => n.Id).ToHashSet();
        foreach (var n in graph.Nodes)
        {
            if (!n.Terminal && n.Actions.Length == 0) throw new InvalidOperationException("Dead end " + n.Id);
            if (n.Terminal && n.Actions.Length != 0) throw new InvalidOperationException("Ending still mutable");
            if (n.Actions.Select(a => a.Id).Distinct().Count() != n.Actions.Length) throw new InvalidOperationException("Duplicate action");
            if (n.Actions.Any(a => !known.Contains(a.Target))) throw new InvalidOperationException("Broken target");
        }
        // Every reachable state must still have a route to a conclusion (cycles such as rewinding are allowed).
        var canFinish = graph.Nodes.Where(n => n.Terminal).Select(n => n.Id).ToHashSet();
        bool changed;
        do
        {
            changed = false;
            foreach (var n in graph.Nodes)
                if (!canFinish.Contains(n.Id) && n.Actions.Any(a => canFinish.Contains(a.Target)))
                    changed |= canFinish.Add(n.Id);
        } while (changed);
        if (canFinish.Count != graph.Nodes.Length) throw new InvalidOperationException("Unfinishable state");
    }
}

public static class V
{
    public static Stat S(string label, object value) => new(label, value.ToString()!);
    public static Choice C(string id, string label, string hint, State next) => new(id, label, hint, next);
    public static View End(string title, string text, string feedback, params Stat[] stats) =>
        new(title, text, feedback, stats, Array.Empty<Choice>(), true, title);
    public static int Count(int mask) => System.Numerics.BitOperations.PopCount((uint)mask);
}
