using System.Globalization;

namespace ArchivistClient
{
    public class ArchivistInstance
    {
        private readonly ArchivistNode node;
        private readonly Lock _lock = new Lock();
        private readonly Action<string> onLog;

        public ArchivistInstance(Action<string> onLog, string url)
        {
            this.onLog = onLog;

            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5.0);

            node = new ArchivistNode(httpClient);
            node.BaseUrl = $"{url}/api/archivist/v1/";
            Log("Node baseURL: " + node.BaseUrl);
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
                Log($"Uploading file '{filepath}'...");
                var contentType = "application/octet-stream";
                var disposition = $"attachment; filename=\"UltraDataBurningRom_archive.zip\"";

                using var fileStream = File.OpenRead(filepath);
                node.SetNextUploadInput(contentType, disposition);
                var uploadTask = node.UploadAsync(contentType, disposition, fileStream);
                uploadTask.Wait();
                Log($"Upload successful: '{uploadTask.Result}'");
                return uploadTask.Result;
            }
        }

        public void Download(string cid, string filepath)
        {
            lock (_lock)
            {
                Log($"Downloading file '{cid}' to '{filepath}' ...");
                var task = node.DownloadNetworkStreamAsync(cid);
                if (task.Result.StatusCode != 200)
                {
                    Log("Failed to download.");
                    return;
                }
                var stream = task.Result.Stream;

                if (File.Exists(filepath)) File.Delete(filepath);
                using var fileStream = File.OpenWrite(filepath);
                stream.CopyTo(fileStream);
                Log($"Succesfully downloaded file '{cid}' to '{filepath}'");
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
                Log($"Purchasing storage for '{cid}'...");
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
                Log($"Purchase created for '{cid}' id: '{purchaseTask.Result}'");
                return purchaseTask.Result;
            }
        }

        public bool WaitForPurchaseStarted(string purchaseId, TimeSpan expiry)
        {
            Log($"Waiting for purchase '{purchaseId}' to start...");
            var timeout = DateTime.UtcNow + expiry + TimeSpan.FromSeconds(10);
            while (DateTime.UtcNow < timeout)
            {
                var state = GetPurchaseState(purchaseId);
                if (state == PurchaseState.Started)
                {
                    Log($"Purchase '{purchaseId}' started.");
                    return true;
                }
                if (state == PurchaseState.Errored) return false;
                if (state == PurchaseState.Cancelled) return false;
                if (state == PurchaseState.Failed) return false;
                if (state == PurchaseState.Finished) throw new Exception("Purchase jumped to Finished state without being Started.");

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
            return false;
        }

        private PurchaseState GetPurchaseState(string purchaseId)
        {
            lock (_lock)
            {
                var task = node.GetPurchaseAsync(purchaseId);
                task.Wait();
                return task.Result.State;
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
            onLog(msg);
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
