using Microsoft.AspNetCore.Http;

namespace ControllerLayer.Models;

public class UploadProductImagesRequest
{
    public List<IFormFile> Files { get; set; } = [];
}
