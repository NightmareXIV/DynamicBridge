using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NightmareLoc;

internal class NightmareLoc
{
    static void Main(string[] args)
    {
        try
        {
            using var workspace = MSBuildWorkspace.Create();
            var project = workspace.OpenProjectAsync(args[0]).Result;

            foreach(var document in project.Documents)
            {
                var model = document.GetSemanticModelAsync().Result;
                var methodInvocation = document.GetSyntaxRootAsync().Result;
                try
                {
                    var node = methodInvocation?.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(x => ((MemberAccessExpressionSyntax)x.Expression).Name.ToString() == "ECommons.EzLocalizationManager.EzLocalization.Loc").FirstOrDefault();

                    if(node != null)
                    {
                        Console.WriteLine($"Found call with {node.ArgumentList.Arguments.Count} arguments");
                    }
                }
                catch (Exception exception)
                {
                    // Swallow the exception of type cast. 
                    // Could be avoided by a better filtering on above linq.
                    continue;
                }
            }
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
        }
        Console.ReadLine();
    }
}
