using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("bucket")]
    public class BucketController : ControllerBase
    {
        private static readonly Bucket bucket = new Bucket
        {
            Entries =
            [
                new BucketEntry
                {
                    Id = 101,
                    Filename = "filename_1.bin",
                    ByteSize = 1024 * 1024 * 4
                },
                new BucketEntry
                {
                    Id = 102,
                    Filename = "filename_2.bin",
                    ByteSize = 1024 * 1024 * 5
                },
                new BucketEntry
                {
                    Id = 103,
                    Filename = "filename_3.bin",
                    ByteSize = 1024 * 1024 * 6
                },
                new BucketEntry
                {
                    Id = 104,
                    Filename = "filename_4.bin",
                    ByteSize = 1024 * 1024 * 7
                },
                new BucketEntry
                {
                    Id = 105,
                    Filename = "filename_5.bin",
                    ByteSize = 1024 * 1024 * 8
                },
                new BucketEntry
                {
                    Id = 106,
                    Filename = "filename_6.bin",
                    ByteSize = 1024 * 1024 * 9
                }
            ],
            ExpiryUtc = new DateTimeOffset(DateTime.UtcNow.AddHours(3)).ToUnixTimeMilliseconds(),
            State = 0,
            VolumeSize = 1024 * 1024 * 650,
            RomCid = string.Empty,
        };

        [HttpGet("{username}")]
        public Bucket Get(string username)
        {
            return bucket;
        }

        [HttpDelete("{username}/{entryId}")]
        public void Delete(string username, ulong entryId)
        {
            try
            {
                bucket.Entries = bucket.Entries.Where(e => e.Id != entryId).ToArray();
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
            }
        }

        [HttpPost("{username}")]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> UploadMultipartReader(string username)
        {
            if (!Request.ContentType?.StartsWith("multipart/form-data") ?? true)
            {
                return BadRequest("The request does not contain valid multipart form data.");
            }

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(Request.ContentType).Boundary).Value;
            if (string.IsNullOrWhiteSpace(boundary))
            {
                return BadRequest("Missing boundary in multipart form data.");
            }

            var cancellationToken = HttpContext.RequestAborted;
            var filePath = await SaveViaMultipartReaderAsync(boundary, Request.Body, cancellationToken);
            return Ok("Saved file at " + filePath);
        }

        [HttpPost("{username}/burnrom")]
        public async Task<IActionResult> BurnRom(string username, [FromBody] RomInfo romInfo)
        {
            Console.WriteLine("rominfo: " + romInfo.Title);
            Console.WriteLine("rominfo: " + romInfo.Author);
            Console.WriteLine("rominfo: " + romInfo.Tags);
            Console.WriteLine("rominfo: " + romInfo.Description);

            var _ = Task.Run(() =>
            {
                while (bucket.State < 5)
                {
                    bucket.State++;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                bucket.State = 5;
                bucket.RomCid = "romCIDhere";
            });
            Console.WriteLine("Burn!");
            return Ok();
        }

        [HttpPost("{username}/clear")]
        public async Task<IActionResult> Clear(string username)
        {
            if (bucket.State == 5)
            {
                bucket.RomCid = string.Empty;
                bucket.State = 0;
            }
            return Ok();
        }

        private const string UploadFilePath = "uploadedfile.bin";
        private const int BufferSize = 1024 * 1024;

        public async Task<string> SaveViaMultipartReaderAsync(string boundary, Stream contentStream, CancellationToken cancellationToken)
        {
            string targetFilePath = Path.Combine(Directory.GetCurrentDirectory(), UploadFilePath);
            CheckAndRemoveLocalFile(targetFilePath);

            using FileStream outputFileStream = new FileStream(
                path: targetFilePath,
                mode: FileMode.Create,
                access: FileAccess.Write,
                share: FileShare.None,
                bufferSize: BufferSize,
                useAsync: true);

            var reader = new MultipartReader(boundary, contentStream);
            MultipartSection? section;
            long totalBytesRead = 0;

            // Process each section in the multipart body
            targetFilePath = "aaaoutput:";
            targetFilePath += "boundary:" + boundary;
            targetFilePath += "canread:" + contentStream.CanRead;
            try
            {
                while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
                {
                    targetFilePath += "start,";
                    // Check if the section is a file
                    var contentDisposition = section.GetContentDispositionHeader();
                    if (contentDisposition != null && contentDisposition.IsFileDisposition())
                    {
                        targetFilePath += "isfile,";
                        //_logger.LogInformation($"Processing file: {contentDisposition.FileName.Value}");

                        // Write the file content to the target file
                        await section.Body.CopyToAsync(outputFileStream, cancellationToken);
                        targetFilePath += "bodycopyto,";
                        totalBytesRead += section.Body.Length;
                    }
                    else if (contentDisposition != null && contentDisposition.IsFormDisposition())
                    {
                        targetFilePath += "isform,";
                        // Handle metadata (form fields)
                        string key = contentDisposition.Name.Value!;
                        using var streamReader = new StreamReader(section.Body);
                        targetFilePath += "readtoend,";
                        string value = await streamReader.ReadToEndAsync(cancellationToken);
                        //_logger.LogInformation($"Received metadata: {key} = {value}");
                        targetFilePath += $"{key}={value},";
                    }
                }
            }
            catch (Exception ex)
            {
                targetFilePath += "exception" + ex;
            }
            targetFilePath += "done";

            //_logger.LogInformation($"File upload completed (via multipart). Total bytes read: {totalBytesRead} bytes.");
            return targetFilePath;
        }

        private void CheckAndRemoveLocalFile(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                //_logger.LogDebug($"Removed existing output file: {filePath}");
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var factories = context.ValueProviderFactories;
            factories.RemoveType<FormValueProviderFactory>();
            factories.RemoveType<JQueryFormValueProviderFactory>();
            factories.RemoveType<FormFileValueProviderFactory>();
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }

    public class Bucket
    {
        public BucketEntry[] Entries { get; set; } = Array.Empty<BucketEntry>();
        public ulong VolumeSize { get; set; } = 0;
        public int State { get; set; } = 0;
        public long ExpiryUtc { get; set; } = 0;
        public string RomCid { get; set; } = string.Empty;
    }

    public class BucketEntry
    {
        public ulong Id { get; set; } = 0;
        public string Filename { get; set; } = string.Empty;
        public ulong ByteSize { get; set; } = 0;
    }
}
