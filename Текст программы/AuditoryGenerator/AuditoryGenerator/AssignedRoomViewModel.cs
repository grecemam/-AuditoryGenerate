namespace AuditoryGenerator
{
    public class AssignedRoomViewModel
    {
        public int? Id { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public int CampusId { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
    }

}