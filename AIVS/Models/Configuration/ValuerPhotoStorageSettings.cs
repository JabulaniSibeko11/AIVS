namespace AIVS.Models.Configuration
{
    public class ValuerPhotoStorageSettings
    {
        public string RootFolder { get; set; } = @"C:\AIVS\ValuerInspectionPhotos";

        public List<string> AllowedExtensions { get; set; } = new()
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        public int MaxFileSizeMb { get; set; } = 5;
    }
}
