// Models/Attendance.cs
using School_Management_System.Pages;

public class Attendance
{
    public int AttendanceId { get; set; }
    public int TeacherId { get; set; }
    public int StudentId { get; set; }
    public DateTime DateOfAttendance { get; set; }
    public string Status { get; set; }  // 'Present' or 'Absent'

    public Teacher Teacher { get; set; }
    public Student Student { get; set; }
}
