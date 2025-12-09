using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using UltraDataBurningROM.Server.Services;

namespace UltraDataBurningROM.Server.Controllers
{
    [ApiController]
    [Route("bucket")]
    public class BucketController : ControllerBase
    {
        private readonly IBucketService bucketService;

        public BucketController(IBucketService bucketService)
        {
            this.bucketService = bucketService;
        }

        [HttpGet("{username}")]
        public Bucket Get(string username)
        {
            return bucketService.GetBucket(username);
        }

        [HttpDelete("{username}/{filename}")]
        public void Delete(string username, string filename)
        {
            try
            {
                bucketService.DeleteFile(username, filename);
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

            //var filename = file.FileName;
            //Console.WriteLine("receiving file: " + filename);

            //var fullPath = bucketService.GetWriteableBucketFilePath(username, filename);
            //Console.WriteLine("fullPath: " + fullPath);
            //if (System.IO.File.Exists(fullPath)) return BadRequest("Already exists");

            //using (var stream = System.IO.File.Create(fullPath))
            //{
            //    await file.CopyToAsync(stream);
            //}

            var cancellationToken = HttpContext.RequestAborted;
            await SaveViaMultipartReaderAsync(username, boundary, Request.Body, cancellationToken);

            Console.WriteLine("refreshing");
            bucketService.Refresh(username);
            return Ok();
        }

        [HttpPost("{username}/burnrom")]
        public async Task<IActionResult> BurnRom(string username, [FromBody] BurnInfo burnInfo)
        {
            var romInfo = burnInfo.Fields;
            Console.WriteLine("durabilityId: " + burnInfo.DurabilityOptionId);
            Console.WriteLine("rominfo: " + romInfo.Title);
            Console.WriteLine("rominfo: " + romInfo.Author);
            Console.WriteLine("rominfo: " + romInfo.Tags);
            Console.WriteLine("rominfo: " + romInfo.Description);

            //var _ = Task.Run(() =>
            //{
            //    while (bucket.State < 5)
            //    {
            //        bucket.State++;
            //        Thread.Sleep(TimeSpan.FromSeconds(1));
            //    }
            //    bucket.State = 5;
            //    bucket.RomCid = "romCIDhere";
            //});
            Console.WriteLine("Burn!");
            return Ok();
        }

        [HttpPost("{username}/clear")]
        public async Task<IActionResult> Clear(string username)
        {
            //if (bucket.State == 5)
            //{
            //    bucket.RomCid = string.Empty;
            //    bucket.State = 0;
            //}
            return Ok();
        }

        //private const string UploadFilePath = "uploadedfile.bin";
        private const int BufferSize = 1024 * 1024;

        public async Task<string> SaveViaMultipartReaderAsync(string username, string boundary, Stream contentStream, CancellationToken cancellationToken)
        {
            //string targetFilePath = Path.Combine(Directory.GetCurrentDirectory(), UploadFilePath);
            //CheckAndRemoveLocalFile(targetFilePath);

            var reader = new MultipartReader(boundary, contentStream);
            MultipartSection? section;
            long totalBytesRead = 0;

            // Process each section in the multipart body
            try
            {
                while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
                {
                    // Check if the section is a file
                    var contentDisposition = section.GetContentDispositionHeader();
                    if (contentDisposition != null && contentDisposition.IsFileDisposition())
                    {
                        var filename = contentDisposition.FileName.Value;
                        if (string.IsNullOrEmpty(filename)) throw new Exception("No filename provided");
                        var fullPath = bucketService.GetWriteableBucketFilePath(username, filename);

                        using FileStream outputFileStream = new FileStream(
                            path: fullPath,
                            mode: FileMode.Create,
                            access: FileAccess.Write,
                            share: FileShare.None,
                            bufferSize: BufferSize,
                            useAsync: true);


                        // Write the file content to the target file
                        await section.Body.CopyToAsync(outputFileStream, cancellationToken);
                        totalBytesRead += section.Body.Length;
                    }
                    else if (contentDisposition != null && contentDisposition.IsFormDisposition())
                    {
                        // Handle metadata (form fields)
                        string key = contentDisposition.Name.Value!;
                        using var streamReader = new StreamReader(section.Body);
                        string value = await streamReader.ReadToEndAsync(cancellationToken);
                        //_logger.LogInformation($"Received metadata: {key} = {value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception" + ex);
            }

            return "targetFilePath";
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
}
