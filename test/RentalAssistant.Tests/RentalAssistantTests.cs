namespace RentalAssistant.Tests;

public sealed class RentalAssistantTests(ChromaFixture chroma) : IClassFixture<ChromaFixture>
{
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Stores_the_documents_in_Chroma()
    {
        using var assistant = await CreateAssistantAsync(new RecordingChatClient());

        var collection = chroma.ChromaClient.GetCollectionClient(
            await chroma.ChromaClient.GetCollection(RentalAssistant.CollectionName, cancellationToken: _cancellationToken));
        var prices = await collection.Get("prices", cancellationToken: _cancellationToken);

        Assert.Equal(Documents.All.Count, await collection.Count(_cancellationToken));
        Assert.NotNull(prices);
        Assert.Equal(Documents.All.Single(document => document.SourceId == "prices").Text, prices.Metadata!["Text"]);
    }

    [Fact]
    public async Task Storing_the_documents_again_replaces_them()
    {
        using var first = await CreateAssistantAsync(new RecordingChatClient());
        using var second = await CreateAssistantAsync(new RecordingChatClient());

        var collection = chroma.ChromaClient.GetCollectionClient(
            await chroma.ChromaClient.GetCollection(RentalAssistant.CollectionName, cancellationToken: _cancellationToken));

        Assert.Equal(Documents.All.Count, await collection.Count(_cancellationToken));
    }

    [Fact]
    public async Task Puts_the_passage_that_answers_each_question_in_the_prompt()
    {
        const string Prices = "An e-bike costs 30 euros a day or 8 euros an hour.";
        const string Helmets = "Children under 14 must wear a helmet.";
        var model = new RecordingChatClient();
        using var assistant = await CreateAssistantAsync(model);

        await assistant.AskAsync("How much does an e-bike cost a day?", _cancellationToken);
        var pricePrompt = model.LastMessagesText;

        await assistant.AskAsync("Must children wear a helmet?", _cancellationToken);
        var helmetPrompt = model.LastMessagesText;

        Assert.Contains(Prices, pricePrompt);
        Assert.DoesNotContain(Helmets, pricePrompt);
        Assert.Contains("Question: How much does an e-bike cost a day?", pricePrompt);
        Assert.Contains(Helmets, helmetPrompt);
        Assert.DoesNotContain(Prices, helmetPrompt);
    }

    [Fact]
    public async Task Never_puts_the_staff_notes_in_the_prompt()
    {
        var model = new RecordingChatClient();
        using var assistant = await CreateAssistantAsync(model);

        await assistant.AskAsync("Is there a discount code?", _cancellationToken);

        Assert.Contains("Students and groups of five or more get a 10% discount.", model.LastMessagesText);
        Assert.DoesNotContain("STAFF50", model.LastMessagesText);
    }

    private Task<RentalAssistant> CreateAssistantAsync(RecordingChatClient model)
        => RentalAssistant.CreateAsync(model, chroma.VectorStore, ChromaFixture.EmbeddingDimensions, _cancellationToken);
}
