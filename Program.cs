using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

public class EventHubSequenceNumberRetriever
{
    public static string? connectionString;
    public static string? eventHubName;
    public static string? consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

    public static string? BlobServiceEndpoint;
    public static string? StorageAccountName;
    public static string? StorageConnectionString;
    public static string? StorageEventHubContainer;

    public static async Task Main()
    {
        await PrintFinalResults(await GetUnprocessedEvents(await GetStorageCheckpointValues(), await GetEventHubLastSequences()));

        Console.ReadLine();
    }

    public static async Task SetConfigurations()
    {
        await Task.Delay(1);

        var builder = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

        IConfiguration config = builder.Build();

        if (config["consumerGroup"] != "")
        {
            consumerGroup = config["consumerGroup"];
            Console.WriteLine("ConsumerGroup is not set in appsettings.json or environment variables.");
        }

        connectionString = config["EventHubConnectionString"];
        eventHubName = config["EventHubName"];
        BlobServiceEndpoint = config["BlobServiceEndpoint"];
        StorageAccountName = config["StorageAccountName"];
        StorageConnectionString = config["StorageConnectionString"];
        StorageEventHubContainer = config["StorageEventHubContainer"];
    }

    public static async Task PrintFinalResults(Dictionary<int, int> finalResultsInput)
    {
        await Task.Delay(1);
        foreach (var kvp in finalResultsInput)
        {
            Console.WriteLine("PartitionId: " + kvp.Key + " UnprocessedEvents: " + kvp.Value);
        }
    }

    public static async Task<Dictionary<int, int>> GetUnprocessedEvents(Dictionary<int, int> storChecks, Dictionary<int, int> EventSeqs)
    {
        await Task.Delay(1);
        var resultDict = new Dictionary<int, int>();

        foreach (var kvp in EventSeqs)
        {
            if (storChecks.ContainsKey(kvp.Key))
            {
                resultDict[kvp.Key] = kvp.Value - storChecks[kvp.Key];
            }
            else
            {
                resultDict[kvp.Key] = kvp.Value;
            }
        }

        foreach (var kvp in storChecks)
        {
            if (!EventSeqs.ContainsKey(kvp.Key))
            {
                resultDict[kvp.Key] = -kvp.Value;
            }
        }

        return resultDict;
    }

    public static async Task<Dictionary<int, int>> GetEventHubLastSequences()
    {
        Dictionary<int, int> propsList = new Dictionary<int, int>();

        await using var consumer = new EventHubConsumerClient(consumerGroup, connectionString, eventHubName);

        var partitionIds = await consumer.GetPartitionIdsAsync();

        foreach (var partition in partitionIds)
        {
            var prop = await consumer.GetPartitionPropertiesAsync(partition.ToString());

            propsList.Add(Convert.ToInt32(prop.Id), Convert.ToInt32(prop.LastEnqueuedSequenceNumber));
        }

        foreach (var seq in propsList)
        {
            Console.WriteLine("PartitionId: " + seq.Key + " LastSequenceNumber: " + seq.Value);
        }

        return propsList;
    }

    public static string RemoveProtocol(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
        {
            // Remove the protocol part and return the rest of the URL
            return uri.Host + uri.PathAndQuery;
        }
        else
        {
            throw new ArgumentException("Invalid URL", nameof(url));
        }
    }

    public static async Task<Dictionary<int, int>> GetStorageCheckpointValues()
    {
        Dictionary<int, int> sequenceNums = new Dictionary<int, int>();

        BlobServiceClient blobServiceClient = new BlobServiceClient(StorageConnectionString);
        BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(StorageEventHubContainer);

        string[] splitResult = connectionString.Split(';');
        Dictionary<string, string> keyValueDictionary = new Dictionary<string, string>();

        foreach (var kvp in splitResult)
        {
            // Further split by '=' to separate key and value
            string[] keyValue = kvp.Split('=');
            if (keyValue.Length == 2) // Ensure it's a valid key-value pair
            {
                keyValueDictionary[keyValue[0]] = keyValue[1];
            }
        }

        string prefix = (new Uri(keyValueDictionary["Endpoint"])).Host + "/" + eventHubName + "/" + consumerGroup + "/checkpoint";

        var directory = blobContainerClient.GetBlobs(prefix: prefix.ToLower());

        var testArray = directory.ToArray();

        foreach (var blobItem in directory)
        {
            if (blobItem.Name.ToLower().Contains("checkpoint"))
            {
                BlobClient blobClient = blobContainerClient.GetBlobClient(blobItem.Name);

                BlobProperties properties = await blobClient.GetPropertiesAsync();
                foreach (var metadataItem in properties.Metadata)
                {
                    if (metadataItem.Key.ToLower().Contains("sequence"))
                    {
                        Console.WriteLine("Partition Id: " + LastNum(blobItem.Name));
                        Console.WriteLine($"{metadataItem.Key}: {metadataItem.Value}");

                        sequenceNums.Add(LastNum(blobItem.Name), Convert.ToInt32(metadataItem.Value));
                    }
                }
            }
        }

        return sequenceNums;
    }

    static int LastNum(string input)
    {
        MatchCollection matches = Regex.Matches(input, @"\d+");
        if (matches.Count > 0)
        {
            return int.Parse(matches[matches.Count - 1].Value);
        }
        else
        {
            return -1;
        }
    }
}
