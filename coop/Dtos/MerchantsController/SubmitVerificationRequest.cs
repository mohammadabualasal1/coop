namespace coop.Dtos.MerchantsController
{
    public class SubmitVerificationRequest
    {
        public List<Guid> DocumentIds { get; set; }
        public string? Note { get; set; }
    }
}
