using Microsoft.Azure.Cosmos;
using System;
using System.Threading.Tasks;

namespace CosmosDBDemo
{
	class Program
	{
		private static string accountEndpoint = "https://<account>.documents.azure.com:443/";
		private static string authKey = "<authKey>";

		private static string databaseId = "stuff";
		private static string containerId = "items";
		private static string partitionKey = "/id";

		static async Task Main(string[] args)
		{
			// Create new CosmosClient to communiciate with Azure Cosmos DB
			using (var cosmosClient = new CosmosClient(accountEndpoint, authKey))
			{
				// Create new database
				Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);

				// Create new container
				Container container = await database.CreateContainerIfNotExistsAsync(containerId, partitionKey);

				for (int i = 0; i < 100; i++)
				{
					Console.WriteLine($"Run: {i}");

					// This will complete successfully.
					var authKeyItemId = await CreateItemAsync(container);
					var authKeyItem = await ReadItemAsync(container, authKeyItemId);

					string userId = Guid.NewGuid().ToString();
					UserResponse userResponse = await database.CreateUserAsync(userId);
					User user = userResponse.User;

					string permissionId = Guid.NewGuid().ToString();
					PermissionProperties permissionProperties = new PermissionProperties(permissionId, PermissionMode.Read, container);
					PermissionResponse permissionResponse = await user.CreatePermissionAsync(permissionProperties);

					using (var tokenCosmosClient = new CosmosClient(accountEndpoint, permissionResponse.Resource.Token))
					{
						var tokenContainer = tokenCosmosClient.GetContainer(databaseId, containerId);

						// This will fail.
						var tokenItemId = await CreateItemAsync(tokenContainer);

						// This will succeed.
						var tokenItem = await ReadItemAsync(tokenContainer, authKeyItemId);
					}
				}
			}
		}

		private static async Task<string> CreateItemAsync(Container container)
		{
			var color = Console.ForegroundColor;

			try
			{
				var item = new Item
				{
					Id = Guid.NewGuid().ToString(),
					Name = Guid.NewGuid().ToString()
				};

				var response = await container.CreateItemAsync(item, new PartitionKey(item.Id));

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"Creating item with Id: {item.Id} succeeded!");
				Console.ForegroundColor = color;

				return item.Id;
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.Message);
				Console.ForegroundColor = color;
			}

			return null;
		}

		private static async Task<Item> ReadItemAsync(Container container, string id)
		{
			var color = Console.ForegroundColor;

			try
			{
				var item = await container.ReadItemAsync<Item>(id, new PartitionKey(id));
				
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"Reading item with Id: {item.Resource.Id} succeeded!");
				Console.ForegroundColor = color;

				return item.Resource;
			}
			catch (Exception ex)
			{
				
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.Message);
				Console.ForegroundColor = color;
			}

			return null;
		}

	}
}
