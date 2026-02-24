// import packages
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// values from OpenAI deployment
var modelId = "gpt-4o-mini";
var endpoint = "";
var apiKey = "";

// kernel with Azure OpenAI chat completion
var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// enterprise components
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

// build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// adding a plugin with custom functions to the kernel
kernel.Plugins.AddFromType<LightsPlugin>("Lights");

// enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// history store the conversation
var history = new ChatHistory();

//  back-and-forth chat
string? userInput;
do
{
    //  user input
    Console.Write("User > ");
    userInput = Console.ReadLine();

    if (string.IsNullOrEmpty(userInput))
    {
        break;
    }

    // add user input
    history.AddUserMessage(userInput);

    // get the response from the AI
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // print the results
    Console.WriteLine("Assistant > " + result);

    // add the message from the agent to the chat history
    history.AddMessage(result.Role, result.Content ?? string.Empty);
} while (userInput is not null);


// class LightsPlugin
public class LightsPlugin
{
    // mock data for the lights
    private readonly List<LightModel> lights = new()
    {
        new LightModel { Id = 1, Name = "Table Lamp", IsOn = false },
        new LightModel { Id = 2, Name = "Porch light", IsOn = false },
        new LightModel { Id = 3, Name = "Chandelier", IsOn = true }
    };

    [KernelFunction("get_lights")]
    [Description("Gets a list of lights and their current state")]
    public Task<List<LightModel>> GetLightsAsync()
    {
        return Task.FromResult(lights);
    }

    [KernelFunction("change_state")]
    [Description("Changes the state of the light")]
    public Task<LightModel?> ChangeStateAsync(int id, bool isOn)
    {
        var light = lights.FirstOrDefault(light => light.Id == id);

        if (light == null)
        {
            return Task.FromResult<LightModel?>(null);
        }

        // update the light with the new state
        light.IsOn = isOn;

        return Task.FromResult<LightModel?>(light);
    }
}

public class LightModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_on")]
    public bool? IsOn { get; set; }
}
