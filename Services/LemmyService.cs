namespace Veggy.Services;

public class LemmyService
{
	public LemmyService()
	{
	}

	public Task CreateComment(Comment comment, User user)
	{
		throw new NotImplementedException();

	}

	public Task<Post> CreatePost(Post post, User user)
	{
		throw new NotImplementedException();
	}

	public async Task<Community> CreateCommunity(Community community, User user)
	{
		throw new NotImplementedException();

		var options = new RestClientOptions("https://lemmy.ml/api/v3/community");
		var client = new RestClient(options);
		var request = new RestRequest("");
		request.AddHeader("accept", "application/json");
		request.AddHeader("content-type", "application/json");
		var response = await client.PostAsync(request);

		Console.WriteLine("{0}", response.Content);
	}
}