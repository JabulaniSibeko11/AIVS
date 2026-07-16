namespace AIVS.Models.Attributes
{
    public class LisPropertyDetail
    {
        public string? UnitKey { get; set; }
        public string? PremiseId { get; set; }
        public string? PropertyId { get; set; }
        public string? ValuationKey { get; set; }
        public string? SGNumber { get; set; }
        public string? PropertyDesc { get; set; }
        public string? TownNameDesc { get; set; }
        public int Erf { get; set; }
        public string? Ptn { get; set; }
        public string? Re { get; set; }
        public string? RateableArea { get; set; }
        public string? LisStreetAddress { get; set; }
        public string? CatDesc { get; set; }
        public string? MarketValue { get; set; }
        public string? RateableAreaVal { get; set; }
        public string? ValuationDate { get; set; }
        public string? WefDate { get; set; }
        public string? ValuationStatus { get; set; }
        public string? RollType { get; set; }
        public string? Reason { get; set; }
        public string? SchemeName { get; set; }
        public string? SchemeNumber { get; set; }
        public string? SchemeYear { get; set; }
        public int UnitNo { get; set; }
        public string? UnitType { get; set; }
        public string? UnitLegalArea { get; set; }
        public string? ParticipationQuota { get; set; }
        public string? OwnerName { get; set; }
        public string? TitleDeedNumber { get; set; }
        public string? ZoneCode { get; set; }
        public string? Zoning { get; set; }
        public string? TpsName { get; set; }
        public string? TpsCode { get; set; }
        public string? TpsYear { get; set; }
        public string? LandTypeName { get; set; }
        // SAP contact fields for form pre-fill
        public string? TelNo { get; set; }
        public string? CellNo { get; set; }
        public string? Email { get; set; }
        public string? AccountNo { get; set; }
        public string? SapMarketValue { get; set; }
        public string? SapWefDate { get; set; }
        public string? PremiseErfPtn { get; set; }
        public string? PremiseTown { get; set; }

        public string? OwnerId { get; set; }
        public string? OwnerFirstNames { get; set; }   // "FIRSTNAME MIDDLENAME"
        public string? OwnerLastName { get; set; }   // "SURNAME"
                                                     // OwnerId already exists

        public string? ADDR1 { get; set; }
        public string? ADDR2 { get; set; }
        public string? ADDR3 { get; set; }
        public string? ADDR4 { get; set; }
        public string? ADDR5 { get; set; }
    }
}
