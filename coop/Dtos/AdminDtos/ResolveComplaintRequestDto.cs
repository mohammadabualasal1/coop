using coop.Enums;

namespace coop.Dtos.AdminController
{
    public class ResolveComplaintRequestDto
    {
        public ComplaintStatus Status { get; set; }

        public string AdminResponse { get; set; }

    }
}
