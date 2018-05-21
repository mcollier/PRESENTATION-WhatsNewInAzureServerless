using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

public static class HelloSequence
{
    [FunctionName("E1_HelloSequence")]
    public static async Task<List<string>> Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
    {
        var outputs = new List<string>();

        outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "Seattle"));
        outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "Columbus"));

        return outputs;
    }

    [FunctionName("E1_SayHello")]
    public static string SayHello([ActivityTrigger] string name)
    {
        return $"Hello {name}!";
    }
}