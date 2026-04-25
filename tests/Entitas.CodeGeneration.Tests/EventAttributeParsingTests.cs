using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Entitas.Generators.IntegrationTests;

public class EventAttributeParsingTests
{
    const string Source = @"
using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace MyGame
{
    public sealed class MainAttribute : ContextAttribute
    {
        public MainAttribute() : base(""Main"") { }
    }

    [Main]
    [Event(EventTarget.Self, EventType.Removed, 7)]
    public sealed class CountdownComponent : IComponent
    {
        public int Value;
    }
}
";

    [Fact]
    public void ParsesEventAttributeEnumValues()
    {
        var result = CodeGenerationTestHelper.RunGenerator(Source, "My.Gameplay");
        var fileNames = GetGeneratedFileNames(result);

        fileNames.Should().Contain("MyGame.IMyGameCountdownRemovedListener.g.cs");
        fileNames.Should().Contain("MyGame.MyGameCountdownRemovedEventSystem.g.cs");
        fileNames.Should().NotContain("MyGame.IMyGameAnyCountdownRemovedListener.g.cs");
        fileNames.Should().NotContain("MyGame.IMyGameCountdownListener.g.cs");

        GetGeneratedSource(result, "MyGame.IMyGameCountdownRemovedListener.g.cs")
            .Should().Contain("void OnMyGameCountdownRemoved(MainEntity entity);");

        var eventSystemSource = GetGeneratedSource(result, "MyGame.MyGameCountdownRemovedEventSystem.g.cs");
        eventSystemSource.Should().Contain("TriggerOnEventMatcherExtension.Removed");
        eventSystemSource.Should().Contain("return builder.AddEventSystem(\"MyGameCountdownRemovedEventSystem\", 7, contexts => new MyGameCountdownRemovedEventSystem(contexts));");
    }

    static string[] GetGeneratedFileNames(Microsoft.CodeAnalysis.GeneratorDriverRunResult result) =>
        result.GeneratedTrees
            .Select(tree => Path.GetFileName(tree.FilePath))
            .ToArray();

    static string GetGeneratedSource(Microsoft.CodeAnalysis.GeneratorDriverRunResult result, string fileName) =>
        result.GeneratedTrees
            .Single(tree => Path.GetFileName(tree.FilePath) == fileName)
            .GetText()
            .ToString();
}
