using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace pax.BlazorChartJs;

// This class provides an example of how JavaScript functionality can be wrapped
// in a .NET class for easy consumption. The associated JavaScript module is
// loaded on demand when first needed.
//
// This class can be registered as scoped DI service and then injected into Blazor
// components for use.

/// <summary>
/// ChartJsInterop
/// </summary>
public class ChartJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    private readonly ILogger<ChartJsInterop> logger;
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// ChartJsInterop
    /// </summary>
    public ChartJsInterop(IJSRuntime jsRuntime, ILogger<ChartJsInterop> logger)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/pax.BlazorChartJs/chartJsInterop.js").AsTask());

        this.logger = logger;
        // this.logger.LogInformation("init");
    }

    /// <summary>
    /// (Re-)initializes chart
    /// </summary>
    public async ValueTask InitChart(ChartJsConfig config, DotNetObjectReference<ChartComponent> dotnetRef)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(dotnetRef);

        var module = await moduleTask.Value.ConfigureAwait(false);
        await module.InvokeVoidAsync("initChart", config.ChartJsConfigGuid, SerializeConfig(config), dotnetRef)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// ChartJsInterop
    /// </summary>
    public async ValueTask ShowChart(ChartJsConfig chart)
    {
        ArgumentNullException.ThrowIfNull(chart);
        var module = await moduleTask.Value.ConfigureAwait(false);
        await module.InvokeVoidAsync("showChart", chart.ChartJsConfigGuid, chart).ConfigureAwait(false);
    }

    /// <summary>
    /// ChartJsInterop
    /// </summary>
    public async ValueTask AddDataToDataset(string chartId, string datasetId, string label)
    {
        var module = await moduleTask.Value.ConfigureAwait(false);
        await module.InvokeVoidAsync("addDataToDataset", chartId, datasetId, label).ConfigureAwait(false);
    }

    private JsonObject? SerializeConfig(ChartJsConfig config)
    {
        var json = JsonSerializer.Serialize(config, jsonOptions);

        if (json == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return JsonSerializer.Deserialize<JsonObject>(json);
    }

    /// <summary>
    /// DisposeAsync
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value.ConfigureAwait(false);
            await module.DisposeAsync().ConfigureAwait(false);
        }
        // logger.LogInformation("dispose");
    }
}
