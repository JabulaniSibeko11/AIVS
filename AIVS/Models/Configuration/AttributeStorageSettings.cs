namespace AIVS.Models.Configuration
{
    public class AttributeStorageSettings
    {
        public string PhysicalRootPath { get; set; } = @"C:\Attributes";

        public string RootFolderName { get; set; } = "AttributePacks";

        public string EvidenceFolderName { get; set; } = "Attribute Lodged Evidence";

        public string RepresentativeFolderName { get; set; } = "Representative Documentations";

        public string ProcessorFolderName { get; set; } = "Processor";

        public string ProcessorEvidenceFolderName { get; set; } = "Evidence";

        public string ProcessorEmailFolderName { get; set; } = "Client Emails";
    }
}