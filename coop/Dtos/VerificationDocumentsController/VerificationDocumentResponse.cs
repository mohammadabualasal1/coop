using coop.Enums;

namespace coop.Dtos.VerificationDocumentsController
{
    public class VerificationDocumentResponse
    {
        public Guid Id { get; set; }
        public string DocumentType { get; set; }
        public string FileUrl { get; set; }
        public VerificationStatus Status { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
