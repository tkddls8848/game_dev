using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using ConceptPoCs;

// Each game's test project compiles its own rules, this suite, and the shared graph exporter.
public class ConceptGraphTests
{
    private Graph graph = null!;
    private IGame game = null!;
    private Scenario data = null!;
    private string root = "";

    [OneTimeSetUp]
    public void Build()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "data", "scenario.json"))) dir = dir.Parent;
        root = dir?.FullName ?? throw new Exception("Scenario not found");
        data = GraphBuilder.Read(Path.Combine(root, "data", "scenario.json"));
        var type = GetType().Assembly.GetTypes().Single(t => typeof(IGame).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        game = (IGame)Activator.CreateInstance(type)!;
        graph = GraphBuilder.Build(game, data);
    }

    [Test] public void EveryReachableStateHasValidActionsAndCanFinish() => GraphBuilder.Validate(graph);
    [Test] public void SameScenarioProducesByteIdenticalGraph() => Assert.That(
        JsonSerializer.Serialize(GraphBuilder.Build(game, data), GraphBuilder.Json),
        Is.EqualTo(JsonSerializer.Serialize(graph, GraphBuilder.Json)));
    [Test] public void RestartIsStableAndEndingsAreDistinct()
    {
        Assert.That(graph.Nodes[0].State, Is.EqualTo(game.Start(data)));
        Assert.That(graph.Nodes.Count(n => n.Terminal), Is.GreaterThanOrEqualTo(2));
        Assert.That(graph.Nodes.Where(n => n.Terminal).Select(n => n.Ending).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
    }
    [Test] public void ScenarioHasUniqueIdsAndReadableInstructions()
    {
        Assert.That(data.Items.Select(i => i.Id).Distinct().Count(), Is.EqualTo(data.Items.Length));
        Assert.That(data.Instructions.Length, Is.GreaterThan(20));
        Assert.That(graph.Nodes.All(n => n.Title.Length > 0 && n.Prompt.Length > 0), Is.True);
    }
    private (string[] actions, string ending) Solution() => data.Kind switch
    {
        "scent" => (new[]{"sample","time1","sample","time2","sample","deduce-coat"}, "외투가 옮긴 냄새"),
        "diplomacy" => (new[]{"literal","literal","literal"}, "같은 문서, 같은 약속"),
        "phone" => (new[]{"decline","expand","location","rescue"}, "마지막 수신인"),
        "memory" => (new[]{"person","act","implant"}, "남아 있는 온기"),
        "map" => (new[]{"road","gate","petition","door","approve"}, "모두의 주소가 남은 아침"),
        "curse" => (new[]{"audit","talk","renegotiate","grain","grain"}, "처음 쓰는 공정한 장부"),
        "baton" => (new[]{"cue2","cue2","cue2","cue2"}, "네 사람의 한 호흡"),
        "editor" => (new[]{"move","return"}, "작가가 다시 보낸 목소리"),
        "apartment" => (new[]{"listen2","move2"}, "조용한 집의 다음 주소"),
        "prayer" => (new[]{"morning","read","gate","dispatch"}, "세 통의 감사 편지"),
        _ => throw new Exception("Unknown game")
    };
    private string[] Risky() => data.Kind switch
    {
        "scent" => new[]{"sample","time1","sample","time2","sample","deduce-clerk"},
        "diplomacy" => new[]{"soften","soften","soften"},
        "phone" => new[]{"answer","charge","wait","rescue"},
        "memory" => new[]{"implant"},
        "map" => new[]{"road","gate","approve"},
        "curse" => new[]{"defer","defer","defer"},
        "baton" => new[]{"cue0","cue0","cue0","cue0"},
        "editor" => new[]{"strike","replace","return"},
        "apartment" => new[]{"listen0","move0"},
        "prayer" => new[]{"morning","dispatch"},
        _ => throw new Exception("Unknown game")
    };
    private Node Play(string[] actions)
    {
        var n=graph.Nodes[0];
        foreach(var id in actions)
        {
            var action=n.Actions.Single(a=>a.Id==id);
            n=graph.Nodes.Single(v=>v.Id==action.Target);
        }
        return n;
    }
    [Test] public void IntendedSolutionReachesTheExpectedEnding()
    {
        var solution=Solution();
        var n=Play(solution.actions);
        Assert.That(n.Terminal,Is.True);
        Assert.That(n.Ending,Is.EqualTo(solution.ending));
    }
    [Test] public void RiskyChoicesHaveDifferentConsequences()
    {
        var n=Play(Risky());
        Assert.That(n.Terminal,Is.True);
        Assert.That(n.Ending,Is.Not.EqualTo(Solution().ending));
    }
    [Test] public void CoreConstraintsCannotBeBypassed()
    {
        switch(data.Kind)
        {
            case "scent": Assert.That(graph.Nodes.Where(n=>n.State.Flags!=7).All(n=>n.Actions.All(a=>!a.Id.StartsWith("deduce"))),Is.True); break;
            case "diplomacy": Assert.That(graph.Nodes.All(n=>n.State.A+n.State.B==n.State.Step),Is.True); break;
            case "phone": Assert.That(Play(new[]{"answer","charge","wait"}).Stats.Single(s=>s.Label=="위치").Value,Does.Contain("구청까지만")); break;
            case "memory": Assert.That(Play(new[]{"person","implant"}).Ending,Is.EqualTo("꿈의 거부 반응")); break;
            case "map": Assert.That(graph.Nodes.Where(n=>n.State.Flags==0).All(n=>n.Actions.All(a=>a.Id!="door")),Is.True); break;
            case "curse":
                Assert.That(graph.Nodes.All(n=>n.State.A>=0 && n.State.B>=0),Is.True);
                Assert.That(graph.Nodes.Where(n=>(n.State.Flags&3)!=3).All(n=>n.Actions.All(a=>a.Id!="renegotiate")),Is.True);
                Assert.That(graph.Nodes.Where(n=>n.State.A<data.Values[2]).All(n=>n.Actions.All(a=>a.Id!="grain")),Is.True);
                break;
            case "baton":
                Assert.That(Play(new[]{"cue1"}).State.A,Is.Zero);
                Assert.That(Play(new[]{"cue2"}).State.A,Is.EqualTo(1));
                Assert.That(Play(new[]{"cue3"}).State.A,Is.EqualTo(1));
                Assert.That(Play(new[]{"cue4"}).State.A,Is.Zero); break;
            case "editor": Assert.That(Play(new[]{"strike","strike","move","move","replace","replace"}).State,Is.EqualTo(game.Start(data))); break;
            case "apartment":
                Assert.That(graph.Nodes.All(n=>n.State.A>=0),Is.True);
                Assert.That(graph.Nodes[0].Actions.All(a=>!a.Id.StartsWith("move")),Is.True); break;
            case "prayer": Assert.That(graph.Nodes.Where(n=>n.State.Flags==0).All(n=>n.Actions.All(a=>a.Id!="gate")),Is.True); break;
        }
    }
    [Test, Order(99)] public void ExportVerifiedBrowserGraph()
    {
        GraphBuilder.Validate(graph);
        File.WriteAllText(Path.Combine(root, "data", "graph.json"), JsonSerializer.Serialize(graph, GraphBuilder.Json));
        TestContext.WriteLine($"{data.Slug}: {graph.Nodes.Length} states; {graph.Nodes.Count(n => n.Terminal)} terminal states");
    }
}
