using System.Globalization;

namespace ArchivistClient
{
    public class ArchivistInstance
    {
        private readonly ArchivistNode node;
        private readonly object _lock = new object();

        public ArchivistInstance(string url)
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5.0);

            node = new ArchivistNode(httpClient);
            node.BaseUrl = $"{url}/api/archivist/v1/";
            Log("BaseURL: " + node.BaseUrl);
        }

        public bool Ping()
        {
            lock (_lock)
            {
                Log("Ping...");
                var timeout = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                while (DateTime.UtcNow < timeout)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    try
                    {
                        var task = node.GetDebugInfoAsync();
                        task.Wait();
                        return !string.IsNullOrEmpty(task.Result.Id);
                    }
                    catch 
                    {
                        Log("Ping attempt failed...");
                    }
                }
                return false;
            }
        }

        public string UploadFile(string filepath)
        {
            lock (_lock)
            {
                Log("Uploading file...");
                var contentType = "application/octet-stream";
                var disposition = $"attachment; filename=\"UltraDataBurningRom_archive.zip\"";

                using var fileStream = File.OpenRead(filepath);
                node.SetNextUploadInput(contentType, disposition);
                var uploadTask = node.UploadAsync(contentType, disposition, fileStream);
                uploadTask.Wait();
                Log("Upload successful: " + uploadTask.Result);
                return uploadTask.Result;
            }
        }

        public string PurchaseStorage(
            string cid,
            int nodes,
            int tolerance,
            TimeSpan duration,
            TimeSpan expiry,
            ulong pricePerBytePerSecond,
            ulong collateralPerByte,
            int proofProbability
        )
        {
            lock (_lock)
            {
                Log("Purchasing storage...");
                var purchaseTask = node.CreateStorageRequestAsync(cid, new StorageRequestCreation
                {
                    Nodes = nodes,
                    Tolerance = tolerance,
                    Duration = Convert.ToInt64(duration.TotalSeconds),
                    Expiry = Convert.ToInt64(expiry.TotalSeconds),
                    PricePerBytePerSecond = pricePerBytePerSecond.ToString(CultureInfo.InvariantCulture),
                    CollateralPerByte = collateralPerByte.ToString(CultureInfo.InvariantCulture),
                    ProofProbability = proofProbability.ToString(CultureInfo.InvariantCulture)
                });

                purchaseTask.Wait();
                Log("Purchase successful: " + purchaseTask.Result);
                return purchaseTask.Result;
            }
        }

        public bool IsPurchaseStarted(string purchaseId)
        {
            lock (_lock)
            {
                var task = node.GetPurchaseAsync(purchaseId);
                task.Wait();
                if (task.Result.State == PurchaseState.Started)
                {
                    Log("Purchase started.");
                    return true;
                }
                return false;
            }
        }

        public string GetPurchaseCid(string purchaseId)
        {
            lock (_lock)
            {
                var task = node.GetPurchaseAsync(purchaseId);
                task.Wait();
                return task.Result.Request.Content.Cid;
            }
        }

        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }

    }

    public partial class ArchivistNode
    {
        private string contentType = string.Empty;
        private string contentDisposition = string.Empty;

        public void SetNextUploadInput(string contentType, string contentDisposition)
        {
            this.contentType = contentType;
            this.contentDisposition = contentDisposition;
        }

        partial void PrepareRequest(System.Net.Http.HttpClient client, System.Net.Http.HttpRequestMessage request, System.Text.StringBuilder urlBuilder)
        {
            if (request == null) return;
            if (request.Content == null) return;
            if (string.IsNullOrEmpty(contentType)) return;
            if (string.IsNullOrEmpty(contentDisposition)) return;

            request.Content.Headers.Remove("Content-Type");
            request.Content.Headers.Add("Content-Type", contentType);
            request.Content.Headers.Add("Content-Disposition", contentDisposition);

            contentType = string.Empty;
            contentDisposition = string.Empty;
        }
    }
}
