using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> UploadMultipartReader(string username, IFormFile file)
        {
            if (!Request.ContentType?.StartsWith("multipart/form-data") ?? true)
            {
                return BadRequest("The request does not contain valid multipart form data.");
            }

            var filename = file.FileName;
            Console.WriteLine("receiving file: " + filename);

            var fullPath = bucketService.GetWriteableBucketFilePath(username, filename);
            Console.WriteLine("fullPath: " + fullPath);
            if (System.IO.File.Exists(fullPath)) return BadRequest("Already exists");

            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

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
        //private const int BufferSize = 1024 * 1024;

        //public async Task<string> SaveViaMultipartReaderAsync(string boundary, Stream contentStream, CancellationToken cancellationToken)
        //{
        //    string targetFilePath = Path.Combine(Directory.GetCurrentDirectory(), UploadFilePath);
        //    CheckAndRemoveLocalFile(targetFilePath);

        //    using FileStream outputFileStream = new FileStream(
        //        path: targetFilePath,
        //        mode: FileMode.Create,
        //        access: FileAccess.Write,
        //        share: FileShare.None,
        //        bufferSize: BufferSize,
        //        useAsync: true);

        //    var reader = new MultipartReader(boundary, contentStream);
        //    MultipartSection? section;
        //    long totalBytesRead = 0;

        //    // Process each section in the multipart body
        //    targetFilePath = "aaaoutput:";
        //    targetFilePath += "boundary:" + boundary;
        //    targetFilePath += "canread:" + contentStream.CanRead;
        //    try
        //    {
        //        while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
        //        {
        //            targetFilePath += "start,";
        //            // Check if the section is a file
        //            var contentDisposition = section.GetContentDispositionHeader();
        //            if (contentDisposition != null && contentDisposition.IsFileDisposition())
        //            {
        //                targetFilePath += "isfile,";
        //                //_logger.LogInformation($"Processing file: {contentDisposition.FileName.Value}");

        //                // Write the file content to the target file
        //                await section.Body.CopyToAsync(outputFileStream, cancellationToken);
        //                targetFilePath += "bodycopyto,";
        //                totalBytesRead += section.Body.Length;
        //            }
        //            else if (contentDisposition != null && contentDisposition.IsFormDisposition())
        //            {
        //                targetFilePath += "isform,";
        //                // Handle metadata (form fields)
        //                string key = contentDisposition.Name.Value!;
        //                using var streamReader = new StreamReader(section.Body);
        //                targetFilePath += "readtoend,";
        //                string value = await streamReader.ReadToEndAsync(cancellationToken);
        //                //_logger.LogInformation($"Received metadata: {key} = {value}");
        //                targetFilePath += $"{key}={value},";
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        targetFilePath += "exception" + ex;
        //    }
        //    targetFilePath += "done";

        //    //_logger.LogInformation($"File upload completed (via multipart). Total bytes read: {totalBytesRead} bytes.");
        //    return targetFilePath;
        //}

        //private void CheckAndRemoveLocalFile(string filePath)
        //{
        //    if (System.IO.File.Exists(filePath))
        //    {
        //        System.IO.File.Delete(filePath);
        //        //_logger.LogDebug($"Removed existing output file: {filePath}");
        //    }
        //}
    }

    //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    //public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
    //{
    //    public void OnResourceExecuting(ResourceExecutingContext context)
    //    {
    //        var factories = context.ValueProviderFactories;
    //        factories.RemoveType<FormValueProviderFactory>();
    //        factories.RemoveType<JQueryFormValueProviderFactory>();
    //        factories.RemoveType<FormFileValueProviderFactory>();
    //    }

    //    public void OnResourceExecuted(ResourceExecutedContext context)
    //    {
    //    }
    //}
}
