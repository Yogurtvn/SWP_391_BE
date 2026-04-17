using Microsoft.AspNetCore.Http;

namespace ControllerLayer.Models;

public class UploadPrescriptionImageRequest
{
    public IFormFile? File { get; set; }
}
