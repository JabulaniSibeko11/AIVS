namespace AIVS.Models.Attributes
{
    public class AttrValuerInspectionDetail
    {
        public int Id { get; set; }

        public string SapNumber { get; set; } = string.Empty;

        public string ValuerName { get; set; } = string.Empty;

        public string? EmailAddress { get; set; }

        public string? CellNumber { get; set; }

        public string? VehicleRegistration { get; set; }

        public string? VehicleMake { get; set; }

        public string? VehicleColour { get; set; }

        public string? PhotoFileName { get; set; }

        public string? PhotoPath { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}