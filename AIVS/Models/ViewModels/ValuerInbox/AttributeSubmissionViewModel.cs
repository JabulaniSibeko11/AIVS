namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class AttributeSubmissionViewModel
    {
        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        public string? FormType { get; set; }

        public AttributePropertyDetailsVm PropertyDetails { get; set; } = new();

        public AttributeValuationDetailsVm ValuationDetails { get; set; } = new();

        public AttributeAccessVm Access { get; set; } = new();

        public List<AttributeContactInfoVm> ContactInfos { get; set; } = new();

        public AttributePrimaryAttributesVm PrimaryAttributes { get; set; } = new();

        public AttributeSecondaryAttributesVm SecondaryAttributes { get; set; } = new();

        public AttributeCalculationsVm Calculations { get; set; } = new();

        public List<AttributeBusinessBuildingVm> BusinessBuildings { get; set; } = new();

        public List<AttributeBusinessSectionVm> BusinessSections { get; set; } = new();

        public AttributeBusinessGeneralVm BusinessGeneral { get; set; } = new();

        public List<AttributeDrcBuildingVm> DrcBuildings { get; set; } = new();

        public List<AttributeDrcImprovementVm> DrcImprovements { get; set; } = new();

        public List<AttributeDrcVacantLandVm> DrcVacantLands { get; set; } = new();

        public AttributeDrcMarketValueDemolitionVm DrcMarketValueDemolition { get; set; } = new();

        public AttributeDeclarationVm Declaration { get; set; } = new();

        public string? ClientComment { get; set; }
    }
}
