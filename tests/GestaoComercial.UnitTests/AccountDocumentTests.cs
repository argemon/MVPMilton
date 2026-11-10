extern alias WebHost;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

public sealed class AccountDocumentTests
{
    [Fact]
    public async Task ErrorDocumentDisplaysSafeMessageAndPreservesLocalReturn()
    {
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<WebHost::GestaoComercial.Web.Components.DocumentoConta>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["Erro"] = "Serviço indisponível. Tente novamente.",
                    ["ReturnUrl"] = "/produtos"
                }));
            return System.Net.WebUtility.HtmlDecode(output.ToHtmlString());
        });
        Assert.Contains("Serviço indisponível. Tente novamente.", html);
        Assert.Contains("value=\"/produtos\"", html);
        Assert.DoesNotContain(">Erro</p>", html);
    }
}
